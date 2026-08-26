using Group_V_26_LPR381_Project.Models;
using LinearProgrammingSolver.Algorithms;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Group_V_26_LPR381_Project.Tests
{
    /// <summary>
    /// Executable regression fixtures for the Cutting Plane contribution.  They verify the
    /// live-tableau/Gomory path without depending on the WinForms UI.
    /// </summary>
    internal static class CuttingPlaneFixtureTests
    {
        private const double Tolerance = 1e-6;

        public static int Main()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                VerifySuppliedFixtures();
                VerifyGoldenTieBreakAndCutSigns();
                VerifyMultiCutFixture();
                VerifyIntegerInfeasibleFixture();
                VerifyMinimisationFixture();
                VerifyMixedIntegerFixture();
                VerifyFractionalPartBehaviour();
                VerifyNearIntegerTolerance();
                VerifyUnsupportedModelsAreRejected();
                VerifyBinaryUpperBound();
                VerifySolutionMetadata();
                VerifyGoldenCutValidity();
                VerifyBranchAndBoundCrossChecks();
                VerifySharedSimplexRegressions();
                Console.WriteLine("PASS: Cutting Plane fixtures");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception.Message);
                return 1;
            }
        }

        private static void VerifySuppliedFixtures()
        {
            Solution first = SolveFixture("CuttingPlane1.txt");
            AssertClose("CuttingPlane1 x1", 4, first.VariableValues["x1"]);
            AssertClose("CuttingPlane1 x2", 0, first.VariableValues["x2"]);
            AssertClose("CuttingPlane1 z", 12, first.OptimalValue);
            AssertTrue("CuttingPlane1 needs no cuts", CountCuts(first) == 0);

            Solution second = SolveFixture("CuttingPlane2.txt");
            AssertClose("CuttingPlane2 x1", 4, second.VariableValues["x1"]);
            AssertClose("CuttingPlane2 x2", 0, second.VariableValues["x2"]);
            AssertClose("CuttingPlane2 z", 20, second.OptimalValue);
            AssertTrue("CuttingPlane2 needs exactly one cut", CountCuts(second) == 1);
        }

        private static void VerifyGoldenTieBreakAndCutSigns()
        {
            LinearProgram program = LinearProgram.Parse(
                File.ReadAllText(FixturePath("CuttingPlaneGolden.txt")));
            Solution solution = new CuttingPlane().Solve(program);
            AssertClose("golden x1", 4, solution.VariableValues["x1"]);
            AssertClose("golden x2", 0, solution.VariableValues["x2"]);
            AssertClose("golden z", 20, solution.OptimalValue);
            AssertTrue("golden has one cut", CountCuts(solution) == 1);
            AssertTrue("cut does not append an original decision variable", program.Variables.Count == 2);
            AssertTrue("cut does not append a reconstructed model constraint", program.Constraints.Count == 2);
            AssertContains("tie break chooses x1", solution.Messages,
                "Cut 1: selected x1 = 3.500");

            string cutStep = solution.Steps.First(step => step.StartsWith("Cut 1 Generation"));
            AssertContains("first Gomory cut has negative tableau coefficients", cutStep,
                "Stored tableau equality: -0.500s1 - 0.500s2 + 1g1 = -0.500");
            AssertTrue("cut slack remains auxiliary", !solution.VariableValues.ContainsKey("g1"));
            AssertTrue("generated cut slack is retained in the tableau headers",
                solution.IterationColumnHeaders.Any(headers => headers != null && headers.Contains("g1")));
        }

        private static void VerifyMultiCutFixture()
        {
            Solution solution = SolveFixture("CuttingPlaneMultiCut.txt");
            AssertClose("multi-cut x1", 1, solution.VariableValues["x1"]);
            AssertClose("multi-cut x2", 1, solution.VariableValues["x2"]);
            AssertClose("multi-cut z", 10, solution.OptimalValue);
            AssertTrue("multi-cut requires more than one cut", CountCuts(solution) > 1);

            string secondCut = solution.Steps.First(step => step.StartsWith("Cut 2 Generation"));
            AssertContains("generated cut slack is treated as continuous", secondCut,
                "g1 (continuous auxiliary)");
            AssertContains("negative cut coefficient is retained", secondCut,
                "- 1g1 + 1g2");
        }

        private static void VerifyIntegerInfeasibleFixture()
        {
            Solution solution = SolveFixture("CuttingPlaneIntegerInfeasible.txt");
            AssertContains("integer infeasibility is clear", solution.Messages,
                "Result: INTEGER INFEASIBLE");
            AssertTrue("integer-infeasible fixture has no result variables",
                solution.VariableValues.Count == 0);
            AssertEqual("integer-infeasible variable count", 1, solution.VariableCount);
            AssertEqual("integer-infeasible artificial count", 1, solution.ArtificialCount);
        }

        private static void VerifyMinimisationFixture()
        {
            Solution solution = SolveFixture("CuttingPlaneMinimisation.txt");
            AssertClose("minimisation x1", 2, solution.VariableValues["x1"]);
            AssertClose("minimisation x2", 1, solution.VariableValues["x2"]);
            AssertClose("minimisation z", 20, solution.OptimalValue);
            AssertTrue("minimisation requires a cut", CountCuts(solution) >= 1);
            AssertContains("minimisation is reported as an integer solution", solution.Messages,
                "Result: OPTIMAL INTEGER SOLUTION");
        }

        private static void VerifyMixedIntegerFixture()
        {
            Solution solution = SolveFixture("CuttingPlaneMixed.txt");
            AssertClose("mixed x1", 35d / 9d, solution.VariableValues["x1"]);
            AssertClose("mixed x2", 2d, solution.VariableValues["x2"]);
            AssertClose("mixed z", 370d / 9d, solution.OptimalValue);
            AssertTrue("mixed model requires a GMI cut", CountCuts(solution) >= 1);

            string cutStep = solution.Steps.First(step => step.StartsWith("Cut 1 Generation"));
            AssertContains("mixed slack s1 is continuous", cutStep, "s1 (continuous auxiliary)");
            AssertContains("mixed slack s2 is continuous", cutStep, "s2 (continuous auxiliary)");
        }

        private static void VerifyFractionalPartBehaviour()
        {
            AssertClose("frac(-2.4)", 0.6, CuttingPlane.FractionalPart(-2.4));
            AssertClose("frac(-1.2)", 0.8, CuttingPlane.FractionalPart(-1.2));
            AssertClose("frac(2.4)", 0.4, CuttingPlane.FractionalPart(2.4));
            AssertClose("frac(3.0)", 0.0, CuttingPlane.FractionalPart(3.0));
        }

        private static void VerifyNearIntegerTolerance()
        {
            AssertTrue("1.999999999 is integer at tolerance",
                CuttingPlane.IsIntegerWithinTolerance(1.999999999));
            AssertTrue("2.000000001 is integer at tolerance",
                CuttingPlane.IsIntegerWithinTolerance(2.000000001));
            AssertTrue("1.999998 is fractional outside tolerance",
                !CuttingPlane.IsIntegerWithinTolerance(1.999998));

            Solution solution = Solve("max + 1\n+ 1 = 1.999999999\nint");
            AssertTrue("near-integer value does not create a cut", CountCuts(solution) == 0);
            AssertContains("near-integer value is accepted at tolerance", solution.Messages,
                "Result: OPTIMAL INTEGER SOLUTION");
        }

        private static void VerifyUnsupportedModelsAreRejected()
        {
            Solution continuous = Solve("max + 1\n+ 1 <= 1\n+");
            AssertContains("continuous model rejection", continuous.Messages,
                "Cutting Plane requires at least one original integer or binary decision variable.");
        }

        private static void VerifyBinaryUpperBound()
        {
            Solution solution = Solve("max + 1\n+ 1 <= 2\nbin");
            AssertClose("binary upper bound", 1, solution.VariableValues["x1"]);
            AssertClose("binary objective", 1, solution.OptimalValue);
        }

        private static void VerifySolutionMetadata()
        {
            AssertMetadata("zero-cut", SolveFixture("CuttingPlane1.txt"), 2, 2, 0, 0);
            AssertMetadata("one-cut", SolveFixture("CuttingPlaneGolden.txt"), 2, 2, 0, 0);
            AssertMetadata("multi-cut", SolveFixture("CuttingPlaneMultiCut.txt"), 2, 2, 0, 0);
            AssertMetadata("minimisation", SolveFixture("CuttingPlaneMinimisation.txt"), 2, 0, 2, 0);
            AssertMetadata("mixed", SolveFixture("CuttingPlaneMixed.txt"), 2, 2, 0, 0);
        }

        private static void VerifyGoldenCutValidity()
        {
            Solution solution = SolveFixture("CuttingPlaneGolden.txt");
            int snapshot = solution.IterationMessages.FindIndex(message =>
                message.Contains("After appending Gomory cut g1"));
            if (snapshot < 0) throw new InvalidOperationException("The first golden cut tableau was not recorded.");

            double[,] tableau = solution.IterationTableaux[snapshot];
            var headers = solution.IterationColumnHeaders[snapshot];
            int cutRow = tableau.GetLength(0) - 1;
            int rhsColumn = tableau.GetLength(1) - 1;
            AssertClose("golden cut RHS", -0.5, tableau[cutRow, rhsColumn]);

            // Evaluate the generated inequality -cutCoefficient * variable >= -cutRhs
            // for every feasible integer point in the bounded golden model.  This catches a
            // reversed cut sign while testing the actual stored tableau row.
            for (int x1 = 0; x1 <= 4; x1++)
            {
                for (int x2 = 0; x2 <= 5; x2++)
                {
                    if (3 * x1 + x2 > 12 || x1 + x2 > 5)
                        continue;

                    double cutLeft = 0;
                    for (int column = 0; column < rhsColumn; column++)
                    {
                        double value = GoldenColumnValue(headers[column], x1, x2);
                        cutLeft += -tableau[cutRow, column] * value;
                    }
                    if (cutLeft < -tableau[cutRow, rhsColumn] - Tolerance)
                    {
                        throw new InvalidOperationException("Golden cut excludes feasible integer point (" +
                            x1 + ", " + x2 + ").");
                    }
                }
            }
        }

        private static double GoldenColumnValue(string header, int x1, int x2)
        {
            switch (header)
            {
                case "x1": return x1;
                case "x2": return x2;
                case "s1": return 12 - 3 * x1 - x2;
                case "s2": return 5 - x1 - x2;
                // The generated slack is excluded from the inequality form of its own cut.
                case "g1": return 0;
                default: return 0;
            }
        }

        private static void VerifyBranchAndBoundCrossChecks()
        {
            VerifyBranchAndBoundCrossCheck("golden", "CuttingPlaneGolden.txt");
            VerifyBranchAndBoundCrossCheck("CuttingPlane1", "CuttingPlane1.txt");
            VerifyBranchAndBoundCrossCheck("CuttingPlane2", "CuttingPlane2.txt");
            VerifyBranchAndBoundCrossCheck("multi-cut", "CuttingPlaneMultiCut.txt");
        }

        private static void VerifyBranchAndBoundCrossCheck(string name, string fixture)
        {
            string input = File.ReadAllText(FixturePath(fixture));
            Solution cuttingPlane = Solve(input);
            Solution branchAndBound = new Group_V_26_LPR381_Project.Algorithms.BranchAndBound().Solve(
                LinearProgram.Parse(input));
            AssertClose(name + " B&B objective", cuttingPlane.OptimalValue, branchAndBound.OptimalValue);
            foreach (string variable in new[] { "x1", "x2" })
                AssertClose(name + " B&B " + variable, cuttingPlane.VariableValues[variable],
                    branchAndBound.VariableValues[variable]);
        }

        private static void VerifySharedSimplexRegressions()
        {
            const string relaxation = "max + 3 + 2\n+ 1 + 1 <= 4\n+ 1 + 3 <= 6\n+ +";
            var primal = new Group_V_26_LPR381_Project.Algorithms.PrimalSimplex().Solve(
                LinearProgram.Parse(relaxation));
            AssertClose("Primal Simplex regression z", 12, primal.OptimalValue);
            AssertClose("Primal Simplex regression x1", 4, primal.VariableValues["x1"]);
            AssertClose("Primal Simplex regression x2", 0, primal.VariableValues["x2"]);

            var dual = new Group_V_26_LPR381_Project.Algorithms.DualSimplex();
            Solution dualInitial = dual.Solve(LinearProgram.Parse(relaxation));
            AssertClose("Dual Simplex regression z", 12, dualInitial.OptimalValue);

            var addedConstraint = new LinearProgram.Constraint
            {
                Relation = LinearProgram.Relation.LessThanOrEqual,
                Rhs = 3
            };
            addedConstraint.Coefficients.Add(1);
            addedConstraint.Coefficients.Add(0);
            Solution updated = dual.AddConstraintAndResolve(addedConstraint);
            AssertClose("AddConstraintAndResolve regression z", 11, updated.OptimalValue);
            AssertClose("AddConstraintAndResolve regression x1", 3, updated.VariableValues["x1"]);
            AssertClose("AddConstraintAndResolve regression x2", 1, updated.VariableValues["x2"]);

            Solution equality = new Group_V_26_LPR381_Project.Algorithms.DualSimplex().Solve(
                LinearProgram.Parse("max + 1\n+ 1 = 0.5\n+"));
            AssertClose("Dual equality regression z", 0.5, equality.OptimalValue);
            AssertClose("Dual equality regression x1", 0.5, equality.VariableValues["x1"]);
            AssertTrue("feasible equality is not marked infeasible", !equality.Messages.Any(message =>
                message.IndexOf("infeasible", StringComparison.OrdinalIgnoreCase) >= 0));

            Solution infeasible = new Group_V_26_LPR381_Project.Algorithms.DualSimplex().Solve(
                LinearProgram.Parse("max + 1\n+ 1 <= 0\n+ 1 >= 1\n+"));
            AssertTrue("Dual infeasible LP remains detected", infeasible.Messages.Any(message =>
                message.IndexOf("infeasible", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static Solution SolveFixture(string fileName)
        {
            return Solve(File.ReadAllText(FixturePath(fileName)));
        }

        private static Solution Solve(string input)
        {
            return new CuttingPlane().Solve(LinearProgram.Parse(input));
        }

        private static string FixturePath(string fileName)
        {
            string[] candidates =
            {
                Path.Combine(Environment.CurrentDirectory, "Data", fileName),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "Data", fileName))
            };
            string path = candidates.FirstOrDefault(File.Exists);
            if (path == null) throw new FileNotFoundException("Fixture was not found: " + fileName);
            return path;
        }

        private static int CountCuts(Solution solution)
        {
            return solution.Messages.Count(message => message.StartsWith("Cut ") &&
                message.Contains(": selected"));
        }

        private static void AssertMetadata(string name, Solution solution, int variables,
            int slacks, int excesses, int artificials)
        {
            AssertEqual(name + " variable count", variables, solution.VariableCount);
            AssertEqual(name + " slack count", slacks, solution.SlackCount);
            AssertEqual(name + " excess count", excesses, solution.ExcessCount);
            AssertEqual(name + " artificial count", artificials, solution.ArtificialCount);
        }

        private static void AssertContains(string name, System.Collections.Generic.IEnumerable<string> values,
            string expected)
        {
            if (!values.Any(value => value.Contains(expected)))
                throw new InvalidOperationException(name + " did not contain: " + expected);
        }

        private static void AssertContains(string name, string actual, string expected)
        {
            if (!actual.Contains(expected))
                throw new InvalidOperationException(name + " did not contain: " + expected);
        }

        private static void AssertTrue(string name, bool condition)
        {
            if (!condition) throw new InvalidOperationException(name + " failed.");
        }

        private static void AssertClose(string name, double expected, double actual)
        {
            if (Math.Abs(expected - actual) > Tolerance)
                throw new InvalidOperationException(name + " expected " + expected + " but was " + actual + ".");
        }

        private static void AssertEqual(string name, int expected, int actual)
        {
            if (expected != actual)
                throw new InvalidOperationException(name + " expected " + expected + " but was " + actual + ".");
        }
    }
}
