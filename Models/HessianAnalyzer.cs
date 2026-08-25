using System;
using System.Text;

namespace Group_V_26_LPR381_Project.Models
{
    /// <summary>
    /// Builds the Hessian matrix of a multi-variable function (all partial second
    /// derivatives, computed exactly via symbolic differentiation), evaluates it
    /// at a point, and classifies convex / concave / saddle point using leading
    /// principal minors - the same rule set worked through in the course slides:
    ///   n = 2: |H| > 0 and d2f/dx1^2 > 0 -> convex (local min)
    ///          |H| > 0 and d2f/dx1^2 < 0 -> concave (local max)
    ///          |H| < 0                   -> saddle point
    ///   n > 2: all leading principal minors >= 0            -> convex
    ///          leading principal minors alternate -,+,-,+... -> concave
    ///          otherwise                                     -> saddle / indefinite
    /// </summary>
    public static class HessianAnalyzer
    {
        public class HessianResult
        {
            public MultiVariableNonLinearFunction[,] SymbolicHessian { get; set; }
            public double[,] NumericHessian { get; set; }
            public double[] LeadingPrincipalMinors { get; set; }
            public string Classification { get; set; }
        }

        public static HessianResult Analyze(MultiVariableNonLinearFunction f, int n, double[] point)
        {
            var firstPartials = new MultiVariableNonLinearFunction[n];
            for (int i = 0; i < n; i++)
                firstPartials[i] = f.PartialDerivative(i + 1);

            var symbolic = new MultiVariableNonLinearFunction[n, n];
            var numeric = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    symbolic[i, j] = firstPartials[i].PartialDerivative(j + 1);
                    numeric[i, j] = symbolic[i, j].Evaluate(point);
                }
            }

            var minors = new double[n];
            for (int k = 1; k <= n; k++)
                minors[k - 1] = LeadingPrincipalMinorDeterminant(numeric, k);

            string classification = Classify(numeric, minors, n);

            return new HessianResult
            {
                SymbolicHessian = symbolic,
                NumericHessian = numeric,
                LeadingPrincipalMinors = minors,
                Classification = classification
            };
        }

        private static double LeadingPrincipalMinorDeterminant(double[,] matrix, int k)
        {
            var sub = new double[k, k];
            for (int i = 0; i < k; i++)
                for (int j = 0; j < k; j++)
                    sub[i, j] = matrix[i, j];
            return Determinant(sub);
        }

        private static double Determinant(double[,] m)
        {
            int n = m.GetLength(0);
            if (n == 1) return m[0, 0];
            if (n == 2) return m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0];

            double det = 0;
            for (int col = 0; col < n; col++)
            {
                var minor = new double[n - 1, n - 1];
                for (int i = 1; i < n; i++)
                {
                    int mc = 0;
                    for (int j = 0; j < n; j++)
                    {
                        if (j == col) continue;
                        minor[i - 1, mc++] = m[i, j];
                    }
                }
                double sign = (col % 2 == 0) ? 1 : -1;
                det += sign * m[0, col] * Determinant(minor);
            }
            return det;
        }

        private static string Classify(double[,] hessian, double[] minors, int n)
        {
            const double eps = 1e-9;

            if (n == 1)
            {
                double fpp = hessian[0, 0];
                if (fpp > eps) return $"f''(x1) = {NumberFormatter.Format(fpp)} >= 0 -> CONVEX (local minimum)";
                if (fpp < -eps) return $"f''(x1) = {NumberFormatter.Format(fpp)} <= 0 -> CONCAVE (local maximum)";
                return $"f''(x1) = {NumberFormatter.Format(fpp)} -> INCONCLUSIVE (degenerate case)";
            }

            if (n == 2)
            {
                double det = minors[1]; // |H|
                double d2f_dx1sq = hessian[0, 0];

                if (det > eps && d2f_dx1sq > eps)
                    return $"|H| = {NumberFormatter.Format(det)} > 0 and d2f/dx1^2 = {NumberFormatter.Format(d2f_dx1sq)} > 0 -> CONVEX (local minimum)";
                if (det > eps && d2f_dx1sq < -eps)
                    return $"|H| = {NumberFormatter.Format(det)} > 0 and d2f/dx1^2 = {NumberFormatter.Format(d2f_dx1sq)} < 0 -> CONCAVE (local maximum)";
                if (det < -eps)
                    return $"|H| = {NumberFormatter.Format(det)} < 0 -> SADDLE POINT";
                return $"|H| = {NumberFormatter.Format(det)} -> INCONCLUSIVE (degenerate case, |H| = 0)";
            }

            // n > 2: general leading-principal-minor test.
            bool allNonNegative = true;
            bool alternatingFromNegative = true;
            for (int k = 1; k <= n; k++)
            {
                double minor = minors[k - 1];
                if (minor < -eps) allNonNegative = false;

                double expectedSign = (k % 2 == 1) ? -1 : 1;
                if (expectedSign * minor < -eps) alternatingFromNegative = false;
            }

            if (allNonNegative)
                return "All leading principal minors >= 0 -> CONVEX (local minimum)";
            if (alternatingFromNegative)
                return "Leading principal minors alternate in sign starting negative -> CONCAVE (local maximum)";
            return "Leading principal minors satisfy neither pattern -> SADDLE POINT / INDEFINITE";
        }

        public static string FormatMatrix(double[,] m)
        {
            int n = m.GetLength(0);
            var sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append("[ ");
                for (int j = 0; j < n; j++)
                    sb.Append(NumberFormatter.Format(m[i, j])).Append(j < n - 1 ? ", " : " ");
                sb.Append("]");
                if (i < n - 1) sb.Append("\n");
            }
            return sb.ToString();
        }
    }
}