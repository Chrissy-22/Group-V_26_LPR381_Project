using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Group_V_26_LPR381_Project.Algorithms
{
    /// <summary>
    /// Sensitivity calculations use the lecturer's c_j* = Zj - Cj convention.
    /// The available basis snapshot is intentionally limited to maximisation LPs with
    /// non-negative continuous variables, <= constraints, and non-negative RHS values.
    /// </summary>
    public class SensitivityAnalysis : ISolver
    {
        private const double Tolerance = 1e-9;

        public Solution Solve(LinearProgram program)
        {
            BasisSnapshot snapshot;
            string error;
            if (!TryCreateBasisSnapshot(program, out snapshot, out error))
                return Failure(error);

            var solution = BuildReport(snapshot);

            solution.AddGroupHeader("Editable Model (Add a New Constraint or Activity)", 1);
            solution.AddMessage("Copy the block below into the Problem Input box. To add a new CONSTRAINT,\n" +
                "insert a new line above the sign-restrictions line (e.g. \"+ 1 + 0 + 0 <= 5\" for x1 <= 5).\n" +
                "To add a new ACTIVITY, append a new sign/value pair to the objective line, one matching\n" +
                "coefficient to every constraint line, and one sign token to the sign-restrictions line.\n" +
                "Then re-solve with any algorithm button to get the updated optimum.\n");
            solution.AddStep("Problem Input", FormatAsParseableInput(program));

            return solution;
        }

        // Solver failures are rendered like ordinary solver output so the UI can show the
        // reason without depending on exception handling for unsupported LP forms.
        private static Solution Failure(string message)
        {
            var solution = new Solution();
            solution.AddGroupHeader("Sensitivity Analysis");
            solution.AddMessage(message);
            return solution;
        }

        /// <summary>
        /// Captures the basis in tableau-row order. A row with anything other than one
        /// unit-column candidate is rejected rather than guessed from variable values.
        /// </summary>
        public bool TryCreateBasisSnapshot(LinearProgram program,
            out BasisSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = null;
            if (!Supports(program, out error))
                return false;

            Solution solved = new PrimalSimplex().Solve(program);
            if (solved.IterationTableaux == null || solved.IterationTableaux.Count == 0)
            {
                error = "Primal Simplex did not produce a final optimal tableau.";
                return false;
            }

            int m = program.Constraints.Count;
            int n = program.Variables.Count;
            int columns = n + m;
            double[,] tableau = solved.IterationTableaux.Last();
            if (tableau.GetLength(0) != m + 1 || tableau.GetLength(1) != columns + 1)
            {
                error = "The final tableau is not compatible with the standard canonical model.";
                return false;
            }

            int[] basicColumns;
            if (!TryFindBasis(tableau, columns, m, out basicColumns, out error))
                return false;

            double[,] canonical = CanonicalMatrix(program);
            double[] objective = CanonicalObjective(program);
            double[,] basis = Basis(canonical, basicColumns);
            double[,] inverse;
            if (!TryInvert(basis, out inverse))
            {
                error = "The reconstructed basis matrix is singular.";
                return false;
            }

            double[] rhs = program.Constraints.Select(c => c.Rhs).ToArray();
            double[] basicCosts = basicColumns.Select(column => objective[column]).ToArray();
            double[] basicValues = Multiply(inverse, rhs);
            double[] prices = MultiplyRow(basicCosts, inverse);
            double[] reducedCosts = ReducedCosts(canonical, objective, prices);

            if (basicValues.Any(value => value < -Tolerance))
            {
                error = "The recovered basis is infeasible because B^-1 b contains a negative value.";
                return false;
            }

            if (!IsIdentity(Multiply(basis, inverse)) ||
                !Matches(tableau, inverse, canonical, basicValues, reducedCosts))
            {
                error = "The reconstructed basis does not reproduce the final tableau. " +
                    "Sensitivity analysis was stopped rather than use an unsafe basis.";
                return false;
            }

            if (reducedCosts.Any(value => value < -Tolerance))
            {
                error = "The recovered basis is not optimal under Zj - Cj >= 0.";
                return false;
            }

            snapshot = new BasisSnapshot
            {
                ColumnNames = ColumnNames(program),
                BasicColumnIndices = basicColumns.ToList(),
                CanonicalMatrix = canonical,
                ObjectiveCoefficients = objective,
                OriginalRhs = rhs,
                BasisMatrix = basis,
                BasisInverse = inverse,
                BasicCosts = basicCosts,
                BasicValues = basicValues,
                ShadowPrices = prices,
                ReducedCosts = reducedCosts,
                ObjectiveValue = Dot(prices, rhs),
                FinalTableau = (double[,])tableau.Clone()
            };
            return true;
        }

        /// <summary>
        /// Calculates B^-1 a_j and q a_j - c_j for a replacement non-basic decision column.
        /// B and B^-1 are deliberately not rebuilt.
        /// </summary>
        public ColumnSensitivityResult AnalyzeNonBasicColumn(LinearProgram program,
            string variableName, IEnumerable<double> replacementColumn,
            double newObjectiveCoefficient)
        {
            BasisSnapshot snapshot;
            string error;
            if (!TryCreateBasisSnapshot(program, out snapshot, out error))
                throw new InvalidOperationException(error);

            int column = snapshot.ColumnNames.IndexOf(variableName);
            if (column < 0 || column >= program.Variables.Count ||
                snapshot.BasicColumnIndices.Contains(column))
            {
                throw new ArgumentException("The selected variable must be a current non-basic " +
                    "decision variable.", nameof(variableName));
            }

            return EvaluateColumn(snapshot, variableName, replacementColumn,
                newObjectiveCoefficient);
        }

        /// <summary>
        /// Treats a proposed activity as a new non-basic column without changing the model.
        /// </summary>
        public ColumnSensitivityResult AnalyzeNewActivity(LinearProgram program,
            string activityName, IEnumerable<double> activityColumn,
            double objectiveCoefficient)
        {
            BasisSnapshot snapshot;
            string error;
            if (!TryCreateBasisSnapshot(program, out snapshot, out error))
                throw new InvalidOperationException(error);

            return EvaluateColumn(snapshot, activityName, activityColumn,
                objectiveCoefficient);
        }

        /// <summary>
        /// Starts from the current optimal tableau when adding a constraint. The added row is
        /// first canonicalised against all known basic columns; a violated row is restored by
        /// the existing Dual Simplex mechanism rather than solving the original LP again.
        /// </summary>
        public Solution AnalyzeAddedConstraint(LinearProgram program,
            LinearProgram.Constraint constraint)
        {
            BasisSnapshot snapshot;
            string error;
            if (!TryCreateBasisSnapshot(program, out snapshot, out error))
                return Failure(error);
            if (constraint == null || constraint.Coefficients == null ||
                constraint.Coefficients.Count != program.Variables.Count)
            {
                return Failure("The added constraint must include every decision variable.");
            }

            var result = new Solution
            {
                OptimalValue = snapshot.ObjectiveValue,
                VariableValues = DecisionValues(snapshot)
            };
            result.AddGroupHeader("Sensitivity Analysis: Adding a Constraint");
            result.AddStep("Current Optimal Basis", FormatBasis(snapshot));
            result.AddStep("New Row After Eliminating Current Basic Variables",
                FormatAddedRow(program, constraint, snapshot));

            if (Satisfies(program, constraint, snapshot))
            {
                result.AddMessage("The current optimum satisfies the new constraint and remains optimal.");
                return result;
            }

            result.AddMessage("The current optimum violates the new constraint. " +
                "The augmented tableau is resolved from the current basis.");
            var dual = new DualSimplex();
            dual.LoadOptimalTableau(program, snapshot.FinalTableau, program.Constraints.Count,
                program.Constraints.Count, 0, 0,
                Enumerable.Range(1, program.Constraints.Count)
                    .Select(index => "s" + index).ToList(),
                snapshot.BasicColumnIndices);

            Solution updated = dual.AddConstraintAndResolve(constraint);
            CopyOutput(result, updated);
            if (updated.Messages.Any(message => message.StartsWith("Problem is infeasible")))
            {
                result.OptimalValue = double.NaN;
                result.VariableValues = new Dictionary<string, double>();
                result.AddMessage("The added constraint makes the LP infeasible; no new optimum exists.");
                return result;
            }

            result.OptimalValue = updated.OptimalValue;
            result.VariableValues = updated.VariableValues;
            return result;
        }

        private static Solution BuildReport(BasisSnapshot snapshot)
        {
            var solution = new Solution
            {
                OptimalValue = snapshot.ObjectiveValue,
                VariableValues = DecisionValues(snapshot)
            };
            solution.AddGroupHeader("Sensitivity Analysis");
            solution.AddMessage("Reduced costs use c_j* = Zj - Cj. For maximisation, all " +
                "non-basic c_j* values must be >= 0.");

            solution.AddGroupHeader("Current Optimal Solution", 1);
            solution.AddStep("Decision Variables", FormatDictionary(solution.VariableValues));
            solution.AddStep("Objective Value", "z* = q * b = " +
                NumberFormatter.Format(snapshot.ObjectiveValue));

            solution.AddGroupHeader("Basis", 1);
            solution.AddStep("Basic Variables", FormatBasis(snapshot));
            solution.AddStep("Non-Basic Variables", string.Join(", ",
                NonBasic(snapshot).Select(column => snapshot.ColumnNames[column])));
            solution.AddStep("C_BV", FormatColumns(snapshot, snapshot.BasicColumnIndices,
                snapshot.BasicCosts, true));
            solution.AddStep("C_NBV", FormatColumns(snapshot, NonBasic(snapshot),
                snapshot.ObjectiveCoefficients, false));
            solution.AddStep("B (original canonical columns)", FormatMatrix(snapshot.BasisMatrix));
            solution.AddStep("B^-1", FormatMatrix(snapshot.BasisInverse));
            solution.AddStep("b and b* = B^-1 b",
                "b = [" + string.Join(", ", snapshot.OriginalRhs.Select(NumberFormatter.Format)) +
                "]" + Environment.NewLine + "b* = [" +
                string.Join(", ", snapshot.BasicValues.Select(NumberFormatter.Format)) + "]");

            solution.AddGroupHeader("Reduced Costs and Shadow Prices", 1);
            solution.AddStep("q = C_BV * B^-1 (Shadow Prices)",
                FormatPrices(snapshot.ShadowPrices));
            solution.AddStep("c_j* = q * a_j - c_j",
                FormatColumns(snapshot, Enumerable.Range(0, snapshot.ColumnNames.Count).ToList(),
                    snapshot.ReducedCosts, false));

            solution.AddGroupHeader("Objective Coefficient Ranges", 1);
            solution.AddStep("Non-Basic Variables", FormatNonBasicRanges(snapshot));
            solution.AddStep("Basic Variables", FormatBasicRanges(snapshot));

            solution.AddGroupHeader("RHS Ranges", 1);
            solution.AddStep("Feasibility and Objective Effects", FormatRhsRanges(snapshot));
            solution.AddGroupHeader("Result and Interpretation", 1);
            solution.AddMessage("All range boundaries are included. A zero reduced cost can " +
                "indicate an alternate optimal basis.");
            solution.AddMessage("For RHS changes, b*(Delta) = b* + column_i(B^-1)Delta must " +
                "remain non-negative. Within the range, z'(Delta) = z* + q_iDelta.");
            return solution;
        }

        private static bool Supports(LinearProgram program, out string error)
        {
            error = null;
            if (program == null)
            {
                error = "No linear program was provided.";
                return false;
            }
            if (program.isKnapsackProblem || program.WeightConstraints.Any())
            {
                error = "Knapsack input is not supported by sensitivity analysis.";
                return false;
            }
            if (!program.IsMaximization)
            {
                error = "The available minimisation solver does not expose a safe basis snapshot. " +
                    "Sensitivity analysis currently accepts maximisation LPs only.";
                return false;
            }
            if (program.Variables.Count == 0 || program.Constraints.Count == 0)
            {
                error = "The LP needs decision variables and at least one constraint.";
                return false;
            }
            if (program.Variables.Any(variable =>
                variable.Type != LinearProgram.VariableType.NonNegative))
            {
                error = "Only non-negative continuous variables are supported.";
                return false;
            }
            for (int row = 0; row < program.Constraints.Count; row++)
            {
                LinearProgram.Constraint constraint = program.Constraints[row];
                if (constraint.Relation != LinearProgram.Relation.LessThanOrEqual ||
                    constraint.Rhs < -Tolerance ||
                    constraint.Coefficients.Count != program.Variables.Count)
                {
                    error = "Each constraint must be <=, have a non-negative RHS, and include " +
                        "one coefficient per decision variable.";
                    return false;
                }
            }
            return true;
        }

        private static bool TryFindBasis(double[,] tableau, int columnCount, int rows,
            out int[] basicColumns, out string error)
        {
            basicColumns = new int[rows];
            error = null;
            for (int basisRow = 0; basisRow < rows; basisRow++)
            {
                List<int> candidates = Enumerable.Range(0, columnCount)
                    .Where(column => IsUnitColumn(tableau, column, basisRow, rows))
                    .ToList();
                if (candidates.Count != 1)
                {
                    error = "Basis row " + (basisRow + 1) + " has " + candidates.Count +
                        " basic-column candidates, so the ordered basis is ambiguous.";
                    return false;
                }
                basicColumns[basisRow] = candidates[0];
            }
            if (basicColumns.Distinct().Count() != rows)
            {
                error = "The final tableau does not contain a distinct basic column for each row.";
                return false;
            }
            return true;
        }

        private static bool IsUnitColumn(double[,] tableau, int column, int basisRow, int rows)
        {
            if (Math.Abs(tableau[0, column]) > Tolerance) return false;
            for (int row = 0; row < rows; row++)
            {
                double expected = row == basisRow ? 1 : 0;
                if (Math.Abs(tableau[row + 1, column] - expected) > Tolerance)
                    return false;
            }
            return true;
        }

        private static double[,] CanonicalMatrix(LinearProgram program)
        {
            int m = program.Constraints.Count;
            int n = program.Variables.Count;
            var result = new double[m, n + m];
            for (int row = 0; row < m; row++)
            {
                for (int column = 0; column < n; column++)
                    result[row, column] = program.Constraints[row].Coefficients[column];
                result[row, n + row] = 1;
            }
            return result;
        }

        private static double[] CanonicalObjective(LinearProgram program)
        {
            var result = new double[program.Variables.Count + program.Constraints.Count];
            for (int column = 0; column < program.Variables.Count; column++)
                result[column] = program.Variables[column].Coefficient;
            return result;
        }

        private static List<string> ColumnNames(LinearProgram program)
        {
            var names = program.Variables.Select(variable => "x" + variable.Index).ToList();
            names.AddRange(Enumerable.Range(1, program.Constraints.Count)
                .Select(index => "s" + index));
            return names;
        }

        private static double[,] Basis(double[,] canonical, int[] basicColumns)
        {
            var result = new double[canonical.GetLength(0), basicColumns.Length];
            for (int row = 0; row < result.GetLength(0); row++)
                for (int column = 0; column < result.GetLength(1); column++)
                    result[row, column] = canonical[row, basicColumns[column]];
            return result;
        }

        private static double[] ReducedCosts(double[,] canonical, double[] objective,
            double[] shadowPrices)
        {
            var result = new double[objective.Length];
            for (int column = 0; column < result.Length; column++)
                result[column] = Dot(shadowPrices, Column(canonical, column)) - objective[column];
            return result;
        }

        private static bool Matches(double[,] tableau, double[,] inverse, double[,] canonical,
            double[] basicValues, double[] reducedCosts)
        {
            int rhsColumn = tableau.GetLength(1) - 1;
            for (int row = 0; row < basicValues.Length; row++)
                if (Math.Abs(tableau[row + 1, rhsColumn] - basicValues[row]) > Tolerance)
                    return false;
            for (int column = 0; column < reducedCosts.Length; column++)
            {
                if (Math.Abs(tableau[0, column] - reducedCosts[column]) > Tolerance)
                    return false;
                double[] transformed = Multiply(inverse, Column(canonical, column));
                for (int row = 0; row < transformed.Length; row++)
                    if (Math.Abs(tableau[row + 1, column] - transformed[row]) > Tolerance)
                        return false;
            }
            return true;
        }

        private static bool TryInvert(double[,] matrix, out double[,] inverse)
        {
            int size = matrix.GetLength(0);
            inverse = new double[size, size];
            var work = (double[,])matrix.Clone();
            for (int index = 0; index < size; index++) inverse[index, index] = 1;

            for (int pivot = 0; pivot < size; pivot++)
            {
                int selected = pivot;
                for (int row = pivot + 1; row < size; row++)
                    if (Math.Abs(work[row, pivot]) > Math.Abs(work[selected, pivot]))
                        selected = row;
                if (Math.Abs(work[selected, pivot]) <= Tolerance) return false;
                Swap(work, selected, pivot);
                Swap(inverse, selected, pivot);

                double divisor = work[pivot, pivot];
                for (int column = 0; column < size; column++)
                {
                    work[pivot, column] /= divisor;
                    inverse[pivot, column] /= divisor;
                }
                for (int row = 0; row < size; row++)
                {
                    if (row == pivot) continue;
                    double factor = work[row, pivot];
                    for (int column = 0; column < size; column++)
                    {
                        work[row, column] -= factor * work[pivot, column];
                        inverse[row, column] -= factor * inverse[pivot, column];
                    }
                }
            }
            return true;
        }

        private static void Swap(double[,] matrix, int first, int second)
        {
            if (first == second) return;
            for (int column = 0; column < matrix.GetLength(1); column++)
            {
                double value = matrix[first, column];
                matrix[first, column] = matrix[second, column];
                matrix[second, column] = value;
            }
        }

        private static bool IsIdentity(double[,] matrix)
        {
            for (int row = 0; row < matrix.GetLength(0); row++)
                for (int column = 0; column < matrix.GetLength(1); column++)
                    if (Math.Abs(matrix[row, column] - (row == column ? 1 : 0)) > Tolerance)
                        return false;
            return true;
        }

        private static ColumnSensitivityResult EvaluateColumn(BasisSnapshot snapshot,
            string name, IEnumerable<double> columnValues, double objectiveCoefficient)
        {
            if (columnValues == null) throw new ArgumentNullException(nameof(columnValues));
            double[] column = columnValues.ToArray();
            if (column.Length != snapshot.OriginalRhs.Length)
                throw new ArgumentException("The column must contain one value per constraint.",
                    nameof(columnValues));

            double reducedCost = Dot(snapshot.ShadowPrices, column) - objectiveCoefficient;
            return new ColumnSensitivityResult
            {
                VariableName = name,
                TransformedColumn = Multiply(snapshot.BasisInverse, column),
                ReducedCost = reducedCost,
                CurrentBasisRemainsOptimal = reducedCost >= -Tolerance
            };
        }

        private static SensitivityRange BasicDeltaRange(BasisSnapshot snapshot, int basisRow)
        {
            double lower = double.NegativeInfinity;
            double upper = double.PositiveInfinity;
            bool[] isBasic = IsBasic(snapshot);
            double[] inverseRow = Row(snapshot.BasisInverse, basisRow);
            for (int column = 0; column < snapshot.ColumnNames.Count; column++)
            {
                if (isBasic[column]) continue;
                double value = snapshot.ReducedCosts[column];
                double slope = Dot(inverseRow, Column(snapshot.CanonicalMatrix, column));
                if (slope > Tolerance) lower = Math.Max(lower, -value / slope);
                else if (slope < -Tolerance) upper = Math.Min(upper, -value / slope);
                else if (value < -Tolerance) return SensitivityRange.Empty;
            }
            return new SensitivityRange(lower, upper);
        }

        private static SensitivityRange RhsDeltaRange(BasisSnapshot snapshot, int rhsColumn)
        {
            double lower = double.NegativeInfinity;
            double upper = double.PositiveInfinity;
            for (int row = 0; row < snapshot.BasicValues.Length; row++)
            {
                double slope = snapshot.BasisInverse[row, rhsColumn];
                if (slope > Tolerance) lower = Math.Max(lower, -snapshot.BasicValues[row] / slope);
                else if (slope < -Tolerance) upper = Math.Min(upper, -snapshot.BasicValues[row] / slope);
            }
            return new SensitivityRange(lower, upper);
        }

        private static string FormatNonBasicRanges(BasisSnapshot snapshot)
        {
            var builder = new StringBuilder();
            foreach (int column in NonBasic(snapshot))
            {
                double qA = snapshot.ReducedCosts[column] +
                    snapshot.ObjectiveCoefficients[column];
                var coefficient = new SensitivityRange(double.NegativeInfinity, qA);
                builder.AppendLine(snapshot.ColumnNames[column] + ": c_j* = " +
                    NumberFormatter.Format(Clean(snapshot.ReducedCosts[column])) +
                    ", allowable Delta = " + FormatRange(coefficient.Translate(
                        -snapshot.ObjectiveCoefficients[column])) +
                    ", coefficient range = " + FormatRange(coefficient));
            }
            return builder.ToString().TrimEnd();
        }

        private static string FormatBasicRanges(BasisSnapshot snapshot)
        {
            var builder = new StringBuilder();
            for (int row = 0; row < snapshot.BasicColumnIndices.Count; row++)
            {
                int column = snapshot.BasicColumnIndices[row];
                SensitivityRange delta = BasicDeltaRange(snapshot, row);
                builder.AppendLine(snapshot.ColumnNames[column] + ": allowable Delta = " +
                    FormatRange(delta) + ", coefficient range = " +
                    FormatRange(delta.Translate(snapshot.ObjectiveCoefficients[column])));
            }
            return builder.ToString().TrimEnd();
        }

        private static string FormatRhsRanges(BasisSnapshot snapshot)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < snapshot.OriginalRhs.Length; index++)
            {
                SensitivityRange delta = RhsDeltaRange(snapshot, index);
                builder.AppendLine("Constraint " + (index + 1) + ": RHS range = " +
                    FormatRange(delta.Translate(snapshot.OriginalRhs[index])) +
                    ", allowable Delta = " + FormatRange(delta) + ", q_" + (index + 1) +
                    " = " + NumberFormatter.Format(Clean(snapshot.ShadowPrices[index])) +
                    ", z'(Delta) = " + NumberFormatter.Format(snapshot.ObjectiveValue) +
                    " + (" + NumberFormatter.Format(snapshot.ShadowPrices[index]) + ")Delta");
            }
            return builder.ToString().TrimEnd();
        }

        private static string FormatBasis(BasisSnapshot snapshot)
        {
            var builder = new StringBuilder();
            for (int row = 0; row < snapshot.BasicColumnIndices.Count; row++)
                builder.AppendLine("Row " + (row + 1) + ": " +
                    snapshot.ColumnNames[snapshot.BasicColumnIndices[row]] + " = " +
                    NumberFormatter.Format(Clean(snapshot.BasicValues[row])));
            return builder.ToString().TrimEnd();
        }

        private static string FormatColumns(BasisSnapshot snapshot, List<int> columns,
            double[] values, bool valuesFollowBasisRows)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < columns.Count; index++)
            {
                int column = columns[index];
                double value = valuesFollowBasisRows ? values[index] : values[column];
                builder.AppendLine(snapshot.ColumnNames[column] + " = " +
                    NumberFormatter.Format(Clean(value)));
            }
            return builder.ToString().TrimEnd();
        }

        private static string FormatPrices(double[] prices)
        {
            return string.Join(Environment.NewLine, prices.Select((value, index) => "q_" +
                (index + 1) + " = " + NumberFormatter.Format(Clean(value))));
        }

        private static string FormatDictionary(Dictionary<string, double> values)
        {
            return string.Join(Environment.NewLine, values.OrderBy(pair => pair.Key).Select(pair =>
                pair.Key + " = " + NumberFormatter.Format(pair.Value)));
        }

        private static string FormatMatrix(double[,] matrix)
        {
            var builder = new StringBuilder();
            for (int row = 0; row < matrix.GetLength(0); row++)
            {
                builder.Append("[");
                for (int column = 0; column < matrix.GetLength(1); column++)
                {
                    if (column > 0) builder.Append("  ");
                    builder.Append(NumberFormatter.Format(Clean(matrix[row, column])));
                }
                builder.AppendLine("]");
            }
            return builder.ToString().TrimEnd();
        }

        private static string FormatAsParseableInput(LinearProgram program)
        {
            var lines = new List<string>();

            var objective = new StringBuilder(program.IsMaximization ? "max" : "min");
            foreach (var variable in program.Variables)
            {
                objective.Append(' ').Append(variable.Coefficient >= 0 ? "+" : "-")
                    .Append(' ').Append(NumberFormatter.Format(Math.Abs(variable.Coefficient)));
            }
            lines.Add(objective.ToString());

            foreach (var constraint in program.Constraints)
            {
                var line = new StringBuilder();
                for (int i = 0; i < constraint.Coefficients.Count; i++)
                {
                    double coeff = constraint.Coefficients[i];
                    line.Append(coeff >= 0 ? "+" : "-").Append(' ')
                        .Append(NumberFormatter.Format(Math.Abs(coeff))).Append(' ');
                }
                string relation = constraint.Relation == LinearProgram.Relation.LessThanOrEqual ? "<="
                    : constraint.Relation == LinearProgram.Relation.GreaterThanOrEqual ? ">=" : "=";
                line.Append(relation).Append(' ').Append(NumberFormatter.Format(constraint.Rhs));
                lines.Add(line.ToString());
            }

            var signs = new StringBuilder();
            for (int i = 0; i < program.Variables.Count; i++)
            {
                if (i > 0) signs.Append(' ');
                signs.Append(SignRestrictionToken(program.Variables[i].Type));
            }
            lines.Add(signs.ToString());

            return string.Join(Environment.NewLine, lines);
        }

        private static string SignRestrictionToken(LinearProgram.VariableType type)
        {
            switch (type)
            {
                case LinearProgram.VariableType.NonNegative: return "+";
                case LinearProgram.VariableType.NonPositive: return "-";
                case LinearProgram.VariableType.Unrestricted: return "urs";
                case LinearProgram.VariableType.Integer: return "int";
                case LinearProgram.VariableType.Binary: return "bin";
                default: return "+";
            }
        }


        private static string FormatAddedRow(LinearProgram program,
            LinearProgram.Constraint constraint, BasisSnapshot snapshot)
        {
            double sign = constraint.Relation == LinearProgram.Relation.GreaterThanOrEqual ? -1 : 1;
            var row = new double[snapshot.ColumnNames.Count];
            for (int column = 0; column < program.Variables.Count; column++)
                row[column] = sign * constraint.Coefficients[column];
            double rhs = sign * constraint.Rhs;

            for (int basisRow = 0; basisRow < snapshot.BasicColumnIndices.Count; basisRow++)
            {
                double factor = row[snapshot.BasicColumnIndices[basisRow]];
                if (Math.Abs(factor) <= Tolerance) continue;
                for (int column = 0; column < row.Length; column++)
                    row[column] -= factor * snapshot.FinalTableau[basisRow + 1, column];
                rhs -= factor * snapshot.FinalTableau[basisRow + 1,
                    snapshot.FinalTableau.GetLength(1) - 1];
            }

            string auxiliary = constraint.Relation == LinearProgram.Relation.LessThanOrEqual
                ? "s" + (program.Constraints.Count + 1)
                : constraint.Relation == LinearProgram.Relation.GreaterThanOrEqual ? "e1" : "a1";
            return LinearExpression(row, snapshot.ColumnNames) + " + " + auxiliary + " = " +
                NumberFormatter.Format(Clean(rhs)) + Environment.NewLine +
                (rhs < -Tolerance
                    ? "Negative RHS: Dual Simplex is required."
                    : "Non-negative RHS: the current basis remains feasible.");
        }

        private static string LinearExpression(double[] coefficients, List<string> names)
        {
            var terms = new List<string>();
            for (int column = 0; column < coefficients.Length; column++)
                if (Math.Abs(coefficients[column]) > Tolerance)
                    terms.Add(NumberFormatter.Format(Clean(coefficients[column])) + names[column]);
            return terms.Count == 0 ? "0" : string.Join(" + ", terms);
        }

        private static bool Satisfies(LinearProgram program, LinearProgram.Constraint constraint,
            BasisSnapshot snapshot)
        {
            Dictionary<string, double> values = DecisionValues(snapshot);
            double lhs = 0;
            for (int column = 0; column < program.Variables.Count; column++)
                lhs += constraint.Coefficients[column] * values["x" + program.Variables[column].Index];
            if (constraint.Relation == LinearProgram.Relation.LessThanOrEqual)
                return lhs <= constraint.Rhs + Tolerance;
            if (constraint.Relation == LinearProgram.Relation.GreaterThanOrEqual)
                return lhs >= constraint.Rhs - Tolerance;
            return Math.Abs(lhs - constraint.Rhs) <= Tolerance;
        }

        private static Dictionary<string, double> DecisionValues(BasisSnapshot snapshot)
        {
            var values = new Dictionary<string, double>();
            for (int column = 0; column < snapshot.ColumnNames.Count; column++)
            {
                if (!snapshot.ColumnNames[column].StartsWith("x")) continue;
                int row = snapshot.BasicColumnIndices.IndexOf(column);
                values[snapshot.ColumnNames[column]] = row < 0 ? 0 :
                    Clean(snapshot.BasicValues[row]);
            }
            return values;
        }

        private static List<int> NonBasic(BasisSnapshot snapshot)
        {
            bool[] isBasic = IsBasic(snapshot);
            return Enumerable.Range(0, snapshot.ColumnNames.Count)
                .Where(column => !isBasic[column]).ToList();
        }

        private static bool[] IsBasic(BasisSnapshot snapshot)
        {
            var result = new bool[snapshot.ColumnNames.Count];
            foreach (int column in snapshot.BasicColumnIndices) result[column] = true;
            return result;
        }

        private static void CopyOutput(Solution target, Solution source)
        {
            target.Steps.AddRange(source.Steps);
            target.Messages.AddRange(source.Messages);
            target.OutputBlocks.AddRange(source.OutputBlocks);
            target.IterationTableaux.AddRange(source.IterationTableaux);
            target.IterationMessages.AddRange(source.IterationMessages);
            target.IterationPivotRows.AddRange(source.IterationPivotRows);
            target.IterationPivotCols.AddRange(source.IterationPivotCols);
            target.IterationColumnHeaders.AddRange(source.IterationColumnHeaders);
        }

        private static string FormatRange(SensitivityRange range)
        {
            if (range.IsEmpty) return "empty";
            return "[" + Bound(range.Lower) + ", " + Bound(range.Upper) + "]";
        }

        private static string Bound(double value)
        {
            if (double.IsNegativeInfinity(value)) return "-infinity";
            if (double.IsPositiveInfinity(value)) return "+infinity";
            return NumberFormatter.Format(Clean(value));
        }

        private static double[] Multiply(double[,] matrix, double[] vector)
        {
            var result = new double[matrix.GetLength(0)];
            for (int row = 0; row < matrix.GetLength(0); row++)
                for (int column = 0; column < matrix.GetLength(1); column++)
                    result[row] += matrix[row, column] * vector[column];
            return result;
        }

        private static double[,] Multiply(double[,] left, double[,] right)
        {
            var result = new double[left.GetLength(0), right.GetLength(1)];
            for (int row = 0; row < left.GetLength(0); row++)
                for (int column = 0; column < right.GetLength(1); column++)
                    for (int index = 0; index < left.GetLength(1); index++)
                        result[row, column] += left[row, index] * right[index, column];
            return result;
        }

        private static double[] MultiplyRow(double[] vector, double[,] matrix)
        {
            var result = new double[matrix.GetLength(1)];
            for (int column = 0; column < matrix.GetLength(1); column++)
                for (int row = 0; row < matrix.GetLength(0); row++)
                    result[column] += vector[row] * matrix[row, column];
            return result;
        }

        private static double[] Column(double[,] matrix, int column)
        {
            var result = new double[matrix.GetLength(0)];
            for (int row = 0; row < result.Length; row++) result[row] = matrix[row, column];
            return result;
        }

        private static double[] Row(double[,] matrix, int row)
        {
            var result = new double[matrix.GetLength(1)];
            for (int column = 0; column < result.Length; column++) result[column] = matrix[row, column];
            return result;
        }

        private static double Dot(double[] left, double[] right)
        {
            double result = 0;
            for (int index = 0; index < left.Length; index++) result += left[index] * right[index];
            return result;
        }

        private static double Clean(double value)
        {
            return Math.Abs(value) <= Tolerance ? 0 : value;
        }
    }

    public sealed class BasisSnapshot
    {
        public List<string> ColumnNames { get; internal set; }
        public List<int> BasicColumnIndices { get; internal set; }
        public double[,] CanonicalMatrix { get; internal set; }
        public double[] ObjectiveCoefficients { get; internal set; }
        public double[] OriginalRhs { get; internal set; }
        public double[,] BasisMatrix { get; internal set; }
        public double[,] BasisInverse { get; internal set; }
        public double[] BasicCosts { get; internal set; }
        public double[] BasicValues { get; internal set; }
        public double[] ShadowPrices { get; internal set; }
        public double[] ReducedCosts { get; internal set; }
        public double ObjectiveValue { get; internal set; }
        internal double[,] FinalTableau { get; set; }
    }

    public sealed class ColumnSensitivityResult
    {
        public string VariableName { get; internal set; }
        public double[] TransformedColumn { get; internal set; }
        public double ReducedCost { get; internal set; }
        public bool CurrentBasisRemainsOptimal { get; internal set; }
    }

    internal sealed class SensitivityRange
    {
        public static readonly SensitivityRange Empty = new SensitivityRange(1, 0);

        public SensitivityRange(double lower, double upper)
        {
            Lower = lower;
            Upper = upper;
        }

        public double Lower { get; private set; }
        public double Upper { get; private set; }
        public bool IsEmpty { get { return Lower > Upper + 1e-9; } }

        public SensitivityRange Translate(double amount)
        {
            return new SensitivityRange(
                double.IsNegativeInfinity(Lower) ? Lower : Lower + amount,
                double.IsPositiveInfinity(Upper) ? Upper : Upper + amount);
        }
    }
}
