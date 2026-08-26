using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Group_V_26_LPR381_Project.Models.LinearProgram;

namespace Group_V_26_LPR381_Project.Algorithms
{
    public class DualSimplex : ISolver
    {
        private const double TOL = 1e-6;
        private const double BIG_M = 1_000_000; // Big-M penalty for artificial variables

        private LinearProgram _program;
        private double[,] _matrix;
        private int _rows;
        private int _cols;
        private int _slackCount;
        private int _excessCount;
        private int _artificialCount;
        private List<string> _auxiliaryVariableNames;
        private List<int> _basisColumnIndices;
        private ConstraintHandler _constraintHandler;
        public int IterationCount { get; private set; }

        public DualSimplex()
        {
            _constraintHandler = new ConstraintHandler();
        }

        /// <summary>
        /// Seeds constraint addition with a known optimal tableau. This keeps the lecturer's
        /// add-constraint flow on the current basis instead of resolving the original LP.
        /// </summary>
        public void LoadOptimalTableau(LinearProgram program, double[,] tableau, int m,
            int slackCount, int excessCount, int artificialCount,
            List<string> auxiliaryVariableNames, List<int> basicColumnIndices)
        {
            if (program == null || tableau == null || auxiliaryVariableNames == null ||
                basicColumnIndices == null)
            {
                throw new ArgumentNullException("A program, tableau, auxiliary names, and basis are required.");
            }
            if (m != program.Constraints.Count || basicColumnIndices.Count != m ||
                tableau.GetLength(0) != m + 1 ||
                tableau.GetLength(1) != program.Variables.Count + auxiliaryVariableNames.Count + 1)
            {
                throw new ArgumentException("The supplied optimal tableau does not match the program dimensions.");
            }

            for (int row = 0; row < m; row++)
            {
                int column = basicColumnIndices[row];
                if (column < 0 || column >= tableau.GetLength(1) - 1 ||
                    !IsUnitBasicColumn(tableau, column, row, m))
                {
                    throw new ArgumentException("The supplied basis is not canonical in tableau-row order.");
                }
            }

            _program = program;
            _matrix = (double[,])tableau.Clone();
            _rows = _matrix.GetLength(0);
            _cols = _matrix.GetLength(1);
            _slackCount = slackCount;
            _excessCount = excessCount;
            _artificialCount = artificialCount;
            _auxiliaryVariableNames = new List<string>(auxiliaryVariableNames);
            _basisColumnIndices = new List<int>(basicColumnIndices);
            IterationCount = 0;
        }

        public Solution Solve(LinearProgram program)
        {
            _program = program ?? throw new ArgumentNullException(nameof(program));
            BuildTableau();

            var solution = new Solution
            {
                VariableCount = _program.Variables.Count,
                SlackCount = _slackCount,
                ExcessCount = _excessCount,
                ArtificialCount = _artificialCount
            };

            if (!_program.IsMaximization)
                solution.AddMessage("Converted minimization problem to maximization by negating objective");

            solution.AddStep("Initial Tableau:", ToString());
            solution.AddIteration((double[,])_matrix.Clone(), "Initial Tableau", -1, -1, GetColumnHeaders());

            // Phase 1: Dual Simplex - Fix negative RHS values
            while (HasNegativeRHS())
            {
                int pivotRow = FindDualPivotRow();
                if (pivotRow == -1)
                {
                    solution.AddMessage("Problem is infeasible - no negative RHS found.");
                    return solution;
                }

                int pivotCol = FindDualPivotColumn(pivotRow);
                if (pivotCol == -1)
                {
                    solution.AddMessage("Problem is infeasible - no valid pivot column found in dual simplex.");
                    return solution;
                }

                solution.AddMessage($"Dual Phase: Pivoting on row {pivotRow + 1}, column {pivotCol + 1} (RHS = {NumberFormatter.Format(_matrix[pivotRow, _cols - 1])})");
                Pivot(pivotRow, pivotCol);
                solution.AddStep($"Dual Iteration {IterationCount}: Pivot on row {pivotRow + 1}, column {pivotCol + 1}", ToString());
                solution.AddIteration((double[,])_matrix.Clone(), $"After Dual Iteration {IterationCount}", pivotRow, pivotCol, GetColumnHeaders());
            }

            solution.AddMessage("Dual phase complete - all RHS values are non-negative. Starting primal phase for optimality.");

            // Phase 2: Primal Simplex - Optimize the objective function
            while (!IsOptimal())
            {
                int pivotCol = FindPrimalPivotColumn();
                if (pivotCol == -1)
                {
                    solution.AddMessage("Optimal solution reached - no negative coefficients in objective row.");
                    break;
                }

                int pivotRow = FindPrimalPivotRow(pivotCol);
                if (pivotRow == -1)
                {
                    solution.AddMessage("Problem is unbounded - no valid pivot row found.");
                    return solution;
                }

                solution.AddMessage($"Primal Phase: Pivoting on row {pivotRow + 1}, column {pivotCol + 1}");
                Pivot(pivotRow, pivotCol);
                solution.AddStep($"Primal Iteration {IterationCount}: Pivot on row {pivotRow + 1}, column {pivotCol + 1}", ToString());
                solution.AddIteration((double[,])_matrix.Clone(), $"After Primal Iteration {IterationCount}", pivotRow, pivotCol, GetColumnHeaders());
            }

            if (HasArtificialInBasisWithPositiveValue())
                solution.AddMessage("Warning: an artificial variable remains basic at a positive value - the original problem is infeasible.");

            solution.OptimalValue = GetObjectiveValue();
            solution.VariableValues = GetSolution();
            solution.AddStep("Final Tableau:", ToString());

            solution.FinalTableau = new double[_rows, _cols];
            for (int i = 0; i < _rows; i++)
                for (int j = 0; j < _cols; j++)
                    solution.FinalTableau[i, j] = _matrix[i, j];

            return solution;
        }

        public Solution AddConstraintAndResolve(LinearProgram.Constraint constraint)
        {
            if (_matrix == null)
                throw new InvalidOperationException("Must solve the initial problem before adding constraints");

            var solution = new Solution
            {
                VariableCount = _program.Variables.Count
            };

            solution.AddMessage($"Adding new constraint: {ConstraintToString(constraint)}");
            solution.AddStep("Current Optimal Tableau:", ToString());

            var result = _constraintHandler.AddConstraint(
                _matrix,
                constraint,
                _auxiliaryVariableNames,
                _program.Variables.Count,
                _slackCount,
                _excessCount,
                _artificialCount,
                _basisColumnIndices);
            _basisColumnIndices = null;

            foreach (var message in result.Messages)
                solution.AddMessage(message);

            _matrix = result.NewTableau;
            _rows = _matrix.GetLength(0);
            _cols = _matrix.GetLength(1);
            _slackCount = result.NewSlackCount;
            _excessCount = result.NewExcessCount;
            _artificialCount = result.NewArtificialCount;
            _auxiliaryVariableNames = result.NewAuxiliaryVariableNames;

            solution.SlackCount = _slackCount;
            solution.ExcessCount = _excessCount;
            solution.ArtificialCount = _artificialCount;
            solution.AddIteration(_matrix, "After adding and fixing constraint", -1, -1, GetColumnHeaders());

            if (result.RequiresDualSimplex)
            {
                solution.AddMessage("Negative RHS detected. Performing dual simplex.");

                while (HasNegativeRHS())
                {
                    int pivotRow = FindDualPivotRow();
                    if (pivotRow == -1)
                    {
                        solution.AddMessage("Problem is infeasible - no negative RHS found.");
                        return solution;
                    }

                    int pivotCol = FindDualPivotColumn(pivotRow);
                    if (pivotCol == -1)
                    {
                        solution.AddMessage("Problem is infeasible - no valid pivot column found in dual simplex.");
                        return solution;
                    }

                    solution.AddMessage($"Dual Phase: Pivoting on row {pivotRow + 1}, column {pivotCol + 1} (RHS = {NumberFormatter.Format(_matrix[pivotRow, _cols - 1])})");
                    Pivot(pivotRow, pivotCol);
                    solution.AddStep($"Dual Iteration {IterationCount}: Pivot on row {pivotRow + 1}, column {pivotCol + 1}", ToString());
                    solution.AddIteration((double[,])_matrix.Clone(), $"After Dual Iteration {IterationCount}", pivotRow, pivotCol, GetColumnHeaders());
                }

                solution.AddMessage("Dual phase complete - all RHS values are non-negative.");
            }

            while (!IsOptimal())
            {
                int pivotCol = FindPrimalPivotColumn();
                if (pivotCol == -1)
                {
                    solution.AddMessage("Optimal solution reached - no negative coefficients in objective row.");
                    break;
                }

                int pivotRow = FindPrimalPivotRow(pivotCol);
                if (pivotRow == -1)
                {
                    solution.AddMessage("Problem is unbounded - no valid pivot row found.");
                    return solution;
                }

                solution.AddMessage($"Primal Phase: Pivoting on row {pivotRow + 1}, column {pivotCol + 1}");
                Pivot(pivotRow, pivotCol);
                solution.AddStep($"Primal Iteration {IterationCount}: Pivot on row {pivotRow + 1}, column {pivotCol + 1}", ToString());
                solution.AddIteration((double[,])_matrix.Clone(), $"After Primal Iteration {IterationCount}", pivotRow, pivotCol, GetColumnHeaders());
            }

            // A positive artificial variable means that the added equality
            // cannot be satisfied. Do not present its Big-M penalty as an
            // objective value for a feasible LP.
            if (HasArtificialInBasisWithPositiveValue())
            {
                solution.AddMessage("Problem is infeasible: an artificial variable remains basic at a positive value.");
                return solution;
            }

            solution.OptimalValue = GetObjectiveValue();
            solution.VariableValues = GetSolution();
            solution.AddStep("Final Tableau:", ToString());
            solution.FinalTableau = new double[_rows, _cols];
            for (int i = 0; i < _rows; i++)
                for (int j = 0; j < _cols; j++)
                    solution.FinalTableau[i, j] = _matrix[i, j];

            return solution;
        }

        /// <summary>
        /// Appends a Gomory/GMI cut to the current optimal tableau without rebuilding the
        /// original LP.  The caller supplies the cut in tableau coordinates (one entry for
        /// every current non-RHS column), already arranged as
        ///     cutCoefficients * columns + cutSlack = cutRhs.
        /// The new cut-slack column is deliberately an auxiliary tableau column, never a
        /// decision variable in <see cref="LinearProgram.Variables"/>.
        /// </summary>
        public Solution AppendGomoryCutAndResolve(
            double[] cutCoefficients,
            double cutRhs,
            string cutSlackName)
        {
            if (_matrix == null)
                throw new InvalidOperationException("Must solve the LP relaxation before adding a Gomory cut.");
            if (cutCoefficients == null || cutCoefficients.Length != _cols - 1)
                throw new ArgumentException("A Gomory cut must provide one coefficient for every current tableau column.");
            if (string.IsNullOrWhiteSpace(cutSlackName))
                throw new ArgumentException("A name is required for the Gomory cut slack variable.");
            if (_auxiliaryVariableNames.Contains(cutSlackName))
                throw new ArgumentException("The Gomory cut slack variable name is already in use.");

            var solution = new Solution
            {
                VariableCount = _program.Variables.Count,
                SlackCount = _slackCount,
                ExcessCount = _excessCount,
                ArtificialCount = _artificialCount
            };

            solution.AddStep("Current Optimal Tableau:", ToString());
            solution.AddMessage("Appending Gomory cut with continuous auxiliary " + cutSlackName + ".");

            int oldRows = _rows;
            int oldCols = _cols;
            var expanded = new double[oldRows + 1, oldCols + 1];

            // Keep every existing tableau coefficient and the objective row exactly as it
            // is.  Only insert the new auxiliary column immediately before RHS.
            for (int row = 0; row < oldRows; row++)
            {
                for (int column = 0; column < oldCols - 1; column++)
                    expanded[row, column] = _matrix[row, column];

                expanded[row, oldCols] = _matrix[row, oldCols - 1];
            }

            int cutRow = oldRows;
            for (int column = 0; column < oldCols - 1; column++)
                expanded[cutRow, column] = cutCoefficients[column];

            expanded[cutRow, oldCols - 1] = 1.0;
            expanded[cutRow, oldCols] = cutRhs;

            _matrix = expanded;
            _rows = expanded.GetLength(0);
            _cols = expanded.GetLength(1);
            _auxiliaryVariableNames.Add(cutSlackName);
            _basisColumnIndices = null;

            solution.AddIteration((double[,])_matrix.Clone(),
                "After appending Gomory cut " + cutSlackName, -1, -1, GetColumnHeaders());

            // A correctly formed cut leaves the objective row dual-feasible and gives the
            // new cut-slack a negative RHS.  Restore primal feasibility from this tableau;
            // do not solve a newly reconstructed model.
            while (HasNegativeRHS())
            {
                int pivotRow = FindDualPivotRow();
                if (pivotRow == -1)
                {
                    solution.AddMessage("Problem is integer infeasible - no negative RHS row was found after the Gomory cut.");
                    return solution;
                }

                int pivotCol = FindDualPivotColumn(pivotRow);
                if (pivotCol == -1)
                {
                    solution.AddMessage("Problem is integer infeasible - no valid dual-simplex pivot exists after the Gomory cut.");
                    return solution;
                }

                solution.AddMessage($"Dual Phase: Pivoting on row {pivotRow + 1}, column {pivotCol + 1} (RHS = {NumberFormatter.Format(_matrix[pivotRow, _cols - 1])})");
                Pivot(pivotRow, pivotCol);
                solution.AddStep($"Dual Iteration {IterationCount}: Pivot on row {pivotRow + 1}, column {pivotCol + 1}", ToString());
                solution.AddIteration((double[,])_matrix.Clone(),
                    $"After Dual Iteration {IterationCount}", pivotRow, pivotCol, GetColumnHeaders());
            }

            solution.AddMessage("Dual phase complete - all RHS values are non-negative.");

            // The cut keeps dual feasibility, so this phase is normally a no-op.  Retaining
            // it guards against numerical drift without changing the existing solver rules.
            while (!IsOptimal())
            {
                int pivotCol = FindPrimalPivotColumn();
                if (pivotCol == -1)
                    break;

                int pivotRow = FindPrimalPivotRow(pivotCol);
                if (pivotRow == -1)
                {
                    solution.AddMessage("Problem is unbounded - no valid primal pivot exists after the Gomory cut.");
                    return solution;
                }

                solution.AddMessage($"Primal Phase: Pivoting on row {pivotRow + 1}, column {pivotCol + 1}");
                Pivot(pivotRow, pivotCol);
                solution.AddStep($"Primal Iteration {IterationCount}: Pivot on row {pivotRow + 1}, column {pivotCol + 1}", ToString());
                solution.AddIteration((double[,])_matrix.Clone(),
                    $"After Primal Iteration {IterationCount}", pivotRow, pivotCol, GetColumnHeaders());
            }

            if (HasArtificialInBasisWithPositiveValue())
            {
                solution.AddMessage("Problem is integer infeasible: an artificial variable remains basic at a positive value.");
                return solution;
            }

            solution.OptimalValue = GetObjectiveValue();
            solution.VariableValues = GetSolution();
            solution.AddStep("Final Tableau:", ToString());
            solution.FinalTableau = (double[,])_matrix.Clone();
            return solution;
        }

        /// <summary>
        /// Exposes the ordered headers of the live tableau so Cutting Plane can create a cut
        /// in the same coordinates.  A copy prevents callers from mutating solver state.
        /// </summary>
        public List<string> GetCurrentColumnHeaders()
        {
            if (_matrix == null)
                throw new InvalidOperationException("No tableau is available.");

            return new List<string>(GetColumnHeaders());
        }

        private void BuildTableau()
        {
            _rows = _program.Constraints.Count + 1;
            _cols = _program.Variables.Count + _program.Constraints.Count + 1;
            _matrix = new double[_rows, _cols];
            _auxiliaryVariableNames = new List<string>();
            _basisColumnIndices = null;
            _slackCount = 0;
            _excessCount = 0;
            _artificialCount = 0;

            // Objective row: stored as -coefficient (maximization convention).
            // For minimization problems the caller's coefficients are expected to already
            // represent the negated/maximization-equivalent objective.
            for (int j = 0; j < _program.Variables.Count; j++)
            {
                _matrix[0, j] = _program.IsMaximization ? -_program.Variables[j].Coefficient : _program.Variables[j].Coefficient;
            }
            _matrix[0, _cols - 1] = 0;

            // Track which row each artificial variable's basic column sits in, so we can
            // eliminate it from the objective row after the whole tableau is built.
            var artificialColumns = new List<int>();

            int auxIndex = _program.Variables.Count;
            for (int i = 0; i < _program.Constraints.Count; i++)
            {
                var constraint = _program.Constraints[i];
                for (int j = 0; j < _program.Variables.Count; j++)
                {
                    _matrix[i + 1, j] = constraint.Coefficients[j];
                }
                _matrix[i + 1, _cols - 1] = constraint.Rhs;

                string auxName;
                switch (constraint.Relation)
                {
                    case Relation.LessThanOrEqual:
                        _slackCount++;
                        auxName = $"s{_slackCount}";
                        _matrix[i + 1, auxIndex] = 1;
                        break;

                    case Relation.GreaterThanOrEqual:
                        _excessCount++;
                        auxName = $"e{_excessCount}";
                        // Multiply the row by -1 so the excess variable's coefficient is +1,
                        // which pushes the RHS negative and lets Phase 1 dual simplex handle it.
                        for (int j = 0; j < _cols; j++)
                        {
                            _matrix[i + 1, j] *= -1;
                        }
                        _matrix[i + 1, auxIndex] = 1;
                        break;

                    case Relation.Equal:
                        _artificialCount++;
                        auxName = $"a{_artificialCount}";
                        _matrix[i + 1, auxIndex] = 1;
                        // Raw Big-M penalty placed in the objective row for this column.
                        // This is NOT yet in canonical form - it must be eliminated below
                        // so the artificial variable's column reads 0 in row 0 (since it's basic).
                        _matrix[0, auxIndex] = BIG_M;
                        artificialColumns.Add(auxIndex);
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported relation");
                }
                _auxiliaryVariableNames.Add(auxName);
                auxIndex++;
            }

            // Eliminate each artificial variable's column from the objective row so the
            // tableau is canonical (objective row = 0 under every basic variable's column).
            // Since artificial column has a 1 in its own constraint row (row i+1), and BIG_M
            // in row 0, subtracting BIG_M * row(i+1) zeroes that cell and propagates the
            // penalty across the rest of the row - the standard Big-M setup step.
            foreach (int col in artificialColumns)
            {
                int basicRow = FindRowWithUnitCoefficient(col);
                if (basicRow == -1) continue;

                double factor = _matrix[0, col]; // = BIG_M before elimination
                for (int j = 0; j < _cols; j++)
                {
                    _matrix[0, j] -= factor * _matrix[basicRow, j];
                }
            }
        }

        private int FindRowWithUnitCoefficient(int col)
        {
            for (int i = 1; i < _rows; i++)
            {
                if (Math.Abs(_matrix[i, col] - 1.0) < TOL)
                    return i;
            }
            return -1;
        }

        private bool IsUnitBasicColumn(double[,] tableau, int column, int basisRow, int constraintCount)
        {
            if (Math.Abs(tableau[0, column]) > TOL)
                return false;

            for (int row = 0; row < constraintCount; row++)
            {
                double expected = row == basisRow ? 1.0 : 0.0;
                if (Math.Abs(tableau[row + 1, column] - expected) > TOL)
                    return false;
            }

            return true;
        }

        private bool HasArtificialInBasisWithPositiveValue()
        {
            for (int a = 0; a < _artificialCount; a++)
            {
                int col = _program.Variables.Count + _slackCount + _excessCount + a;
                int row = FindRowWithUnitCoefficient(col);
                // A unit entry in a constraint row alone is not enough after pivots: a
                // nonbasic artificial column can still contain a 1 in a row.  It is basic
                // only when its objective-row coefficient is also zero in the canonical
                // tableau.  Without this check a feasible equality LP such as x = 0.5 is
                // falsely declared infeasible before Cutting Plane can add its first cut.
                if (row != -1 && Math.Abs(_matrix[0, col]) <= TOL &&
                    _matrix[row, _cols - 1] > TOL)
                    return true;
            }
            return false;
        }

        private List<string> GetColumnHeaders()
        {
            var headers = _program.Variables.Select(v => $"x{v.Index}").ToList();
            headers.AddRange(_auxiliaryVariableNames);
            headers.Add("RHS");
            return headers;
        }

        private bool HasNegativeRHS()
        {
            for (int i = 1; i < _rows; i++)
            {
                if (_matrix[i, _cols - 1] < -TOL)
                    return true;
            }
            return false;
        }

        private int FindDualPivotRow()
        {
            double minRhs = 0;
            int pivotRow = -1;
            for (int i = 1; i < _rows; i++)
            {
                double rhs = _matrix[i, _cols - 1];
                if (rhs < -TOL && rhs < minRhs)
                {
                    minRhs = rhs;
                    pivotRow = i;
                }
            }
            return pivotRow;
        }

        private int FindDualPivotColumn(int pivotRow)
        {
            double minRatio = double.MaxValue;
            int pivotCol = -1;
            for (int j = 0; j < _cols - 1; j++)
            {
                if (_matrix[pivotRow, j] < -TOL)
                {
                    double ratio = Math.Abs(_matrix[0, j] / _matrix[pivotRow, j]);
                    // A zero ratio is valid at a degenerate or alternate-optimal
                    // basis. Ignoring it incorrectly reports a feasible added
                    // constraint as infeasible.
                    if (ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotCol = j;
                    }
                }
            }
            return pivotCol;
        }

        private bool IsOptimal()
        {
            for (int j = 0; j < _cols - 1; j++)
            {
                if (_matrix[0, j] < -TOL)
                    return false;
            }
            return true;
        }

        private int FindPrimalPivotColumn()
        {
            double minValue = 0;
            int pivotCol = -1;
            for (int j = 0; j < _cols - 1; j++)
            {
                if (_matrix[0, j] < -TOL && _matrix[0, j] < minValue)
                {
                    minValue = _matrix[0, j];
                    pivotCol = j;
                }
            }
            return pivotCol;
        }

        private int FindPrimalPivotRow(int pivotCol)
        {
            double minRatio = double.MaxValue;
            int pivotRow = -1;
            for (int i = 1; i < _rows; i++)
            {
                if (_matrix[i, pivotCol] > TOL)
                {
                    double ratio = _matrix[i, _cols - 1] / _matrix[i, pivotCol];
                    // The primal ratio test also permits a zero ratio, otherwise
                    // a degenerate tableau cannot be restored after a dual pivot.
                    if (ratio >= -TOL && ratio < minRatio)
                    {
                        minRatio = ratio;
                        pivotRow = i;
                    }
                }
            }
            return pivotRow;
        }

        private void Pivot(int pivotRow, int pivotCol)
        {
            IterationCount++;
            double pivotElement = _matrix[pivotRow, pivotCol];

            for (int j = 0; j < _cols; j++)
            {
                _matrix[pivotRow, j] /= pivotElement;
            }

            for (int i = 0; i < _rows; i++)
            {
                if (i == pivotRow) continue;

                double multiplier = _matrix[i, pivotCol];
                for (int j = 0; j < _cols; j++)
                {
                    _matrix[i, j] -= multiplier * _matrix[pivotRow, j];
                }
            }
        }

        public double GetObjectiveValue()
        {
            return _program.IsMaximization ? _matrix[0, _cols - 1] : -_matrix[0, _cols - 1];
        }

        public Dictionary<string, double> GetSolution()
        {
            var solution = new Dictionary<string, double>();

            for (int j = 0; j < _program.Variables.Count; j++)
            {
                double value = 0.0;

                for (int i = 1; i < _rows; i++)
                {
                    if (Math.Abs(_matrix[i, j] - 1.0) < TOL)
                    {
                        bool isBasic = true;
                        for (int k = 0; k < _rows; k++)
                        {
                            if (k != i && Math.Abs(_matrix[k, j]) > TOL)
                            {
                                isBasic = false;
                                break;
                            }
                        }

                        if (isBasic)
                        {
                            value = _matrix[i, _cols - 1];
                            break;
                        }
                    }
                }

                solution[$"x{_program.Variables[j].Index}"] = Math.Max(0, value);
            }

            for (int auxIndex = 0; auxIndex < _auxiliaryVariableNames.Count; auxIndex++)
            {
                int colIndex = _program.Variables.Count + auxIndex;
                double value = 0.0;
                string auxVarName = _auxiliaryVariableNames[auxIndex];

                for (int i = 1; i < _rows; i++)
                {
                    double pivotValue = _matrix[i, colIndex];
                    if (Math.Abs(pivotValue - 1.0) < TOL || (auxVarName.StartsWith("e") && Math.Abs(pivotValue + 1.0) < TOL))
                    {
                        bool isBasic = true;
                        for (int k = 0; k < _rows; k++)
                        {
                            if (k != i && Math.Abs(_matrix[k, colIndex]) > TOL)
                            {
                                isBasic = false;
                                break;
                            }
                        }

                        if (isBasic)
                        {
                            value = auxVarName.StartsWith("e") && Math.Abs(pivotValue + 1.0) < TOL ? -_matrix[i, _cols - 1] : _matrix[i, _cols - 1];
                            break;
                        }
                    }
                }

                solution[auxVarName] = Math.Max(0, value);
            }

            return solution;
        }

        private string ConstraintToString(LinearProgram.Constraint constraint)
        {
            var sb = new StringBuilder();
            for (int j = 0; j < constraint.Coefficients.Count; j++)
            {
                double coeff = constraint.Coefficients[j];
                if (coeff != 0)
                {
                    sb.Append(coeff >= 0 ? "+ " : "- ");
                    sb.Append(NumberFormatter.Format(Math.Abs(coeff))).Append("x").Append(j + 1).Append(" ");
                }
            }
            string op;
            switch (constraint.Relation)
            {
                case Relation.LessThanOrEqual: op = "<= "; break;
                case Relation.GreaterThanOrEqual: op = ">= "; break;
                case Relation.Equal: op = "= "; break;
                default: op = "? "; break;
            }
            sb.Append(op).Append(NumberFormatter.Format(constraint.Rhs));
            return sb.ToString().TrimStart();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dual Simplex Tableau:");

            sb.Append("        ");

            for (int j = 0; j < _program.Variables.Count; j++)
                sb.Append($"{"x" + _program.Variables[j].Index,-10}");

            foreach (var auxVarName in _auxiliaryVariableNames)
                sb.Append($"{auxVarName,-10}");

            sb.AppendLine("RHS");

            for (int i = 0; i < _rows; i++)
            {
                if (i == 0)
                    sb.Append("Z:      ");
                else
                    sb.Append($"R{i}:     ");

                for (int j = 0; j < _cols; j++)
                {
                    sb.Append($"{NumberFormatter.Format(_matrix[i, j]),10}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public DualSimplex Clone()
        {
            var cloned = new DualSimplex();
            cloned._matrix = (double[,])this._matrix.Clone();
            cloned._rows = this._rows;
            cloned._cols = this._cols;
            cloned._slackCount = this._slackCount;
            cloned._excessCount = this._excessCount;
            cloned._artificialCount = this._artificialCount;
            cloned._auxiliaryVariableNames = new List<string>(this._auxiliaryVariableNames);
            cloned._basisColumnIndices = this._basisColumnIndices == null
                ? null
                : new List<int>(this._basisColumnIndices);
            cloned._program = this._program;
            return cloned;
        }
    }
}
