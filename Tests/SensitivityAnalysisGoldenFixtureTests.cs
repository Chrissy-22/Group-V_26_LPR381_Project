using Group_V_26_LPR381_Project.Algorithms;
using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Group_V_26_LPR381_Project.Tests
{
    /// <summary>
    /// Executable lecturer fixture for the ordered [s1, x3, x1] basis. It validates
    /// the exact Zj-Cj convention and range values used by SensitivityAnalysis.
    /// </summary>
    internal static class SensitivityAnalysisGoldenFixtureTests
    {
        private const double Tolerance = 1e-9;

        public static int Main()
        {
            try
            {
                VerifyLimpopoFixture();
                VerifyUnsupportedMinimisationIsRejected();
                Console.WriteLine("PASS: Limpopo Sensitivity Analysis golden fixture");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception.Message);
                return 1;
            }
        }

        private static void VerifyLimpopoFixture()
        {
            var program = LinearProgram.Parse(File.ReadAllText(FixturePath()));
            var sensitivity = new SensitivityAnalysis();
            BasisSnapshot snapshot;
            string error;
            if (!sensitivity.TryCreateBasisSnapshot(program, out snapshot, out error))
                throw new InvalidOperationException(error);

            AssertSequence("ordered basis", new[] { "s1", "x3", "x1" },
                snapshot.BasicColumnIndices.Select(index => snapshot.ColumnNames[index]));
            AssertMatrix("B", new[,] { { 1d, 1d, 8d }, { 0d, 1.5d, 4d }, { 0d, 0.5d, 2d } },
                snapshot.BasisMatrix);
            AssertMatrix("B^-1", new[,] { { 1d, 2d, -8d }, { 0d, 2d, -4d }, { 0d, -0.5d, 1.5d } },
                snapshot.BasisInverse);
            AssertVector("C_BV", new[] { 0d, 20d, 60d }, snapshot.BasicCosts);
            AssertVector("q", new[] { 0d, 10d, 10d }, snapshot.ShadowPrices);
            AssertVector("B^-1 b", new[] { 24d, 8d, 2d }, snapshot.BasicValues);
            AssertClose("z", 280d, snapshot.ObjectiveValue);

            var solution = sensitivity.Solve(program);
            AssertClose("solution z", 280d, solution.OptimalValue);
            AssertClose("x1", 2d, solution.VariableValues["x1"]);
            AssertClose("x2", 0d, solution.VariableValues["x2"]);
            AssertClose("x3", 8d, solution.VariableValues["x3"]);
            AssertReducedCost(snapshot, "x2", 5d);
            AssertReducedCost(snapshot, "s2", 10d);
            AssertReducedCost(snapshot, "s3", 10d);

            var x2Column = program.Constraints.Select(constraint => constraint.Coefficients[1]);
            ColumnSensitivityResult atUpperBound = sensitivity.AnalyzeNonBasicColumn(
                program, "x2", x2Column, 35d);
            AssertClose("x2 reduced cost at c2 = 35", 0d, atUpperBound.ReducedCost);
            AssertTrue("x2 remains optimal at c2 = 35", atUpperBound.CurrentBasisRemainsOptimal);

            ColumnSensitivityResult aboveUpperBound = sensitivity.AnalyzeNonBasicColumn(
                program, "x2", x2Column, 35d + 1e-6);
            AssertTrue("x2 is not optimal above c2 = 35", !aboveUpperBound.CurrentBasisRemainsOptimal);

            string nonBasicRanges = Step(solution, "Non-Basic Variables", "allowable Delta");
            AssertContains("x2 coefficient upper bound", nonBasicRanges,
                "x2: c_j* = 5, allowable Delta = [-infinity, 5], coefficient range = [-infinity, 35]");

            string rhsRanges = Step(solution, "Feasibility and Objective Effects");
            AssertRhsRange(rhsRanges, 1, 48d, -24d, double.PositiveInfinity);
            AssertRhsRange(rhsRanges, 2, 20d, -4d, 4d);
            AssertRhsRange(rhsRanges, 3, 8d, -4d / 3d, 2d);
        }

        private static void VerifyUnsupportedMinimisationIsRejected()
        {
            var minimisation = LinearProgram.Parse("min + 1\n+ 1 <= 1\n+");
            Solution rejected = new SensitivityAnalysis().Solve(minimisation);
            AssertTrue("minimisation is rejected", rejected.Messages.Any(message =>
                message.Contains("Sensitivity analysis currently accepts maximisation LPs only.")));
        }

        private static string FixturePath()
        {
            string[] candidates =
            {
                Path.Combine(Environment.CurrentDirectory, "Data", "SensitivityLimpopoGolden.txt"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "Data", "SensitivityLimpopoGolden.txt"))
            };
            string path = candidates.FirstOrDefault(File.Exists);
            if (path == null) throw new FileNotFoundException("Limpopo fixture was not found.");
            return path;
        }

        private static string Step(Solution solution, string title, string requiredText = null)
        {
            string prefix = title + Environment.NewLine;
            string step = solution.Steps.FirstOrDefault(value => value.StartsWith(prefix) &&
                (requiredText == null || value.Contains(requiredText)));
            if (step == null) throw new InvalidOperationException("Missing report step: " + title);
            return step;
        }

        private static void AssertReducedCost(BasisSnapshot snapshot, string columnName, double expected)
        {
            int column = snapshot.ColumnNames.IndexOf(columnName);
            if (column < 0) throw new InvalidOperationException("Missing column: " + columnName);
            AssertClose("Zj-Cj " + columnName, expected, snapshot.ReducedCosts[column]);
        }

        private static void AssertRhsRange(string report, int constraint, double rhs,
            double lower, double upper)
        {
            string upperText = double.IsPositiveInfinity(upper) ? "+infinity" :
                NumberFormatter.Format(upper);
            string expected = "Constraint " + constraint + ": RHS range = [" +
                NumberFormatter.Format(rhs + lower) + ", " +
                (double.IsPositiveInfinity(upper) ? "+infinity" : NumberFormatter.Format(rhs + upper)) +
                "], allowable Delta = [" + NumberFormatter.Format(lower) + ", " + upperText + "]";
            AssertContains("constraint " + constraint + " RHS range", report, expected);
        }

        private static void AssertMatrix(string name, double[,] expected, double[,] actual)
        {
            if (expected.GetLength(0) != actual.GetLength(0) || expected.GetLength(1) != actual.GetLength(1))
                throw new InvalidOperationException(name + " has an unexpected shape.");
            for (int row = 0; row < expected.GetLength(0); row++)
                for (int column = 0; column < expected.GetLength(1); column++)
                    AssertClose(name + "[" + row + "," + column + "]", expected[row, column], actual[row, column]);
        }

        private static void AssertVector(string name, double[] expected, double[] actual)
        {
            if (expected.Length != actual.Length)
                throw new InvalidOperationException(name + " has an unexpected length.");
            for (int index = 0; index < expected.Length; index++)
                AssertClose(name + "[" + index + "]", expected[index], actual[index]);
        }

        private static void AssertSequence(string name, IEnumerable<string> expected, IEnumerable<string> actual)
        {
            if (!expected.SequenceEqual(actual))
                throw new InvalidOperationException(name + " was [" + string.Join(", ", actual) + "].");
        }

        private static void AssertContains(string name, string actual, string expected)
        {
            if (!actual.Contains(expected))
                throw new InvalidOperationException(name + " was not reported as expected: " + actual);
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
