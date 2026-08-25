using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Group_V_26_LPR381_Project.Models;

namespace Group_V_26_LPR381_Project.Algorithms
{
    /// <summary>
    /// Solves an unconstrained multi-variable NLP (n >= 1 variables, typically
    /// n >= 2 - single-variable problems should use GoldenSectionSearch instead)
    /// using the Steepest Ascent / Descent algorithm, narrated the way the course
    /// slides work it by hand:
    ///
    ///   Step 1: Start from an initial guessed point x_0.
    ///   Step 2: Compute the gradient grad f at the current point.
    ///   Step 3: Treat x_(i+1) = x_i + h * grad(f) as a function of the single
    ///           unknown h, forming g(h) = f(x_i + h * grad(f)). Find the h that
    ///           optimizes g(h) - analytically via dg/dh = 0 when g(h) is a
    ///           low-degree polynomial (exactly like the slide's worked example),
    ///           or via Golden Section Search otherwise.
    ///   Step 4: Substitute h back into x_(i+1) = x_i + h * grad(f) to get the new
    ///           point, then repeat from Step 2 until grad f = 0.
    ///
    /// When the objective is a genuine polynomial (built only from +, -, *, /by a
    /// constant, and ^ by a whole-number constant), this expands it into fully
    /// collected form via MultivariatePolynomial and shows every algebraic step -
    /// the expanded f, its symbolic partials, the substituted g(h), and the exact
    /// solve of g'(h) = 0 - exactly as worked by hand on the slides. For anything
    /// else (sin/cos/sqrt/exp/ln of a variable), the algebra can't be expanded, so
    /// only the numeric gradient/point values are shown and h is found via
    /// Golden Section Search on g(h) directly.
    /// </summary>
    public class SteepestAscentDescent
    {
        private const double DefaultTolerance = 1e-6;
        private const int MaxIterations = 200;

        public Solution Solve(string expression, double[] startingPoint, bool maximize,
            double tolerance = DefaultTolerance)
        {
            var solution = new Solution();

            MultiVariableNonLinearFunction f;
            try
            {
                f = new MultiVariableNonLinearFunction(expression);
            }
            catch (Exception ex)
            {
                solution.AddMessage("Result: INVALID FUNCTION");
                solution.AddMessage($"Could not parse '{expression}': {ex.Message}");
                return solution;
            }

            int n = f.VariableCount;
            if (n < 1)
            {
                solution.AddMessage("Result: INVALID FUNCTION");
                solution.AddMessage("No variables (x1, x2, ...) were found in the expression.");
                return solution;
            }
            if (startingPoint.Length != n)
            {
                solution.AddMessage("Result: INVALID INPUT");
                solution.AddMessage($"Expression uses {n} variable(s) but {startingPoint.Length} starting value(s) were given.");
                return solution;
            }

            string[] varNames = Enumerable.Range(1, n).Select(i => "x" + i).ToArray();

            solution.AddMessage($"{(maximize ? "max" : "min")} z = f({string.Join(",", varNames)}) = {expression}");
            solution.AddMessage($"Starting point: ({PointList(startingPoint)})");
            solution.AddMessage("");

            MultivariatePolynomial poly = f.TryExpandPolynomial();
            bool isPolynomial = poly != null;
            string expandedFormula = isPolynomial ? poly.ToDisplayString(varNames) : expression;

            MultivariatePolynomial[] partialPolys = null;
            MultiVariableNonLinearFunction[] partialFunctions = new MultiVariableNonLinearFunction[n];
            for (int i = 0; i < n; i++)
                partialFunctions[i] = f.PartialDerivative(i + 1);

            if (isPolynomial)
            {
                partialPolys = new MultivariatePolynomial[n];
                for (int i = 0; i < n; i++) partialPolys[i] = poly.PartialDerivative(i + 1);

                solution.AddMessage($"f({string.Join(",", varNames)}) = {expandedFormula}");
                solution.AddMessage("");
                for (int i = 0; i < n; i++)
                    solution.AddMessage($"f'_{varNames[i]} = {partialPolys[i].ToDisplayString(varNames)}");
                solution.AddMessage("");
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        solution.AddMessage($"f_({varNames[i]}){varNames[j]} = {partialPolys[i].PartialDerivative(j + 1).ToDisplayString(varNames)}");
                solution.AddMessage("");
            }

            // ---- Convexity / Concavity Analysis via Hessian, evaluated at the starting point ----
            var hessianResult = HessianAnalyzer.Analyze(f, n, startingPoint);
            solution.AddGroupHeader("Convexity / Concavity Analysis (Hessian at starting point)", 0);
            solution.AddMessage($"H(x1..x{n}) =");
            solution.AddMessage(HessianAnalyzer.FormatMatrix(hessianResult.NumericHessian));
            solution.AddMessage("");
            solution.AddMessage(hessianResult.Classification);
            solution.AddMessage("");

            double[] x = (double[])startingPoint.Clone();
            int iteration = 0;
            double gradientNorm = double.MaxValue;
            bool converged = false;
            bool unbounded = false;

            while (iteration < MaxIterations)
            {
                iteration++;

                double[] gradient = new double[n];
                for (int i = 0; i < n; i++)
                    gradient[i] = isPolynomial ? partialPolys[i].Evaluate(x) : partialFunctions[i].Evaluate(x);

                gradientNorm = 0;
                foreach (double gVal in gradient) gradientNorm += gVal * gVal;
                gradientNorm = Math.Sqrt(gradientNorm);

                double fCurrent = f.Evaluate(x);

                solution.AddGroupHeader($"Iteration {iteration}:", 0);

                if (isPolynomial)
                    solution.AddMessage($"f({string.Join(",", varNames)}) = {expandedFormula}");

                solution.AddMessage($"f({PointList(x)}) = {NumberFormatter.Format(fCurrent)}");
                solution.AddMessage("");

                for (int i = 0; i < n; i++)
                {
                    if (isPolynomial)
                        solution.AddMessage($"f'_{varNames[i]} = {partialPolys[i].ToDisplayString(varNames)} at ({PointList(x)}) = {NumberFormatter.Format(gradient[i])}");
                    else
                        solution.AddMessage($"f'_{varNames[i]} at ({PointList(x)}) = {NumberFormatter.Format(gradient[i])}");
                }
                solution.AddMessage("");

                if (gradientNorm <= tolerance)
                {
                    solution.AddMessage($"{GradientVectorNotation(gradient, n)} stop because gradient = 0");
                    solution.AddMessage("");
                    solution.AddMessage($"f({PointList(x)}) = {NumberFormatter.Format(fCurrent)} Best");
                    converged = true;
                    break;
                }

                solution.AddMessage(GradientVectorNotation(gradient, n));
                solution.AddMessage("");

                double[] PointAt(double h)
                {
                    var p = new double[n];
                    for (int i = 0; i < n; i++) p[i] = x[i] + h * gradient[i];
                    return p;
                }

                string x0Str = "{" + PointList(x) + "}";
                string gradStr = "{" + PointList(gradient) + "}";
                string combinedStr = "{" + string.Join(", ", Enumerable.Range(0, n)
                    .Select(i => $"{NumberFormatter.Format(x[i])} + {NumberFormatter.Format(gradient[i])}h")) + "}";
                solution.AddMessage($"x(i+1) = {x0Str} + h{gradStr} = {combinedStr}");
                solution.AddMessage("");

                double hStep;
                bool stepUnbounded = false;

                if (isPolynomial)
                {
                    double[] hCoeffs = poly.SubstituteAffine(x, gradient);
                    string substitutedText = SubstituteFormulaText(expandedFormula, varNames, x, gradient);
                    solution.AddMessage($"f(x(i+1)) = {substitutedText} = g(h)");
                    solution.AddMessage("");
                    solution.AddMessage($"g(h) = {Polynomial1D.Format(hCoeffs, "h")}");
                    solution.AddMessage("");

                    double[] gPrime = Polynomial1D.Derivative(hCoeffs);

                    if (gPrime.Length <= 2)
                    {
                        solution.AddMessage($"g'(h) = {Polynomial1D.Format(gPrime, "h")} = 0");

                        if (gPrime.Length == 2 && Math.Abs(gPrime[1]) > 1e-12)
                        {
                            hStep = -gPrime[0] / gPrime[1];
                        }
                        else if (Math.Abs(gPrime.Length > 0 ? gPrime[0] : 0) < 1e-9)
                        {
                            // g(h) is constant along this direction - any h leaves f unchanged.
                            hStep = 0;
                        }
                        else
                        {
                            // g'(h) is a nonzero constant - g(h) is monotonic, no finite
                            // critical point along this direction; fall back to a bounded search.
                            var found = LineSearch1D.FindStep(h => poly.Evaluate(PointAt(h)), maximize);
                            if (found == null) { stepUnbounded = true; hStep = 0; }
                            else hStep = found.Value;
                        }
                    }
                    else
                    {
                        solution.AddMessage("g(h) has degree > 2 - solving via Golden Section Search instead of dg/dh = 0.");
                        var found = LineSearch1D.FindStep(h => poly.Evaluate(PointAt(h)), maximize);
                        if (found == null) { stepUnbounded = true; hStep = 0; }
                        else hStep = found.Value;
                    }
                }
                else
                {
                    var found = LineSearch1D.FindStep(h => f.Evaluate(PointAt(h)), maximize);
                    if (found == null) { stepUnbounded = true; hStep = 0; }
                    else hStep = found.Value;
                }

                if (stepUnbounded)
                {
                    unbounded = true;
                    break;
                }

                solution.AddMessage($"h = {NumberFormatter.Format(hStep)} {(maximize ? "(ascending)" : "(descending)")}");
                solution.AddMessage("");

                double[] xNext = PointAt(hStep);
                string substNumeric = "{" + string.Join(", ", Enumerable.Range(0, n)
                    .Select(i => $"{NumberFormatter.Format(x[i])} + {NumberFormatter.Format(gradient[i])}({NumberFormatter.Format(hStep)})")) + "}";
                solution.AddMessage($"{substNumeric} = {{{PointList(xNext)}}}");
                solution.AddMessage("");

                double fNext = f.Evaluate(xNext);
                bool better = maximize ? fNext > fCurrent : fNext < fCurrent;
                solution.AddMessage($"f({PointList(xNext)}) = {NumberFormatter.Format(fNext)} {(better ? "better" : "worse")}");
                solution.AddMessage("");

                x = xNext;
            }

            double fOptimal = f.Evaluate(x);
            solution.AddMessage("");

            if (converged)
            {
                solution.AddMessage($"Converged after {iteration} iterations (||grad|| < {tolerance}).");
                solution.AddMessage("Result: OPTIMAL SOLUTION");
            }
            else if (unbounded)
            {
                solution.AddMessage($"Stopped after {iteration} iterations: f(x) appears unbounded along the gradient direction");
                solution.AddMessage("(no finite optimal step size h was found). The values below are the LAST point reached,");
                solution.AddMessage("not a confirmed optimum.");
                solution.AddMessage("Result: DID NOT CONVERGE (unbounded)");
            }
            else
            {
                solution.AddMessage($"Did NOT converge after {iteration} iterations - reached the iteration limit ({MaxIterations})");
                solution.AddMessage($"with ||grad|| = {NumberFormatter.Format(gradientNorm)}, still above the tolerance ({tolerance}).");
                solution.AddMessage("The values below are the LAST point reached, not a confirmed optimum.");
                solution.AddMessage("Result: DID NOT CONVERGE (iteration limit)");
            }

            solution.OptimalValue = fOptimal;
            for (int i = 0; i < n; i++)
                solution.VariableValues[$"x{i + 1}"] = x[i];

            solution.AddMessage($"x = ({PointList(x)})");
            solution.AddMessage($"f(x) = {NumberFormatter.Format(fOptimal)}");

            if (converged &&
                (hessianResult.Classification.Contains("SADDLE") ||
                 hessianResult.Classification.Contains("INCONCLUSIVE") ||
                 hessianResult.Classification.Contains("INDEFINITE")))
            {
                solution.AddMessage("");
                solution.AddMessage("Note: the Hessian classification at the starting point did not confirm global");
                solution.AddMessage("convexity/concavity, so this result may be a local optimum, not the global one.");
                solution.AddMessage("Try a different starting point to check for a better solution.");
            }

            return solution;
        }

        private static string PointList(double[] x) => string.Join(", ", x.Select(NumberFormatter.Format));

        private static string GradientVectorNotation(double[] gradient, int n)
        {
            if (n <= 3)
            {
                string[] units = { "i", "j", "k" };
                return "grad f = " + string.Join(" + ", gradient.Select((g, idx) => $"{NumberFormatter.Format(g)}{units[idx]}"));
            }
            return "grad f = [" + string.Join(", ", gradient.Select(NumberFormatter.Format)) + "]";
        }

        /// <summary>Textually substitutes x_i -> "(x0_i + grad_i*h)" into the expanded
        /// formula string, WITHOUT re-expanding - a display-only intermediate step
        /// matching the slide's unexpanded "f(x(i+1)) = -(1+4h)^2+6(1+4h)-..." line,
        /// shown right before the fully collapsed g(h).</summary>
        private static string SubstituteFormulaText(string formula, string[] varNames, double[] x0, double[] direction)
        {
            string result = formula;
            for (int i = 0; i < varNames.Length; i++)
            {
                string replacement = $"({NumberFormatter.Format(x0[i])} + {NumberFormatter.Format(direction[i])}h)";
                result = Regex.Replace(result, $@"\b{Regex.Escape(varNames[i])}\b", replacement);
            }
            return result;
        }
    }
}