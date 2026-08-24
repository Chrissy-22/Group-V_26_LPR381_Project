using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Group_V_26_LPR381_Project.Models.LinearProgram;

namespace Group_V_26_LPR381_Project.Algorithms
{
    internal class ConstraintHandler
    {
        private const double TOL = 1e-6;
        private const double BIG_M = 1_000_000; // Must match DualSimplex.BIG_M

        public class ConstraintAdditionResult
        {
            public double[,] NewTableau { get; set; }
            public List<string> NewAuxiliaryVariableNames { get; set; }
            public List<string> Messages { get; set; }
            public bool RequiresDualSimplex { get; set; }
            public int NewSlackCount { get; set; }
            public int NewExcessCount { get; set; }
            public int NewArtificialCount { get; set; }

            public ConstraintAdditionResult()
            {
                Messages = new List<string>();
            }
        }

        public ConstraintAdditionResult AddConstraint(
            double[,] currentTableau,
            LinearProgram.Constraint constraint,
            List<string> auxiliaryVariableNames,
            int variableCount,
            int slackCount,
            int excessCount,
            int artificialCount,
            IList<int> orderedBasicColumns = null)
        {
            var result = new ConstraintAdditionResult();

            int currentRows = currentTableau.GetLength(0);
            int currentCols = currentTableau.GetLength(1);

            string newAuxVarName = "";
            result.NewSlackCount = slackCount;
            result.NewExcessCount = excessCount;
            result.NewArtificialCount = artificialCount;

            switch (constraint.Relation)
            {
                case LinearProgram.Relation.LessThanOrEqual:
                    result.NewSlackCount++;
                    newAuxVarName = $"s{result.NewSlackCount}";
                    break;
                case LinearProgram.Relation.GreaterThanOrEqual:
                    result.NewExcessCount++;
                    newAuxVarName = $"e{result.NewExcessCount}";
                    break;
                case LinearProgram.Relation.Equal:
                    result.NewArtificialCount++;
                    newAuxVarName = $"a{result.NewArtificialCount}";
                    break;
            }

            int newRows = currentRows + 1;
            int newCols = currentCols + 1;
            result.NewTableau = new double[newRows, newCols];

            for (int i = 0; i < currentRows; i++)
            {
                for (int j = 0; j < currentCols - 1; j++)
                {
                    result.NewTableau[i, j] = currentTableau[i, j];
                }
                result.NewTableau[i, newCols - 2] = 0; // New aux column
                result.NewTableau[i, newCols - 1] = currentTableau[i, currentCols - 1];
            }

            int newConstraintRow = newRows - 1;

            for (int j = 0; j < variableCount; j++)
            {
                result.NewTableau[newConstraintRow, j] = j < constraint.Coefficients.Count ? constraint.Coefficients[j] : 0;
            }

            for (int j = variableCount; j < newCols - 2; j++)
            {
                result.NewTableau[newConstraintRow, j] = 0;
            }

            switch (constraint.Relation)
            {
                case LinearProgram.Relation.LessThanOrEqual:
                    result.NewTableau[newConstraintRow, newCols - 2] = 1;
                    break;
                case LinearProgram.Relation.GreaterThanOrEqual:
                    for (int j = 0; j < newCols - 1; j++)
                    {
                        result.NewTableau[newConstraintRow, j] *= -1;
                    }
                    result.NewTableau[newConstraintRow, newCols - 1] = -constraint.Rhs;
                    result.NewTableau[newConstraintRow, newCols - 2] = 1; // Positive excess
                    break;
                case LinearProgram.Relation.Equal:
                    // Artificial coefficient only - the Big-M penalty is applied AFTER the row
                    // is fully finalized below (see ApplyBigMPenalty), not raw here, so the
                    // tableau stays in canonical form (objective row = 0 under every basic column).
                    result.NewTableau[newConstraintRow, newCols - 2] = 1;
                    break;
            }

            result.NewTableau[newConstraintRow, newCols - 1] = constraint.Relation == Relation.GreaterThanOrEqual ? -constraint.Rhs : constraint.Rhs;

            result.NewAuxiliaryVariableNames = new List<string>(auxiliaryVariableNames);
            result.NewAuxiliaryVariableNames.Add(newAuxVarName);

            result.Messages.Add($"Added new constraint with {newAuxVarName}");

            result = FixBasicVariables(result, orderedBasicColumns);
            result = FixNegativeAuxiliaryVariable(result);

            // Apply the Big-M penalty and eliminate the artificial column from the objective
            // row only now that the row is finalized (sign fixes, basic-variable fixes done).
            if (newAuxVarName.StartsWith("a"))
            {
                result = ApplyBigMPenalty(result, newCols - 2, newConstraintRow);
            }

            result.RequiresDualSimplex = HasNegativeRHS(result.NewTableau);

            return result;
        }

        private ConstraintAdditionResult ApplyBigMPenalty(ConstraintAdditionResult result, int auxCol, int constraintRow)
        {
            int cols = result.NewTableau.GetLength(1);

            // Assumes the artificial variable's coefficient in its own row is 1 (guaranteed
            // by the setup + sign-fix steps above). Set the raw penalty, then eliminate it
            // from row 0 so the tableau is canonical for the new basic (artificial) variable.
            result.NewTableau[0, auxCol] = BIG_M;
            for (int j = 0; j < cols; j++)
            {
                result.NewTableau[0, j] -= BIG_M * result.NewTableau[constraintRow, j];
            }

            result.Messages.Add("Applied Big-M penalty and eliminated the artificial variable from the objective row.");
            return result;
        }

        private ConstraintAdditionResult FixBasicVariables(
            ConstraintAdditionResult result,
            IList<int> orderedBasicColumns)
        {
            int rows = result.NewTableau.GetLength(0);
            int cols = result.NewTableau.GetLength(1);
            int newConstraintRow = rows - 1;
            int existingConstraintCount = rows - 2;

            if (orderedBasicColumns != null)
            {
                if (orderedBasicColumns.Count != existingConstraintCount)
                    throw new InvalidOperationException("The supplied ordered basis does not match the tableau rows.");

                for (int basisPosition = 0; basisPosition < orderedBasicColumns.Count; basisPosition++)
                {
                    int basicRow = basisPosition + 1;
                    int basicColumn = orderedBasicColumns[basisPosition];
                    if (basicColumn < 0 || basicColumn >= cols - 2 ||
                        !IsUnitBasicColumn(result.NewTableau, basicColumn, basicRow,
                            existingConstraintCount))
                    {
                        throw new InvalidOperationException("A supplied basis column is not canonical.");
                    }

                    EliminateBasicVariable(result, newConstraintRow, basicRow, basicColumn);
                }
                return result;
            }

            // Existing callers do not provide a snapshot. Detect all canonical columns,
            // including slacks/excess variables, instead of only decision variables.
            for (int varCol = 0; varCol < cols - 2; varCol++)
            {
                int basicRow = FindUniqueBasicRow(
                    result.NewTableau, varCol, existingConstraintCount);
                if (basicRow != -1)
                {
                    EliminateBasicVariable(result, newConstraintRow, basicRow, varCol);
                }
            }

            return result;
        }

        private void EliminateBasicVariable(ConstraintAdditionResult result,
            int newConstraintRow, int basicRow, int basicColumn)
        {
            double factor = result.NewTableau[newConstraintRow, basicColumn];
            if (Math.Abs(factor) <= TOL)
                return;

            result.Messages.Add("Eliminated current basic column " + (basicColumn + 1) +
                " from the added constraint.");
            for (int column = 0; column < result.NewTableau.GetLength(1); column++)
            {
                result.NewTableau[newConstraintRow, column] -=
                    factor * result.NewTableau[basicRow, column];
            }
        }

        private bool IsUnitBasicColumn(double[,] tableau, int column, int basicRow,
            int existingConstraintCount)
        {
            for (int row = 1; row <= existingConstraintCount; row++)
            {
                double expected = row == basicRow ? 1.0 : 0.0;
                if (Math.Abs(tableau[row, column] - expected) > TOL)
                    return false;
            }
            return true;
        }

        private int FindUniqueBasicRow(double[,] tableau, int column,
            int existingConstraintCount)
        {
            int basicRow = -1;
            for (int row = 1; row <= existingConstraintCount; row++)
            {
                if (!IsUnitBasicColumn(tableau, column, row, existingConstraintCount))
                    continue;

                if (basicRow != -1)
                    return -1;
                basicRow = row;
            }
            return basicRow;
        }

        private ConstraintAdditionResult FixNegativeAuxiliaryVariable(ConstraintAdditionResult result)
        {
            int rows = result.NewTableau.GetLength(0);
            int cols = result.NewTableau.GetLength(1);
            int newConstraintRow = rows - 1;
            int newAuxVarCol = cols - 2;

            string auxVarName = result.NewAuxiliaryVariableNames.Last();
            double auxCoefficient = result.NewTableau[newConstraintRow, newAuxVarCol];

            if (auxVarName.StartsWith("s"))
            {
                if (auxCoefficient < -TOL)
                {
                    result.Messages.Add("The new slack variable (s) coefficient is negative. Fixed by multiplying the row by -1.");
                    for (int j = 0; j < cols; j++)
                        result.NewTableau[newConstraintRow, j] *= -1;
                }
            }
            else if (auxVarName.StartsWith("e"))
            {
                if (auxCoefficient < -TOL)
                {
                    result.Messages.Add("The new excess variable (e) coefficient is negative. Fixed by multiplying the row by -1 for dual simplex.");
                    for (int j = 0; j < cols; j++)
                        result.NewTableau[newConstraintRow, j] *= -1;
                }
                else
                {
                    result.Messages.Add("Excess variable coefficient is positive, ready for dual simplex.");
                }
            }
            else if (auxVarName.StartsWith("a"))
            {
                if (auxCoefficient < -TOL)
                {
                    result.Messages.Add("Artificial variable coefficient is negative. Fixed by multiplying the row by -1.");
                    for (int j = 0; j < cols; j++)
                        result.NewTableau[newConstraintRow, j] *= -1;
                }
            }

            return result;
        }

        private bool HasNegativeRHS(double[,] tableau)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            for (int i = 1; i < rows; i++)
            {
                if (tableau[i, cols - 1] < -TOL)
                    return true;
            }
            return false;
        }

        public string TableauToString(double[,] tableau, List<string> variableNames, List<string> auxiliaryVariableNames)
        {
            var sb = new StringBuilder();
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);

            sb.AppendLine("Tableau:");
            sb.Append("        ");

            for (int j = 0; j < variableNames.Count; j++)
                sb.Append($"{variableNames[j],-10}");

            foreach (var auxVarName in auxiliaryVariableNames)
                sb.Append($"{auxVarName,-10}");

            sb.AppendLine("RHS");

            for (int i = 0; i < rows; i++)
            {
                sb.Append(i == 0 ? "Z:      " : $"Con {i}:  ");

                for (int j = 0; j < cols; j++)
                {
                    sb.Append($"{NumberFormatter.Format(tableau[i, j]),10}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
