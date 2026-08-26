using Group_V_26_LPR381_Project.Algorithms;
using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinearProgrammingSolver.Algorithms
{
    /// <summary>
    /// Gomory cutting-plane solver for pure integer maximisation models.
    ///
    /// Cuts are formed from the live optimal tableau and appended directly to it.  This is
    /// important: a cut row is in tableau coordinates, not in the original model's decision
    /// variable coordinates, and its cut-slack remains a continuous tableau auxiliary.
    /// </summary>
    public class CuttingPlane : ISolver
    {
        private const double TOLERANCE = 1e-6;
        private const int MAX_CUTS = 50;
        private readonly DualSimplex _dualSimplex;

        public CuttingPlane()
        {
            _dualSimplex = new DualSimplex();
        }

        public Solution Solve(LinearProgram program)
        {
            var solution = new Solution();
            if (!ValidateSupportedModel(program, solution))
                return solution;

            // Binary is an integrality restriction as well as an upper bound.  Work on a
            // clone so callers' model data remains unchanged while its LP relaxation honours
            // x <= 1 for every declared binary variable.
            LinearProgram workingProgram = CreateRelaxationProgram(program);
            solution.AddStep("Canonical Form", FormatCanonicalForm(workingProgram));

            Solution currentSolution = _dualSimplex.Solve(workingProgram);
            CopySimplexIterations(solution, currentSolution, "LP Relaxation");

            if (HasSolverFailure(currentSolution))
            {
                solution.AddMessage("Initial LP relaxation is infeasible or unbounded. Cutting Plane cannot proceed.");
                CopyMessages(solution, currentSolution);
                return solution;
            }

            HashSet<string> integralAuxiliaries = GetIntegralOriginalAuxiliaries(workingProgram);
            int cutCount = 0;

            while (cutCount < MAX_CUTS)
            {
                Tuple<string, double> fractionalVariable =
                    FindMostFractionalIntegerVariable(currentSolution, workingProgram);

                if (fractionalVariable == null)
                {
                    solution.OptimalValue = currentSolution.OptimalValue;
                    solution.VariableValues = GetOriginalDecisionValues(currentSolution, program);
                    solution.FinalTableau = currentSolution.FinalTableau == null
                        ? null
                        : (double[,])currentSolution.FinalTableau.Clone();
                    solution.AddMessage("Result: OPTIMAL INTEGER SOLUTION");
                    solution.AddMessage("Optimal integer solution found with value: " +
                        NumberFormatter.Format(currentSolution.OptimalValue));
                    return solution;
                }

                cutCount++;
                string cutSlackName = "g" + cutCount;
                double fraction = FractionalPart(fractionalVariable.Item2);
                solution.AddMessage($"Cut {cutCount}: selected {fractionalVariable.Item1} = " +
                    $"{NumberFormatter.Format(fractionalVariable.Item2)} " +
                    $"(fractional part {NumberFormatter.Format(fraction)})." );

                CutInfo cut = GenerateGmiCut(
                    currentSolution,
                    fractionalVariable.Item1,
                    workingProgram,
                    integralAuxiliaries,
                    cutSlackName);

                if (cut == null)
                {
                    solution.AddMessage("Unable to generate a valid cut from the current tableau. No integer result is claimed.");
                    return solution;
                }

                solution.AddStep("Cut " + cutCount + " Generation", cut.GenerationSteps);
                solution.AddMessage("Appending tableau cut: " + cut.CanonicalEquation);

                // This call preserves the existing optimal tableau and restores feasibility
                // with Dual Simplex.  It must not rebuild the LP from the original model.
                currentSolution = _dualSimplex.AppendGomoryCutAndResolve(
                    cut.TableauCoefficients,
                    cut.RightHandSide,
                    cutSlackName);

                CopySimplexIterations(solution, currentSolution, "After Cut " + cutCount);

                if (HasSolverFailure(currentSolution))
                {
                    solution.AddMessage("Result: INTEGER INFEASIBLE");
                    CopyMessages(solution, currentSolution);
                    return solution;
                }

                solution.AddMessage("Cut " + cutCount + " result: LP value = " +
                    NumberFormatter.Format(currentSolution.OptimalValue));
            }

            // A cut limit is a guard against cycling/slow convergence, not evidence of an
            // integer solution.  Expose the current relaxation only as diagnostic output.
            solution.OptimalValue = currentSolution.OptimalValue;
            solution.VariableValues = GetOriginalDecisionValues(currentSolution, program);
            solution.FinalTableau = currentSolution.FinalTableau == null
                ? null
                : (double[,])currentSolution.FinalTableau.Clone();
            solution.AddMessage("Maximum number of Gomory cuts reached; no integer optimality claim is made.");
            return solution;
        }

        /// <summary>
        /// The current implementation deliberately supports pure integer maximisation only.
        /// Mixed models need the complete GMI treatment for continuous original variables;
        /// accepting them here would make an invalid cut look like a valid answer.
        /// </summary>
        private bool ValidateSupportedModel(LinearProgram program, Solution solution)
        {
            if (program == null)
            {
                solution.AddMessage("Result: UNSUPPORTED MODEL");
                solution.AddMessage("No linear program was provided.");
                return false;
            }

            if (!program.IsMaximization)
            {
                solution.AddMessage("Result: UNSUPPORTED MODEL");
                solution.AddMessage("Cutting Plane currently supports maximisation models only.");
                return false;
            }

            bool hasIntegerVariable = program.Variables.Any(IsIntegerRestricted);
            bool hasContinuousVariable = program.Variables.Any(variable => !IsIntegerRestricted(variable));
            if (!hasIntegerVariable)
            {
                solution.AddMessage("Result: UNSUPPORTED MODEL");
                solution.AddMessage("Cutting Plane requires at least one original integer or binary decision variable.");
                return false;
            }

            if (hasContinuousVariable)
            {
                solution.AddMessage("Result: UNSUPPORTED MODEL");
                solution.AddMessage("Mixed-integer Cutting Plane is intentionally unsupported: a complete GMI implementation is required for continuous original variables.");
                return false;
            }

            for (int row = 0; row < program.Constraints.Count; row++)
            {
                if (program.Constraints[row].Coefficients.Count != program.Variables.Count)
                {
                    solution.AddMessage("Result: UNSUPPORTED MODEL");
                    solution.AddMessage("Constraint " + (row + 1) + " does not match the number of original decision variables.");
                    return false;
                }
            }

            return true;
        }

        private LinearProgram CreateRelaxationProgram(LinearProgram program)
        {
            LinearProgram relaxation = program.Clone();
            for (int i = 0; i < relaxation.Variables.Count; i++)
            {
                if (relaxation.Variables[i].Type != LinearProgram.VariableType.Binary)
                    continue;

                var upperBound = new LinearProgram.Constraint
                {
                    Relation = LinearProgram.Relation.LessThanOrEqual,
                    Rhs = 1
                };
                for (int column = 0; column < relaxation.Variables.Count; column++)
                    upperBound.Coefficients.Add(column == i ? 1 : 0);

                relaxation.Constraints.Add(upperBound);
            }
            return relaxation;
        }

        private static bool IsIntegerRestricted(LinearProgram.Variable variable)
        {
            return variable.Type == LinearProgram.VariableType.Integer ||
                variable.Type == LinearProgram.VariableType.Binary;
        }

        /// <summary>
        /// Selects only original integer-restricted decision variables.  Tableau auxiliaries
        /// (including every generated g# cut slack) are never candidates, even when their
        /// displayed value happens to be fractional.
        /// </summary>
        private Tuple<string, double> FindMostFractionalIntegerVariable(
            Solution solution,
            LinearProgram program)
        {
            string selectedName = null;
            double selectedValue = 0;
            double closestToHalf = double.MaxValue;
            int lowestOriginalIndex = int.MaxValue;

            foreach (LinearProgram.Variable variable in program.Variables
                .Where(IsIntegerRestricted)
                .OrderBy(variable => variable.Index))
            {
                string name = "x" + variable.Index;
                double value;
                if (!solution.VariableValues.TryGetValue(name, out value))
                    continue;

                // Gomory rows may be generated only from a fractional *basic* integer
                // variable.  This check intentionally uses the tableau, not just the value
                // dictionary, so a stale/display-only value can never be used as a basis row.
                int column = program.Variables.IndexOf(variable);
                if (solution.FinalTableau == null ||
                    FindBasicVariableRow(solution.FinalTableau, column) == -1)
                    continue;

                double fractionalPart = FractionalPart(value);
                if (fractionalPart <= TOLERANCE || fractionalPart >= 1 - TOLERANCE)
                    continue;

                double distanceFromHalf = Math.Abs(fractionalPart - 0.5);
                if (distanceFromHalf < closestToHalf - TOLERANCE ||
                    (Math.Abs(distanceFromHalf - closestToHalf) <= TOLERANCE &&
                        variable.Index < lowestOriginalIndex))
                {
                    selectedName = name;
                    selectedValue = value;
                    closestToHalf = distanceFromHalf;
                    lowestOriginalIndex = variable.Index;
                }
            }

            return selectedName == null ? null : Tuple.Create(selectedName, selectedValue);
        }

        /// <summary>
        /// Produces a Gomory mixed-integer cut from one fractional basic integer row.  In a
        /// pure integer model this reduces to the usual fractional cut for original integral
        /// slacks; generated cut slacks and artificials are correctly treated as continuous
        /// in later cuts.
        /// </summary>
        private CutInfo GenerateGmiCut(
            Solution solution,
            string fractionalVariableName,
            LinearProgram program,
            HashSet<string> integralAuxiliaries,
            string cutSlackName)
        {
            if (solution.FinalTableau == null)
                return null;

            double[,] tableau = solution.FinalTableau;
            List<string> headers = _dualSimplex.GetCurrentColumnHeaders();
            if (headers.Count != tableau.GetLength(1))
                throw new InvalidOperationException("The live tableau headers do not match the tableau dimensions.");

            int fractionalVariableColumn = GetOriginalVariableColumn(program, fractionalVariableName);
            int sourceRow = FindBasicVariableRow(tableau, fractionalVariableColumn);
            if (sourceRow == -1)
                return null;

            int rhsColumn = tableau.GetLength(1) - 1;
            double rhs = tableau[sourceRow, rhsColumn];
            double fractionalRhs = FractionalPart(rhs);
            if (fractionalRhs <= TOLERANCE || fractionalRhs >= 1 - TOLERANCE)
                return null;

            var cut = new CutInfo
            {
                TableauCoefficients = new double[rhsColumn],
                RightHandSide = -fractionalRhs
            };
            var steps = new StringBuilder();
            steps.AppendLine("Source tableau row (basic integer variable):");
            steps.Append("  ").Append(fractionalVariableName);
            AppendRowTerms(steps, tableau, sourceRow, headers);
            steps.Append(" = ").AppendLine(NumberFormatter.Format(rhs));
            steps.AppendLine();
            steps.AppendLine("Gomory/GMI coefficients (fractional part uses a - floor(a), including negative a):");
            steps.AppendLine("  f0 = frac(" + NumberFormatter.Format(rhs) + ") = " +
                NumberFormatter.Format(fractionalRhs));

            for (int column = 0; column < rhsColumn; column++)
            {
                if (IsBasicColumn(tableau, column))
                    continue;

                double coefficient = tableau[sourceRow, column];
                bool integerColumn = IsIntegerTableauColumn(
                    column, headers[column], program, integralAuxiliaries);
                double alpha;

                if (integerColumn)
                {
                    double fractionalCoefficient = FractionalPart(coefficient);
                    alpha = fractionalCoefficient <= fractionalRhs + TOLERANCE
                        ? fractionalCoefficient
                        : fractionalRhs * (1 - fractionalCoefficient) / (1 - fractionalRhs);
                    if (Math.Abs(alpha) > TOLERANCE)
                    {
                        steps.AppendLine("  " + headers[column] + " (integer): frac(a) = " +
                            NumberFormatter.Format(fractionalCoefficient) + ", alpha = " +
                            NumberFormatter.Format(alpha));
                    }
                }
                else
                {
                    alpha = coefficient >= -TOLERANCE
                        ? coefficient
                        : -fractionalRhs * coefficient / (1 - fractionalRhs);
                    if (Math.Abs(alpha) > TOLERANCE)
                    {
                        steps.AppendLine("  " + headers[column] + " (continuous auxiliary): a = " +
                            NumberFormatter.Format(coefficient) + ", alpha = " +
                            NumberFormatter.Format(alpha));
                    }
                }

                cut.TableauCoefficients[column] = Math.Abs(alpha) <= TOLERANCE ? 0 : -alpha;
            }

            steps.AppendLine();
            steps.AppendLine("Cut before tableau slack: sum(alpha_j * nonbasic_j) >= f0");
            cut.CanonicalEquation = FormatCutEquation(cut.TableauCoefficients, headers,
                cutSlackName, cut.RightHandSide);
            steps.AppendLine("Stored tableau equality: " + cut.CanonicalEquation);
            steps.AppendLine("The new " + cutSlackName + " column is a continuous cut auxiliary, not an original x variable.");
            steps.AppendLine("The negative RHS is restored by Dual Simplex from this existing tableau.");
            cut.GenerationSteps = steps.ToString();
            return cut;
        }

        private static bool IsIntegerTableauColumn(
            int column,
            string header,
            LinearProgram program,
            HashSet<string> integralAuxiliaries)
        {
            if (column < program.Variables.Count)
                return IsIntegerRestricted(program.Variables[column]);

            return integralAuxiliaries.Contains(header);
        }

        private static HashSet<string> GetIntegralOriginalAuxiliaries(LinearProgram program)
        {
            var names = new HashSet<string>();
            int slackIndex = 0;
            int excessIndex = 0;

            foreach (LinearProgram.Constraint constraint in program.Constraints)
            {
                bool isIntegralRow = IsNearlyInteger(constraint.Rhs) &&
                    constraint.Coefficients.All(IsNearlyInteger);

                if (constraint.Relation == LinearProgram.Relation.LessThanOrEqual)
                {
                    slackIndex++;
                    if (isIntegralRow)
                        names.Add("s" + slackIndex);
                }
                else if (constraint.Relation == LinearProgram.Relation.GreaterThanOrEqual)
                {
                    excessIndex++;
                    if (isIntegralRow)
                        names.Add("e" + excessIndex);
                }
            }

            return names;
        }

        private static bool IsNearlyInteger(double value)
        {
            return Math.Abs(value - Math.Round(value)) <= TOLERANCE;
        }

        private static double FractionalPart(double value)
        {
            double fraction = value - Math.Floor(value);
            if (fraction <= TOLERANCE || 1 - fraction <= TOLERANCE)
                return 0;
            return fraction;
        }

        private static int GetOriginalVariableColumn(LinearProgram program, string variableName)
        {
            int index = int.Parse(variableName.Substring(1));
            for (int column = 0; column < program.Variables.Count; column++)
            {
                if (program.Variables[column].Index == index)
                    return column;
            }
            throw new InvalidOperationException("The fractional variable is not an original decision variable.");
        }

        private static int FindBasicVariableRow(double[,] tableau, int variableColumn)
        {
            for (int row = 1; row < tableau.GetLength(0); row++)
            {
                if (Math.Abs(tableau[row, variableColumn] - 1) > TOLERANCE)
                    continue;

                bool isBasic = true;
                for (int otherRow = 0; otherRow < tableau.GetLength(0); otherRow++)
                {
                    if (otherRow != row && Math.Abs(tableau[otherRow, variableColumn]) > TOLERANCE)
                    {
                        isBasic = false;
                        break;
                    }
                }

                if (isBasic)
                    return row;
            }
            return -1;
        }

        private static bool IsBasicColumn(double[,] tableau, int column)
        {
            int basicRow = -1;
            for (int row = 1; row < tableau.GetLength(0); row++)
            {
                double value = tableau[row, column];
                if (Math.Abs(value - 1) <= TOLERANCE)
                {
                    if (basicRow != -1)
                        return false;
                    basicRow = row;
                }
                else if (Math.Abs(value) > TOLERANCE)
                {
                    return false;
                }
            }

            return basicRow != -1 && Math.Abs(tableau[0, column]) <= TOLERANCE;
        }

        private static void AppendRowTerms(
            StringBuilder builder,
            double[,] tableau,
            int row,
            IList<string> headers)
        {
            for (int column = 0; column < tableau.GetLength(1) - 1; column++)
            {
                if (IsBasicColumn(tableau, column))
                    continue;
                AppendSignedTerm(builder, tableau[row, column], headers[column], true);
            }
        }

        private static string FormatCutEquation(
            double[] coefficients,
            IList<string> headers,
            string cutSlackName,
            double rightHandSide)
        {
            var builder = new StringBuilder();
            bool hasTerm = false;
            for (int column = 0; column < coefficients.Length; column++)
            {
                double coefficient = coefficients[column];
                if (Math.Abs(coefficient) <= TOLERANCE)
                    continue;

                AppendSignedTerm(builder, coefficient, headers[column], hasTerm);
                hasTerm = true;
            }

            AppendSignedTerm(builder, 1, cutSlackName, hasTerm);
            return builder + " = " + NumberFormatter.Format(rightHandSide);
        }

        private static void AppendSignedTerm(
            StringBuilder builder,
            double coefficient,
            string variableName,
            bool hasPreviousTerm)
        {
            if (Math.Abs(coefficient) <= TOLERANCE)
                return;

            if (hasPreviousTerm)
                builder.Append(coefficient < 0 ? " - " : " + ");
            else if (coefficient < 0)
                builder.Append("-");

            builder.Append(NumberFormatter.Format(Math.Abs(coefficient))).Append(variableName);
        }

        private static Dictionary<string, double> GetOriginalDecisionValues(
            Solution solution,
            LinearProgram originalProgram)
        {
            var values = new Dictionary<string, double>();
            foreach (LinearProgram.Variable variable in originalProgram.Variables)
            {
                string name = "x" + variable.Index;
                double value;
                values[name] = solution.VariableValues.TryGetValue(name, out value) &&
                    Math.Abs(value) > TOLERANCE ? value : 0;
            }
            return values;
        }

        private static bool HasSolverFailure(Solution solution)
        {
            return solution.Messages.Any(message =>
                message.IndexOf("infeasible", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("unbounded", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void CopyMessages(Solution target, Solution source)
        {
            foreach (string message in source.Messages)
                target.AddMessage(message);
        }

        /// <summary>
        /// Copies exact solver snapshots so the existing renderer continues to display the
        /// live tableau and its pivots for the relaxation and every cut.
        /// </summary>
        private static void CopySimplexIterations(Solution target, Solution source, string labelPrefix)
        {
            for (int index = 0; index < source.IterationTableaux.Count; index++)
            {
                List<string> headers = index < source.IterationColumnHeaders.Count
                    ? source.IterationColumnHeaders[index]
                    : null;
                int pivotRow = index < source.IterationPivotRows.Count
                    ? source.IterationPivotRows[index]
                    : -1;
                int pivotColumn = index < source.IterationPivotCols.Count
                    ? source.IterationPivotCols[index]
                    : -1;
                string iterationMessage = source.IterationMessages[index];
                string label = string.IsNullOrEmpty(labelPrefix)
                    ? iterationMessage
                    : labelPrefix + " - " + iterationMessage;

                target.AddIteration(source.IterationTableaux[index], label,
                    pivotRow, pivotColumn, headers);
            }
        }

        public string FormatCanonicalForm(LinearProgram program)
        {
            var result = new StringBuilder();
            result.AppendLine("Canonical Form (with original tableau auxiliaries):");
            result.Append("z");
            for (int index = 0; index < program.Variables.Count; index++)
            {
                double coefficient = -program.Variables[index].Coefficient;
                AppendSignedTerm(result, coefficient, "x" + program.Variables[index].Index, index > 0);
            }
            result.AppendLine(" = 0");
            result.AppendLine();

            int slackIndex = 0;
            int excessIndex = 0;
            int artificialIndex = 0;
            foreach (LinearProgram.Constraint constraint in program.Constraints)
            {
                bool hasTerm = false;
                for (int column = 0; column < constraint.Coefficients.Count; column++)
                {
                    AppendSignedTerm(result, constraint.Coefficients[column],
                        "x" + program.Variables[column].Index, hasTerm);
                    if (Math.Abs(constraint.Coefficients[column]) > TOLERANCE)
                        hasTerm = true;
                }

                if (!hasTerm)
                    result.Append("0");

                if (constraint.Relation == LinearProgram.Relation.LessThanOrEqual)
                    result.Append(" + s").Append(++slackIndex);
                else if (constraint.Relation == LinearProgram.Relation.GreaterThanOrEqual)
                    result.Append(" - e").Append(++excessIndex);
                else
                    result.Append(" + a").Append(++artificialIndex);

                result.Append(" = ").AppendLine(NumberFormatter.Format(constraint.Rhs));
            }

            result.AppendLine();
            result.AppendLine("Generated g# cut slacks are continuous tableau auxiliaries.");
            return result.ToString();
        }

        private class CutInfo
        {
            public double[] TableauCoefficients { get; set; }
            public double RightHandSide { get; set; }
            public string CanonicalEquation { get; set; }
            public string GenerationSteps { get; set; }
        }
    }
}
