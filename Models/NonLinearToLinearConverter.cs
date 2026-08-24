using System;

namespace Group_V_26_LPR381_Project.Models
{
    /// <summary>
    /// Converts a single-variable non-linear function into a piecewise-linear
    /// approximation, formulated as a mixed-integer LinearProgram that the
    /// project's existing solvers (Dual Simplex, Branch and Bound, Cutting Plane,
    /// Revised Simplex) can solve directly - no separate non-linear algorithm
    /// needed for those buttons.
    ///
    /// HOW IT WORKS:
    /// f(x) is sampled at K+1 evenly-spaced breakpoints x0..xK across [lower, upper].
    /// Any point on the piecewise-linear approximation is expressed as a weighted
    /// combination of exactly two ADJACENT breakpoints:
    ///     x  = sum(lambda_i * x_i),   f(x) ~= sum(lambda_i * f(x_i))
    /// with sum(lambda_i) = 1 and every lambda_i >= 0.
    ///
    /// The "adjacent only" restriction (so the LP can't cheat by blending two
    /// breakpoints that aren't next to each other, which would draw a false
    /// straight line across the shape of the curve) is enforced with one binary
    /// "segment selector" variable z_j per segment [x_(j-1), x_j]:
    ///     sum(z_j) = 1                          (exactly one segment is active)
    ///     lambda_i <= z_i + z_(i+1)              (each lambda can only be nonzero
    ///                                              if an adjacent segment is on)
    /// This pattern is the standard SOS2 (Special Ordered Set, type 2) constraint
    /// set used throughout operations research for exactly this purpose.
    ///
    /// All variables are named x1, x2, ... (lambdas first, then z's) purely so the
    /// existing solvers - which look for variables named "x&lt;n&gt;" when finding
    /// fractional values to branch on - work unmodified.
    /// </summary>
    public static class NonLinearToLinearConverter
    {
        public static PiecewiseLinearizationResult BuildPiecewiseLinearApproximation(
            string expression, double lowerBound, double upperBound, bool maximize, int segments)
        {
            if (lowerBound >= upperBound)
                throw new ArgumentException("Lower bound must be less than upper bound.");
            if (segments < 2)
                throw new ArgumentException("At least 2 segments are required.");

            var f = new NonLinearFunction(expression);

            int K = segments;
            double[] breakpoints = new double[K + 1];
            double[] values = new double[K + 1];
            double step = (upperBound - lowerBound) / K;

            for (int i = 0; i <= K; i++)
            {
                breakpoints[i] = lowerBound + i * step;
                values[i] = f.Evaluate(breakpoints[i]);
            }

            int lambdaCount = K + 1;
            int zCount = K;
            int totalVars = lambdaCount + zCount;

            var program = new LinearProgram { IsMaximization = maximize };

            // Lambda variables: their objective coefficient is f(breakpoint_i), since
            // the approximated objective is sum(lambda_i * f(x_i)).
            for (int i = 0; i < lambdaCount; i++)
            {
                program.Variables.Add(new LinearProgram.Variable
                {
                    Index = program.Variables.Count + 1,
                    Coefficient = values[i],
                    Type = LinearProgram.VariableType.NonNegative
                });
            }

            // Segment-selector variables: don't appear in the objective, must be binary.
            for (int j = 0; j < zCount; j++)
            {
                program.Variables.Add(new LinearProgram.Variable
                {
                    Index = program.Variables.Count + 1,
                    Coefficient = 0,
                    Type = LinearProgram.VariableType.Binary
                });
            }

            // Constraint: sum of all lambda_i = 1
            var sumLambda = new LinearProgram.Constraint { Relation = LinearProgram.Relation.Equal, Rhs = 1 };
            for (int j = 0; j < totalVars; j++)
                sumLambda.Coefficients.Add(j < lambdaCount ? 1 : 0);
            program.Constraints.Add(sumLambda);

            // Constraint: exactly one segment is selected
            var sumZ = new LinearProgram.Constraint { Relation = LinearProgram.Relation.Equal, Rhs = 1 };
            for (int j = 0; j < totalVars; j++)
                sumZ.Coefficients.Add(j >= lambdaCount ? 1 : 0);
            program.Constraints.Add(sumZ);

            // SOS2 adjacency: lambda_i <= z_i + z_(i+1) (breakpoint 0 only touches
            // segment 1; breakpoint K only touches segment K; interior breakpoints
            // touch the two segments on either side of them).
            for (int i = 0; i < lambdaCount; i++)
            {
                var adjacency = new LinearProgram.Constraint { Relation = LinearProgram.Relation.LessThanOrEqual, Rhs = 0 };
                for (int j = 0; j < totalVars; j++)
                {
                    double coeff = 0;
                    if (j == i) coeff = 1; // the lambda_i column itself

                    int zIndex = j - lambdaCount; // 0-based segment index if j is a z column
                    if (zIndex >= 0 && (zIndex == i - 1 || zIndex == i))
                        coeff -= 1;

                    adjacency.Coefficients.Add(coeff);
                }
                program.Constraints.Add(adjacency);
            }

            return new PiecewiseLinearizationResult
            {
                Program = program,
                Breakpoints = breakpoints,
                Values = values
            };
        }

        /// <summary>
        /// Recovers the original x value from a solved piecewise-linear approximation's
        /// lambda variables (x1..x_(breakpoints.Length)).
        /// </summary>
        public static double RecoverXValue(Solution solution, double[] breakpoints)
        {
            double x = 0;
            for (int i = 0; i < breakpoints.Length; i++)
            {
                if (solution.VariableValues.TryGetValue($"x{i + 1}", out double lambda))
                    x += lambda * breakpoints[i];
            }
            return x;
        }
    }

    public class PiecewiseLinearizationResult
    {
        public LinearProgram Program { get; set; }
        public double[] Breakpoints { get; set; }
        public double[] Values { get; set; }
    }
}