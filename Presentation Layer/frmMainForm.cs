using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using ClosedXML.Excel;

using Group_V_26_LPR381_Project.Models;
using Group_V_26_LPR381_Project.Algorithms;

namespace Group_V_26_LPR381_Project.Presentation_Layer
{
    public partial class frmMainForm : Form
    {
        // ============================================================
        // FILE DIALOGS
        // ============================================================

        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;


        // ============================================================
        // UI COLOURS
        // ============================================================

        private readonly Color SidebarColor =
            Color.FromArgb(15, 23, 42);

        private readonly Color SidebarHoverColor =
            Color.FromArgb(30, 41, 59);

        private readonly Color AccentColor =
            Color.FromArgb(37, 99, 235);

        private readonly Color NavigationTextColor =
            Color.FromArgb(203, 213, 225);

        private readonly Color MutedTextColor =
            Color.FromArgb(148, 163, 184);

        private Button selectedAlgorithmButton;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public frmMainForm()
        {
            InitializeComponent();

            SetupModernUI();

            lblAlgorithmName.Text =
                "Programming Model Solver";

            lblHeaderSubtitle.Text =
                "Choose an optimisation method to solve your model.";

            txtSolutionOutput.WordWrap = false;

            txtSolutionOutput.ScrollBars =
                RichTextBoxScrollBars.Both;


            openFileDialog = new OpenFileDialog
            {
                Filter =
                    "Text files (*.txt)|*.txt|" +
                    "All files (*.*)|*.*",

                Title =
                    "Load Problem File"
            };


            saveFileDialog = new SaveFileDialog
            {
                Filter =
                    "Excel Workbook (*.xlsx)|*.xlsx",

                Title =
                    "Export Results to Excel",

                DefaultExt =
                    "xlsx",

                AddExtension =
                    true,

                FileName =
                    "OPTIMA_Results.xlsx"
            };
        }


        // ============================================================
        // UI
        // ============================================================

        #region UI

        private void SetupModernUI()
        {
            StyleNavigationButton(btnSolvePrimal);
            StyleNavigationButton(btnSolveRevised);
            StyleNavigationButton(btnSolveBranchAndBound);
            StyleNavigationButton(btnSolveKnapsack);
            StyleNavigationButton(btnSolveCuttingPlane);
            StyleNavigationButton(btnSolveSensitivityAnalysis);
            StyleNavigationButton(btnSolveGoldenSection);

            StylePrimaryButton(btnLoadProblem);

            StyleSecondaryButton(btnSampleProblem);
            StyleSecondaryButton(btnClearProblem);
            StyleSecondaryButton(btnExportResults);

            ShowWelcomeMessage();

            SetStatusReady();


            Button[] navigationButtons =
            {
                btnSolvePrimal,
                btnSolveRevised,
                btnSolveBranchAndBound,
                btnSolveKnapsack,
                btnSolveCuttingPlane,
                btnSolveSensitivityAnalysis,
                btnSolveGoldenSection
            };


            foreach (Button button in navigationButtons)
            {
                button.Cursor = Cursors.Hand;

                button.MouseEnter +=
                    NavigationButton_MouseEnter;

                button.MouseLeave +=
                    NavigationButton_MouseLeave;
            }


            btnLoadProblem.Cursor =
                Cursors.Hand;

            btnSampleProblem.Cursor =
                Cursors.Hand;

            btnClearProblem.Cursor =
                Cursors.Hand;

            btnExportResults.Cursor =
                Cursors.Hand;
        }


        private void StyleNavigationButton(
            Button button)
        {
            button.BackColor =
                SidebarColor;

            button.ForeColor =
                NavigationTextColor;

            button.FlatStyle =
                FlatStyle.Flat;

            button.FlatAppearance.BorderSize =
                0;

            button.FlatAppearance.MouseOverBackColor =
                SidebarHoverColor;

            button.FlatAppearance.MouseDownBackColor =
                AccentColor;

            button.Font =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Regular
                );

            button.Padding =
                new Padding(
                    8,
                    0,
                    0,
                    0
                );
        }


        private void StylePrimaryButton(
            Button button)
        {
            button.BackColor =
                AccentColor;

            button.ForeColor =
                Color.White;

            button.FlatStyle =
                FlatStyle.Flat;

            button.FlatAppearance.BorderSize =
                0;

            button.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(
                    29,
                    78,
                    216
                );

            button.FlatAppearance.MouseDownBackColor =
                Color.FromArgb(
                    30,
                    64,
                    175
                );

            button.Font =
                new Font(
                    "Segoe UI",
                    9F,
                    FontStyle.Bold
                );
        }


        private void StyleSecondaryButton(
            Button button)
        {
            button.BackColor =
                Color.FromArgb(
                    30,
                    41,
                    59
                );

            button.ForeColor =
                Color.FromArgb(
                    226,
                    232,
                    240
                );

            button.FlatStyle =
                FlatStyle.Flat;

            button.FlatAppearance.BorderSize =
                0;

            button.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(
                    51,
                    65,
                    85
                );

            button.Font =
                new Font(
                    "Segoe UI",
                    9F,
                    FontStyle.Bold
                );
        }


        private void NavigationButton_MouseEnter(
            object sender,
            EventArgs e)
        {
            Button button =
                sender as Button;

            if (button == null)
                return;

            if (button != selectedAlgorithmButton)
            {
                button.BackColor =
                    SidebarHoverColor;
            }
        }


        private void NavigationButton_MouseLeave(
            object sender,
            EventArgs e)
        {
            Button button =
                sender as Button;

            if (button == null)
                return;

            if (button != selectedAlgorithmButton)
            {
                button.BackColor =
                    SidebarColor;
            }
        }


        private void SelectAlgorithm(
            Button selectedButton,
            string title,
            string description)
        {
            ResetAlgorithmButtons();

            selectedAlgorithmButton =
                selectedButton;

            selectedButton.BackColor =
                AccentColor;

            selectedButton.ForeColor =
                Color.White;

            selectedButton.Font =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Bold
                );

            lblAlgorithmName.Text =
                title;

            lblAlgorithmName.Visible =
                true;

            lblHeaderSubtitle.Text =
                description;

            lblOutputTitle.Text =
            "Solution Output";
        }


        private void ResetAlgorithmButtons()
        {
            Button[] buttons =
            {
                btnSolvePrimal,
                btnSolveRevised,
                btnSolveBranchAndBound,
                btnSolveKnapsack,
                btnSolveCuttingPlane,
                btnSolveSensitivityAnalysis,
                btnSolveGoldenSection
            };


            foreach (Button button in buttons)
            {
                button.BackColor =
                    SidebarColor;

                button.ForeColor =
                    NavigationTextColor;

                button.Font =
                    new Font(
                        "Segoe UI",
                        10F,
                        FontStyle.Regular
                    );
            }
        }
        private void ShowWelcomeMessage()
        {
            txtSolutionOutput.Clear();

            lblOutputTitle.Text = "Welcome";

            txtSolutionOutput.BackColor =
                Color.FromArgb(248, 250, 252);


            // ========================================================
            // MAIN TITLE
            // ========================================================

            txtSolutionOutput.SelectionAlignment =
                HorizontalAlignment.Center;

            txtSolutionOutput.SelectionIndent = 0;

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    26F,
                    FontStyle.Bold
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(15, 23, 42);

            txtSolutionOutput.AppendText(
                Environment.NewLine +
                "OPTIMA" +
                Environment.NewLine
            );


            // ========================================================
            // SUBTITLE
            // ========================================================

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    12F,
                    FontStyle.Regular
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(100, 116, 139);

            txtSolutionOutput.AppendText(
                "Linear Programming & Optimisation Solver" +
                Environment.NewLine +
                Environment.NewLine
            );


            // ========================================================
            // READY MESSAGE
            // ========================================================

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    11F,
                    FontStyle.Bold
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(37, 99, 235);

            txtSolutionOutput.AppendText(
                "READY TO SOLVE" +
                Environment.NewLine
            );


            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    10F,
                    FontStyle.Regular
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(71, 85, 105);

            txtSolutionOutput.AppendText(
                "Enter a programming model or load one from a file, " +
                "then select a solver method." +
                Environment.NewLine +
                Environment.NewLine +
                Environment.NewLine
            );


            // ========================================================
            // QUICK START
            // ========================================================

            txtSolutionOutput.SelectionAlignment =
                HorizontalAlignment.Left;

            txtSolutionOutput.SelectionIndent =
                80;

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    13F,
                    FontStyle.Bold
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(15, 23, 42);

            txtSolutionOutput.AppendText(
                "QUICK START" +
                Environment.NewLine +
                Environment.NewLine
            );


            // ========================================================
            // STEPS
            // ========================================================

            AppendWelcomeStep(
                "1",
                "Enter or Load a Problem",
                "Type your model in the Problem Input area, use Load, " +
                "or click Sample for an example."
            );


            AppendWelcomeStep(
                "2",
                "Choose a Solver Method",
                "Select Primal Simplex, Revised Simplex, Branch & Bound, " +
                "Knapsack, Cutting Plane, Sensitivity Analysis, or Golden Section."
            );


            AppendWelcomeStep(
                "3",
                "Review the Solution",
                "OPTIMA will display iterations, tableaux, pivot operations, " +
                "decision variables, and the objective value."
            );


            AppendWelcomeStep(
                "4",
                "Export Your Results",
                "Use Export Results to save the problem and solution " +
                "to an Excel workbook."
            );


            // ========================================================
            // FOOTER
            // ========================================================

            txtSolutionOutput.SelectionAlignment =
                HorizontalAlignment.Center;

            txtSolutionOutput.SelectionIndent =
                0;

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    9F,
                    FontStyle.Italic
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(148, 163, 184);

            txtSolutionOutput.AppendText(
                Environment.NewLine +
                "Select a solver method from the sidebar to begin."
            );


            // Return scroll position to the top.
            txtSolutionOutput.SelectionStart =
                0;

            txtSolutionOutput.ScrollToCaret();
        }


        // ============================================================
        // WELCOME SCREEN STEP
        // ============================================================

        private void AppendWelcomeStep(
            string number,
            string title,
            string description)
        {
            // --------------------------------------------------------
            // NUMBER
            // --------------------------------------------------------

            txtSolutionOutput.SelectionAlignment =
                HorizontalAlignment.Left;

            txtSolutionOutput.SelectionIndent =
                80;

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    11F,
                    FontStyle.Bold
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(37, 99, 235);

            txtSolutionOutput.AppendText(
                number + ".  "
            );


            // --------------------------------------------------------
            // TITLE
            // --------------------------------------------------------

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    11F,
                    FontStyle.Bold
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(15, 23, 42);

            txtSolutionOutput.AppendText(
                title +
                Environment.NewLine
            );


            // --------------------------------------------------------
            // DESCRIPTION
            // --------------------------------------------------------

            txtSolutionOutput.SelectionIndent =
                110;

            txtSolutionOutput.SelectionFont =
                new Font(
                    "Segoe UI",
                    9.5F,
                    FontStyle.Regular
                );

            txtSolutionOutput.SelectionColor =
                Color.FromArgb(100, 116, 139);

            txtSolutionOutput.AppendText(
                description +
                Environment.NewLine +
                Environment.NewLine
            );
        }

        // ============================================================
        // STATUS - READY
        // ============================================================

        private void SetStatusReady()
        {
            lblStatus.Text =
                "Ready";

            lblStatus.ForeColor =
                Color.FromArgb(
                    148,
                    163,
                    184
                );
        }


        // ============================================================
        // STATUS - WORKING
        // ============================================================

        private void SetStatusWorking(
            string message)
        {
            lblStatus.Text =
                message;

            lblStatus.ForeColor =
                Color.FromArgb(
                    59,
                    130,
                    246
                );

            // Forces the label to update before a solver starts working.
            lblStatus.Refresh();
        }


        // ============================================================
        // STATUS - SUCCESS
        // ============================================================

        private void SetStatusSuccess(
            string message)
        {
            lblStatus.Text =
                message;

            lblStatus.ForeColor =
                Color.FromArgb(
                    34,
                    197,
                    94
                );
        }


        private void SetStatusError(
            string message)
        {
            lblStatus.Text =
                message;

            lblStatus.ForeColor =
                Color.FromArgb(
                    239,
                    68,
                    68
                );
        }

        #endregion


        // ============================================================
        // SOLUTION RENDERING
        // ============================================================

        #region RENDERING

        private void RenderSolution(
            Solution solution)
        {
            lblOutputTitle.Text =
            "Solution Output";

            txtSolutionOutput.Clear();


            if (solution == null)
            {
                TableauRenderer.AppendError(
                    txtSolutionOutput,
                    "No solution found."
                );

                return;
            }


            // --------------------------------------------------------
            // OUTPUT BLOCKS
            // --------------------------------------------------------

            if (
                solution.OutputBlocks != null &&
                solution.OutputBlocks.Count > 0)
            {
                foreach (
                    Solution.OutputBlock block
                    in solution.OutputBlocks)
                {
                    switch (block.Type)
                    {
                        case Solution.OutputBlockType.Text:

                            if (
                                !string.IsNullOrEmpty(
                                    block.Title))
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


                txtSolutionOutput.SelectionStart =
                    0;

                txtSolutionOutput.ScrollToCaret();

                return;
            }


            // --------------------------------------------------------
            // MESSAGES
            // --------------------------------------------------------

            if (
                solution.Messages != null &&
                solution.Messages.Any())
            {
                foreach (
                    string message
                    in solution.Messages)
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


            // --------------------------------------------------------
            // TABLEAUX
            // --------------------------------------------------------

            if (
                solution.IterationTableaux != null &&
                solution.IterationTableaux.Any())
            {
                for (
                    int i = 0;
                    i < solution.IterationTableaux.Count;
                    i++)
                {
                    List<string> headers =
                        i <
                        solution.IterationColumnHeaders.Count
                            ? solution
                                .IterationColumnHeaders[i]
                            : null;


                    int pivotRow =
                        i <
                        solution.IterationPivotRows.Count
                            ? solution
                                .IterationPivotRows[i]
                            : -1;


                    int pivotCol =
                        i <
                        solution.IterationPivotCols.Count
                            ? solution
                                .IterationPivotCols[i]
                            : -1;


                    string title =
                        i <
                        solution.IterationMessages.Count
                            ? solution
                                .IterationMessages[i]
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

            else if (
                solution.Steps != null &&
                solution.Steps.Any())
            {
                foreach (
                    string step
                    in solution.Steps)
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


            // --------------------------------------------------------
            // FINAL ANSWER
            // --------------------------------------------------------

            TableauRenderer.AppendGroupHeader(
                txtSolutionOutput,
                "OPTIMAL SOLUTION"
            );


            if (
                solution.VariableValues != null &&
                solution.VariableValues.Any())
            {
                TableauRenderer.AppendResultValue(
                    txtSolutionOutput,
                    "Objective Value",
                    NumberFormatter.Format(
                        solution.OptimalValue
                    )
                );


                TableauRenderer.AppendPlainLine(
                    txtSolutionOutput,
                    ""
                );


                TableauRenderer.AppendSectionLabel(
                    txtSolutionOutput,
                    "Decision Variables"
                );


                foreach (
                    var value
                    in solution.VariableValues
                    .OrderBy(x => x.Key))
                {
                    TableauRenderer.AppendResultValue(
                        txtSolutionOutput,
                        value.Key,
                        NumberFormatter.Format(
                            value.Value
                        )
                    );
                }


                TableauRenderer.AppendPlainLine(
                    txtSolutionOutput,
                    ""
                );


                TableauRenderer.AppendSuccess(
                    txtSolutionOutput,
                    "Optimal solution found"
                );
            }

            else
            {
                TableauRenderer.AppendError(
                    txtSolutionOutput,
                    "No feasible solution found."
                );
            }


            txtSolutionOutput.SelectionStart =
                0;

            txtSolutionOutput.ScrollToCaret();
        }

        #endregion


        // ============================================================
        // LOAD PROBLEM
        // ============================================================

        private void btnLoadProblem_Click(
            object sender,
            EventArgs e)
        {
            if (
                openFileDialog.ShowDialog()
                != DialogResult.OK)
            {
                return;
            }


            try
            {
                txtProblemInput.Text =
                    File.ReadAllText(
                        openFileDialog.FileName
                    );


                ShowWelcomeMessage();


                SetStatusSuccess(
                    "Problem loaded"
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Load failed"
                );


                MessageBox.Show(
                    "Error loading file: " +
                    ex.Message,
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ============================================================
        // SAMPLE
        // ============================================================

        private void btnSampleProblem_Click(
            object sender,
            EventArgs e)
        {
            txtProblemInput.Text =
                "max + 5 + 4" +
                Environment.NewLine +

                "+ 6 + 4 <= 24" +
                Environment.NewLine +

                "+ 1 + 2 <= 6" +
                Environment.NewLine +

                "- 1 + 1 <= 1" +
                Environment.NewLine +

                "+ +";


            lblAlgorithmName.Text =
                "Programming Model Solver";

            lblHeaderSubtitle.Text =
                "Choose an optimisation method to solve your model.";


            ResetAlgorithmButtons();

            selectedAlgorithmButton =
                null;


            ShowWelcomeMessage();


            SetStatusSuccess(
                "Sample problem loaded"
            );
        }


        // ============================================================
        // CLEAR
        // ============================================================

        private void btnClearProblem_Click(
            object sender,
            EventArgs e)
        {
            txtProblemInput.Clear();


            lblAlgorithmName.Text =
                "Programming Model Solver";


            lblHeaderSubtitle.Text =
                "Choose an optimisation method to solve your model.";


            ResetAlgorithmButtons();

            selectedAlgorithmButton =
                null;


            ShowWelcomeMessage();

            SetStatusReady();


            txtProblemInput.Focus();
        }


        // ============================================================
        // PRIMAL SIMPLEX
        // ============================================================

        private void btnSolvePrimal_Click(
            object sender,
            EventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "Please enter or load a problem first.",
                    "Problem Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            SelectAlgorithm(
                btnSolvePrimal,
                "Primal Simplex Algorithm",
                "Solving a linear programming model using the Primal Simplex method."
            );


            SetStatusWorking(
                "Solving..."
            );


            try
            {
                LinearProgram program;

                double[] breakpoints =
                    null;


                if (
                    NonLinearRouter.IsNonLinearInput(
                        txtProblemInput.Text))
                {
                    var nlp =
                        NonLinearRouter.Parse(
                            txtProblemInput.Text
                        );


                    var pwl =
                        NonLinearToLinearConverter
                        .BuildPiecewiseLinearApproximation(
                            nlp.Expression,
                            nlp.LowerBound,
                            nlp.UpperBound,
                            nlp.Maximize,
                            nlp.Segments
                        );


                    program =
                        pwl.Program;

                    breakpoints =
                        pwl.Breakpoints;
                }

                else
                {
                    program =
                        LinearProgram.Parse(
                            txtProblemInput.Text
                        );
                }


                var solver =
                    new PrimalSimplex();


                var solution =
                    solver.Solve(
                        program
                    );


                if (breakpoints != null)
                {
                    double xValue =
                        NonLinearToLinearConverter
                        .RecoverXValue(
                            solution,
                            breakpoints
                        );


                    solution.AddMessage("");

                    solution.AddMessage(
                        "Recovered non-linear solution: " +
                        "x = " +
                        NumberFormatter.Format(xValue) +
                        ", f(x) = " +
                        NumberFormatter.Format(
                            solution.OptimalValue
                        )
                    );
                }


                RenderSolution(
                    solution
                );


                SetStatusSuccess(
                    "Optimal solution found"
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Solver failed"
                );


                MessageBox.Show(
                    "Error solving problem: " +
                    ex.Message,
                    "Solver Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ============================================================
        // REVISED SIMPLEX
        // ============================================================

        private void btnSolveRevised_Click(
            object sender,
            EventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "Please enter or load a problem first.",
                    "Problem Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            SelectAlgorithm(
                btnSolveRevised,
                "Revised Primal Simplex Algorithm",
                "Solving the model using the Revised Simplex method."
            );


            SetStatusWorking(
                "Solving..."
            );


            try
            {
                LinearProgram program;

                double[] breakpoints =
                    null;


                if (
                    NonLinearRouter.IsNonLinearInput(
                        txtProblemInput.Text))
                {
                    var nlp =
                        NonLinearRouter.Parse(
                            txtProblemInput.Text
                        );


                    var pwl =
                        NonLinearToLinearConverter
                        .BuildPiecewiseLinearApproximation(
                            nlp.Expression,
                            nlp.LowerBound,
                            nlp.UpperBound,
                            nlp.Maximize,
                            nlp.Segments
                        );


                    program =
                        pwl.Program;

                    breakpoints =
                        pwl.Breakpoints;
                }

                else
                {
                    program =
                        LinearProgram.Parse(
                            txtProblemInput.Text
                        );
                }


                var solver =
                    new RevisedPrimalSimplex();


                var solution =
                    solver.Solve(
                        program
                    );


                if (breakpoints != null)
                {
                    double xValue =
                        NonLinearToLinearConverter
                        .RecoverXValue(
                            solution,
                            breakpoints
                        );


                    solution.AddMessage("");

                    solution.AddMessage(
                        "Recovered non-linear solution: " +
                        "x = " +
                        NumberFormatter.Format(xValue) +
                        ", f(x) = " +
                        NumberFormatter.Format(
                            solution.OptimalValue
                        )
                    );
                }


                RenderSolution(
                    solution
                );


                SetStatusSuccess(
                    "Optimal solution found"
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Solver failed"
                );


                MessageBox.Show(
                    "Error solving problem: " +
                    ex.Message,
                    "Solver Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ============================================================
        // BRANCH AND BOUND
        // ============================================================

        private async void btnSolveBranchAndBound_Click(
            object sender,
            EventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "Please enter or load a problem first.",
                    "Problem Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            SelectAlgorithm(
                btnSolveBranchAndBound,
                "Branch & Bound Algorithm",
                "Solving an integer programming model using Branch and Bound."
            );


            SetStatusWorking(
                "Running Branch & Bound..."
            );


            btnSolveBranchAndBound.Enabled =
                false;


            try
            {
                LinearProgram program;

                double[] breakpoints =
                    null;


                if (
                    NonLinearRouter.IsNonLinearInput(
                        txtProblemInput.Text))
                {
                    var nlp =
                        NonLinearRouter.Parse(
                            txtProblemInput.Text
                        );


                    var pwl =
                        NonLinearToLinearConverter
                        .BuildPiecewiseLinearApproximation(
                            nlp.Expression,
                            nlp.LowerBound,
                            nlp.UpperBound,
                            nlp.Maximize,
                            nlp.Segments
                        );


                    program =
                        pwl.Program;

                    breakpoints =
                        pwl.Breakpoints;
                }

                else
                {
                    program =
                        LinearProgram.Parse(
                            txtProblemInput.Text
                        );
                }


                var solver =
                    new BranchAndBound();


                var solution =
                    await Task.Run(
                        () => solver.Solve(program)
                    );


                if (breakpoints != null)
                {
                    double xValue =
                        NonLinearToLinearConverter
                        .RecoverXValue(
                            solution,
                            breakpoints
                        );


                    solution.AddMessage("");

                    solution.AddMessage(
                        "Recovered non-linear solution: " +
                        "x = " +
                        NumberFormatter.Format(xValue) +
                        ", f(x) = " +
                        NumberFormatter.Format(
                            solution.OptimalValue
                        )
                    );
                }


                RenderSolution(
                    solution
                );


                SetStatusSuccess(
                    "Branch & Bound complete"
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Branch & Bound failed"
                );


                MessageBox.Show(
                    "Error solving problem: " +
                    ex.Message,
                    "Solver Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            finally
            {
                btnSolveBranchAndBound.Enabled =
                    true;
            }
        }


        // ============================================================
        // KNAPSACK
        // ============================================================

        private void btnSolveKnapsack_Click(
            object sender,
            EventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "Please enter or load a problem first.",
                    "Problem Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            if (
                NonLinearRouter.IsNonLinearInput(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "The Knapsack algorithm does not apply " +
                    "to non-linear problems.",
                    "Not Applicable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }


            SelectAlgorithm(
                btnSolveKnapsack,
                "Knapsack Algorithm",
                "Finding the best combination under the capacity constraint."
            );


            SetStatusWorking(
                "Solving Knapsack..."
            );


            btnSolveKnapsack.Enabled =
                false;


            try
            {
                var program =
                    LinearProgram.Parse(
                        txtProblemInput.Text
                    );


                var solver =
                    new LinearProgrammingSolver
                    .Algorithms
                    .Knapsack();


                var solution =
                    solver.Solve(
                        program
                    );


                RenderSolution(
                    solution
                );


                SetStatusSuccess(
                    "Knapsack solution found"
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Knapsack failed"
                );


                MessageBox.Show(
                    "Error solving problem: " +
                    ex.Message,
                    "Solver Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            finally
            {
                btnSolveKnapsack.Enabled =
                    true;
            }
        }


        // ============================================================
        // CUTTING PLANE
        // ============================================================

        private async void btnSolveCuttingPlane_Click(
            object sender,
            EventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "Please enter or load a problem first.",
                    "Problem Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            SelectAlgorithm(
                btnSolveCuttingPlane,
                "Cutting Plane Algorithm",
                "Solving an integer programming model using cutting-plane constraints."
            );


            SetStatusWorking(
                "Running Cutting Plane..."
            );


            btnSolveCuttingPlane.Enabled =
                false;


            try
            {
                LinearProgram program;

                double[] breakpoints =
                    null;


                if (
                    NonLinearRouter.IsNonLinearInput(
                        txtProblemInput.Text))
                {
                    var nlp =
                        NonLinearRouter.Parse(
                            txtProblemInput.Text
                        );


                    var pwl =
                        NonLinearToLinearConverter
                        .BuildPiecewiseLinearApproximation(
                            nlp.Expression,
                            nlp.LowerBound,
                            nlp.UpperBound,
                            nlp.Maximize,
                            nlp.Segments
                        );


                    program =
                        pwl.Program;

                    breakpoints =
                        pwl.Breakpoints;
                }

                else
                {
                    program =
                        LinearProgram.Parse(
                            txtProblemInput.Text
                        );
                }


                var solver =
                    new LinearProgrammingSolver
                    .Algorithms
                    .CuttingPlane();


                var solution =
                    await Task.Run(
                        () => solver.Solve(program)
                    );


                if (breakpoints != null)
                {
                    double xValue =
                        NonLinearToLinearConverter
                        .RecoverXValue(
                            solution,
                            breakpoints
                        );


                    solution.AddMessage("");

                    solution.AddMessage(
                        "Recovered non-linear solution: " +
                        "x = " +
                        NumberFormatter.Format(xValue) +
                        ", f(x) = " +
                        NumberFormatter.Format(
                            solution.OptimalValue
                        )
                    );
                }


                RenderSolution(
                    solution
                );


                SetStatusSuccess(
                    "Cutting Plane complete"
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Cutting Plane failed"
                );


                MessageBox.Show(
                    "Error solving problem: " +
                    ex.Message,
                    "Solver Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            finally
            {
                btnSolveCuttingPlane.Enabled =
                    true;
            }
        }


        // ============================================================
        // SENSITIVITY
        // ============================================================

        private void btnSolveSensitivityAnalysis_Click(
            object sender,
            EventArgs e)
        {
            SelectAlgorithm(
                btnSolveSensitivityAnalysis,
                "Sensitivity Analysis",
                "Analyse how model parameter changes affect the optimal solution."
            );


            lblStatus.Text =
                "Sensitivity Analysis selected";
        }


        // ============================================================
        // GOLDEN SECTION
        // ============================================================

        private void btnSolveGoldenSection_Click(
            object sender,
            EventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "Please enter a non-linear problem first." +
                    Environment.NewLine +
                    Environment.NewLine +
                    "Example:" +
                    Environment.NewLine +
                    "nlp min x^2" +
                    Environment.NewLine +
                    "-5 5",
                    "Problem Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }


            if (
                !NonLinearRouter.IsNonLinearInput(
                    txtProblemInput.Text))
            {
                MessageBox.Show(
                    "Golden Section Search only applies " +
                    "to non-linear problems.",
                    "Not Applicable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }


            SelectAlgorithm(
                btnSolveGoldenSection,
                "Golden Section Search",
                "Optimising a single-variable nonlinear objective."
            );


            SetStatusWorking(
                "Running Golden Section..."
            );


            try
            {
                var nlp =
                    NonLinearRouter.Parse(
                        txtProblemInput.Text
                    );


                var solver =
                    new GoldenSectionSearch();


                var solution =
                    solver.Solve(
                        nlp.Expression,
                        nlp.LowerBound,
                        nlp.UpperBound,
                        nlp.Maximize
                    );


                RenderSolution(
                    solution
                );


                SetStatusSuccess(
                    "Golden Section complete"
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Golden Section failed"
                );


                MessageBox.Show(
                    "Error solving problem: " +
                    ex.Message,
                    "Solver Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // ============================================================
        // EXCEL EXPORT
        // ============================================================

        private void btnExportResults_Click(
            object sender,
            EventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    txtSolutionOutput.Text) ||

                txtSolutionOutput.Text.StartsWith(
                    "Ready to solve.",
                    StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "There are no solved results to export yet.",
                    "Nothing to Export",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }


            if (
                saveFileDialog.ShowDialog()
                != DialogResult.OK)
            {
                return;
            }


            SetStatusWorking(
                "Exporting Excel..."
            );


            try
            {
                using (
                    XLWorkbook workbook =
                        new XLWorkbook())
                {
                    // =================================================
                    // PROBLEM INPUT SHEET
                    // =================================================

                    var problemSheet =
                        workbook.Worksheets.Add(
                            "Problem Input"
                        );


                    problemSheet.Cell("A1").Value =
                        "OPTIMA";

                    problemSheet.Cell("A1")
                        .Style.Font.Bold =
                        true;

                    problemSheet.Cell("A1")
                        .Style.Font.FontSize =
                        20;

                    problemSheet.Cell("A1")
                        .Style.Font.FontColor =
                        XLColor.FromHtml(
                            "#0F172A"
                        );


                    problemSheet.Cell("A2").Value =
                        "Problem Input";

                    problemSheet.Cell("A2")
                        .Style.Font.Bold =
                        true;

                    problemSheet.Cell("A2")
                        .Style.Font.FontColor =
                        XLColor.FromHtml(
                            "#2563EB"
                        );


                    problemSheet.Cell("A4").Value =
                        "Line";

                    problemSheet.Cell("B4").Value =
                        "Programming Model";


                    problemSheet.Range("A4:B4")
                        .Style.Font.Bold =
                        true;

                    problemSheet.Range("A4:B4")
                        .Style.Fill.BackgroundColor =
                        XLColor.FromHtml(
                            "#E2E8F0"
                        );


                    string[] problemLines =
                        txtProblemInput.Lines;


                    for (
                        int i = 0;
                        i < problemLines.Length;
                        i++)
                    {
                        problemSheet
                            .Cell(i + 5, 1)
                            .Value =
                            i + 1;


                        problemSheet
                            .Cell(i + 5, 2)
                            .Value =
                            problemLines[i];
                    }


                    problemSheet.Column(1).Width =
                        10;

                    problemSheet.Column(2).Width =
                        55;


                    problemSheet.Column(2)
                        .Style.Font.FontName =
                        "Consolas";


                    problemSheet.SheetView
                        .FreezeRows(4);


                    // =================================================
                    // SOLUTION SHEET
                    // =================================================

                    var solutionSheet =
                        workbook.Worksheets.Add(
                            "Solution Output"
                        );


                    solutionSheet.Cell("A1").Value =
                        "OPTIMA";

                    solutionSheet.Cell("A1")
                        .Style.Font.Bold =
                        true;

                    solutionSheet.Cell("A1")
                        .Style.Font.FontSize =
                        20;

                    solutionSheet.Cell("A1")
                        .Style.Font.FontColor =
                        XLColor.FromHtml(
                            "#0F172A"
                        );


                    solutionSheet.Cell("A2").Value =
                        lblAlgorithmName.Text;

                    solutionSheet.Cell("A2")
                        .Style.Font.Bold =
                        true;

                    solutionSheet.Cell("A2")
                        .Style.Font.FontColor =
                        XLColor.FromHtml(
                            "#2563EB"
                        );


                    solutionSheet.Cell("A4").Value =
                        "Solution Output";

                    solutionSheet.Cell("A4")
                        .Style.Font.Bold =
                        true;

                    solutionSheet.Cell("A4")
                        .Style.Fill.BackgroundColor =
                        XLColor.FromHtml(
                            "#E2E8F0"
                        );


                    string[] solutionLines =
                        txtSolutionOutput.Lines;


                    int row =
                        5;


                    foreach (
                        string line
                        in solutionLines)
                    {
                        if (line.Contains("\t"))
                        {
                            string[] cells =
                                line.Split('\t');


                            for (
                                int column = 0;
                                column < cells.Length;
                                column++)
                            {
                                solutionSheet.Cell(
                                    row,
                                    column + 1
                                ).Value =
                                    cells[column].Trim();
                            }
                        }

                        else
                        {
                            solutionSheet
                                .Cell(row, 1)
                                .Value =
                                line;
                        }


                        row++;
                    }


                    solutionSheet.Column(1).Width =
                        35;

                    solutionSheet.Column(2).Width =
                        18;

                    solutionSheet.Column(3).Width =
                        18;

                    solutionSheet.Column(4).Width =
                        18;

                    solutionSheet.Column(5).Width =
                        18;

                    solutionSheet.Column(6).Width =
                        18;


                    solutionSheet.SheetView
                        .FreezeRows(4);


                    workbook.SaveAs(
                        saveFileDialog.FileName
                    );
                }


                SetStatusSuccess(
                    "Excel exported successfully"
                );


                MessageBox.Show(
                    "Results exported successfully to Excel.",
                    "Export Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            catch (Exception ex)
            {
                SetStatusError(
                    "Excel export failed"
                );


                MessageBox.Show(
                    "Could not create the Excel file." +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}