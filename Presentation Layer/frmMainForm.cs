using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Group_V_26_LPR381_Project.Models;
using Group_V_26_LPR381_Project.Algorithms;

namespace Group_V_26_LPR381_Project.Presentation_Layer
{
    public partial class frmMainForm : Form
    {
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;

        public frmMainForm()
        {
            InitializeComponent();

            lblAlgorithmName.Text = "Programming Model Solver";

            txtSolutionOutput.WordWrap = false;
            txtSolutionOutput.ScrollBars = RichTextBoxScrollBars.Both;

            openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Load Problem File"
            };

            saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Export Results"
            };
        }
        #region SHARED RENDERING

        /// <summary>
        /// Renders a Solution into txtSolutionOutput: messages, then tableau iterations
        /// (with pivot row/column highlighted in yellow) if the solver produced any,
        /// otherwise falls back to plain Steps text (e.g. Revised Simplex, Knapsack).
        /// Requires txtSolutionOutput to be a RichTextBox.
        /// </summary>
        private void RenderSolution(Solution solution)
        {
            txtSolutionOutput.Clear();

            if (solution == null)
            {
                TableauRenderer.AppendPlainLine(
                    txtSolutionOutput,
                    "No solution found."
                );
                return;
            }

            // ---------------------------------------------------------
            // Render everything in the exact order it was created.
            // This is important for Branch and Bound because each
            // sub-problem contains:
            //
            //   Header
            //   Branch constraint
            //   Final Pivot Table
            //   LP relaxation value
            //   Variable values
            //   Result
            //   Branching information
            //
            // OutputBlocks preserves this order.
            // ---------------------------------------------------------

            if (solution.OutputBlocks != null &&
                solution.OutputBlocks.Count > 0)
            {
                foreach (Solution.OutputBlock block in solution.OutputBlocks)
                {
                    switch (block.Type)
                    {
                        case Solution.OutputBlockType.Text:

                            if (!string.IsNullOrEmpty(block.Title))
                            {
                                TableauRenderer.AppendTextBlock(
                                    txtSolutionOutput,
                                    block.Title,
                                    block.Text
                                );
                            }
                            else
                            {
                                TableauRenderer.AppendPlainLine(
                                    txtSolutionOutput,
                                    block.Text ?? ""
                                );
                            }

                            break;


                        case Solution.OutputBlockType.GroupHeader:

                            TableauRenderer.AppendGroupHeader(
                                txtSolutionOutput,
                                block.Title,
                                block.IndentLevel
                            );

                            break;


                        case Solution.OutputBlockType.Tableau:

                            TableauRenderer.AppendTableau(
                                txtSolutionOutput,
                                block.Tableau,
                                block.ColumnHeaders,
                                block.PivotRow,
                                block.PivotCol,
                                block.Title
                            );

                            break;
                    }
                }

                return;
            }

            // ---------------------------------------------------------
            // FALLBACK
            //
            // Solvers that do not use OutputBlocks can still use the
            // older Messages / IterationTableaux / Steps structure.
            // ---------------------------------------------------------

            if (solution.Messages != null &&
                solution.Messages.Any())
            {
                foreach (string message in solution.Messages)
                {
                    TableauRenderer.AppendPlainLine(
                        txtSolutionOutput,
                        message
                    );
                }

                TableauRenderer.AppendPlainLine(
                    txtSolutionOutput,
                    ""
                );
            }

            if (solution.IterationTableaux != null &&
                solution.IterationTableaux.Any())
            {
                for (int i = 0;
                     i < solution.IterationTableaux.Count;
                     i++)
                {
                    List<string> headers =
                        i < solution.IterationColumnHeaders.Count
                            ? solution.IterationColumnHeaders[i]
                            : null;

                    int pivotRow =
                        i < solution.IterationPivotRows.Count
                            ? solution.IterationPivotRows[i]
                            : -1;

                    int pivotCol =
                        i < solution.IterationPivotCols.Count
                            ? solution.IterationPivotCols[i]
                            : -1;

                    string title =
                        i < solution.IterationMessages.Count
                            ? solution.IterationMessages[i]
                            : "Final Pivot Table";

                    TableauRenderer.AppendTableau(
                        txtSolutionOutput,
                        solution.IterationTableaux[i],
                        headers,
                        pivotRow,
                        pivotCol,
                        title
                    );
                }
            }
            else if (solution.Steps != null &&
                     solution.Steps.Any())
            {
                foreach (string step in solution.Steps)
                {
                    TableauRenderer.AppendPlainLine(
                        txtSolutionOutput,
                        step
                    );

                    TableauRenderer.AppendPlainLine(
                        txtSolutionOutput,
                        ""
                    );
                }
            }

            // ---------------------------------------------------------
            // Final solution summary for non-Branch-and-Bound solvers.
            // Branch and Bound already writes its own final result into
            // OutputBlocks.
            // ---------------------------------------------------------

            TableauRenderer.AppendPlainLine(
                txtSolutionOutput,
                ""
            );

            TableauRenderer.AppendPlainLine(
                txtSolutionOutput,
                "Optimal Solution:"
            );

            if (solution.VariableValues != null &&
                solution.VariableValues.Any())
            {
                foreach (var kvp in solution.VariableValues.OrderBy(
                    k => k.Key))
                {
                    TableauRenderer.AppendPlainLine(
                        txtSolutionOutput,
                        $"{kvp.Key} = {NumberFormatter.Format(kvp.Value)}"
                    );
                }

                TableauRenderer.AppendPlainLine(
                    txtSolutionOutput,
                    $"Optimal Value: {NumberFormatter.Format(solution.OptimalValue)}"
                );
            }
            else
            {
                TableauRenderer.AppendPlainLine(
                    txtSolutionOutput,
                    "No solution found."
                );
            }
        }

        #endregion

        #region FORM BUTTONS

        // Input
        private void btnLoadProblem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                txtProblemInput.Text = File.ReadAllText(openFileDialog.FileName);
                txtSolutionOutput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        // Primal Simplex
        private void btnSolvePrimal_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProblemInput.Text))
            {
                MessageBox.Show("Please load a problem first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblAlgorithmName.Text = "Primal Simplex Algorithm";

                LinearProgram program;
                double[] breakpoints = null;

                if (NonLinearRouter.IsNonLinearInput(txtProblemInput.Text))
                {
                    var nlp = NonLinearRouter.Parse(txtProblemInput.Text);
                    var pwl = NonLinearToLinearConverter.BuildPiecewiseLinearApproximation(
                        nlp.Expression, nlp.LowerBound, nlp.UpperBound, nlp.Maximize, nlp.Segments);
                    program = pwl.Program;
                    breakpoints = pwl.Breakpoints;
                }
                else
                {
                    program = LinearProgram.Parse(txtProblemInput.Text);
                }

                var solver = new PrimalSimplex();
                var solution = solver.Solve(program);

                if (breakpoints != null)
                {
                    double xValue = NonLinearToLinearConverter.RecoverXValue(solution, breakpoints);
                    solution.AddMessage("");
                    solution.AddMessage($"Recovered non-linear solution: x = {NumberFormatter.Format(xValue)}, f(x) = {NumberFormatter.Format(solution.OptimalValue)}");
                }

                RenderSolution(solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving problem: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Revised Primal Simplex
        private void btnSolveRevised_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProblemInput.Text))
            {
                MessageBox.Show("Please load a problem first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblAlgorithmName.Visible = true;
                lblAlgorithmName.Text = "Revised Primal Simplex Algorithm";

                LinearProgram program;
                double[] breakpoints = null;

                if (NonLinearRouter.IsNonLinearInput(txtProblemInput.Text))
                {
                    var nlp = NonLinearRouter.Parse(txtProblemInput.Text);
                    var pwl = NonLinearToLinearConverter.BuildPiecewiseLinearApproximation(
                        nlp.Expression, nlp.LowerBound, nlp.UpperBound, nlp.Maximize, nlp.Segments);
                    program = pwl.Program;
                    breakpoints = pwl.Breakpoints;
                }
                else
                {
                    program = LinearProgram.Parse(txtProblemInput.Text);
                }

                var solver = new RevisedPrimalSimplex();
                var solution = solver.Solve(program);

                if (breakpoints != null)
                {
                    double xValue = NonLinearToLinearConverter.RecoverXValue(solution, breakpoints);
                    solution.AddMessage("");
                    solution.AddMessage($"Recovered non-linear solution: x = {NumberFormatter.Format(xValue)}, f(x) = {NumberFormatter.Format(solution.OptimalValue)}");
                }

                RenderSolution(solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving problem: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Branch and Bound
        private async void btnSolveBranchAndBound_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProblemInput.Text))
            {
                MessageBox.Show("Please load a problem first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblAlgorithmName.Visible = true;
                lblAlgorithmName.Text = "Branch and Bound Simplex Algorithm";

                btnSolveBranchAndBound.Enabled = false;

                LinearProgram program;
                double[] breakpoints = null;

                if (NonLinearRouter.IsNonLinearInput(txtProblemInput.Text))
                {
                    var nlp = NonLinearRouter.Parse(txtProblemInput.Text);
                    var pwl = NonLinearToLinearConverter.BuildPiecewiseLinearApproximation(
                        nlp.Expression, nlp.LowerBound, nlp.UpperBound, nlp.Maximize, nlp.Segments);
                    program = pwl.Program;
                    breakpoints = pwl.Breakpoints;
                }
                else
                {
                    program = LinearProgram.Parse(txtProblemInput.Text);
                }

                var solver = new BranchAndBound();
                var solution = await Task.Run(() => solver.Solve(program));

                if (breakpoints != null)
                {
                    double xValue = NonLinearToLinearConverter.RecoverXValue(solution, breakpoints);
                    solution.AddMessage("");
                    solution.AddMessage($"Recovered non-linear solution: x = {NumberFormatter.Format(xValue)}, f(x) = {NumberFormatter.Format(solution.OptimalValue)}");
                }

                RenderSolution(solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving problem: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSolveBranchAndBound.Enabled = true;
            }
        }

        // Knapsack
        private void btnSolveKnapsack_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProblemInput.Text))
            {
                MessageBox.Show("Please load a problem first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (NonLinearRouter.IsNonLinearInput(txtProblemInput.Text))
            {
                MessageBox.Show("The Knapsack algorithm doesn't apply to non-linear problems - use another algorithm or Golden Section Search.",
                    "Not applicable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                lblAlgorithmName.Visible = true;
                lblAlgorithmName.Text = "Branch and Bound: Knapsack Algorithm";

                btnSolveKnapsack.Enabled = false;

                var program = LinearProgram.Parse(txtProblemInput.Text);
                var solver = new LinearProgrammingSolver.Algorithms.Knapsack();
                var solution = solver.Solve(program);

                RenderSolution(solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving problem: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSolveKnapsack.Enabled = true;
            }
        }

        // Cutting Plane
        private async void btnSolveCuttingPlane_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProblemInput.Text))
            {
                MessageBox.Show("Please load a problem first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblAlgorithmName.Visible = true;
                lblAlgorithmName.Text = "Cutting Plane Algorithm";

                btnSolveCuttingPlane.Enabled = false;
                txtSolutionOutput.Clear();

                LinearProgram program;
                double[] breakpoints = null;

                if (NonLinearRouter.IsNonLinearInput(txtProblemInput.Text))
                {
                    var nlp = NonLinearRouter.Parse(txtProblemInput.Text);
                    var pwl = NonLinearToLinearConverter.BuildPiecewiseLinearApproximation(
                        nlp.Expression, nlp.LowerBound, nlp.UpperBound, nlp.Maximize, nlp.Segments);
                    program = pwl.Program;
                    breakpoints = pwl.Breakpoints;
                }
                else
                {
                    program = LinearProgram.Parse(txtProblemInput.Text);
                }

                var solver = new LinearProgrammingSolver.Algorithms.CuttingPlane();
                var solution = await Task.Run(() => solver.Solve(program));

                if (breakpoints != null)
                {
                    double xValue = NonLinearToLinearConverter.RecoverXValue(solution, breakpoints);
                    solution.AddMessage("");
                    solution.AddMessage($"Recovered non-linear solution: x = {NumberFormatter.Format(xValue)}, f(x) = {NumberFormatter.Format(solution.OptimalValue)}");
                }

                RenderSolution(solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving problem: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSolveCuttingPlane.Enabled = true;
            }
        }

        // Sensitivity Analysis
        private void btnSolveSensitivityAnalysis_Click(object sender, EventArgs e)
        {
            lblAlgorithmName.Visible = true;
            lblAlgorithmName.Text = "Sensitivity Analysis";
        }

        // Export
        private void btnExportResults_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSolutionOutput.Text))
            {
                MessageBox.Show("No results to export.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                File.WriteAllText(saveFileDialog.FileName, txtSolutionOutput.Text);
                MessageBox.Show("Results exported successfully.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting results: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
      

        private void btnSolveGoldenSection_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProblemInput.Text))
            {
                MessageBox.Show("Please enter a non-linear problem first, e.g.\nnlp min x^2\n-5 5", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!NonLinearRouter.IsNonLinearInput(txtProblemInput.Text))
            {
                MessageBox.Show("Golden Section Search only applies to non-linear problems.\nUse the format:\nnlp min x^2\n-5 5",
                    "Not applicable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                lblAlgorithmName.Visible = true;
                lblAlgorithmName.Text = "Golden Section Search";

                var nlp = NonLinearRouter.Parse(txtProblemInput.Text);

                var solver = new GoldenSectionSearch();
                var solution = solver.Solve(nlp.Expression, nlp.LowerBound, nlp.UpperBound, nlp.Maximize);

                RenderSolution(solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving problem: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
