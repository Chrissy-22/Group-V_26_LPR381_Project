using Group_V_26_LPR381_Project.Algorithms;
using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinearProgrammingSolver.Algorithms
{
    public class CuttingPlane : ISolver
    {
        private readonly DualSimplex _dualSimplex;
        private const double TOLERANCE = 1e-6;

        public CuttingPlane()
        {
            _dualSimplex = new DualSimplex();
        }

        public Solution Solve(LinearProgram program)
        {
            var solution = new Solution();
            solution.AddStep("Canonical Form", FormatCanonicalForm(program));

            // Step 1: Solve the LP relaxation using dual simplex
            var initialSolution = _dualSimplex.Solve(program);

            if (initialSolution.Messages.Any(m => m.Contains("infeasible") || m.Contains("unbounded")))
            {
                solution.AddMessage("Initial LP relaxation is infeasible or unbounded. Cannot proceed with Cutting Plane.");
                foreach (var message in initialSolution.Messages)
                    solution.AddMessage(message);
                return solution;
            }

            // Forward the real tableau iterations (with pivot info) instead of copying plain text
            CopySimplexIterations(solution, initialSolution, "LP Relaxation");

            int cutIteration = 0;
            var currentProgram = program.Clone();
            var currentSolution = initialSolution;

            while (true)
            {
                var fractionalVar = FindMostFractionalVariable(currentSolution, currentProgram);

                if (fractionalVar == null)
                {
                    solution.OptimalValue = currentSolution.OptimalValue;
                    solution.VariableValues = currentSolution.VariableValues;
                    solution.AddMessage($"\nOptimal integer solution found with value: {NumberFormatter.Format(currentSolution.OptimalValue)}");
                    break;
                }

                cutIteration++;
                solution.AddMessage($"\nCut {cutIteration}: Variable {fractionalVar.Item1} = {NumberFormatter.Format(fractionalVar.Item2)} is fractional " +
                    $"(distance from 0.5: {NumberFormatter.Format(Math.Abs(fractionalVar.Item2 - Math.Floor(fractionalVar.Item2) - 0.5))})");

                var cut = GenerateGomoryCut(currentSolution, fractionalVar.Item1, currentProgram);
                if (cut == null)
                {
                    solution.AddMessage("Unable to generate cut. Terminating.");
                    break;
                }
                solution.AddStep($"Cut {cutIteration} Generation", cut.GenerationSteps);

                currentProgram.Constraints.Add(cut.Constraint);

                var slackVar = new LinearProgram.Variable
                {
                    Index = currentProgram.Variables.Count + 1,
                    Coefficient = 0,
                    Type = LinearProgram.VariableType.NonNegative
                };
                currentProgram.Variables.Add(slackVar);

                foreach (var constraint in currentProgram.Constraints)
                {
                    while (constraint.Coefficients.Count < currentProgram.Variables.Count)
                    {
                        constraint.Coefficients.Add(0);
                    }
                }

                cut.Constraint.Coefficients[currentProgram.Variables.Count - 1] = 1;

                solution.AddMessage($"Added cut: {FormatConstraint(cut.Constraint, currentProgram.Variables.Count - 1)}");

                // Step 4: Solve the updated LP using dual simplex
                currentSolution = _dualSimplex.Solve(currentProgram);

                if (currentSolution.Messages.Any(m => m.Contains("infeasible")))
                {
                    solution.AddMessage("Problem became infeasible after adding cut. No integer solution exists.");
                    foreach (var message in currentSolution.Messages)
                        solution.AddMessage(message);
                    return solution;
                }
                if (currentSolution.Messages.Any(m => m.Contains("unbounded")))
                {
                    solution.AddMessage("Problem became unbounded after adding cut.");
                    foreach (var message in currentSolution.Messages)
                        solution.AddMessage(message);
                    return solution;
                }

                CopySimplexIterations(solution, currentSolution, $"After Cut {cutIteration}");

                solution.AddMessage($"Cut {cutIteration} result: Optimal value = {NumberFormatter.Format(currentSolution.OptimalValue)}");

                if (cutIteration >= 50)
                {
                    solution.AddMessage("Maximum number of cuts reached. Terminating.");
                    break;
                }
            }

            return solution;
        }

        /// <summary>
        /// Copies a DualSimplex sub-solve's tableau iterations (matrix, pivot row/col, column
        /// headers) into the outer solution so the UI can render and highlight them.
        /// </summary>
        private void CopySimplexIterations(Solution target, Solution source, string labelPrefix)
        {
            for (int i = 0; i < source.IterationTableaux.Count; i++)
            {
                var headers = i < source.IterationColumnHeaders.Count ? source.IterationColumnHeaders[i] : null;
                var pivotRow = i < source.IterationPivotRows.Count ? source.IterationPivotRows[i] : -1;
                var pivotCol = i < source.IterationPivotCols.Count ? source.IterationPivotCols[i] : -1;

                string label = string.IsNullOrEmpty(labelPrefix)
                    ? source.IterationMessages[i]
                    : $"{labelPrefix} - {source.IterationMessages[i]}";

                target.AddIteration(source.IterationTableaux[i], label, pivotRow, pivotCol, headers);
            }
        }

        private Tuple<string, double> FindMostFractionalVariable(Solution solution, LinearProgram program)
        {
            string mostFractionalVar = null;
            double mostFractionalValue = 0;
            double closestToHalf = double.MaxValue;
            int lowestVarIndex = int.MaxValue;

            var decisionVars = solution.VariableValues
                .Where(kvp => kvp.Key.StartsWith("x"))
                .OrderBy(kvp => GetVariableIndex(kvp.Key))
                .ToList();

            foreach (var kvp in decisionVars)
            {
                double fractionalPart = kvp.Value - Math.Floor(kvp.Value);
                if (fractionalPart > TOLERANCE && fractionalPart < 1 - TOLERANCE)
                {
                    double distanceFromHalf = Math.Abs(fractionalPart - 0.5);
                    int varIndex = GetVariableIndex(kvp.Key);
                    if (distanceFromHalf < closestToHalf ||
                        (Math.Abs(distanceFromHalf - closestToHalf) < TOLERANCE && varIndex < lowestVarIndex))
                    {
                        closestToHalf = distanceFromHalf;
                        mostFractionalVar = kvp.Key;
                        mostFractionalValue = kvp.Value;
                        lowestVarIndex = varIndex;
                    }
                }
            }

            return mostFractionalVar != null ? Tuple.Create(mostFractionalVar, mostFractionalValue) : null;
        }

        private int GetVariableIndex(string varName)
        {
            return int.Parse(varName.Substring(1));
        }

        private CutInfo GenerateGomoryCut(Solution solution, string fractionalVarName, LinearProgram program)
        {
            var cutInfo = new CutInfo();
            var sb = new StringBuilder();

            if (solution.FinalTableau == null)
            {
                sb.AppendLine("Final tableau not available for cut generation.");
                cutInfo.GenerationSteps = sb.ToString();
                return null;
            }

            int varIndex = GetVariableIndex(fractionalVarName) - 1;
            int pivotRow = FindBasicVariableRow(solution.FinalTableau, varIndex);
            if (pivotRow == -1)
            {
                sb.AppendLine($"Variable {fractionalVarName} is not basic. Cannot generate cut.");
                cutInfo.GenerationSteps = sb.ToString();
                return null;
            }

            sb.AppendLine($"Generating Gomory cut from row {pivotRow + 1} (basic variable {fractionalVarName}):");
            sb.AppendLine();

            var tableau = solution.FinalTableau;
            int cols = tableau.GetLength(1);
            double rhs = tableau[pivotRow, cols - 1];

            sb.AppendLine("Step 1: Extract constraint equation from tableau:");
            sb.Append($"{fractionalVarName} = {NumberFormatter.Format(rhs)}");

            var coefficients = new List<double>();
            var variableNames = GetVariableNames(program, solution);

            for (int j = 0; j < cols - 1; j++)
            {
                double coeff = tableau[pivotRow, j];
                coefficients.Add(coeff);
                if (j < variableNames.Count && j != varIndex)
                {
                    if (Math.Abs(coeff) > TOLERANCE)
                    {
                        if (coeff > 0)
                            sb.Append($" + {NumberFormatter.Format(coeff)}{variableNames[j]}");
                        else
                            sb.Append($" - {NumberFormatter.Format(Math.Abs(coeff))}{variableNames[j]}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine();

            sb.AppendLine("Step 2: Split into integer and fractional parts (ensuring positive fractions):");

            var integerParts = new List<double>();
            var fractionalParts = new List<double>();

            double rhsInteger = Math.Floor(rhs);
            double rhsFractional = rhs - rhsInteger;

            if (rhsFractional < 0)
            {
                rhsInteger -= 1;
                rhsFractional = rhs - rhsInteger;
            }

            sb.AppendLine($"RHS: {NumberFormatter.Format(rhs)} = {NumberFormatter.Format(rhsInteger)} + {NumberFormatter.Format(rhsFractional)}");

            for (int j = 0; j < cols - 1; j++)
            {
                double coeff = tableau[pivotRow, j];

                if (j == varIndex)
                {
                    integerParts.Add(0);
                    fractionalParts.Add(0);
                }
                else
                {
                    double intPart = Math.Floor(coeff);
                    double fracPart = coeff - intPart;
                    if (fracPart < 0)
                    {
                        intPart -= 1;
                        fracPart = coeff - intPart;
                    }
                    integerParts.Add(intPart);
                    fractionalParts.Add(fracPart);
                    if (j < variableNames.Count && Math.Abs(coeff) > TOLERANCE)
                    {
                        sb.AppendLine($"{variableNames[j]}: {NumberFormatter.Format(coeff)} = {NumberFormatter.Format(intPart)} + {NumberFormatter.Format(fracPart)}");
                    }
                }
            }

            sb.AppendLine();

            sb.AppendLine("Step 3: Rearrange - move integers to left, fractions to right:");
            sb.Append($"{fractionalVarName}");

            for (int j = 0; j < Math.Min(integerParts.Count, variableNames.Count); j++)
            {
                if (Math.Abs(integerParts[j]) > TOLERANCE && !IsBasicVariable(tableau, j))
                {
                    sb.Append($" - ({NumberFormatter.Format(integerParts[j])}){variableNames[j]}");
                }
            }

            sb.Append($" - ({NumberFormatter.Format(rhsInteger)}) = ");

            bool first = true;
            for (int j = 0; j < Math.Min(fractionalParts.Count, variableNames.Count); j++)
            {
                if (Math.Abs(fractionalParts[j]) > TOLERANCE && !IsBasicVariable(tableau, j))
                {
                    if (!first) sb.Append(" + ");
                    sb.Append($"{NumberFormatter.Format(fractionalParts[j])}{variableNames[j]}");
                    first = false;
                }
            }

            if (!first) sb.Append(" - ");
            else sb.Append("-");

            sb.AppendLine($"{NumberFormatter.Format(rhsFractional)}");
            sb.AppendLine();

            sb.AppendLine("Step 4: Generate cut constraint (fractional part <= 0):");
            var cutConstraint = new LinearProgram.Constraint();

            int totalVars = Math.Max(program.Variables.Count, fractionalParts.Count);
            for (int i = 0; i < totalVars; i++)
            {
                cutConstraint.Coefficients.Add(0);
            }

            sb.Append("Cut: ");
            bool firstTerm = true;

            for (int j = 0; j < fractionalParts.Count; j++)
            {
                if (Math.Abs(fractionalParts[j]) > TOLERANCE)
                {
                    if (!firstTerm && fractionalParts[j] > 0) sb.Append(" + ");
                    if (fractionalParts[j] < 0) sb.Append(" - ");
                    else if (!firstTerm) sb.Append(" + ");
                    sb.Append($"{NumberFormatter.Format(Math.Abs(fractionalParts[j]))}{variableNames[j]}");
                    if (j < cutConstraint.Coefficients.Count)
                    {
                        cutConstraint.Coefficients[j] = fractionalParts[j];
                    }
                    firstTerm = false;
                }
            }

            sb.AppendLine($" <= {NumberFormatter.Format(-rhsFractional)}");
            sb.AppendLine();
            sb.AppendLine("In canonical form with slack variable:");
            sb.Append("Cut: ");
            firstTerm = true;

            for (int j = 0; j < fractionalParts.Count; j++)
            {
                if (Math.Abs(fractionalParts[j]) > TOLERANCE)
                {
                    if (!firstTerm && fractionalParts[j] > 0) sb.Append(" + ");
                    if (fractionalParts[j] < 0) sb.Append(" - ");
                    else if (!firstTerm) sb.Append(" + ");
                    sb.Append($"{NumberFormatter.Format(Math.Abs(fractionalParts[j]))}{variableNames[j]}");
                    firstTerm = false;
                }
            }

            sb.AppendLine($" + s{program.Variables.Count + 1} = {NumberFormatter.Format(-rhsFractional)}");

            cutConstraint.Relation = LinearProgram.Relation.Equal;
            cutConstraint.Rhs = -rhsFractional;

            cutInfo.Constraint = cutConstraint;
            cutInfo.GenerationSteps = sb.ToString();

            return cutInfo;
        }

        private List<string> GetVariableNames(LinearProgram program, Solution solution)
        {
            var names = new List<string>();

            for (int i = 0; i < program.Variables.Count; i++)
                names.Add($"x{i + 1}");
            for (int i = 0; i < solution.SlackCount; i++)
                names.Add($"s{i + 1}");
            for (int i = 0; i < solution.ExcessCount; i++)
                names.Add($"e{i + 1}");
            for (int i = 0; i < solution.ArtificialCount; i++)
                names.Add($"a{i + 1}");

            return names;
        }

        private int FindBasicVariableRow(double[,] tableau, int varColumn)
        {
            int rows = tableau.GetLength(0);
            for (int i = 1; i < rows; i++)
            {
                if (Math.Abs(tableau[i, varColumn] - 1.0) < TOLERANCE)
                {
                    bool isBasic = true;
                    for (int k = 0; k < rows; k++)
                    {
                        if (k != i && Math.Abs(tableau[k, varColumn]) > TOLERANCE)
                        {
                            isBasic = false;
                            break;
                        }
                    }
                    if (isBasic)
                        return i;
                }
            }
            return -1;
        }

        private bool IsBasicVariable(double[,] tableau, int column)
        {
            int rows = tableau.GetLength(0);
            int onesCount = 0;
            for (int i = 0; i < rows; i++)
            {
                if (Math.Abs(tableau[i, column] - 1.0) < TOLERANCE)
                    onesCount++;
                else if (Math.Abs(tableau[i, column]) > TOLERANCE)
                    return false;
            }
            return onesCount == 1;
        }

        private string FormatConstraint(LinearProgram.Constraint constraint, int slackVarIndex)
        {
            var sb = new StringBuilder();
            bool first = true;
            for (int i = 0; i < constraint.Coefficients.Count - 1; i++)
            {
                double coeff = constraint.Coefficients[i];
                if (Math.Abs(coeff) > TOLERANCE)
                {
                    if (!first && coeff > 0) sb.Append(" + ");
                    if (coeff < 0) sb.Append(" - ");
                    else if (!first) sb.Append(" + ");
                    sb.Append($"{NumberFormatter.Format(Math.Abs(coeff))}x{i + 1}");
                    first = false;
                }
            }
            sb.Append($" + s{slackVarIndex} = {NumberFormatter.Format(constraint.Rhs)}");
            return sb.ToString();
        }

        public string FormatCanonicalForm(LinearProgram program)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Canonical Form (with slack variables):");

            sb.Append("z");
            for (int i = 0; i < program.Variables.Count; i++)
            {
                double coeff = program.IsMaximization ? -program.Variables[i].Coefficient : program.Variables[i].Coefficient;
                if (coeff >= 0)
                    sb.Append($" + {NumberFormatter.Format(coeff)}x{i + 1}");
                else
                    sb.Append($" - {NumberFormatter.Format(Math.Abs(coeff))}x{i + 1}");
            }
            sb.AppendLine(" = 0");
            sb.AppendLine();

            int slackIndex = 1;
            for (int i = 0; i < program.Constraints.Count; i++)
            {
                var constraint = program.Constraints[i];
                bool first = true;
                for (int j = 0; j < constraint.Coefficients.Count && j < program.Variables.Count; j++)
                {
                    double coeff = constraint.Coefficients[j];
                    if (Math.Abs(coeff) < TOLERANCE) continue;
                    if (first)
                    {
                        sb.Append($"{NumberFormatter.Format(coeff)}x{j + 1}");
                        first = false;
                    }
                    else
                    {
                        if (coeff >= 0)
                            sb.Append($" + {NumberFormatter.Format(coeff)}x{j + 1}");
                        else
                            sb.Append($" - {NumberFormatter.Format(Math.Abs(coeff))}x{j + 1}");
                    }
                }
                if (constraint.Relation == LinearProgram.Relation.LessThanOrEqual)
                {
                    sb.Append($" + s{slackIndex}");
                    slackIndex++;
                }
                else if (constraint.Relation == LinearProgram.Relation.GreaterThanOrEqual)
                {
                    sb.Append($" - s{slackIndex}");
                    slackIndex++;
                }
                sb.AppendLine($" = {NumberFormatter.Format(constraint.Rhs)}");
            }

            sb.AppendLine();
            sb.AppendLine("All variables >= 0");

            return sb.ToString();
        }

        private class CutInfo
        {
            public LinearProgram.Constraint Constraint { get; set; }
            public string GenerationSteps { get; set; }
        }
    }
}