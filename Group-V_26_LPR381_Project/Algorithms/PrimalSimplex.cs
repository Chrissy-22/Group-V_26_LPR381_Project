using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Group_V_26_LPR381_Project.Algorithms
{
    public class PrimalSimplex : ISolver
    {
        private const double TOLERANCE = 1e-6;

        public Solution Solve(LinearProgram program)
        {
            var solution = new Solution();

            if (program == null)
            {
                solution.AddMessage("No linear program was provided.");
                return solution;
            }

            if (!ValidateProgram(program, solution))
            {
                return solution;
            }

            solution.AddMessage("Running Primal Simplex algorithm...");
            solution.AddMessage("");

            var tableau = new Tableau(program);

            // ---------------------------------------------------------
            // SIMPLEX ITERATIONS
            //
            // The tableau displayed for an iteration is the tableau
            // BEFORE the pivot, with the pivot row and pivot column
            // highlighted.
            // ---------------------------------------------------------

            while (!tableau.IsOptimal())
            {
                // Find the entering variable.
                int pivotColumn = tableau.FindPivotColumn();

                if (pivotColumn == -1)
                {
                    solution.AddMessage(
                        "Result: UNBOUNDED"
                    );

                    solution.AddMessage(
                        "No valid entering variable was found."
                    );

                    return solution;
                }

                // Find the leaving variable using the ratio test.
                int pivotRow = tableau.FindPivotRow(pivotColumn);

                if (pivotRow == -1)
                {
                    solution.AddMessage(
                        "Result: UNBOUNDED"
                    );

                    solution.AddMessage(
                        "No valid leaving variable was found."
                    );

                    return solution;
                }

                string pivotVariable =
                    tableau.GetColumnHeaders()[pivotColumn];

                // -----------------------------------------------------
                // DISPLAY THE CURRENT TABLEAU BEFORE PIVOTING.
                //
                // This is important:
                // pivotRow and pivotColumn refer to the current tableau,
                // so this is the tableau that must be highlighted.
                // -----------------------------------------------------

                string iterationTitle =
                    "Iteration " +
                    (tableau.IterationCount + 1) +
                    ": Pivot on " +
                    pivotVariable +
                    " (row " +
                    pivotRow +
                    ", column " +
                    pivotColumn +
                    ")";

                solution.AddIteration(
                    tableau.GetMatrix(),
                    iterationTitle,
                    pivotRow,
                    pivotColumn,
                    tableau.GetColumnHeaders()
                );

                // -----------------------------------------------------
                // NOW PERFORM THE PIVOT.
                // -----------------------------------------------------

                tableau.Pivot(
                    pivotRow,
                    pivotColumn
                );
            }

            // ---------------------------------------------------------
            // THE TABLEAU AFTER THE FINAL PIVOT IS NOW OPTIMAL.
            // Display it without a pivot highlight.
            // ---------------------------------------------------------

            solution.AddIteration(
                tableau.GetMatrix(),
                "Final Tableau",
                -1,
                -1,
                tableau.GetColumnHeaders()
            );

            // ---------------------------------------------------------
            // GET FINAL SOLUTION
            // ---------------------------------------------------------

            solution.OptimalValue =
                tableau.GetObjectiveValue();

            solution.VariableValues =
                tableau.GetSolution();

            solution.AddMessage(
                "Result: OPTIMAL SOLUTION"
            );

            solution.AddMessage(
                "Optimal Value: " +
                NumberFormatter.Format(
                    solution.OptimalValue
                )
            );

            solution.AddMessage(
                "Variable values:"
            );

            foreach (KeyValuePair<string, double> variable
                     in solution.VariableValues)
            {
                solution.AddMessage(
                    variable.Key +
                    " = " +
                    NumberFormatter.Format(
                        variable.Value
                    )
                );
            }

            return solution;
        }

        // =============================================================
        // VALIDATION
        // =============================================================

        private bool ValidateProgram(
            LinearProgram program,
            Solution solution)
        {
            // Standard primal simplex tableau implemented here is for
            // maximization problems.

            if (!program.IsMaximization)
            {
                solution.AddMessage(
                    "Result: INVALID INPUT FOR PRIMAL SIMPLEX"
                );

                solution.AddMessage(
                    "This Primal Simplex implementation requires " +
                    "a maximization problem."
                );

                solution.AddMessage(
                    "Minimization problems require conversion or " +
                    "a suitable alternative method."
                );

                return false;
            }

            // Every constraint must be <=.
            for (int i = 0;
                 i < program.Constraints.Count;
                 i++)
            {
                LinearProgram.Constraint constraint =
                    program.Constraints[i];

                if (constraint.Relation !=
                    LinearProgram.Relation.LessThanOrEqual)
                {
                    solution.AddMessage(
                        "Result: INVALID INPUT FOR PRIMAL SIMPLEX"
                    );

                    solution.AddMessage(
                        "Constraint " +
                        (i + 1) +
                        " is not a <= constraint."
                    );

                    solution.AddMessage(
                        "The standard primal simplex tableau " +
                        "requires <= constraints."
                    );

                    return false;
                }

                // The initial slack-variable basis is feasible only
                // when the RHS is non-negative.
                if (constraint.Rhs < -TOLERANCE)
                {
                    solution.AddMessage(
                        "Result: INVALID INPUT FOR PRIMAL SIMPLEX"
                    );

                    solution.AddMessage(
                        "Constraint " +
                        (i + 1) +
                        " has a negative RHS."
                    );

                    solution.AddMessage(
                        "A Phase I / Two-Phase approach is required " +
                        "for this case."
                    );

                    return false;
                }

                if (constraint.Coefficients.Count !=
                    program.Variables.Count)
                {
                    solution.AddMessage(
                        "Result: INVALID INPUT"
                    );

                    solution.AddMessage(
                        "Constraint " +
                        (i + 1) +
                        " does not contain the correct " +
                        "number of coefficients."
                    );

                    return false;
                }
            }

            return true;
        }
    }

    // =================================================================
    // SIMPLEX TABLEAU
    // =================================================================

    public partial class Tableau
    {
        private const double TOLERANCE = 1e-6;

        private double[,] _matrix;
        private readonly int _rows;
        private readonly int _cols;
        private readonly LinearProgram _program;

        public int IterationCount { get; private set; }

        // -------------------------------------------------------------
        // CONSTRUCTOR
        // -------------------------------------------------------------

        public Tableau(LinearProgram program)
        {
            if (program == null)
            {
                throw new ArgumentNullException(
                    nameof(program)
                );
            }

            _program = program;

            int variableCount =
                _program.Variables.Count;

            int constraintCount =
                _program.Constraints.Count;

            // One objective row + one row per constraint.
            _rows =
                constraintCount + 1;

            // Decision variables +
            // one slack variable per constraint +
            // RHS.
            _cols =
                variableCount +
                constraintCount +
                1;

            _matrix =
                new double[_rows, _cols];

            IterationCount = 0;

            InitializeTableau();
        }

        // -------------------------------------------------------------
        // INITIAL TABLEAU
        // -------------------------------------------------------------

        private void InitializeTableau()
        {
            int variableCount =
                _program.Variables.Count;

            int constraintCount =
                _program.Constraints.Count;

            // ---------------------------------------------------------
            // Objective row
            //
            // For:
            //
            // Max Z = c1x1 + c2x2 + ...
            //
            // Tableau stores:
            //
            // Z - c1x1 - c2x2 - ... = 0
            //
            // Therefore coefficients are negative.
            // ---------------------------------------------------------

            for (int j = 0;
                 j < variableCount;
                 j++)
            {
                _matrix[0, j] =
                    -_program.Variables[j].Coefficient;
            }

            _matrix[0, _cols - 1] = 0;

            // ---------------------------------------------------------
            // Constraint rows
            //
            // For <= constraints:
            //
            // a1x1 + a2x2 + s = b
            //
            // ---------------------------------------------------------

            for (int i = 0;
                 i < constraintCount;
                 i++)
            {
                LinearProgram.Constraint constraint =
                    _program.Constraints[i];

                int row = i + 1;

                // Decision-variable coefficients
                for (int j = 0;
                     j < variableCount;
                     j++)
                {
                    _matrix[row, j] =
                        constraint.Coefficients[j];
                }

                // Slack variable for this constraint
                int slackColumn =
                    variableCount + i;

                _matrix[row, slackColumn] = 1;

                // RHS
                _matrix[row, _cols - 1] =
                    constraint.Rhs;
            }
        }

        // -------------------------------------------------------------
        // GET MATRIX
        // -------------------------------------------------------------

        public double[,] GetMatrix()
        {
            return (double[,])_matrix.Clone();
        }

        // -------------------------------------------------------------
        // COLUMN HEADERS
        // -------------------------------------------------------------

        public List<string> GetColumnHeaders()
        {
            var headers =
                new List<string>();

            // Decision variables
            for (int j = 0;
                 j < _program.Variables.Count;
                 j++)
            {
                headers.Add(
                    "x" +
                    _program.Variables[j].Index
                );
            }

            // Slack variables
            int slackCount =
                _program.Constraints.Count;

            for (int j = 0;
                 j < slackCount;
                 j++)
            {
                headers.Add(
                    "s" +
                    (j + 1)
                );
            }

            // RHS
            headers.Add("RHS");

            return headers;
        }

        // -------------------------------------------------------------
        // CHECK OPTIMALITY
        // -------------------------------------------------------------

        /// <summary>
        /// For a maximization tableau, the solution is optimal when
        /// there are no negative coefficients remaining in the
        /// objective row.
        /// </summary>
        public bool IsOptimal()
        {
            for (int j = 0;
                 j < _cols - 1;
                 j++)
            {
                if (_matrix[0, j] < -TOLERANCE)
                {
                    return false;
                }
            }

            return true;
        }

        // -------------------------------------------------------------
        // FIND PIVOT COLUMN
        // -------------------------------------------------------------

        /// <summary>
        /// For maximization:
        /// choose the most negative coefficient in the objective row.
        ///
        /// Example:
        ///
        /// -3  -5   0   0
        ///
        /// -5 is the smallest value, therefore x2 enters.
        /// </summary>
        public int FindPivotColumn()
        {
            int pivotColumn = -1;

            double mostNegative =
                -TOLERANCE;

            for (int j = 0;
                 j < _cols - 1;
                 j++)
            {
                if (_matrix[0, j] <
                    mostNegative)
                {
                    mostNegative =
                        _matrix[0, j];

                    pivotColumn = j;
                }
            }

            return pivotColumn;
        }

        // -------------------------------------------------------------
        // FIND PIVOT ROW
        // -------------------------------------------------------------

        /// <summary>
        /// Performs the minimum positive ratio test:
        ///
        /// ratio = RHS / pivot-column coefficient
        ///
        /// Only positive pivot-column coefficients are considered.
        /// The smallest non-negative ratio determines the leaving row.
        /// </summary>
        public int FindPivotRow(int pivotColumn)
        {
            if (pivotColumn < 0 || pivotColumn >= _cols - 1)
                return -1;

            int pivotRow = -1;
            double smallestRatio = double.MaxValue;

            for (int i = 1; i < _rows; i++)
            {
                double pivotValue = _matrix[i, pivotColumn];
                double rhs = _matrix[i, _cols - 1];

                // Only positive pivot-column values are allowed
                // in the primal simplex ratio test.
                if (pivotValue > TOLERANCE)
                {
                    double ratio = rhs / pivotValue;

                    // Ignore negative ratios.
                    if (ratio >= -TOLERANCE &&
                        ratio < smallestRatio)
                    {
                        smallestRatio = ratio;
                        pivotRow = i;
                    }
                }
            }

            return pivotRow;
        }


        public string GetRatioTest(int pivotColumn)
        {
            var sb = new StringBuilder();

            List<string> headers = GetColumnHeaders();

            sb.AppendLine(
                "Ratio Test for " +
                headers[pivotColumn] +
                ":"
            );

            sb.AppendLine(
                "Row\tRHS\t" +
                headers[pivotColumn] +
                "\tθ"
            );

            for (int i = 1; i < _rows; i++)
            {
                double pivotValue =
                    _matrix[i, pivotColumn];

                double rhs =
                    _matrix[i, _cols - 1];

                sb.Append(
                    "R" +
                    i +
                    "\t" +
                    NumberFormatter.Format(rhs) +
                    "\t" +
                    NumberFormatter.Format(pivotValue) +
                    "\t"
                );

                if (pivotValue > 1e-6)
                {
                    double ratio =
                        rhs / pivotValue;

                    sb.AppendLine(
                        NumberFormatter.Format(ratio)
                    );
                }
                else
                {
                    sb.AppendLine("-");
                }
            }

            return sb.ToString();
        }
        // -------------------------------------------------------------
        // PIVOT
        // -------------------------------------------------------------

        public void Pivot(
            int pivotRow,
            int pivotColumn)
        {
            if (pivotRow <= 0 ||
                pivotRow >= _rows)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pivotRow)
                );
            }

            if (pivotColumn < 0 ||
                pivotColumn >= _cols - 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pivotColumn)
                );
            }

            double pivotValue =
                _matrix[
                    pivotRow,
                    pivotColumn
                ];

            if (Math.Abs(pivotValue) <
                TOLERANCE)
            {
                throw new InvalidOperationException(
                    "Cannot pivot on a zero element."
                );
            }

            IterationCount++;

            // ---------------------------------------------------------
            // Step 1:
            // Divide pivot row by pivot element.
            // ---------------------------------------------------------

            for (int j = 0;
                 j < _cols;
                 j++)
            {
                _matrix[
                    pivotRow,
                    j
                ] /= pivotValue;
            }

            // ---------------------------------------------------------
            // Step 2:
            // Eliminate pivot-column entries from every other row.
            // ---------------------------------------------------------

            for (int i = 0;
                 i < _rows;
                 i++)
            {
                if (i == pivotRow)
                {
                    continue;
                }

                double factor =
                    _matrix[i, pivotColumn];

                if (Math.Abs(factor) <
                    TOLERANCE)
                {
                    continue;
                }

                for (int j = 0;
                     j < _cols;
                     j++)
                {
                    _matrix[i, j] -=
                        factor *
                        _matrix[
                            pivotRow,
                            j
                        ];
                }
            }

            CleanSmallValues();
        }

        // -------------------------------------------------------------
        // REMOVE FLOATING-POINT NOISE
        // -------------------------------------------------------------

        private void CleanSmallValues()
        {
            for (int i = 0;
                 i < _rows;
                 i++)
            {
                for (int j = 0;
                     j < _cols;
                     j++)
                {
                    if (Math.Abs(
                        _matrix[i, j]
                    ) < TOLERANCE)
                    {
                        _matrix[i, j] = 0;
                    }
                }
            }
        }

        // -------------------------------------------------------------
        // OBJECTIVE VALUE
        // -------------------------------------------------------------

        public double GetObjectiveValue()
        {
            return _matrix[
                0,
                _cols - 1
            ];
        }

        // -------------------------------------------------------------
        // GET DECISION-VARIABLE SOLUTION
        // -------------------------------------------------------------

        public Dictionary<string, double>
            GetSolution()
        {
            var solution =
                new Dictionary<string, double>();

            for (int j = 0;
                 j < _program.Variables.Count;
                 j++)
            {
                bool isBasic = false;
                int basicRow = -1;

                // -----------------------------------------------------
                // Check whether this decision-variable column is a
                // basic column.
                //
                // A basic column has exactly one 1 and all other
                // constraint-row entries are 0.
                // -----------------------------------------------------

                for (int i = 1;
                     i < _rows;
                     i++)
                {
                    if (Math.Abs(
                        _matrix[i, j] - 1
                    ) > TOLERANCE)
                    {
                        continue;
                    }

                    bool columnIsBasic =
                        true;

                    for (int k = 1;
                         k < _rows;
                         k++)
                    {
                        if (k == i)
                        {
                            continue;
                        }

                        if (Math.Abs(
                            _matrix[k, j]
                        ) > TOLERANCE)
                        {
                            columnIsBasic =
                                false;

                            break;
                        }
                    }

                    if (columnIsBasic)
                    {
                        isBasic = true;
                        basicRow = i;
                        break;
                    }
                }

                string variableName =
                    "x" +
                    _program.Variables[j].Index;

                solution[variableName] =
                    isBasic
                        ? _matrix[
                            basicRow,
                            _cols - 1
                          ]
                        : 0;
            }

            return solution;
        }

        // -------------------------------------------------------------
        // DISPLAY TABLEAU
        // -------------------------------------------------------------

        public override string ToString()
        {
            var sb =
                new StringBuilder();

            sb.AppendLine("Tableau:");

            sb.Append("        ");

            // Decision variables
            for (int j = 0;
                 j < _program.Variables.Count;
                 j++)
            {
                sb.Append(
                    "x" +
                    _program.Variables[j].Index +
                    "       "
                );
            }

            // Slack variables
            for (int j = 0;
                 j < _program.Constraints.Count;
                 j++)
            {
                sb.Append(
                    "s" +
                    (j + 1) +
                    "       "
                );
            }

            sb.AppendLine("RHS");

            // Rows
            for (int i = 0;
                 i < _rows;
                 i++)
            {
                if (i == 0)
                {
                    sb.Append("Z:    ");
                }
                else
                {
                    sb.Append(
                        "R" +
                        i +
                        ":    "
                    );
                }

                for (int j = 0;
                     j < _cols;
                     j++)
                {
                    sb.Append(
                        NumberFormatter.Format(
                            _matrix[i, j]
                        )
                    );

                    sb.Append("\t");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}