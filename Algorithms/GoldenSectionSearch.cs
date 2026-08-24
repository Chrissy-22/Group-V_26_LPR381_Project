using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;

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
    /// Before searching, this solver classifies the function's shape exactly the way
    /// the course slides do: for a single variable, the Hessian degenerates to the
    /// 1x1 matrix H(x) = [f''(x)], so H(x) >= 0 -> convex (local min), H(x) <= 0 ->
    /// concave (local max), and a sign change indicates neither (possible multiple
    /// local optima). f'(x) and f''(x) are computed by EXACT symbolic
    /// differentiation (NonLinearFunction.Derivative()), not numerical approximation.
    ///
    /// Golden Section Search is one of the simplest correct techniques available for
    /// the actual search: it needs no derivative and works on any UNIMODAL function
    /// (one with a single peak or trough inside the search interval). On each
    /// iteration it evaluates f at two interior points placed using the golden ratio
    /// (~0.618), then discards whichever sub-interval provably cannot contain the
    /// optimum, shrinking the bracket [a, b] by a constant ratio every step. Because
    /// of how the two interior points are placed, one of them can always be reused
    /// on the next iteration, so only ONE new function evaluation is needed per
    /// step. The loop terminates once the bracket is narrower than the requested
    /// tolerance; the midpoint of the final bracket is returned as the optimum.
    /// </summary>
    public class GoldenSectionSearch
    {
        private const double GoldenRatio = 0.6180339887498949;
        private const double DefaultTolerance = 1e-6;
        private const int MaxIterations = 200;

        private const double MinDisplayableBracketWidth = 1e-3; // stop recording table rows once the bracket has shrunk below what NumberFormatter can show as distinct


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

            // ---- Convexity / Concavity Analysis (exact symbolic derivatives, slide format) ----
            NonLinearFunction fPrime, fDoublePrime;
            try
            {
                fPrime = f.Derivative();
                fDoublePrime = fPrime.Derivative();
            }
            catch (Exception ex)
            {
                solution.AddMessage("Result: DIFFERENTIATION ERROR");
                solution.AddMessage($"Could not differentiate '{expression}': {ex.Message}");
                return solution;
            }

            solution.AddGroupHeader("Convexity / Concavity Analysis", 0);
            solution.AddMessage($"f'(x)  = {fPrime}");
            solution.AddMessage($"f''(x) = {fDoublePrime}");
            solution.AddMessage($"H(x) = [f''(x)] = [{fDoublePrime}]");
            solution.AddMessage("");

            const int sampleCount = 9;
            const double eps = 1e-9;
            bool allNonNegative = true;
            bool allNonPositive = true;
            for (int i = 0; i <= sampleCount; i++)
            {
                double xi = lowerBound + i * (upperBound - lowerBound) / sampleCount;
                double fpp = fDoublePrime.Evaluate(xi);
                if (fpp < -eps) allNonNegative = false;
                if (fpp > eps) allNonPositive = false;
            }

            bool isMixedSign = !allNonNegative && !allNonPositive;

            string classification;
            if (allNonNegative && allNonPositive)
                classification = $"|H(x)| = 0 over [{NumberFormatter.Format(lowerBound)}, {NumberFormatter.Format(upperBound)}] -> LINEAR (neither strictly convex nor concave)";
            else if (allNonNegative)
                classification = $"H(x) >= 0 over [{NumberFormatter.Format(lowerBound)}, {NumberFormatter.Format(upperBound)}] -> CONVEX (local minimum)";
            else if (allNonPositive)
                classification = $"H(x) <= 0 over [{NumberFormatter.Format(lowerBound)}, {NumberFormatter.Format(upperBound)}] -> CONCAVE (local maximum)";
            else
                classification = $"H(x) changes sign over [{NumberFormatter.Format(lowerBound)}, {NumberFormatter.Format(upperBound)}] -> NEITHER CONVEX NOR CONCAVE (possible saddle behaviour / multiple local optima)";

            solution.AddMessage(classification);
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

            // Each row: [Iteration, a, b, x1, f(x1), x2, f(x2)] - collected here and
            // rendered as one real table after the loop, instead of one text line per
            // iteration.
            var iterationRows = new List<double[]>();

            bool stoppedRecording = false;

            while (Math.Abs(b - a) > tolerance && iteration < MaxIterations)
            {
                iteration++;

                if (!stoppedRecording)
                {
                    iterationRows.Add(new double[]
                    {
            iteration, a, b, x1, maximize ? -f1 : f1, x2, maximize ? -f2 : f2
                    });

                    if (Math.Abs(b - a) < MinDisplayableBracketWidth)
                        stoppedRecording = true;
                }

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

            int omittedIterations = iteration - iterationRows.Count;

            if (iterationRows.Count > 0)
            {
                var iterationMatrix = new double[iterationRows.Count, 7];
                for (int r = 0; r < iterationRows.Count; r++)
                    for (int c = 0; c < 7; c++)
                        iterationMatrix[r, c] = iterationRows[r][c];

                var columnHeaders = new List<string> { "Iter", "a", "b", "x1", "f(x1)", "x2", "f(x2)" };
                solution.AddIteration(iterationMatrix, "Golden Section Search Iterations", -1, -1, columnHeaders);

                if (omittedIterations > 0)
                {
                    solution.AddMessage($"({omittedIterations} further iteration{(omittedIterations == 1 ? "" : "s")} omitted from the table above — " +
                        $"the bracket had already shrunk below display precision, so those rows would have looked identical to the last one shown.)");
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

            if (isMixedSign)
            {
                solution.AddMessage("");
                solution.AddMessage("Note: f(x) is neither convex nor concave on this interval, so Golden Section Search");
                solution.AddMessage("may have converged to a LOCAL optimum rather than the GLOBAL optimum. Try narrowing");
                solution.AddMessage("[lowerBound, upperBound] around a different region if a better solution is suspected.");
            }

            return solution;
        }
    }
}