using Group_V_26_LPR381_Project.Models;
using System;

namespace Group_V_26_LPR381_Project.Algorithms
{
    /// <summary>
    /// Solves single-variable, non-linear optimization problems - e.g. f(x) = x^2 -
    /// over a bounded interval [lowerBound, upperBound], using Golden Section Search.
    ///
    /// WHY THIS ALGORITHM (for the video/explanation):
    /// Every simplex-family algorithm elsewhere in this project (Primal/Dual/Revised
    /// Simplex, Branch and Bound, Cutting Plane) works by pivoting on a tableau of
    /// coefficients. That only makes sense when the objective and constraints are
    /// LINEAR - straight lines/planes with a fixed slope. A function like f(x) = x^2
    /// has no such tableau: its slope changes continuously with x, so there is no
    /// fixed set of coefficients to pivot on. A fundamentally different technique is
    /// required.
    ///
    /// Golden Section Search is one of the simplest correct techniques available for
    /// this: it needs no derivative and works on any UNIMODAL function (one with a
    /// single peak or trough inside the search interval - true for x^2 on any
    /// interval). On each iteration it evaluates f at two interior points placed
    /// using the golden ratio (~0.618), then discards whichever sub-interval
    /// provably cannot contain the optimum, shrinking the bracket [a, b] by a
    /// constant ratio every step. Because of how the two interior points are
    /// placed, one of them can always be reused on the next iteration, so only ONE
    /// new function evaluation is needed per step. The loop terminates once the
    /// bracket is narrower than the requested tolerance; the midpoint of the final
    /// bracket is returned as the optimum.
    /// </summary>
    public class GoldenSectionSearch
    {
        private const double GoldenRatio = 0.6180339887498949;
        private const double DefaultTolerance = 1e-6;
        private const int MaxIterations = 200;

        public Solution Solve(string expression, double lowerBound, double upperBound, bool maximize,
            double tolerance = DefaultTolerance)
        {
            var solution = new Solution();

            if (lowerBound >= upperBound)
            {
                solution.AddMessage("Result: INVALID INPUT");
                solution.AddMessage("The lower bound must be less than the upper bound.");
                return solution;
            }

            NonLinearFunction f;
            try
            {
                f = new NonLinearFunction(expression);
            }
            catch (Exception ex)
            {
                solution.AddMessage("Result: INVALID FUNCTION");
                solution.AddMessage($"Could not parse '{expression}': {ex.Message}");
                return solution;
            }

            solution.AddMessage($"Running Golden Section Search on f(x) = {expression}");
            solution.AddMessage($"{(maximize ? "Maximizing" : "Minimizing")} over [{NumberFormatter.Format(lowerBound)}, {NumberFormatter.Format(upperBound)}]");
            solution.AddMessage("");

            // f(x) is MAXIMIZED by minimizing -f(x) internally. This lets a single
            // comparison rule (always "pick the smaller value") drive both directions
            // without duplicating the search logic.
            Func<double, double> objective;
            if (maximize)
                objective = x => -f.Evaluate(x);
            else
                objective = x => f.Evaluate(x);

            double a = lowerBound;
            double b = upperBound;

            double x1 = b - GoldenRatio * (b - a);
            double x2 = a + GoldenRatio * (b - a);
            double f1 = objective(x1);
            double f2 = objective(x2);

            int iteration = 0;
            solution.AddGroupHeader("Golden Section Search Iterations", 0);

            while (Math.Abs(b - a) > tolerance && iteration < MaxIterations)
            {
                iteration++;
                solution.AddMessage(
                    $"Iteration {iteration}: a = {NumberFormatter.Format(a)}, b = {NumberFormatter.Format(b)}, " +
                    $"x1 = {NumberFormatter.Format(x1)} (f = {NumberFormatter.Format(maximize ? -f1 : f1)}), " +
                    $"x2 = {NumberFormatter.Format(x2)} (f = {NumberFormatter.Format(maximize ? -f2 : f2)})");

                if (f1 < f2)
                {
                    // The optimum cannot lie to the right of x2 - shrink the bracket to [a, x2].
                    b = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = b - GoldenRatio * (b - a);
                    f1 = objective(x1);
                }
                else
                {
                    // The optimum cannot lie to the left of x1 - shrink the bracket to [x1, b].
                    a = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = a + GoldenRatio * (b - a);
                    f2 = objective(x2);
                }
            }

            double xOptimal = (a + b) / 2.0;
            double fOptimal = f.Evaluate(xOptimal);

            solution.AddMessage("");
            solution.AddMessage($"Converged after {iteration} iterations (bracket width < {tolerance}).");

            solution.OptimalValue = fOptimal;
            solution.VariableValues["x"] = xOptimal;

            solution.AddMessage("Result: OPTIMAL SOLUTION");
            solution.AddMessage($"x = {NumberFormatter.Format(xOptimal)}");
            solution.AddMessage($"f(x) = {NumberFormatter.Format(fOptimal)}");

            return solution;
        }
    }
}