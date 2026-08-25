using System;

namespace Group_V_26_LPR381_Project.Algorithms
{
    /// <summary>
    /// Given a single-variable function g(h), finds the h that maximizes or
    /// minimizes it. This is the "find the step size h along the calculated
    /// direction" step of the Steepest Ascent/Descent algorithm - the slides do
    /// this either analytically (dg/dh = 0, only possible when g(h) is a simple
    /// polynomial you can differentiate by hand) or with Golden Section Search.
    /// This class always uses Golden Section Search, since g(h) is built
    /// numerically at runtime from an arbitrary user expression and can't be
    /// symbolically differentiated without re-deriving calculus rules for
    /// composing it with an unknown direction vector.
    ///
    /// Because the caller doesn't know in advance which direction (positive or
    /// negative h) improves g, this first BRACKETS the optimum - expanding a step
    /// outward (by the golden ratio each try) in whichever direction initially
    /// improves g, until g stops improving - and only then runs Golden Section
    /// Search inside that bracket. This is what lets the result come out negative
    /// (h = -0.5 in the slide's worked example) even though nothing about the
    /// gradient itself hints at a sign.
    /// </summary>
    public static class LineSearch1D
    {
        private const double GoldenRatio = 0.6180339887498949;
        private const double ExpansionFactor = 1.618033988749895;
        private const double MaxAbsStep = 1e8; // guards against runaway expansion on unbounded objectives

        /// <summary>Returns the step size h that optimizes g, or null if g appears to
        /// have no finite optimum along this direction (i.e. it's unbounded).</summary>
        public static double? FindStep(Func<double, double> g, bool maximize, double tolerance = 1e-8, int maxExpansions = 80)
        {
            bool Better(double newVal, double oldVal) => maximize ? newVal > oldVal + tolerance : newVal < oldVal - tolerance;

            double a = 0.0;
            double ga = SafeEvaluate(g, a, maximize);

            double step = 1e-3;
            double b = a + step;
            double gb = SafeEvaluate(g, b, maximize);

            if (!Better(gb, ga))
            {
                // No improvement in the positive direction - try negative.
                step = -step;
                b = a + step;
                gb = SafeEvaluate(g, b, maximize);

                if (!Better(gb, ga))
                {
                    // g doesn't improve in either direction from h=0 - already at (or
                    // essentially at) a stationary point along this gradient.
                    return 0.0;
                }
            }

            double c = b + (b - a) * ExpansionFactor;
            double gc = SafeEvaluate(g, c, maximize);

            int expansions = 0;
            while (Better(gc, gb) && expansions < maxExpansions)
            {
                if (Math.Abs(c) > MaxAbsStep)
                    return null; // no finite optimum found - objective appears unbounded

                a = b; ga = gb;
                b = c; gb = gc;
                c = b + (b - a) * ExpansionFactor;
                gc = SafeEvaluate(g, c, maximize);
                expansions++;
            }

            if (expansions >= maxExpansions)
                return null;

            double lo = Math.Min(a, c);
            double hi = Math.Max(a, c);
            return GoldenSectionOnBracket(g, lo, hi, maximize, tolerance);
        }

        private static double GoldenSectionOnBracket(Func<double, double> g, double lo, double hi, bool maximize, double tolerance)
        {
            // SafeEvaluate already biases invalid points toward "worst" for the
            // requested direction, so this can be used directly - negate for the
            // internal always-minimize comparison the golden section loop performs.
            Func<double, double> objective = h =>
            {
                double v = SafeEvaluate(g, h, maximize);
                return maximize ? -v : v;
            };

            double a = lo, b = hi;
            double x1 = b - GoldenRatio * (b - a);
            double x2 = a + GoldenRatio * (b - a);
            double f1 = objective(x1);
            double f2 = objective(x2);

            int iteration = 0;
            while (Math.Abs(b - a) > tolerance && iteration < 200)
            {
                iteration++;
                if (f1 < f2)
                {
                    b = x2; x2 = x1; f2 = f1;
                    x1 = b - GoldenRatio * (b - a);
                    f1 = objective(x1);
                }
                else
                {
                    a = x1; x1 = x2; f1 = f2;
                    x2 = a + GoldenRatio * (b - a);
                    f2 = objective(x2);
                }
            }

            return (a + b) / 2.0;
        }

        private static double SafeEvaluate(Func<double, double> g, double h, bool maximize)
        {
            try
            {
                double v = g(h);
                if (double.IsNaN(v) || double.IsInfinity(v))
                    return maximize ? double.NegativeInfinity : double.PositiveInfinity;
                return v;
            }
            catch
            {
                // e.g. sqrt/ln of a negative number outside the function's domain at this h.
                return maximize ? double.NegativeInfinity : double.PositiveInfinity;
            }
        }
    }
}