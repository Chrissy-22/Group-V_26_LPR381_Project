using System;

namespace Group_V_26_LPR381_Project.Models
{
    /// <summary>
    /// Formats numeric values for display: rounds to 3 decimal places,
    /// but omits the decimal portion entirely when the value is a whole number.
    /// </summary>
    public static class NumberFormatter
    {
        private const double Tolerance = 1e-9;

        public static string Format(double value)
        {
            double rounded = Math.Round(value, 3, MidpointRounding.AwayFromZero);

            // Avoid printing "-0"
            if (Math.Abs(rounded) < Tolerance) rounded = 0;

            bool isWhole = Math.Abs(rounded - Math.Round(rounded)) < Tolerance;
            return isWhole ? ((long)Math.Round(rounded)).ToString() : rounded.ToString("0.000");
        }
    }
}