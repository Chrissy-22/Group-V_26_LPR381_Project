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
        }

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
                txtSolutionOutput.Text = "Running Primal Simplex algorithm...\n\n";

                var program = LinearProgram.Parse(txtProblemInput.Text);
                var solver = new PrimalSimplex();
                var solution = solver.Solve(program);

                txtSolutionOutput.Text += solution.ToString();

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
                txtSolutionOutput.Text = "Running Revised Simplex algorithm...\n\n";

                var program = LinearProgram.Parse(txtProblemInput.Text);
                var solver = new RevisedPrimalSimplex();
                var solution = solver.Solve(program);

                txtSolutionOutput.Text += solution.ToString();
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
                btnSolveBranchAndBound.Enabled = false;
                txtSolutionOutput.Text = "Running Branch and Bound algorithm...\n\n";

                var program = LinearProgram.Parse(txtProblemInput.Text);
                var solver = new BranchAndBound();
                var solution = await Task.Run(() => solver.Solve(program));

                txtSolutionOutput.Text += solution.ToString();
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

            try
            {
                btnSolveKnapsack.Enabled = false;
                txtSolutionOutput.Text = "Running Branch and Bound Knapsack algorithm...\n\n";

                var program = LinearProgram.Parse(txtProblemInput.Text);
                var solver = new LinearProgrammingSolver.Algorithms.Knapsack();
                var solution = solver.Solve(program);

                txtSolutionOutput.Text += solution.ToString();
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
                btnSolveCuttingPlane.Enabled = false;
                txtSolutionOutput.Text = "Running Cutting Plane algorithm...\n\n";

                var program = LinearProgram.Parse(txtProblemInput.Text);
                var solver = new LinearProgrammingSolver.Algorithms.CuttingPlane();
                var solution = await Task.Run(() => solver.Solve(program));

                txtSolutionOutput.Text += solution.ToString();
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
        #endregion
    }
}
