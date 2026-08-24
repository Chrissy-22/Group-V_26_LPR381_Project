using System;
using System.Linq;
using System.Globalization;

namespace Group_V_26_LPR381_Project.Models
{
    /// <summary>
    /// Detects whether txtProblemInput contains a non-linear problem (prefixed "nlp")
    /// rather than a standard LP, and parses it - dispatching to either the
    /// single-variable format (Golden Section Search) or the multi-variable format
    /// (Steepest Ascent/Descent) based on whether the expression references x1, x2,
    /// ... or just a bare x.
    ///
    /// SINGLE-VARIABLE FORMAT (Golden Section Search):
    ///   nlp &lt;max|min&gt; &lt;expression using "x"&gt;
    ///   &lt;lowerBound&gt; &lt;upperBound&gt;
    ///   &lt;segments&gt;              (optional, only used by NonLinearToLinearConverter, default 10)
    ///
    ///   Example:
    ///     nlp min x^2
    ///     -5 5
    ///     10
    ///
    /// MULTI-VARIABLE FORMAT (Steepest Ascent/Descent):
    ///   nlp &lt;max|min&gt; &lt;expression using x1, x2, ...&gt;
    ///   &lt;x1&gt; &lt;x2&gt; ... &lt;xn&gt;      (starting point - unconstrained, so no bounds needed)
    ///
    ///   Example:
    ///     nlp max x1^2*x2^2
    ///     2 1
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

        /// <summary>True if the "nlp <max|min> <expr>" line's expression uses x1, x2, ...
        /// rather than a bare "x" - i.e. this is a Steepest Ascent/Descent problem,
        /// not a Golden Section Search problem.</summary>
        public static bool IsMultiVariable(string input)
        {
            var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return false;

            var firstLineParts = lines[0].Trim().Split((char[])null, 3, StringSplitOptions.RemoveEmptyEntries);
            if (firstLineParts.Length < 3) return false;

            return MultiVariableNonLinearFunction.LooksMultiVariable(firstLineParts[2]);
        }

        /// <summary>Parses the single-variable format: "nlp <max|min> <expr>" then "<lower> <upper>"
        /// then an optional segments line.</summary>
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

        /// <summary>Parses the multi-variable format: "nlp <max|min> <expression>" then
        /// "<x1> <x2> ... <xn>" (starting point).</summary>
        public static MultiVariableNonLinearInput ParseMultiVariable(string input)
        {
            var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                throw new ArgumentException("Multi-variable non-linear input needs 2 lines: 'nlp <max|min> <expression>' then '<x1> <x2> ... <xn>' (starting point).");

            var firstLineParts = lines[0].Trim().Split((char[])null, 3, StringSplitOptions.RemoveEmptyEntries);
            if (firstLineParts.Length < 3 || !firstLineParts[0].Equals("nlp", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("First line must be: nlp <max|min> <expression>");

            bool maximize = firstLineParts[1].Equals("max", StringComparison.OrdinalIgnoreCase);
            string expression = firstLineParts[2];

            var startParts = lines[1].Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var startingPoint = new double[startParts.Length];
            for (int i = 0; i < startParts.Length; i++)
            {
                if (!double.TryParse(startParts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out startingPoint[i]))
                    throw new ArgumentException($"Could not parse starting-point value '{startParts[i]}' on line 2.");
            }

            return new MultiVariableNonLinearInput
            {
                Expression = expression,
                Maximize = maximize,
                StartingPoint = startingPoint
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

    public class MultiVariableNonLinearInput
    {
        public string Expression { get; set; }
        public bool Maximize { get; set; }
        public double[] StartingPoint { get; set; }
    }
}