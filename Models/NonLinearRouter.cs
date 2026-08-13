using System;
using System.Linq;
using System.Globalization;

namespace Group_V_26_LPR381_Project.Models
{
    /// <summary>
    /// Detects whether txtProblemInput contains a non-linear problem (prefixed "nlp")
    /// rather than a standard LP, and parses it.
    ///
    /// Format:
    ///   nlp &lt;max|min&gt; &lt;expression&gt;
    ///   &lt;lowerBound&gt; &lt;upperBound&gt;
    ///   &lt;segments&gt;              (optional, default 10)
    ///
    /// Example:
    ///   nlp min x^2
    ///   -5 5
    ///   10
    /// </summary>
    public static class NonLinearRouter
    {
        public static bool IsNonLinearInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            string firstToken = input.TrimStart()
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            return string.Equals(firstToken, "nlp", StringComparison.OrdinalIgnoreCase);
        }

        public static NonLinearInput Parse(string input)
        {
            var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                throw new ArgumentException("Non-linear input needs at least 2 lines: 'nlp <max|min> <expression>' then '<lower> <upper>'.");

            var firstLineParts = lines[0].Trim().Split((char[])null, 3, StringSplitOptions.RemoveEmptyEntries);
            if (firstLineParts.Length < 3 || !firstLineParts[0].Equals("nlp", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("First line must be: nlp <max|min> <expression>");

            bool maximize = firstLineParts[1].Equals("max", StringComparison.OrdinalIgnoreCase);
            string expression = firstLineParts[2];

            var boundsParts = lines[1].Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (boundsParts.Length < 2 ||
                !double.TryParse(boundsParts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lower) ||
                !double.TryParse(boundsParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double upper))
                throw new ArgumentException("Second line must be: <lowerBound> <upperBound>");

            int segments = 10;
            if (lines.Length >= 3 && int.TryParse(lines[2].Trim(), out int parsedSegments) && parsedSegments >= 2)
                segments = parsedSegments;

            return new NonLinearInput
            {
                Expression = expression,
                LowerBound = lower,
                UpperBound = upper,
                Maximize = maximize,
                Segments = segments
            };
        }
    }

    public class NonLinearInput
    {
        public string Expression { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public bool Maximize { get; set; }
        public int Segments { get; set; }
    }
}