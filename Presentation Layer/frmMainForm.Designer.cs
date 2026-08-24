namespace Group_V_26_LPR381_Project.Presentation_Layer
{
    partial class frmMainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.tblHeader = new System.Windows.Forms.TableLayoutPanel();
            this.lblAlgorithmName = new System.Windows.Forms.Label();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.tblUserControls = new System.Windows.Forms.TableLayoutPanel();
            this.btnSolveGoldenSection = new System.Windows.Forms.Button();
            this.btnExportResults = new System.Windows.Forms.Button();
            this.btnSolveSensitivityAnalysis = new System.Windows.Forms.Button();
            this.btnSolveCuttingPlane = new System.Windows.Forms.Button();
            this.btnSolveKnapsack = new System.Windows.Forms.Button();
            this.btnSolveBranchAndBound = new System.Windows.Forms.Button();
            this.btnSolveRevised = new System.Windows.Forms.Button();
            this.btnSolvePrimal = new System.Windows.Forms.Button();
            this.btnLoadProblem = new System.Windows.Forms.Button();
            this.txtProblemInput = new System.Windows.Forms.RichTextBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.txtSolutionOutput = new System.Windows.Forms.RichTextBox();
            this.pnlHeader.SuspendLayout();
            this.tblHeader.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.tblUserControls.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.Controls.Add(this.tblHeader);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(319, 0);
            this.pnlHeader.Margin = new System.Windows.Forms.Padding(0);
            this.pnlHeader.MaximumSize = new System.Drawing.Size(0, 40);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(665, 40);
            this.pnlHeader.TabIndex = 0;
            // 
            // tblHeader
            // 
            this.tblHeader.ColumnCount = 1;
            this.tblHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblHeader.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblHeader.Controls.Add(this.lblAlgorithmName, 0, 0);
            this.tblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblHeader.Location = new System.Drawing.Point(0, 0);
            this.tblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.tblHeader.Name = "tblHeader";
            this.tblHeader.RowCount = 1;
            this.tblHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblHeader.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblHeader.Size = new System.Drawing.Size(665, 40);
            this.tblHeader.TabIndex = 0;
            // 
            // lblAlgorithmName
            // 
            this.lblAlgorithmName.AutoSize = true;
            this.lblAlgorithmName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlgorithmName.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAlgorithmName.Location = new System.Drawing.Point(0, 0);
            this.lblAlgorithmName.Margin = new System.Windows.Forms.Padding(0);
            this.lblAlgorithmName.Name = "lblAlgorithmName";
            this.lblAlgorithmName.Size = new System.Drawing.Size(665, 40);
            this.lblAlgorithmName.TabIndex = 0;
            this.lblAlgorithmName.Text = "Algorithm name";
            this.lblAlgorithmName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.tblUserControls);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlButtons.Location = new System.Drawing.Point(0, 0);
            this.pnlButtons.Margin = new System.Windows.Forms.Padding(0);
            this.pnlButtons.MinimumSize = new System.Drawing.Size(200, 0);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(309, 585);
            this.pnlButtons.TabIndex = 1;
            // 
            // tblUserControls
            // 
            this.tblUserControls.ColumnCount = 2;
            this.tblUserControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUserControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblUserControls.Controls.Add(this.btnSolveGoldenSection, 0, 8);
            this.tblUserControls.Controls.Add(this.btnExportResults, 1, 9);
            this.tblUserControls.Controls.Add(this.btnSolveSensitivityAnalysis, 0, 7);
            this.tblUserControls.Controls.Add(this.btnSolveCuttingPlane, 0, 6);
            this.tblUserControls.Controls.Add(this.btnSolveKnapsack, 0, 5);
            this.tblUserControls.Controls.Add(this.btnSolveBranchAndBound, 0, 4);
            this.tblUserControls.Controls.Add(this.btnSolveRevised, 1, 2);
            this.tblUserControls.Controls.Add(this.btnSolvePrimal, 0, 2);
            this.tblUserControls.Controls.Add(this.btnLoadProblem, 0, 0);
            this.tblUserControls.Controls.Add(this.txtProblemInput, 0, 1);
            this.tblUserControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblUserControls.Location = new System.Drawing.Point(0, 0);
            this.tblUserControls.Margin = new System.Windows.Forms.Padding(0);
            this.tblUserControls.Name = "tblUserControls";
            this.tblUserControls.RowCount = 10;
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tblUserControls.Size = new System.Drawing.Size(309, 585);
            this.tblUserControls.TabIndex = 0;
            // 
            // btnSolveGoldenSection
            // 
            this.btnSolveGoldenSection.BackColor = System.Drawing.Color.Gold;
            this.tblUserControls.SetColumnSpan(this.btnSolveGoldenSection, 2);
            this.btnSolveGoldenSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSolveGoldenSection.FlatAppearance.BorderSize = 0;
            this.btnSolveGoldenSection.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnSolveGoldenSection.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnSolveGoldenSection.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSolveGoldenSection.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSolveGoldenSection.ForeColor = System.Drawing.Color.White;
            this.btnSolveGoldenSection.Location = new System.Drawing.Point(5, 510);
            this.btnSolveGoldenSection.Margin = new System.Windows.Forms.Padding(5);
            this.btnSolveGoldenSection.Name = "btnSolveGoldenSection";
            this.btnSolveGoldenSection.Size = new System.Drawing.Size(299, 30);
            this.btnSolveGoldenSection.TabIndex = 10;
            this.btnSolveGoldenSection.Text = "Non-Linear Problems";
            this.btnSolveGoldenSection.UseVisualStyleBackColor = false;
            this.btnSolveGoldenSection.Click += new System.EventHandler(this.btnSolveGoldenSection_Click);
            // 
            // btnExportResults
            // 
            this.btnExportResults.BackColor = System.Drawing.Color.Transparent;
            this.btnExportResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExportResults.FlatAppearance.BorderColor = System.Drawing.Color.Green;
            this.btnExportResults.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(134)))), ((int)(((byte)(187)))), ((int)(((byte)(216)))));
            this.btnExportResults.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(93)))), ((int)(((byte)(144)))), ((int)(((byte)(177)))));
            this.btnExportResults.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportResults.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportResults.ForeColor = System.Drawing.Color.Green;
            this.btnExportResults.Location = new System.Drawing.Point(159, 550);
            this.btnExportResults.Margin = new System.Windows.Forms.Padding(5);
            this.btnExportResults.Name = "btnExportResults";
            this.btnExportResults.Size = new System.Drawing.Size(145, 30);
            this.btnExportResults.TabIndex = 8;
            this.btnExportResults.Text = "Export";
            this.btnExportResults.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnExportResults.UseVisualStyleBackColor = false;
            this.btnExportResults.Click += new System.EventHandler(this.btnExportResults_Click);
            // 
            // btnSolveSensitivityAnalysis
            // 
            this.btnSolveSensitivityAnalysis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.tblUserControls.SetColumnSpan(this.btnSolveSensitivityAnalysis, 2);
            this.btnSolveSensitivityAnalysis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSolveSensitivityAnalysis.FlatAppearance.BorderSize = 0;
            this.btnSolveSensitivityAnalysis.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnSolveSensitivityAnalysis.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnSolveSensitivityAnalysis.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSolveSensitivityAnalysis.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSolveSensitivityAnalysis.ForeColor = System.Drawing.Color.White;
            this.btnSolveSensitivityAnalysis.Location = new System.Drawing.Point(5, 470);
            this.btnSolveSensitivityAnalysis.Margin = new System.Windows.Forms.Padding(5);
            this.btnSolveSensitivityAnalysis.Name = "btnSolveSensitivityAnalysis";
            this.btnSolveSensitivityAnalysis.Size = new System.Drawing.Size(299, 30);
            this.btnSolveSensitivityAnalysis.TabIndex = 7;
            this.btnSolveSensitivityAnalysis.Text = "Sensitivity Analysis";
            this.btnSolveSensitivityAnalysis.UseVisualStyleBackColor = false;
            this.btnSolveSensitivityAnalysis.Click += new System.EventHandler(this.btnSolveSensitivityAnalysis_Click);
            // 
            // btnSolveCuttingPlane
            // 
            this.btnSolveCuttingPlane.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(111)))), ((int)(((byte)(186)))), ((int)(((byte)(49)))));
            this.tblUserControls.SetColumnSpan(this.btnSolveCuttingPlane, 2);
            this.btnSolveCuttingPlane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSolveCuttingPlane.FlatAppearance.BorderSize = 0;
            this.btnSolveCuttingPlane.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnSolveCuttingPlane.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnSolveCuttingPlane.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSolveCuttingPlane.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSolveCuttingPlane.ForeColor = System.Drawing.Color.White;
            this.btnSolveCuttingPlane.Location = new System.Drawing.Point(5, 430);
            this.btnSolveCuttingPlane.Margin = new System.Windows.Forms.Padding(5);
            this.btnSolveCuttingPlane.Name = "btnSolveCuttingPlane";
            this.btnSolveCuttingPlane.Size = new System.Drawing.Size(299, 30);
            this.btnSolveCuttingPlane.TabIndex = 6;
            this.btnSolveCuttingPlane.Text = "Cutting Plane Algorithm";
            this.btnSolveCuttingPlane.UseVisualStyleBackColor = false;
            this.btnSolveCuttingPlane.Click += new System.EventHandler(this.btnSolveCuttingPlane_Click);
            // 
            // btnSolveKnapsack
            // 
            this.btnSolveKnapsack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(137)))), ((int)(((byte)(35)))));
            this.tblUserControls.SetColumnSpan(this.btnSolveKnapsack, 2);
            this.btnSolveKnapsack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSolveKnapsack.FlatAppearance.BorderSize = 0;
            this.btnSolveKnapsack.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnSolveKnapsack.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnSolveKnapsack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSolveKnapsack.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSolveKnapsack.ForeColor = System.Drawing.Color.White;
            this.btnSolveKnapsack.Location = new System.Drawing.Point(5, 390);
            this.btnSolveKnapsack.Margin = new System.Windows.Forms.Padding(5);
            this.btnSolveKnapsack.Name = "btnSolveKnapsack";
            this.btnSolveKnapsack.Size = new System.Drawing.Size(299, 30);
            this.btnSolveKnapsack.TabIndex = 5;
            this.btnSolveKnapsack.Text = "Knapsack Algorithm";
            this.btnSolveKnapsack.UseVisualStyleBackColor = false;
            this.btnSolveKnapsack.Click += new System.EventHandler(this.btnSolveKnapsack_Click);
            // 
            // btnSolveBranchAndBound
            // 
            this.btnSolveBranchAndBound.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(100)))), ((int)(((byte)(25)))));
            this.tblUserControls.SetColumnSpan(this.btnSolveBranchAndBound, 2);
            this.btnSolveBranchAndBound.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSolveBranchAndBound.FlatAppearance.BorderSize = 0;
            this.btnSolveBranchAndBound.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnSolveBranchAndBound.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnSolveBranchAndBound.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSolveBranchAndBound.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSolveBranchAndBound.ForeColor = System.Drawing.Color.White;
            this.btnSolveBranchAndBound.Location = new System.Drawing.Point(5, 350);
            this.btnSolveBranchAndBound.Margin = new System.Windows.Forms.Padding(5);
            this.btnSolveBranchAndBound.Name = "btnSolveBranchAndBound";
            this.btnSolveBranchAndBound.Size = new System.Drawing.Size(299, 30);
            this.btnSolveBranchAndBound.TabIndex = 4;
            this.btnSolveBranchAndBound.Text = "Branch and Bound Algorithm";
            this.btnSolveBranchAndBound.UseVisualStyleBackColor = false;
            this.btnSolveBranchAndBound.Click += new System.EventHandler(this.btnSolveBranchAndBound_Click);
            // 
            // btnSolveRevised
            // 
            this.btnSolveRevised.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(197)))), ((int)(((byte)(183)))));
            this.tblUserControls.SetColumnSpan(this.btnSolveRevised, 2);
            this.btnSolveRevised.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSolveRevised.FlatAppearance.BorderSize = 0;
            this.btnSolveRevised.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnSolveRevised.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnSolveRevised.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSolveRevised.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSolveRevised.ForeColor = System.Drawing.Color.White;
            this.btnSolveRevised.Location = new System.Drawing.Point(5, 310);
            this.btnSolveRevised.Margin = new System.Windows.Forms.Padding(5);
            this.btnSolveRevised.Name = "btnSolveRevised";
            this.btnSolveRevised.Size = new System.Drawing.Size(299, 30);
            this.btnSolveRevised.TabIndex = 3;
            this.btnSolveRevised.Text = "Revised Primal Simplex";
            this.btnSolveRevised.UseVisualStyleBackColor = false;
            this.btnSolveRevised.Click += new System.EventHandler(this.btnSolveRevised_Click);
            // 
            // btnSolvePrimal
            // 
            this.btnSolvePrimal.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(149)))), ((int)(((byte)(161)))));
            this.tblUserControls.SetColumnSpan(this.btnSolvePrimal, 2);
            this.btnSolvePrimal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSolvePrimal.FlatAppearance.BorderSize = 0;
            this.btnSolvePrimal.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnSolvePrimal.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnSolvePrimal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSolvePrimal.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSolvePrimal.ForeColor = System.Drawing.Color.White;
            this.btnSolvePrimal.Location = new System.Drawing.Point(5, 270);
            this.btnSolvePrimal.Margin = new System.Windows.Forms.Padding(5);
            this.btnSolvePrimal.Name = "btnSolvePrimal";
            this.btnSolvePrimal.Size = new System.Drawing.Size(299, 30);
            this.btnSolvePrimal.TabIndex = 2;
            this.btnSolvePrimal.Text = "Primal Simplex";
            this.btnSolvePrimal.UseVisualStyleBackColor = false;
            this.btnSolvePrimal.Click += new System.EventHandler(this.btnSolvePrimal_Click);
            // 
            // btnLoadProblem
            // 
            this.btnLoadProblem.BackColor = System.Drawing.Color.ForestGreen;
            this.tblUserControls.SetColumnSpan(this.btnLoadProblem, 2);
            this.btnLoadProblem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadProblem.FlatAppearance.BorderSize = 0;
            this.btnLoadProblem.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Gray;
            this.btnLoadProblem.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.btnLoadProblem.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadProblem.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLoadProblem.ForeColor = System.Drawing.Color.White;
            this.btnLoadProblem.Location = new System.Drawing.Point(5, 5);
            this.btnLoadProblem.Margin = new System.Windows.Forms.Padding(5);
            this.btnLoadProblem.Name = "btnLoadProblem";
            this.btnLoadProblem.Size = new System.Drawing.Size(299, 30);
            this.btnLoadProblem.TabIndex = 0;
            this.btnLoadProblem.Text = "Load Problem";
            this.btnLoadProblem.UseVisualStyleBackColor = false;
            this.btnLoadProblem.Click += new System.EventHandler(this.btnLoadProblem_Click);
            // 
            // txtProblemInput
            // 
            this.tblUserControls.SetColumnSpan(this.txtProblemInput, 2);
            this.txtProblemInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtProblemInput.Location = new System.Drawing.Point(0, 40);
            this.txtProblemInput.Margin = new System.Windows.Forms.Padding(0);
            this.txtProblemInput.Name = "txtProblemInput";
            this.txtProblemInput.Size = new System.Drawing.Size(309, 225);
            this.txtProblemInput.TabIndex = 1;
            this.txtProblemInput.Text = "";
            // 
            // splitter1
            // 
            this.splitter1.BackColor = System.Drawing.Color.Gray;
            this.splitter1.Location = new System.Drawing.Point(309, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(10, 585);
            this.splitter1.TabIndex = 2;
            this.splitter1.TabStop = false;
            // 
            // pnlContent
            // 
            this.pnlContent.Controls.Add(this.txtSolutionOutput);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(319, 40);
            this.pnlContent.Margin = new System.Windows.Forms.Padding(10);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(10);
            this.pnlContent.Size = new System.Drawing.Size(665, 545);
            this.pnlContent.TabIndex = 3;
            // 
            // txtSolutionOutput
            // 
            this.txtSolutionOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSolutionOutput.Location = new System.Drawing.Point(10, 10);
            this.txtSolutionOutput.Margin = new System.Windows.Forms.Padding(0);
            this.txtSolutionOutput.Name = "txtSolutionOutput";
            this.txtSolutionOutput.Size = new System.Drawing.Size(645, 525);
            this.txtSolutionOutput.TabIndex = 0;
            this.txtSolutionOutput.Text = "";
            // 
            // frmMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(984, 585);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.pnlButtons);
            this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "frmMainForm";
            this.Text = "Linear Programming Solver";
            this.pnlHeader.ResumeLayout(false);
            this.tblHeader.ResumeLayout(false);
            this.tblHeader.PerformLayout();
            this.pnlButtons.ResumeLayout(false);
            this.tblUserControls.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.TableLayoutPanel tblUserControls;
        private System.Windows.Forms.TableLayoutPanel tblHeader;
        private System.Windows.Forms.Label lblAlgorithmName;
        private System.Windows.Forms.Button btnLoadProblem;
        private System.Windows.Forms.RichTextBox txtProblemInput;
        private System.Windows.Forms.Button btnSolveRevised;
        private System.Windows.Forms.Button btnSolvePrimal;
        private System.Windows.Forms.Button btnSolveSensitivityAnalysis;
        private System.Windows.Forms.Button btnSolveCuttingPlane;
        private System.Windows.Forms.Button btnSolveKnapsack;
        private System.Windows.Forms.Button btnSolveBranchAndBound;
        private System.Windows.Forms.Button btnExportResults;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.RichTextBox txtSolutionOutput;
        private System.Windows.Forms.Button btnSolveGoldenSection;
    }
}