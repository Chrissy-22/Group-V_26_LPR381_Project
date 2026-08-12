using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Group_V_26_LPR381_Project.Models
{
    public class Solution
    {
        public enum OutputBlockType { Text, Tableau, GroupHeader }

        /// <summary>A single piece of output in the order it was produced.</summary>
        public class OutputBlock
        {
            public OutputBlockType Type { get; set; }
            public string Title { get; set; }
            public string Text { get; set; }
            public double[,] Tableau { get; set; }
            public List<string> ColumnHeaders { get; set; }
            public int PivotRow { get; set; } = -1;
            public int PivotCol { get; set; } = -1;
            public int IndentLevel { get; set; }
        }

        public double OptimalValue { get; set; }
        public Dictionary<string, double> VariableValues { get; set; } = new Dictionary<string, double>();
        public List<string> Steps { get; } = new List<string>();
        public List<string> Messages { get; } = new List<string>();

        public List<double[,]> IterationTableaux { get; set; } = new List<double[,]>();
        public List<string> IterationMessages { get; set; } = new List<string>();
        public List<int> IterationPivotRows { get; set; } = new List<int>();
        public List<int> IterationPivotCols { get; set; } = new List<int>();
        public List<List<string>> IterationColumnHeaders { get; set; } = new List<List<string>>();

        /// <summary>
        /// All output in the order it was produced (text, tableaux, and group headers
        /// interleaved). Rendering from this list - instead of showing all Messages, then
        /// all tables, then Steps only as a fallback - is what lets Branch and Bound's
        /// per-sub-problem commentary appear right next to the table it describes.
        /// </summary>
        public List<OutputBlock> OutputBlocks { get; } = new List<OutputBlock>();

        public double[,] FinalTableau { get; set; }
        public int VariableCount { get; set; }
        public int SlackCount { get; set; }
        public int ExcessCount { get; set; }
        public int ArtificialCount { get; set; }

        public Solution()
        {
            VariableValues = new Dictionary<string, double>();
            OptimalValue = 0;
            FinalTableau = null;
            VariableCount = 0;
            SlackCount = 0;
            ExcessCount = 0;
            ArtificialCount = 0;
        }

        public void AddStep(string title, string content)
        {
            Steps.Add(string.Concat(title, "\n", content));
            OutputBlocks.Add(new OutputBlock { Type = OutputBlockType.Text, Title = title, Text = content });
        }

        public void AddMessage(string message)
        {
            Messages.Add(message);
            OutputBlocks.Add(new OutputBlock { Type = OutputBlockType.Text, Text = message });
        }

        /// <summary>Adds a titled text block (e.g. Branch and Bound's per-sub-problem
        /// "Solution:" variable listing) without also recording it in Messages.</summary>
        public void AddTextBlock(string content, string title = null)
        {
            OutputBlocks.Add(new OutputBlock { Type = OutputBlockType.Text, Title = title, Text = content });
        }

        /// <summary>Adds a visually distinct section header, e.g. to group each Branch and
        /// Bound sub-problem. indentLevel roughly corresponds to branching depth.</summary>
        public void AddGroupHeader(string title, int indentLevel = 0)
        {
            OutputBlocks.Add(new OutputBlock { Type = OutputBlockType.GroupHeader, Title = title, IndentLevel = indentLevel });
        }

        /// <summary>
        /// Records a tableau snapshot for display.
        /// </summary>
        /// <param name="tableau">The tableau matrix at this point in the solve.</param>
        /// <param name="message">Label for this iteration (e.g. "After Dual Iteration 2").</param>
        /// <param name="pivotRow">Row index that was just pivoted on, or -1 if none (e.g. initial tableau).</param>
        /// <param name="pivotCol">Column index that was just pivoted on, or -1 if none.</param>
        /// <param name="columnHeaders">Column names (x1, x2, ..., s1, e1, a1, ..., RHS) for this snapshot.</param>
        public void AddIteration(double[,] tableau, string message = null, int pivotRow = -1, int pivotCol = -1, List<string> columnHeaders = null)
        {
            string label = string.IsNullOrEmpty(message) ? $"Iteration {IterationTableaux.Count + 1}" : message;

            IterationTableaux.Add((double[,])tableau.Clone());
            IterationMessages.Add(label);
            IterationPivotRows.Add(pivotRow);
            IterationPivotCols.Add(pivotCol);
            IterationColumnHeaders.Add(columnHeaders);

            OutputBlocks.Add(new OutputBlock
            {
                Type = OutputBlockType.Tableau,
                Title = label,
                Tableau = (double[,])tableau.Clone(),
                ColumnHeaders = columnHeaders,
                PivotRow = pivotRow,
                PivotCol = pivotCol
            });
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            if (Steps.Any())
            {
                result.AppendLine(string.Join("\n\n", Steps));
            }
            if (Messages.Any())
            {
                result.AppendLine("\n" + string.Join("\n", Messages));
            }

            if (IterationTableaux.Any())
            {
                result.AppendLine("\nTableau Iterations:");
                for (int i = 0; i < IterationTableaux.Count; i++)
                {
                    result.AppendLine($"--- {IterationMessages[i]} ---");
                    result.AppendLine(FormatTableau(IterationTableaux[i]));
                }
            }

            result.AppendLine("\nOptimal Solution:");
            if (VariableValues != null && VariableValues.Any())
            {
                foreach (var kvp in VariableValues.OrderBy(kvp => kvp.Key))
                {
                    result.AppendLine($"{kvp.Key} = {NumberFormatter.Format(kvp.Value)}");
                }
                result.AppendLine($"\nOptimal Value: {NumberFormatter.Format(OptimalValue)}");
            }
            else
            {
                result.AppendLine("No solution found.");
            }
            return result.ToString();
        }

        private string FormatTableau(double[,] tableau)
        {
            var sb = new StringBuilder();
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sb.Append(NumberFormatter.Format(tableau[i, j])).Append('\t');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}