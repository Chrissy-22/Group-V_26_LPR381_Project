using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Group_V_26_LPR381_Project.Models;

namespace Group_V_26_LPR381_Project.Presentation_Layer
{
    /// <summary>
    /// Renders solution output into a RichTextBox: plain text, titled text blocks, group
    /// headers (for e.g. Branch and Bound sub-problems), and tableaux with the pivot row/
    /// column highlighted in yellow. Requires txtSolutionOutput to be a RichTextBox (plain
    /// TextBox cannot show per-character background colors).
    ///
    /// Tables use Arial (a proportional font) rather than a monospace font, so columns are
    /// aligned using RichTextBox tab stops computed from actual measured text width, not
    /// space-padding (which only lines up in a fixed-width font).
    /// </summary>
    public static class TableauRenderer
    {
        private static readonly Color PivotColor = Color.Yellow;
        private static readonly Color NormalBackColor = Color.White;
        private static readonly Color GroupHeaderColor = Color.FromArgb(0, 51, 153);
        private static readonly Color RuleColor = Color.Gray;

        private static readonly Font BodyFont = new Font("Arial", 9.5f);
        private static readonly Font BoldFont = new Font("Arial", 9.5f, FontStyle.Bold);
        private static readonly Font GroupHeaderFont = new Font("Arial", 11f, FontStyle.Bold);

        private const int CellPadding = 24;  // extra pixels between columns
        private const int MaxTabStops = 32;  // RichTextBox.SelectionTabs hard limit

        public static void AppendPlainLine(RichTextBox rtb, string text)
        {
            AppendText(rtb, text + Environment.NewLine, Color.Black, NormalBackColor, BodyFont);
        }

        public static void AppendTextBlock(RichTextBox rtb, string title, string text)
        {
            if (!string.IsNullOrEmpty(title))
                AppendText(rtb, title + Environment.NewLine, Color.Black, NormalBackColor, BoldFont);

            if (!string.IsNullOrEmpty(text))
                AppendText(rtb, text + Environment.NewLine, Color.Black, NormalBackColor, BodyFont);
        }

        /// <summary>Distinct, indented section header - used to group Branch and Bound sub-problems.</summary>
        public static void AppendGroupHeader(RichTextBox rtb, string title, int indentLevel = 0)
        {
            string indent = new string(' ', Math.Max(0, indentLevel) * 4);
            string bar = new string('-', 64);

            AppendText(rtb, Environment.NewLine, Color.Black, NormalBackColor, BodyFont);
            AppendText(rtb, indent + bar + Environment.NewLine, RuleColor, NormalBackColor, BodyFont);
            AppendText(rtb, indent + title + Environment.NewLine, GroupHeaderColor, NormalBackColor, GroupHeaderFont);
            AppendText(rtb, indent + bar + Environment.NewLine, RuleColor, NormalBackColor, BodyFont);
        }

        public static void AppendTableau(RichTextBox rtb, double[,] tableau, List<string> columnHeaders,
            int pivotRow, int pivotCol, string title = null)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            var headers = (columnHeaders != null && columnHeaders.Count == cols)
                ? columnHeaders.ToArray()
                : Enumerable.Range(0, cols).Select(j => $"C{j}").ToArray();

            var cellText = new string[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    cellText[i, j] = NumberFormatter.Format(tableau[i, j]);

            string RowLabel(int i) => i == 0 ? "Z" : $"R{i}";

            // Measure column widths in Arial, then build cumulative tab-stop positions (in
            // twips) so cells line up despite the font being proportional, not monospace.
            int labelWidthPx = TextRenderer.MeasureText(RowLabel(rows - 1), BodyFont).Width + CellPadding;
            var colWidthsPx = new int[cols];
            for (int j = 0; j < cols; j++)
            {
                int maxPx = TextRenderer.MeasureText(headers[j], BodyFont).Width;
                for (int i = 0; i < rows; i++)
                    maxPx = Math.Max(maxPx, TextRenderer.MeasureText(cellText[i, j], BodyFont).Width);
                colWidthsPx[j] = maxPx + CellPadding;
            }

            // SelectionTabs takes tab stops in pixels, not twips - no unit conversion needed.
            var tabStops = new List<int>();
            int cumulativePx = labelWidthPx;
            tabStops.Add(cumulativePx);
            for (int j = 0; j < cols; j++)
            {
                cumulativePx += colWidthsPx[j];
                tabStops.Add(cumulativePx);
            }
            var tabArray = tabStops.Take(MaxTabStops).ToArray();

            if (!string.IsNullOrEmpty(title))
                AppendText(rtb, title + Environment.NewLine, Color.Black, NormalBackColor, BoldFont);

            int blockStart = rtb.TextLength;

            // Header row
            AppendText(rtb, "\t", Color.Black, NormalBackColor, BodyFont);
            for (int j = 0; j < cols; j++)
            {
                bool isPivotCol = j == pivotCol;
                Color bg = isPivotCol ? PivotColor : NormalBackColor;
                AppendText(rtb, headers[j], Color.Black, bg, BodyFont);
                AppendText(rtb, "\t", Color.Black, bg, BodyFont);
            }
            AppendText(rtb, Environment.NewLine, Color.Black, NormalBackColor, BodyFont);

            // Data rows
            for (int i = 0; i < rows; i++)
            {
                bool isPivotRow = i == pivotRow;
                Color labelBg = isPivotRow ? PivotColor : NormalBackColor;
                AppendText(rtb, RowLabel(i), Color.Black, labelBg, BodyFont);
                AppendText(rtb, "\t", Color.Black, labelBg, BodyFont);

                for (int j = 0; j < cols; j++)
                {
                    bool highlight = isPivotRow || j == pivotCol;
                    Color bg = highlight ? PivotColor : NormalBackColor;
                    AppendText(rtb, cellText[i, j], Color.Black, bg, BodyFont);
                    AppendText(rtb, "\t", Color.Black, bg, BodyFont);
                }
                AppendText(rtb, Environment.NewLine, Color.Black, NormalBackColor, BodyFont);
            }

            // Apply tab stops to the block just written so its columns line up
            rtb.Select(blockStart, rtb.TextLength - blockStart);
            rtb.SelectionTabs = tabArray;
            rtb.Select(rtb.TextLength, 0);

            AppendText(rtb, Environment.NewLine, Color.Black, NormalBackColor, BodyFont);
        }

        private static void AppendText(RichTextBox rtb, string text, Color foreColor, Color backColor, Font font)
        {
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionFont = font;
            rtb.SelectionColor = foreColor;
            rtb.SelectionBackColor = backColor;
            rtb.AppendText(text);
            rtb.SelectionBackColor = NormalBackColor; // reset so the trailing caret doesn't inherit yellow
        }


    }
}