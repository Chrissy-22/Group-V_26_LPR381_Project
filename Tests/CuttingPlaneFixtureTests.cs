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
                VerifyNearIntegerTolerance();
                VerifyUnsupportedModelsAreRejected();
                VerifyBinaryUpperBound();
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
        }

        private static void VerifyNearIntegerTolerance()
        {
            Solution solution = Solve("max + 1\n+ 1 = 1.0000005\nint");
            AssertTrue("near-integer value does not create a cut", CountCuts(solution) == 0);
            AssertContains("near-integer value is accepted at tolerance", solution.Messages,
                "Result: OPTIMAL INTEGER SOLUTION");
        }

        private static void VerifyUnsupportedModelsAreRejected()
        {
            Solution minimisation = Solve("min + 1\n+ 1 <= 1\nint");
            AssertContains("minimisation rejection", minimisation.Messages,
                "Cutting Plane currently supports maximisation models only.");

            Solution mixed = Solve("max + 5 + 2\n+ 3 + 1 <= 12\n+ 1 + 1 <= 5\nint +");
            AssertContains("mixed model rejection", mixed.Messages,
                "Mixed-integer Cutting Plane is intentionally unsupported");
        }

        private static void VerifyBinaryUpperBound()
        {
            Solution solution = Solve("max + 1\n+ 1 <= 2\nbin");
            AssertClose("binary upper bound", 1, solution.VariableValues["x1"]);
            AssertClose("binary objective", 1, solution.OptimalValue);
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
    }
}
