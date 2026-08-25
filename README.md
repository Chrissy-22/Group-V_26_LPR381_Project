# OPTIMA – Linear Programming Solver

OPTIMA is a Windows desktop application developed in C# for solving and displaying Linear Programming and optimisation problems.

The application provides a user-friendly interface for entering optimisation models, selecting an appropriate solving method, viewing step-by-step results, and exporting results to Microsoft Excel.

## Features

* Modern Windows Forms user interface
* Manual problem input
* Load optimisation problems from `.txt` files
* Built-in sample problem
* Clear/reset functionality
* Step-by-step solution output
* Tableau visualisation
* Pivot row and pivot column highlighting
* Status indicators while solving
* Export results to `.xlsx` Excel workbooks
* Support for linear, integer, and selected non-linear optimisation methods

## Solver Methods

OPTIMA includes the following optimisation methods:

* Primal Simplex
* Revised Primal Simplex
* Branch and Bound
* Knapsack
* Cutting Plane
* Sensitivity Analysis
* Golden Section Search

## Technologies Used

| Technology           | Purpose                                     |
| -------------------- | ------------------------------------------- |
| C#                   | Main programming language                   |
| .NET / Windows Forms | Desktop application and graphical interface |
| ClosedXML            | Creation and export of Excel `.xlsx` files  |
| Git                  | Version control                             |
| GitHub               | Source-code hosting and collaboration       |
| Visual Studio        | Development environment                     |

## Problem Input Format

Linear Programming problems are entered using the OPTIMA model format.

Example:

```text
max + 5 + 4
+ 6 + 4 <= 24
+ 1 + 2 <= 6
- 1 + 1 <= 1
+ +
```

This represents:

```text
Maximise:
Z = 5x1 + 4x2

Subject to:

6x1 + 4x2 <= 24
x1 + 2x2 <= 6
-x1 + x2 <= 1

x1, x2 >= 0
```

The built-in **Sample** button automatically inserts a valid example into the Problem Input area.

## Using OPTIMA

1. Launch the application.
2. Enter a programming model in the **Problem Input** area, load a `.txt` file, or click **Sample**.
3. Select a solver method from the sidebar.
4. OPTIMA processes the model and displays the solution.
5. Review the iterations, tableaux, pivot operations, and final result.
6. Click **Export Results** to save the results as an Excel workbook.

## Excel Export

OPTIMA can export solved problems directly to an `.xlsx` file using ClosedXML.

The generated workbook contains:

### Problem Input

Contains the original optimisation model entered by the user.

### Solution Output

Contains the selected algorithm and the generated solution information, including iterations and results.

Example structure:

```text
OPTIMA_Results.xlsx
│
├── Problem Input
│   └── Original programming model
│
└── Solution Output
    ├── Algorithm
    ├── Iterations
    ├── Tableaux
    └── Final solution
```

Microsoft Excel does not need to be installed for OPTIMA to create the workbook.

## User Interface

The OPTIMA interface consists of three main areas.

### Solver Navigation

The left sidebar provides access to the available optimisation methods.

### Problem Input

Users can:

* Enter a model manually
* Load a problem from a file
* Insert a sample problem
* Clear the current problem
* Export completed results

### Solution Output

The main output area displays:

* Solver iterations
* Tableau information
* Pivot rows and columns
* Intermediate calculations
* Decision variable values
* Optimal objective value

## Project Structure

```text
Group-V_26_LPR381_Project
│
├── Algorithms
│   ├── BranchAndBound.cs
│   ├── ConstraintHandler.cs
│   ├── CuttingPlane.cs
│   ├── DualSimplex.cs
│   ├── GoldenSectionSearch.cs
│   ├── ISolver.cs
│   ├── Knapsack.cs
│   ├── PrimalSimplex.cs
│   ├── RevisedPrimalSimplex.cs
│   └── SensitivityAnalysis.cs
│
├── Models
│   ├── LinearProgram.cs
│   ├── NonLinearFunctions.cs
│   ├── NonLinearRouter.cs
│   ├── NonLinearToLinearConverter.cs
│   ├── NumberFormatter.cs
│   └── Solution.cs
│
├── Presentation Layer
│   ├── frmMainForm.cs
│   ├── frmMainForm.Designer.cs
│   ├── frmMainForm.resx
│   └── TableauRenderer.cs
│
├── Data
├── App.config
├── packages.config
└── Program.cs
```

## Running the Project

### Requirements

* Windows
* Visual Studio
* .NET Framework supported by the project
* NuGet package restoration enabled

### Steps

Clone the repository:

```bash
git clone https://github.com/Chrissy-22/Group-V_26_LPR381_Project.git
```

Open the solution in Visual Studio.

Restore NuGet packages if required.

Ensure the `ClosedXML` package is installed:

```powershell
Install-Package ClosedXML
```

Build the project:

```text
Build → Rebuild Solution
```

Run the application using:

```text
F5
```

## ClosedXML

Excel export is implemented using the ClosedXML NuGet package.

If the package is missing, install it using Visual Studio:

```text
Tools
→ NuGet Package Manager
→ Package Manager Console
```

Then run:

```powershell
Install-Package ClosedXML
```

## Example Result

For the model:

```text
max + 5 + 4
+ 6 + 4 <= 24
+ 1 + 2 <= 6
- 1 + 1 <= 1
+ +
```

OPTIMA can determine the optimal solution:

```text
x1 = 3
x2 = 1.5

Optimal Objective Value = 21
```

## Version Control

The project uses Git and GitHub for version control.

Recommended development workflow:

```text
Make Changes
     ↓
Build and Test
     ↓
Commit
     ↓
Pull
     ↓
Resolve Conflicts
     ↓
Push
     ↓
Verify on GitHub
```

Always build and test the project before committing major changes.

## Project Purpose

This project was developed as part of the LPR381 Linear Programming module. Its purpose is to demonstrate the practical implementation of optimisation algorithms while providing understandable intermediate calculations and results through a graphical desktop application.

## License

This project was developed for academic and educational purposes.
