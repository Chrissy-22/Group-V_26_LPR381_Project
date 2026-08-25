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
        private const int MAX_CUTS = 50;

        public CuttingPlane()
        {
            _dualSimplex = new DualSimplex();
        }

        public Solution Solve(LinearProgram program)
        {
            var solution = new Solution();

            solution.AddStep(
                "Canonical Form",
                FormatCanonicalForm(program));

            // 1. Solve LP relaxation.
            var currentSolution = _dualSimplex.Solve(program);

            if (HasFailure(currentSolution))
            {
                CopyMessages(solution, currentSolution);
                return solution;
            }

            CopySimplexIterations(solution, currentSolution, "LP Relaxation");

            var currentProgram = program.Clone();

            int cutNumber = 0;

            while (true)
            {
                // 2. Find a fractional INTEGER/BINARY decision variable.
                var fractional = FindFractionalVariable(
                    currentSolution,
                    currentProgram);

                // 3. All required integer variables are integer.
                if (fractional == null)
                {
                    solution.OptimalValue = currentSolution.OptimalValue;
                    solution.VariableValues =
                        new Dictionary<string, double>(
                            currentSolution.VariableValues);

                    solution.AddMessage(
                        "\nOptimal integer solution found: " +
                        FormatSolution(currentSolution));

                    break;
                }

                cutNumber++;

                if (cutNumber > MAX_CUTS)
                {
                    solution.AddMessage(
                        $"Maximum number of Gomory cuts ({MAX_CUTS}) reached.");
                    break;
                }

                string variableName = fractional.Value.Name;
                double variableValue = fractional.Value.Value;
                int variableColumn =
                    GetVariableIndex(variableName) - 1;

                solution.AddMessage(
                    $"\nGomory Cut #{cutNumber}: " +
                    $"{variableName} = {NumberFormatter.Format(variableValue)} " +
                    "is fractional.");

                // 4. Find the tableau row in which that integer variable is basic.
                int pivotRow = FindBasicVariableRow(
                    currentSolution.FinalTableau,
                    variableColumn);

                if (pivotRow == -1)
                {
                    solution.AddMessage(
                        $"Cannot generate Gomory cut: {variableName} " +
                        "is not basic in the final tableau.");

                    return solution;
                }

                // 5. Build Gomory fractional cut from the selected row.
                var cut = GenerateGomoryCut(
                    currentSolution,
                    currentProgram,
                    pivotRow,
                    variableColumn,
                    variableName,
                    cutNumber);

                if (cut == null)
                {
                    solution.AddMessage(
                        "Unable to generate a valid Gomory cut.");

                    return solution;
                }

                solution.AddStep(
                    $"Gomory Cut #{cutNumber}",
                    cut.Explanation);

                // 6. Add the new cut directly through DualSimplex's
                //    constraint mechanism.
                Solution cutSolution =
                    _dualSimplex.AddConstraintAndResolve(cut.Constraint);

                CopySimplexIterations(
                    solution,
                    cutSolution,
                    $"After Gomory Cut #{cutNumber}");

                CopyMessages(solution, cutSolution);

                if (HasFailure(cutSolution))
                {
                    return solution;
                }

                // 7. Update the mathematical model so future cuts see
                //    the new constraint.
                currentProgram.Constraints.Add(
                    CloneConstraint(cut.Constraint));

                // 8. Continue from the new optimum.
                currentSolution = cutSolution;
            }

            return solution;
        }

        private bool HasFailure(Solution solution)
        {
            return solution.Messages.Any(
                m => m.IndexOf("infeasible",
                               StringComparison.OrdinalIgnoreCase) >= 0
                  || m.IndexOf("unbounded",
                               StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void CopyMessages(
            Solution target,
            Solution source)
        {
            foreach (var message in source.Messages)
                target.AddMessage(message);
        }

        private void CopySimplexIterations(
            Solution target,
            Solution source,
            string labelPrefix)
        {
            for (int i = 0;
                 i < source.IterationTableaux.Count;
                 i++)
            {
                var headers =
                    i < source.IterationColumnHeaders.Count
                        ? source.IterationColumnHeaders[i]
                        : null;

                int pivotRow =
                    i < source.IterationPivotRows.Count
                        ? source.IterationPivotRows[i]
                        : -1;

                int pivotCol =
                    i < source.IterationPivotCols.Count
                        ? source.IterationPivotCols[i]
                        : -1;

                string message =
                    i < source.IterationMessages.Count
                        ? source.IterationMessages[i]
                        : $"Iteration {i + 1}";

                string label =
                    $"{labelPrefix} - {message}";

                target.AddIteration(
                    source.IterationTableaux[i],
                    label,
                    pivotRow,
                    pivotCol,
                    headers);
            }
        }

        private (string Name, double Value)? FindFractionalVariable(
            Solution solution,
            LinearProgram program)
        {
            var integerVariables =
                program.Variables
                    .Where(v =>
                        v.Type == LinearProgram.VariableType.Integer ||
                        v.Type == LinearProgram.VariableType.Binary)
                    .OrderBy(v => v.Index)
                    .ToList();

            foreach (var variable in integerVariables)
            {
                string name = $"x{variable.Index}";

                if (!solution.VariableValues.TryGetValue(
                        name,
                        out double value))
                {
                    continue;
                }

                double fractionalPart =
                    value - Math.Floor(value);

                if (fractionalPart > TOLERANCE &&
                    fractionalPart < 1.0 - TOLERANCE)
                {
                    return (name, value);
                }
            }

            return null;
        }

        /// <summary>Resolves the display name (x1, s1, e1, a1, ...) for tableau column j.</summary>
        private string GetTableauColumnName(int j, LinearProgram program)
        {
            if (j < program.Variables.Count)
                return $"x{program.Variables[j].Index}";

            int auxIndex = j - program.Variables.Count;
            if (auxIndex < 0 || auxIndex >= program.Constraints.Count)
                return $"v{j + 1}";

            var relation = program.Constraints[auxIndex].Relation;
            if (relation == LinearProgram.Relation.LessThanOrEqual) return $"s{auxIndex + 1}";
            if (relation == LinearProgram.Relation.GreaterThanOrEqual) return $"e{auxIndex + 1}";
            return $"a{auxIndex + 1}";
        }

        private CutInfo GenerateGomoryCut(
            Solution solution,
            LinearProgram program,
            int pivotRow,
            int variableColumn,
            string variableName,
            int cutNumber)
        {
            if (solution.FinalTableau == null)
                return null;

            double[,] tableau = solution.FinalTableau;

            int cols = tableau.GetLength(1);
            double rhs = tableau[pivotRow, cols - 1];

            double rhsFraction = FractionalPart(rhs);

            if (rhsFraction <= TOLERANCE)
                return null;

            var explanation = new StringBuilder();

            explanation.AppendLine(
                $"Gomory Cut #{cutNumber} from row {pivotRow}");

            explanation.AppendLine(
                $"Selected basic variable: {variableName}");

            explanation.AppendLine();

            explanation.AppendLine(
                $"RHS = {NumberFormatter.Format(rhs)}");

            explanation.AppendLine(
                $"Fractional part = {NumberFormatter.Format(rhsFraction)}");

            explanation.AppendLine();

            explanation.AppendLine("Tableau row:");

            explanation.Append(
                $"{variableName} = {NumberFormatter.Format(rhs)}");

            for (int j = 0; j < cols - 1; j++)
            {
                if (j == variableColumn)
                    continue;

                double coefficient = tableau[pivotRow, j];

                if (Math.Abs(coefficient) <= TOLERANCE)
                    continue;

                string name = GetTableauColumnName(j, program);

                explanation.Append(
                    coefficient >= 0
                        ? $" - {NumberFormatter.Format(coefficient)}{name}"
                        : $" + {NumberFormatter.Format(-coefficient)}{name}");
            }

            explanation.AppendLine();
            explanation.AppendLine();

            explanation.AppendLine(
                "Fractional-part calculations:");

            // Gomory cut initially exists in tableau-variable form:
            //
            // sum f_j * y_j >= f(b)
            //
            // where y_j are the non-basic tableau variables.

            double[] fractionalParts =
                new double[cols - 1];

            for (int j = 0; j < cols - 1; j++)
            {
                if (j == variableColumn)
                {
                    fractionalParts[j] = 0;
                    continue;
                }

                double tableauCoefficient =
                    tableau[pivotRow, j];

                // Row is:
                //
                // xB + a_j*y_j = b
                //
                // therefore:
                //
                // xB = b - a_j*y_j

                double fraction =
                    FractionalPart(tableauCoefficient);

                fractionalParts[j] = fraction;

                if (Math.Abs(fraction) <= TOLERANCE)
                    continue;

                string name = GetTableauColumnName(j, program);

                explanation.AppendLine(
                    $"{name}: " +
                    $"{NumberFormatter.Format(-tableauCoefficient)} " +
                    $"=> fraction = " +
                    $"{NumberFormatter.Format(fraction)}");
            }

            explanation.AppendLine();
            explanation.AppendLine("Row equation (from the tableau):");
            {
                var rowEquationTerms = new StringBuilder(variableName);
                for (int j = 0; j < cols - 1; j++)
                {
                    if (j == variableColumn) continue;
                    double c = tableau[pivotRow, j];
                    if (Math.Abs(c) <= TOLERANCE) continue;
                    rowEquationTerms.Append(c >= 0 ? " + " : " - ")
                        .Append(NumberFormatter.Format(Math.Abs(c)))
                        .Append(GetTableauColumnName(j, program));
                }
                explanation.AppendLine($"{rowEquationTerms} = {NumberFormatter.Format(rhs)}");
            }

            explanation.AppendLine();
            explanation.AppendLine("Split every coefficient (and the RHS) into an integer part + a fractional part:");
            for (int j = 0; j < cols - 1; j++)
            {
                if (j == variableColumn) continue;
                double c = tableau[pivotRow, j];
                if (Math.Abs(c) <= TOLERANCE) continue;
                explanation.AppendLine(
                    $"  {GetTableauColumnName(j, program)}: {NumberFormatter.Format(c)} = " +
                    $"{NumberFormatter.Format(Math.Floor(c))} + {NumberFormatter.Format(fractionalParts[j])}");
            }
            explanation.AppendLine(
                $"  RHS: {NumberFormatter.Format(rhs)} = " +
                $"{NumberFormatter.Format(Math.Floor(rhs))} + {NumberFormatter.Format(rhsFraction)}");

            explanation.AppendLine();
            explanation.AppendLine("Move every INTEGER-coefficient term left, every FRACTIONAL-coefficient term right:");
            {
                var leftTerms = new StringBuilder(variableName);
                var rightTerms = new StringBuilder();
                for (int j = 0; j < cols - 1; j++)
                {
                    if (j == variableColumn) continue;
                    double c = tableau[pivotRow, j];
                    if (Math.Abs(c) <= TOLERANCE) continue;
                    double intPart = Math.Floor(c);
                    double fracPart = fractionalParts[j];
                    string name = GetTableauColumnName(j, program);
                    if (Math.Abs(intPart) > TOLERANCE)
                        leftTerms.Append(intPart >= 0 ? " + " : " - ")
                            .Append(NumberFormatter.Format(Math.Abs(intPart)))
                            .Append(name);
                    if (Math.Abs(fracPart) > TOLERANCE)
                        rightTerms.Append(" - ").Append(NumberFormatter.Format(fracPart)).Append(name);
                }
                explanation.AppendLine(
                    $"  {leftTerms} - {NumberFormatter.Format(Math.Floor(rhs))}  =  " +
                    $"{NumberFormatter.Format(rhsFraction)}{rightTerms}");
            }

            explanation.AppendLine();
            explanation.AppendLine(
                $"The left side is a whole number ({variableName} and every integer coefficient there are integers).");
            explanation.AppendLine(
                "The right side is < 1, since every non-basic variable is >= 0 and every fractional coefficient is in [0,1).");
            explanation.AppendLine("A whole number that is < 1 must be <= 0:");
            explanation.AppendLine(
                $"  {NumberFormatter.Format(rhsFraction)} - (sum of fractional terms) <= 0");

            // ---------------------------------------------------------
            // END EXPLICIT DERIVATION
            // ---------------------------------------------------------

            explanation.AppendLine();

            explanation.AppendLine(
                "Converting the cut to the original decision variables:");

            // Tableau cut:
            //
            // sum f_j*y_j >= f(b)
            //
            // Every auxiliary variable is substituted back into
            // the original constraints.

            double[] lhsOriginal =
                new double[program.Variables.Count];

            double constantPart = 0.0;

            for (int j = 0; j < cols - 1; j++)
            {
                double fraction = fractionalParts[j];

                if (Math.Abs(fraction) <= TOLERANCE)
                    continue;

                // Original x variable.
                if (j < program.Variables.Count)
                {
                    lhsOriginal[j] += fraction;
                    continue;
                }

                // Auxiliary variable.
                int constraintIndex =
                    j - program.Variables.Count;

                if (constraintIndex < 0 ||
                    constraintIndex >= program.Constraints.Count)
                {
                    continue;
                }

                var source =
                    program.Constraints[constraintIndex];

                if (source.Relation ==
                    LinearProgram.Relation.LessThanOrEqual)
                {
                    // s = b - Ax

                    constantPart +=
                        fraction * source.Rhs;

                    for (int k = 0;
                         k < program.Variables.Count;
                         k++)
                    {
                        lhsOriginal[k] -=
                            fraction *
                            source.Coefficients[k];
                    }
                }
                else if (source.Relation ==
                         LinearProgram.Relation.GreaterThanOrEqual)
                {
                    // e = Ax - b

                    constantPart -=
                        fraction * source.Rhs;

                    for (int k = 0;
                         k < program.Variables.Count;
                         k++)
                    {
                        lhsOriginal[k] +=
                            fraction *
                            source.Coefficients[k];
                    }
                }
                else
                {
                    // a = b - Ax

                    constantPart +=
                        fraction * source.Rhs;

                    for (int k = 0;
                         k < program.Variables.Count;
                         k++)
                    {
                        lhsOriginal[k] -=
                            fraction *
                            source.Coefficients[k];
                    }
                }
            }

            // We currently have:
            //
            // lhsOriginal*x + constantPart >= rhsFraction
            //
            // therefore:
            //
            // lhsOriginal*x >= rhsFraction - constantPart

            double convertedRhs =
                rhsFraction - constantPart;

            explanation.AppendLine();

            explanation.AppendLine(
                "Before normalization:");

            string convertedExpression = string.Join(
                " + ",
                lhsOriginal
                    .Select((c, i) =>
                        $"{NumberFormatter.Format(c)}x{i + 1}")
                    .Where(s => !s.StartsWith("0")));

            explanation.AppendLine(
                $"{convertedExpression} >= " +
                $"{NumberFormatter.Format(convertedRhs)}");
            // Multiply by -1 so the cut is stored as <=.
            //
            // This is preferable because the existing ConstraintHandler
            // already handles <= cuts by adding a normal slack variable.

            double[] finalCoefficients =
                new double[program.Variables.Count];

            for (int i = 0;
                 i < lhsOriginal.Length;
                 i++)
            {
                finalCoefficients[i] =
                    -lhsOriginal[i];
            }

            double finalRhs =
                -convertedRhs;

            var cutConstraint =
                new LinearProgram.Constraint
                {
                    Coefficients =
                        finalCoefficients.ToList(),

                    Relation =
                        LinearProgram.Relation.LessThanOrEqual,

                    Rhs =
                        finalRhs
                };

            var terms =
                new List<string>();

            for (int i = 0;
                 i < finalCoefficients.Length;
                 i++)
            {
                double coefficient =
                    finalCoefficients[i];

                if (Math.Abs(coefficient) <= TOLERANCE)
                    continue;

                string term =
                    $"{NumberFormatter.Format(Math.Abs(coefficient))}x{i + 1}";

                if (terms.Count == 0)
                {
                    terms.Add(
                        coefficient >= 0
                            ? term
                            : "- " + term);
                }
                else
                {
                    terms.Add(
                        coefficient >= 0
                            ? "+ " + term
                            : "- " + term);
                }
            }

            explanation.AppendLine();

            explanation.AppendLine(
                "Final Gomory cut:");

            explanation.AppendLine(
                $"{string.Join(" ", terms)} <= " +
                $"{NumberFormatter.Format(finalRhs)}");

            explanation.AppendLine();

            explanation.AppendLine(
                "Cut added as a <= constraint.");

            return new CutInfo
            {
                Constraint = cutConstraint,
                Explanation = explanation.ToString()
            };
        }
        private double FractionalPart(double value)
        {
            double floor = Math.Floor(value);
            return value - floor;
        }

        private int FindBasicVariableRow(
            double[,] tableau,
            int variableColumn)
        {
            if (tableau == null)
                return -1;

            int rows =
                tableau.GetLength(0);

            for (int i = 1; i < rows; i++)
            {
                if (Math.Abs(
                        tableau[i, variableColumn] - 1.0)
                    > TOLERANCE)
                {
                    continue;
                }

                bool basic = true;

                for (int k = 0; k < rows; k++)
                {
                    if (k == i)
                        continue;

                    if (Math.Abs(
                            tableau[k, variableColumn])
                        > TOLERANCE)
                    {
                        basic = false;
                        break;
                    }
                }

                if (basic)
                    return i;
            }

            return -1;
        }

        private int GetVariableIndex(
            string variableName)
        {
            return int.Parse(
                variableName.Substring(1));
        }

        private List<string> GetTableauVariableNames(
            Solution solution,
            LinearProgram program)
        {
            var names =
                program.Variables
                    .Select(v => $"x{v.Index}")
                    .ToList();

            names.AddRange(
                Enumerable.Range(
                    1,
                    solution.SlackCount)
                .Select(i => $"s{i}"));

            names.AddRange(
                Enumerable.Range(
                    1,
                    solution.ExcessCount)
                .Select(i => $"e{i}"));

            names.AddRange(
                Enumerable.Range(
                    1,
                    solution.ArtificialCount)
                .Select(i => $"a{i}"));

            return names;
        }

        private LinearProgram.Constraint CloneConstraint(
            LinearProgram.Constraint constraint)
        {
            return new LinearProgram.Constraint
            {
                Coefficients =
                    new List<double>(
                        constraint.Coefficients),

                Relation =
                    constraint.Relation,

                Rhs =
                    constraint.Rhs,

                Slack =
                    constraint.Slack
            };
        }

        private string FormatSolution(
            Solution solution)
        {
            var sb =
                new StringBuilder();

            foreach (var value in
                solution.VariableValues
                    .OrderBy(k => k.Key))
            {
                sb.Append(
                    $"{value.Key} = " +
                    $"{NumberFormatter.Format(value.Value)}, ");
            }

            sb.Append(
                $"Z = " +
                $"{NumberFormatter.Format(solution.OptimalValue)}");

            return sb.ToString();
        }

        public string FormatCanonicalForm(
            LinearProgram program)
        {
            var sb =
                new StringBuilder();

            sb.AppendLine(
                "Canonical Form:");

            sb.Append("Z");

            for (int i = 0;
                 i < program.Variables.Count;
                 i++)
            {
                double coefficient =
                    program.IsMaximization
                        ? -program.Variables[i].Coefficient
                        : program.Variables[i].Coefficient;

                if (coefficient >= 0)
                    sb.Append(
                        $" + {NumberFormatter.Format(coefficient)}x{i + 1}");
                else
                    sb.Append(
                        $" - {NumberFormatter.Format(-coefficient)}x{i + 1}");
            }

            sb.AppendLine(" = 0");
            sb.AppendLine();

            int slackIndex = 1;

            foreach (var constraint in
                program.Constraints)
            {
                bool first = true;

                for (int j = 0;
                     j < constraint.Coefficients.Count &&
                     j < program.Variables.Count;
                     j++)
                {
                    double coefficient =
                        constraint.Coefficients[j];

                    if (Math.Abs(coefficient) <= TOLERANCE)
                        continue;

                    if (!first)
                    {
                        sb.Append(
                            coefficient >= 0 ? " + " : " - ");
                    }
                    else if (coefficient < 0)
                    {
                        sb.Append("-");
                    }

                    sb.Append(
                        $"{NumberFormatter.Format(Math.Abs(coefficient))}x{j + 1}");

                    first = false;
                }

                if (constraint.Relation ==
                    LinearProgram.Relation.LessThanOrEqual)
                {
                    sb.Append($" + s{slackIndex}");
                    slackIndex++;
                }
                else if (constraint.Relation ==
                         LinearProgram.Relation.GreaterThanOrEqual)
                {
                    sb.Append($" - e{slackIndex}");
                    slackIndex++;
                }

                sb.AppendLine(
                    $" = {NumberFormatter.Format(constraint.Rhs)}");
            }

            sb.AppendLine();
            sb.AppendLine(
                "Integer variables: " +
                string.Join(
                    ", ",
                    program.Variables
                        .Where(v =>
                            v.Type ==
                                LinearProgram.VariableType.Integer ||
                            v.Type ==
                                LinearProgram.VariableType.Binary)
                        .Select(v => $"x{v.Index}")));

            return sb.ToString();
        }

        private class CutInfo
        {
            public LinearProgram.Constraint Constraint { get; set; }
            public string Explanation { get; set; }
        }
    }
}