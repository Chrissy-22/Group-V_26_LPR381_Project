namespace Group_V_26_LPR381_Project.Presentation_Layer
{
    partial class frmMainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.pnlSidebarContent = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnExportResults = new System.Windows.Forms.Button();
            this.btnClearProblem = new System.Windows.Forms.Button();
            this.btnSampleProblem = new System.Windows.Forms.Button();
            this.btnLoadProblem = new System.Windows.Forms.Button();
            this.txtProblemInput = new System.Windows.Forms.RichTextBox();
            this.lblProblemInput = new System.Windows.Forms.Label();
            this.btnSolveGoldenSection = new System.Windows.Forms.Button();
            this.btnSolveSensitivityAnalysis = new System.Windows.Forms.Button();
            this.btnSolveCuttingPlane = new System.Windows.Forms.Button();
            this.btnSolveKnapsack = new System.Windows.Forms.Button();
            this.btnSolveBranchAndBound = new System.Windows.Forms.Button();
            this.btnSolveRevised = new System.Windows.Forms.Button();
            this.btnSolvePrimal = new System.Windows.Forms.Button();
            this.lblMethods = new System.Windows.Forms.Label();
            this.pnlBrand = new System.Windows.Forms.Panel();
            this.lblBrandSubtitle = new System.Windows.Forms.Label();
            this.lblBrand = new System.Windows.Forms.Label();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlOutputCard = new System.Windows.Forms.Panel();
            this.txtSolutionOutput = new System.Windows.Forms.RichTextBox();
            this.lblOutputTitle = new System.Windows.Forms.Label();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblHeaderSubtitle = new System.Windows.Forms.Label();
            this.lblAlgorithmName = new System.Windows.Forms.Label();
            this.pnlSidebar.SuspendLayout();
            this.pnlSidebarContent.SuspendLayout();
            this.pnlBrand.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.pnlContent.SuspendLayout();
            this.pnlOutputCard.SuspendLayout();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlSidebar
            // 
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.pnlSidebar.Controls.Add(this.pnlSidebarContent);
            this.pnlSidebar.Controls.Add(this.pnlBrand);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Location = new System.Drawing.Point(0, 0);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(320, 721);
            this.pnlSidebar.TabIndex = 0;
            // 
            // pnlSidebarContent
            // 
            this.pnlSidebarContent.AutoScroll = true;
            this.pnlSidebarContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.pnlSidebarContent.Controls.Add(this.lblStatus);
            this.pnlSidebarContent.Controls.Add(this.btnExportResults);
            this.pnlSidebarContent.Controls.Add(this.btnClearProblem);
            this.pnlSidebarContent.Controls.Add(this.btnSampleProblem);
            this.pnlSidebarContent.Controls.Add(this.btnLoadProblem);
            this.pnlSidebarContent.Controls.Add(this.txtProblemInput);
            this.pnlSidebarContent.Controls.Add(this.lblProblemInput);
            this.pnlSidebarContent.Controls.Add(this.btnSolveGoldenSection);
            this.pnlSidebarContent.Controls.Add(this.btnSolveSensitivityAnalysis);
            this.pnlSidebarContent.Controls.Add(this.btnSolveCuttingPlane);
            this.pnlSidebarContent.Controls.Add(this.btnSolveKnapsack);
            this.pnlSidebarContent.Controls.Add(this.btnSolveBranchAndBound);
            this.pnlSidebarContent.Controls.Add(this.btnSolveRevised);
            this.pnlSidebarContent.Controls.Add(this.btnSolvePrimal);
            this.pnlSidebarContent.Controls.Add(this.lblMethods);
            this.pnlSidebarContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSidebarContent.Location = new System.Drawing.Point(0, 100);
            this.pnlSidebarContent.Name = "pnlSidebarContent";
            this.pnlSidebarContent.Padding = new System.Windows.Forms.Padding(18);
            this.pnlSidebarContent.Size = new System.Drawing.Size(320, 621);
            this.pnlSidebarContent.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.lblStatus.Location = new System.Drawing.Point(22, 689);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(50, 20);
            this.lblStatus.TabIndex = 14;
            this.lblStatus.Text = "Ready";
            // 
            // btnExportResults
            // 
            this.btnExportResults.Location = new System.Drawing.Point(22, 638);
            this.btnExportResults.Name = "btnExportResults";
            this.btnExportResults.Size = new System.Drawing.Size(276, 38);
            this.btnExportResults.TabIndex = 13;
            this.btnExportResults.Text = "Export Results";
            this.btnExportResults.UseVisualStyleBackColor = false;
            this.btnExportResults.Click += new System.EventHandler(this.btnExportResults_Click);
            // 
            // btnClearProblem
            // 
            this.btnClearProblem.Location = new System.Drawing.Point(212, 590);
            this.btnClearProblem.Name = "btnClearProblem";
            this.btnClearProblem.Size = new System.Drawing.Size(86, 38);
            this.btnClearProblem.TabIndex = 12;
            this.btnClearProblem.Text = "Clear";
            this.btnClearProblem.UseVisualStyleBackColor = false;
            this.btnClearProblem.Click += new System.EventHandler(this.btnClearProblem_Click);
            // 
            // btnSampleProblem
            // 
            this.btnSampleProblem.Location = new System.Drawing.Point(117, 590);
            this.btnSampleProblem.Name = "btnSampleProblem";
            this.btnSampleProblem.Size = new System.Drawing.Size(86, 38);
            this.btnSampleProblem.TabIndex = 11;
            this.btnSampleProblem.Text = "Sample";
            this.btnSampleProblem.UseVisualStyleBackColor = false;
            this.btnSampleProblem.Click += new System.EventHandler(this.btnSampleProblem_Click);
            // 
            // btnLoadProblem
            // 
            this.btnLoadProblem.Location = new System.Drawing.Point(22, 590);
            this.btnLoadProblem.Name = "btnLoadProblem";
            this.btnLoadProblem.Size = new System.Drawing.Size(86, 38);
            this.btnLoadProblem.TabIndex = 10;
            this.btnLoadProblem.Text = "Load";
            this.btnLoadProblem.UseVisualStyleBackColor = false;
            this.btnLoadProblem.Click += new System.EventHandler(this.btnLoadProblem_Click);
            // 
            // txtProblemInput
            // 
            this.txtProblemInput.AcceptsTab = true;
            this.txtProblemInput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.txtProblemInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtProblemInput.Font = new System.Drawing.Font("Consolas", 10.5F);
            this.txtProblemInput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(232)))), ((int)(((byte)(240)))));
            this.txtProblemInput.Location = new System.Drawing.Point(22, 434);
            this.txtProblemInput.Name = "txtProblemInput";
            this.txtProblemInput.Size = new System.Drawing.Size(276, 150);
            this.txtProblemInput.TabIndex = 9;
            this.txtProblemInput.Text = "";
            this.txtProblemInput.WordWrap = false;
            // 
            // lblProblemInput
            // 
            this.lblProblemInput.AutoSize = true;
            this.lblProblemInput.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblProblemInput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.lblProblemInput.Location = new System.Drawing.Point(22, 395);
            this.lblProblemInput.Name = "lblProblemInput";
            this.lblProblemInput.Size = new System.Drawing.Size(129, 20);
            this.lblProblemInput.TabIndex = 8;
            this.lblProblemInput.Text = "PROBLEM INPUT";
            // 
            // btnSolveGoldenSection
            // 
            this.btnSolveGoldenSection.Location = new System.Drawing.Point(18, 338);
            this.btnSolveGoldenSection.Name = "btnSolveGoldenSection";
            this.btnSolveGoldenSection.Size = new System.Drawing.Size(284, 42);
            this.btnSolveGoldenSection.TabIndex = 7;
            this.btnSolveGoldenSection.Text = "  Non-Linear Problems";
            this.btnSolveGoldenSection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSolveGoldenSection.UseVisualStyleBackColor = true;
            this.btnSolveGoldenSection.Click += new System.EventHandler(this.btnSolveGoldenSection_Click);
            // 
            // btnSolveSensitivityAnalysis
            // 
            this.btnSolveSensitivityAnalysis.Location = new System.Drawing.Point(18, 290);
            this.btnSolveSensitivityAnalysis.Name = "btnSolveSensitivityAnalysis";
            this.btnSolveSensitivityAnalysis.Size = new System.Drawing.Size(284, 42);
            this.btnSolveSensitivityAnalysis.TabIndex = 6;
            this.btnSolveSensitivityAnalysis.Text = "  Sensitivity Analysis";
            this.btnSolveSensitivityAnalysis.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSolveSensitivityAnalysis.UseVisualStyleBackColor = true;
            this.btnSolveSensitivityAnalysis.Click += new System.EventHandler(this.btnSolveSensitivityAnalysis_Click);
            // 
            // btnSolveCuttingPlane
            // 
            this.btnSolveCuttingPlane.Location = new System.Drawing.Point(18, 242);
            this.btnSolveCuttingPlane.Name = "btnSolveCuttingPlane";
            this.btnSolveCuttingPlane.Size = new System.Drawing.Size(284, 42);
            this.btnSolveCuttingPlane.TabIndex = 5;
            this.btnSolveCuttingPlane.Text = "  Cutting Plane";
            this.btnSolveCuttingPlane.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSolveCuttingPlane.UseVisualStyleBackColor = true;
            this.btnSolveCuttingPlane.Click += new System.EventHandler(this.btnSolveCuttingPlane_Click);
            // 
            // btnSolveKnapsack
            // 
            this.btnSolveKnapsack.Location = new System.Drawing.Point(18, 194);
            this.btnSolveKnapsack.Name = "btnSolveKnapsack";
            this.btnSolveKnapsack.Size = new System.Drawing.Size(284, 42);
            this.btnSolveKnapsack.TabIndex = 4;
            this.btnSolveKnapsack.Text = "  Knapsack";
            this.btnSolveKnapsack.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSolveKnapsack.UseVisualStyleBackColor = true;
            this.btnSolveKnapsack.Click += new System.EventHandler(this.btnSolveKnapsack_Click);
            // 
            // btnSolveBranchAndBound
            // 
            this.btnSolveBranchAndBound.Location = new System.Drawing.Point(18, 146);
            this.btnSolveBranchAndBound.Name = "btnSolveBranchAndBound";
            this.btnSolveBranchAndBound.Size = new System.Drawing.Size(284, 42);
            this.btnSolveBranchAndBound.TabIndex = 3;
            this.btnSolveBranchAndBound.Text = "  Branch && Bound";
            this.btnSolveBranchAndBound.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSolveBranchAndBound.UseVisualStyleBackColor = true;
            this.btnSolveBranchAndBound.Click += new System.EventHandler(this.btnSolveBranchAndBound_Click);
            // 
            // btnSolveRevised
            // 
            this.btnSolveRevised.Location = new System.Drawing.Point(18, 98);
            this.btnSolveRevised.Name = "btnSolveRevised";
            this.btnSolveRevised.Size = new System.Drawing.Size(284, 42);
            this.btnSolveRevised.TabIndex = 2;
            this.btnSolveRevised.Text = "  Revised Simplex";
            this.btnSolveRevised.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSolveRevised.UseVisualStyleBackColor = true;
            this.btnSolveRevised.Click += new System.EventHandler(this.btnSolveRevised_Click);
            // 
            // btnSolvePrimal
            // 
            this.btnSolvePrimal.Location = new System.Drawing.Point(18, 50);
            this.btnSolvePrimal.Name = "btnSolvePrimal";
            this.btnSolvePrimal.Size = new System.Drawing.Size(284, 42);
            this.btnSolvePrimal.TabIndex = 1;
            this.btnSolvePrimal.Text = "  Primal Simplex";
            this.btnSolvePrimal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSolvePrimal.UseVisualStyleBackColor = true;
            this.btnSolvePrimal.Click += new System.EventHandler(this.btnSolvePrimal_Click);
            // 
            // lblMethods
            // 
            this.lblMethods.AutoSize = true;
            this.lblMethods.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMethods.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.lblMethods.Location = new System.Drawing.Point(22, 20);
            this.lblMethods.Name = "lblMethods";
            this.lblMethods.Size = new System.Drawing.Size(139, 20);
            this.lblMethods.TabIndex = 0;
            this.lblMethods.Text = "SOLVER METHODS";
            // 
            // pnlBrand
            // 
            this.pnlBrand.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(18)))), ((int)(((byte)(32)))));
            this.pnlBrand.Controls.Add(this.lblBrandSubtitle);
            this.pnlBrand.Controls.Add(this.lblBrand);
            this.pnlBrand.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBrand.Location = new System.Drawing.Point(0, 0);
            this.pnlBrand.Name = "pnlBrand";
            this.pnlBrand.Size = new System.Drawing.Size(320, 100);
            this.pnlBrand.TabIndex = 0;
            // 
            // lblBrandSubtitle
            // 
            this.lblBrandSubtitle.AutoSize = true;
            this.lblBrandSubtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblBrandSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(163)))), ((int)(((byte)(184)))));
            this.lblBrandSubtitle.Location = new System.Drawing.Point(25, 61);
            this.lblBrandSubtitle.Name = "lblBrandSubtitle";
            this.lblBrandSubtitle.Size = new System.Drawing.Size(202, 21);
            this.lblBrandSubtitle.TabIndex = 1;
            this.lblBrandSubtitle.Text = "Linear Programming Solver";
            // 
            // lblBrand
            // 
            this.lblBrand.AutoSize = true;
            this.lblBrand.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblBrand.ForeColor = System.Drawing.Color.White;
            this.lblBrand.Location = new System.Drawing.Point(22, 18);
            this.lblBrand.Name = "lblBrand";
            this.lblBrand.Size = new System.Drawing.Size(168, 50);
            this.lblBrand.TabIndex = 0;
            this.lblBrand.Text = "OPTIMA";
            // 
            // pnlMain
            // 
            this.pnlMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.pnlMain.Controls.Add(this.pnlContent);
            this.pnlMain.Controls.Add(this.pnlHeader);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(320, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(864, 721);
            this.pnlMain.TabIndex = 1;
            // 
            // pnlContent
            // 
            this.pnlContent.AutoScroll = true;
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.pnlContent.Controls.Add(this.pnlOutputCard);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 105);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new System.Windows.Forms.Padding(35);
            this.pnlContent.Size = new System.Drawing.Size(864, 616);
            this.pnlContent.TabIndex = 1;
            // 
            // pnlOutputCard
            // 
            this.pnlOutputCard.BackColor = System.Drawing.Color.White;
            this.pnlOutputCard.Controls.Add(this.txtSolutionOutput);
            this.pnlOutputCard.Controls.Add(this.lblOutputTitle);
            this.pnlOutputCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlOutputCard.Location = new System.Drawing.Point(35, 35);
            this.pnlOutputCard.Name = "pnlOutputCard";
            this.pnlOutputCard.Padding = new System.Windows.Forms.Padding(25, 60, 25, 25);
            this.pnlOutputCard.Size = new System.Drawing.Size(794, 546);
            this.pnlOutputCard.TabIndex = 0;
            // 
            // txtSolutionOutput
            // 
            this.txtSolutionOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.txtSolutionOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtSolutionOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSolutionOutput.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtSolutionOutput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.txtSolutionOutput.Location = new System.Drawing.Point(25, 60);
            this.txtSolutionOutput.Name = "txtSolutionOutput";
            this.txtSolutionOutput.ReadOnly = true;
            this.txtSolutionOutput.Size = new System.Drawing.Size(744, 461);
            this.txtSolutionOutput.TabIndex = 1;
            this.txtSolutionOutput.Text = "";
            this.txtSolutionOutput.WordWrap = false;
            // 
            // lblOutputTitle
            // 
            this.lblOutputTitle.AutoSize = true;
            this.lblOutputTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblOutputTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblOutputTitle.Location = new System.Drawing.Point(25, 20);
            this.lblOutputTitle.Name = "lblOutputTitle";
            this.lblOutputTitle.Size = new System.Drawing.Size(164, 28);
            this.lblOutputTitle.TabIndex = 0;
            this.lblOutputTitle.Text = "Solution Output";
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.lblHeaderSubtitle);
            this.pnlHeader.Controls.Add(this.lblAlgorithmName);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(864, 105);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblHeaderSubtitle
            // 
            this.lblHeaderSubtitle.AutoSize = true;
            this.lblHeaderSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblHeaderSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblHeaderSubtitle.Location = new System.Drawing.Point(38, 67);
            this.lblHeaderSubtitle.Name = "lblHeaderSubtitle";
            this.lblHeaderSubtitle.Size = new System.Drawing.Size(416, 23);
            this.lblHeaderSubtitle.TabIndex = 1;
            this.lblHeaderSubtitle.Text = "Choose an optimisation method to solve your model.";
            // 
            // lblAlgorithmName
            // 
            this.lblAlgorithmName.AutoSize = true;
            this.lblAlgorithmName.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblAlgorithmName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.lblAlgorithmName.Location = new System.Drawing.Point(35, 20);
            this.lblAlgorithmName.Name = "lblAlgorithmName";
            this.lblAlgorithmName.Size = new System.Drawing.Size(507, 50);
            this.lblAlgorithmName.TabIndex = 0;
            this.lblAlgorithmName.Text = "Programming Model Solver";
            // 
            // frmMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1184, 721);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlSidebar);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1050, 760);
            this.Name = "frmMainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OPTIMA - Linear Programming Solver";
            this.pnlSidebar.ResumeLayout(false);
            this.pnlSidebarContent.ResumeLayout(false);
            this.pnlSidebarContent.PerformLayout();
            this.pnlBrand.ResumeLayout(false);
            this.pnlBrand.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.pnlContent.ResumeLayout(false);
            this.pnlOutputCard.ResumeLayout(false);
            this.pnlOutputCard.PerformLayout();
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion


        // ============================================================
        // CONTROL DECLARATIONS
        // ============================================================

        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel pnlSidebarContent;
        private System.Windows.Forms.Panel pnlBrand;

        private System.Windows.Forms.Label lblBrand;
        private System.Windows.Forms.Label lblBrandSubtitle;

        private System.Windows.Forms.Label lblMethods;
        private System.Windows.Forms.Label lblProblemInput;
        private System.Windows.Forms.Label lblStatus;

        private System.Windows.Forms.Button btnSolvePrimal;
        private System.Windows.Forms.Button btnSolveRevised;
        private System.Windows.Forms.Button btnSolveBranchAndBound;
        private System.Windows.Forms.Button btnSolveKnapsack;
        private System.Windows.Forms.Button btnSolveCuttingPlane;
        private System.Windows.Forms.Button btnSolveSensitivityAnalysis;
        private System.Windows.Forms.Button btnSolveGoldenSection;

        private System.Windows.Forms.RichTextBox txtProblemInput;

        private System.Windows.Forms.Button btnLoadProblem;
        private System.Windows.Forms.Button btnSampleProblem;
        private System.Windows.Forms.Button btnClearProblem;
        private System.Windows.Forms.Button btnExportResults;

        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Panel pnlHeader;

        private System.Windows.Forms.Label lblAlgorithmName;
        private System.Windows.Forms.Label lblHeaderSubtitle;

        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.Panel pnlOutputCard;

        private System.Windows.Forms.Label lblOutputTitle;

        private System.Windows.Forms.RichTextBox txtSolutionOutput;
    }
}