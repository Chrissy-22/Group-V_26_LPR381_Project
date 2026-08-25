using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Group_V_26_LPR381_Project.Models
{
    /// <summary>
    /// A sparse multivariate polynomial in x1..xn, represented as a map from
    /// exponent-vector ("2,0" meaning x1^2) to coefficient. Used to turn an
    /// arbitrary polynomial expression (built from +, -, *, / by a constant, and
    /// ^ by a non-negative integer constant) into its fully expanded/collected
    /// canonical form - e.g. -(x1-3)^2-(x2-2)^2 becomes -x1^2+6x1-x2^2+4x2-13 -
    /// which the tree-based Differentiate()/Simplify() in
    /// MultiVariableNonLinearFunction deliberately does NOT do (it only folds
    /// trivial 0/1 identities, not full algebraic expansion).
    ///
    /// Anything involving sin/cos/sqrt/exp/ln, or division/exponentiation by a
    /// non-constant, cannot be represented this way - callers get null back and
    /// should fall back to numeric-only display.
    /// </summary>
    public class MultivariatePolynomial
    {
        private readonly Dictionary<string, double> _terms; // key: "e1,e2,...,en"
        public int VariableCount { get; }

        private MultivariatePolynomial(int variableCount, Dictionary<string, double> terms)
        {
            VariableCount = variableCount;
            _terms = terms;
        }

        public static MultivariatePolynomial Zero(int n) => new MultivariatePolynomial(n, new Dictionary<string, double>());

        public static MultivariatePolynomial Constant(double value, int n)
        {
            var terms = new Dictionary<string, double>();
            if (Math.Abs(value) > 1e-12)
                terms[KeyFor(new int[n])] = value;
            return new MultivariatePolynomial(n, terms);
        }

        public static MultivariatePolynomial Variable(int index1Based, int n)
        {
            var exps = new int[n];
            exps[index1Based - 1] = 1;
            var terms = new Dictionary<string, double> { [KeyFor(exps)] = 1.0 };
            return new MultivariatePolynomial(n, terms);
        }

        private static string KeyFor(int[] exps) => string.Join(",", exps);
        private static int[] ExpsFromKey(string key, int n) => key.Split(',').Select(int.Parse).ToArray();

        public bool IsConstant =>
            _terms.Count == 0 || (_terms.Count == 1 && ExpsFromKey(_terms.Keys.First(), VariableCount).All(e => e == 0));

        public double ConstantValue => _terms.Count == 0 ? 0 : _terms.First().Value;

        public MultivariatePolynomial Add(MultivariatePolynomial other)
        {
            var result = new Dictionary<string, double>(_terms);
            foreach (var kvp in other._terms)
            {
                result.TryGetValue(kvp.Key, out double existing);
                double sum = existing + kvp.Value;
                if (Math.Abs(sum) < 1e-9) result.Remove(kvp.Key);
                else result[kvp.Key] = sum;
            }
            return new MultivariatePolynomial(VariableCount, result);
        }

        public MultivariatePolynomial Negate() =>
            new MultivariatePolynomial(VariableCount, _terms.ToDictionary(kvp => kvp.Key, kvp => -kvp.Value));

        public MultivariatePolynomial Subtract(MultivariatePolynomial other) => Add(other.Negate());

        public MultivariatePolynomial Scale(double scalar)
        {
            if (Math.Abs(scalar) < 1e-15) return Zero(VariableCount);
            return new MultivariatePolynomial(VariableCount, _terms.ToDictionary(kvp => kvp.Key, kvp => kvp.Value * scalar));
        }

        public MultivariatePolynomial Multiply(MultivariatePolynomial other)
        {
            var result = new Dictionary<string, double>();
            foreach (var a in _terms)
            {
                int[] aExps = ExpsFromKey(a.Key, VariableCount);
                foreach (var b in other._terms)
                {
                    int[] bExps = ExpsFromKey(b.Key, VariableCount);
                    var sumExps = new int[VariableCount];
                    for (int i = 0; i < VariableCount; i++) sumExps[i] = aExps[i] + bExps[i];
                    string key = KeyFor(sumExps);
                    double coeff = a.Value * b.Value;
                    result.TryGetValue(key, out double existing);
                    double sum = existing + coeff;
                    if (Math.Abs(sum) < 1e-9) result.Remove(key);
                    else result[key] = sum;
                }
            }
            return new MultivariatePolynomial(VariableCount, result);
        }

        public MultivariatePolynomial Pow(int nonNegativeIntExponent)
        {
            var result = Constant(1, VariableCount);
            for (int i = 0; i < nonNegativeIntExponent; i++)
                result = result.Multiply(this);
            return result;
        }

        public MultivariatePolynomial PartialDerivative(int variableIndex1Based)
        {
            var result = new Dictionary<string, double>();
            foreach (var kvp in _terms)
            {
                int[] exps = ExpsFromKey(kvp.Key, VariableCount);
                int e = exps[variableIndex1Based - 1];
                if (e == 0) continue;
                exps[variableIndex1Based - 1] = e - 1;
                double coeff = kvp.Value * e;
                string key = KeyFor(exps);
                result.TryGetValue(key, out double existing);
                double sum = existing + coeff;
                if (Math.Abs(sum) < 1e-9) result.Remove(key);
                else result[key] = sum;
            }
            return new MultivariatePolynomial(VariableCount, result);
        }

        public double Evaluate(double[] x)
        {
            double total = 0;
            foreach (var kvp in _terms)
            {
                int[] exps = ExpsFromKey(kvp.Key, VariableCount);
                double term = kvp.Value;
                for (int i = 0; i < VariableCount; i++)
                    if (exps[i] != 0) term *= Math.Pow(x[i], exps[i]);
                total += term;
            }
            return total;
        }

        /// <summary>
        /// Substitutes x_i = x0[i] + h*direction[i] for every variable and collapses
        /// the result into a univariate polynomial in h. Returns coefficients in
        /// ascending power order: result[0] + result[1]*h + result[2]*h^2 + ...
        /// This is what turns f(x_i + h*grad_i) into the slide's g(h).
        /// </summary>
        public double[] SubstituteAffine(double[] x0, double[] direction)
        {
            double[] total = { 0 };
            foreach (var kvp in _terms)
            {
                int[] exps = ExpsFromKey(kvp.Key, VariableCount);
                double[] termPoly = { kvp.Value };
                for (int i = 0; i < VariableCount; i++)
                {
                    if (exps[i] == 0) continue;
                    double[] linear = { x0[i], direction[i] };
                    double[] powered = Polynomial1D.Pow(linear, exps[i]);
                    termPoly = Polynomial1D.Multiply(termPoly, powered);
                }
                total = Polynomial1D.Add(total, termPoly);
            }
            return total;
        }

        /// <summary>Formats in descending total-degree order, e.g. "-x1^2 + 6x1 - x2^2 + 4x2 - 13".</summary>
        public string ToDisplayString(string[] variableNames)
        {
            if (_terms.Count == 0) return "0";

            var ordered = _terms
                .Select(kvp => (Exps: ExpsFromKey(kvp.Key, VariableCount), Coeff: kvp.Value))
                .OrderByDescending(t => t.Exps.Sum())
                .ThenByDescending(t => string.Join(",", t.Exps))
                .ToList();

            var sb = new StringBuilder();
            for (int idx = 0; idx < ordered.Count; idx++)
            {
                var (exps, coeff) = ordered[idx];
                bool allZero = exps.All(e => e == 0);
                string monomial = allZero
                    ? ""
                    : string.Concat(exps.Select((e, i) => e == 0 ? "" : (e == 1 ? variableNames[i] : $"{variableNames[i]}^{e}")));

                double absCoeff = Math.Abs(coeff);
                string coeffPart = (!allZero && Math.Abs(absCoeff - 1) < 1e-9) ? "" : NumberFormatter.Format(absCoeff);
                string term = coeffPart + monomial;

                if (idx == 0)
                    sb.Append(coeff < 0 ? "-" : "").Append(term);
                else
                    sb.Append(coeff < 0 ? " - " : " + ").Append(term);
            }

            return sb.ToString();
        }
    }

    /// <summary>Dense univariate polynomial arithmetic on coefficient arrays
    /// (index = power of h, ascending). Backs MultivariatePolynomial.SubstituteAffine
    /// and formats the resulting g(h) / g'(h) for display.</summary>
    public static class Polynomial1D
    {
        public static double[] Add(double[] a, double[] b)
        {
            var result = new double[Math.Max(a.Length, b.Length)];
            for (int i = 0; i < a.Length; i++) result[i] += a[i];
            for (int i = 0; i < b.Length; i++) result[i] += b[i];
            return Trim(result);
        }

        public static double[] Multiply(double[] a, double[] b)
        {
            var result = new double[a.Length + b.Length - 1];
            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < b.Length; j++)
                    result[i + j] += a[i] * b[j];
            return Trim(result);
        }

        public static double[] Pow(double[] a, int exponent)
        {
            double[] result = { 1 };
            for (int i = 0; i < exponent; i++) result = Multiply(result, a);
            return result;
        }

        public static double[] Derivative(double[] a)
        {
            if (a.Length <= 1) return new double[] { 0 };
            var result = new double[a.Length - 1];
            for (int i = 1; i < a.Length; i++) result[i - 1] = a[i] * i;
            return Trim(result);
        }

        private static double[] Trim(double[] a)
        {
            int last = a.Length - 1;
            while (last > 0 && Math.Abs(a[last]) < 1e-9) last--;
            if (last == a.Length - 1) return a;
            var result = new double[last + 1];
            Array.Copy(a, result, last + 1);
            return result;
        }

        /// <summary>Formats coefficients (ascending power) as e.g. "-20h^2 + 20h - 5".</summary>
        public static string Format(double[] coeffs, string variableName)
        {
            if (coeffs.All(c => Math.Abs(c) < 1e-9)) return "0";

            var sb = new StringBuilder();
            bool first = true;
            for (int power = coeffs.Length - 1; power >= 0; power--)
            {
                double c = coeffs[power];
                if (Math.Abs(c) < 1e-9) continue;

                double absC = Math.Abs(c);
                string term;
                if (power == 0) term = NumberFormatter.Format(absC);
                else if (power == 1) term = (Math.Abs(absC - 1) < 1e-9 ? "" : NumberFormatter.Format(absC)) + variableName;
                else term = (Math.Abs(absC - 1) < 1e-9 ? "" : NumberFormatter.Format(absC)) + variableName + "^" + power;

                if (first)
                {
                    sb.Append(c < 0 ? "-" : "").Append(term);
                    first = false;
                }
                else
                {
                    sb.Append(c < 0 ? " - " : " + ").Append(term);
                }
            }

            return sb.ToString();
        }
    }
}