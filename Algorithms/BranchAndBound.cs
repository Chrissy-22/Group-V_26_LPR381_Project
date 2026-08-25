using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Group_V_26_LPR381_Project.Algorithms
{
    /// <summary>
    /// Branch and Bound algorithm for integer linear programming.
    ///
    /// The algorithm:
    /// 1. Solves the LP relaxation of the original problem.
    /// 2. Checks whether the solution is infeasible, unbounded, integer,
    ///    or fractional.
    /// 3. If fractional, branches on a fractional decision variable:
    ///       xi <= floor(value)
    ///       xi >= ceil(value)
    /// 4. Solves both resulting sub-problems using Dual Simplex.
    /// 5. Uses the best known integer solution as the incumbent/bound.
    /// 6. Continues until all branches have been processed or pruned.
    ///
    /// Sub-problems are processed depth-first.
    /// </summary>
    public class BranchAndBound : ISolver
    {
        private const double TOLERANCE = 1e-6;

        // Best integer solution found so far.
        private double _bestKnownValue;
        private bool _hasBestSolution;
        private Solution _bestIntegerSolution;

        private int _maxDepth = 30;

        public BranchAndBound()
        {
        }

        /// <summary>
        /// Solves the integer programming problem using Branch and Bound.
        /// </summary>
        public Solution Solve(LinearProgram program)
        {
            var finalSolution = new Solution();

            // Initialise incumbent.
            if (program.IsMaximization)
                _bestKnownValue = double.MinValue;
            else
                _bestKnownValue = double.MaxValue;

            _hasBestSolution = false;
            _bestIntegerSolution = null;

            //finalSolution.AddMessage("Running Branch and Bound algorithm...");
            finalSolution.AddMessage("");

            finalSolution.AddStep(
                "Canonical Form",
                FormatCanonicalForm(program)
            );

            finalSolution.AddMessage("");
            finalSolution.AddMessage("Branch and Bound Search");
            finalSolution.AddMessage("");

            // ---------------------------------------------------------
            // ROOT LP RELAXATION
            // ---------------------------------------------------------

            var rootDualSimplex = new DualSimplex();
            var rootSolution = rootDualSimplex.Solve(program);

            // Check whether the original LP relaxation can be solved.
            if (IsInfeasible(rootSolution))
            {
                finalSolution.AddGroupHeader("Sub-problem 0", 0);
                finalSolution.AddMessage("Result: INFEASIBLE");
                finalSolution.AddMessage(
                    "The original LP relaxation is infeasible."
                );
                finalSolution.AddMessage(
                    "Branch and Bound cannot continue."
                );

                return finalSolution;
            }

            if (IsUnbounded(rootSolution))
            {
                finalSolution.AddGroupHeader("Sub-problem 0", 0);
                finalSolution.AddMessage("Result: UNBOUNDED");
                finalSolution.AddMessage(
                    "The original LP relaxation is unbounded."
                );
                finalSolution.AddMessage(
                    "Branch and Bound cannot continue."
                );

                return finalSolution;
            }

            // ---------------------------------------------------------
            // SEARCH STACK
            // ---------------------------------------------------------

            var stack = new Stack<SubProblem>();

            stack.Push(new SubProblem
            {
                DualSimplex = rootDualSimplex,
                Solution = rootSolution,
                Path = "0",
                Level = 0,
                BranchConstraint = null,
                ParentBranchVariable = null,
                ParentBranchValue = null
            });

            // ---------------------------------------------------------
            // DEPTH-FIRST SEARCH
            // ---------------------------------------------------------

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current.Level > _maxDepth)
                {
                    finalSolution.AddGroupHeader(
                        $"Sub-problem {current.Path}",
                        current.Level
                    );

                    finalSolution.AddMessage(
                        $"Result: MAXIMUM DEPTH REACHED ({_maxDepth})."
                    );

                    finalSolution.AddMessage(
                        "This branch will not be explored further."
                    );

                    continue;
                }

                var children = ProcessSubProblem(
                    current,
                    finalSolution,
                    program
                );

                /*
                 * Stack is LIFO.
                 *
                 * If we want:
                 *
                 *     1.1
                 *     1.2
                 *
                 * to be processed in that order, 1.2 must be pushed first.
                 */
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i]);
                }
            }

            // ---------------------------------------------------------
            // FINAL RESULT
            // ---------------------------------------------------------

            finalSolution.AddGroupHeader(
                "Branch and Bound Complete",
                0
            );

            if (_bestIntegerSolution != null)
            {
                finalSolution.OptimalValue =
                    _bestIntegerSolution.OptimalValue;

                finalSolution.VariableValues =
                    _bestIntegerSolution.VariableValues;

                finalSolution.AddMessage(
                    $"Best integer solution found: " +
                    $"{NumberFormatter.Format(_bestIntegerSolution.OptimalValue)}"
                );

                finalSolution.AddMessage("");

                finalSolution.AddMessage(
                    "Optimal integer variable values:"
                );

                foreach (var variable in GetDecisionVariables(
                    _bestIntegerSolution))
                {
                    finalSolution.AddMessage(
                        $"  {variable.Key} = " +
                        $"{NumberFormatter.Format(variable.Value)}"
                    );
                }
            }
            else
            {
                finalSolution.AddMessage(
                    "No feasible integer solution was found."
                );
            }

            return finalSolution;
        }

        // =============================================================
        // PROCESS ONE SUB-PROBLEM
        // =============================================================

        private List<SubProblem> ProcessSubProblem(
    SubProblem current,
    Solution mainSolution,
    LinearProgram originalProgram)
        {
            var children = new List<SubProblem>();

            // ---------------------------------------------------------
            // HEADER
            // ---------------------------------------------------------

            mainSolution.AddGroupHeader(
                $"Sub-problem {current.Path}",
                current.Level
            );

            // ---------------------------------------------------------
            // BRANCH CONSTRAINT
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(current.BranchConstraint))
            {
                mainSolution.AddMessage(
                    $"Branch constraint: {current.BranchConstraint}"
                );

                if (!string.IsNullOrWhiteSpace(current.SiblingPath))
                {
                    mainSolution.AddMessage(
                        $"(Paired with Sub-problem {current.SiblingPath}: " +
                        $"{current.SiblingBranchConstraint})"
                    );
                }

                mainSolution.AddMessage("");
            }

            // ---------------------------------------------------------
            // SHOW FINAL TABLEAU
            // ---------------------------------------------------------

            AppendSubProblemTableau(
                mainSolution,
                current
            );

            // ---------------------------------------------------------
            // INFEASIBLE
            // ---------------------------------------------------------

            if (IsInfeasible(current.Solution))
            {
                mainSolution.AddMessage(
                    "Result: INFEASIBLE"
                );

                mainSolution.AddMessage(
                    "This branch is pruned. No further branching."
                );

                mainSolution.AddMessage("");

                return children;
            }

            // ---------------------------------------------------------
            // UNBOUNDED
            // ---------------------------------------------------------

            if (IsUnbounded(current.Solution))
            {
                mainSolution.AddMessage(
                    "Result: UNBOUNDED"
                );

                mainSolution.AddMessage(
                    "This branch is pruned. No further branching."
                );

                mainSolution.AddMessage("");

                return children;
            }

            // ---------------------------------------------------------
            // LP RELAXATION VALUE
            // ---------------------------------------------------------

            mainSolution.AddMessage(
                $"Sub-problem value: " +
                $"{NumberFormatter.Format(current.Solution.OptimalValue)}"
            );

            mainSolution.AddMessage("");

            // ---------------------------------------------------------
            // VARIABLE VALUES
            // ---------------------------------------------------------

            AppendVariableValues(
                mainSolution,
                current.Solution
            );

            // ---------------------------------------------------------
            // BOUND CHECK
            // ---------------------------------------------------------

            if (_hasBestSolution &&
                ShouldPrune(
                    current.Solution.OptimalValue,
                    _bestKnownValue,
                    originalProgram.IsMaximization))
            {
                mainSolution.AddMessage(
                    $"Result: PRUNED BY BOUND"
                );

                mainSolution.AddMessage(
                    $"Sub-problem value " +
                    $"{NumberFormatter.Format(current.Solution.OptimalValue)} " +
                    $"{(originalProgram.IsMaximization ? "<=" : ">=")} " +
                    $"current best integer value " +
                    $"{NumberFormatter.Format(_bestKnownValue)}."
                );

                mainSolution.AddMessage(
                    "No further branching."
                );

                mainSolution.AddMessage("");

                return children;
            }

            // ---------------------------------------------------------
            // FIND FRACTIONAL VARIABLE
            // ---------------------------------------------------------

            var fractionalVariable =
                FindMostFractionalVariable(
                    current.Solution,
                    originalProgram
                );

            // ---------------------------------------------------------
            // INTEGER SOLUTION
            // ---------------------------------------------------------

            if (fractionalVariable == null)
            {
                if (!_hasBestSolution ||
                    IsBetterSolution(
                        current.Solution.OptimalValue,
                        _bestKnownValue,
                        originalProgram.IsMaximization))
                {
                    _bestKnownValue =
                        current.Solution.OptimalValue;

                    _hasBestSolution = true;

                    _bestIntegerSolution =
                        current.Solution;

                    mainSolution.AddMessage(
                        "Result: INTEGER SOLUTION"
                    );

                    mainSolution.AddMessage(
                        "This is a new best integer solution."
                    );
                }
                else
                {
                    mainSolution.AddMessage(
                        "Result: INTEGER SOLUTION"
                    );

                    mainSolution.AddMessage(
                        $"This solution is not better than the " +
                        $"current best value of " +
                        $"{NumberFormatter.Format(_bestKnownValue)}."
                    );
                }

                mainSolution.AddMessage(
                    "No further branching."
                );

                mainSolution.AddMessage("");

                return children;
            }

            // ---------------------------------------------------------
            // FRACTIONAL SOLUTION
            // ---------------------------------------------------------

            string variableName = fractionalVariable.Item1;
            double variableValue = fractionalVariable.Item2;

            int variableIndex =
                GetVariableIndex(variableName);

            double floorValue =
                Math.Floor(variableValue);

            double ceilValue =
                Math.Ceiling(variableValue);

            mainSolution.AddMessage(
                $"Result: FRACTIONAL SOLUTION"
            );

            mainSolution.AddMessage(
                $"{variableName} = " +
                $"{NumberFormatter.Format(variableValue)} " +
                "is fractional."
            );

            mainSolution.AddMessage("");

            // Paths are computed up front so the branching summary below can name
            // both destinations together, instead of only mentioning the first.
            string lowerPath =
                string.IsNullOrEmpty(current.Path) ||
                current.Path == "0"
                    ? "1"
                    : current.Path + ".1";

            string upperPath =
                string.IsNullOrEmpty(current.Path) ||
                current.Path == "0"
                    ? "2"
                    : current.Path + ".2";

            string lowerConstraintText =
                $"{variableName} <= {NumberFormatter.Format(floorValue)}";

            string upperConstraintText =
                $"{variableName} >= {NumberFormatter.Format(ceilValue)}";

            mainSolution.AddMessage(
                "This sub-problem will be branched into two children:"
            );

            mainSolution.AddMessage(
                $"  Sub-problem {lowerPath}: {lowerConstraintText}"
            );

            mainSolution.AddMessage(
                $"  Sub-problem {upperPath}: {upperConstraintText}"
            );

            mainSolution.AddMessage("");

            // ---------------------------------------------------------
            // CREATE LOWER BRANCH
            // ---------------------------------------------------------

            try
            {
                var lowerDualSimplex =
                    CloneDualSimplexState(
                        current.DualSimplex
                    );

                var lowerConstraint =
                    CreateBranchConstraint(
                        originalProgram,
                        variableIndex,
                        LinearProgram.Relation.LessThanOrEqual,
                        floorValue
                    );

                var lowerSolution =
                    lowerDualSimplex.AddConstraintAndResolve(
                        lowerConstraint
                    );

                children.Add(
                    new SubProblem
                    {
                        DualSimplex = lowerDualSimplex,
                        Solution = lowerSolution,
                        Path = lowerPath,
                        Level = current.Level + 1,
                        BranchConstraint = lowerConstraintText,
                        ParentBranchVariable = variableName,
                        ParentBranchValue = floorValue
                    }
                );
            }
            catch (Exception ex)
            {
                mainSolution.AddMessage(
                    $"Error creating sub-problem " +
                    $"{lowerPath}: {ex.Message}"
                );
            }

            // ---------------------------------------------------------
            // CREATE UPPER BRANCH
            // ---------------------------------------------------------

            try
            {
                var upperDualSimplex =
                    CloneDualSimplexState(
                        current.DualSimplex
                    );

                var upperConstraint =
                    CreateBranchConstraint(
                        originalProgram,
                        variableIndex,
                        LinearProgram.Relation.GreaterThanOrEqual,
                        ceilValue
                    );

                var upperSolution =
                    upperDualSimplex.AddConstraintAndResolve(
                        upperConstraint
                    );

                children.Add(
                    new SubProblem
                    {
                        DualSimplex = upperDualSimplex,
                        Solution = upperSolution,
                        Path = upperPath,
                        Level = current.Level + 1,
                        BranchConstraint = upperConstraintText,
                        ParentBranchVariable = variableName,
                        ParentBranchValue = ceilValue
                    }
                );
            }
            catch (Exception ex)
            {
                mainSolution.AddMessage(
                    $"Error creating sub-problem " +
                    $"{upperPath}: {ex.Message}"
                );
            }

            // Link the pair together so each child's header can reference the other,
            // even though DFS means they won't print near each other in the output.
            if (children.Count == 2)
            {
                children[0].SiblingPath = children[1].Path;
                children[0].SiblingBranchConstraint = children[1].BranchConstraint;
                children[1].SiblingPath = children[0].Path;
                children[1].SiblingBranchConstraint = children[0].BranchConstraint;
            }

            // ---------------------------------------------------------
            // NEXT SUB-PROBLEM
            // ---------------------------------------------------------

            mainSolution.AddMessage(
                $"Sub-problem {lowerPath} will be explored first (depth-first); " +
                $"Sub-problem {upperPath} follows once {lowerPath}'s branch is fully resolved."
            );

            mainSolution.AddMessage("");

            return children;
        }

        // =============================================================
        // CREATE BRANCH CONSTRAINT
        // =============================================================

        private LinearProgram.Constraint CreateBranchConstraint(
            LinearProgram originalProgram,
            int variableIndex,
            LinearProgram.Relation relation,
            double rhs)
        {
            var constraint =
                new LinearProgram.Constraint();

            for (int i = 0;
                 i < originalProgram.Variables.Count;
                 i++)
            {
                constraint.Coefficients.Add(0);
            }

            constraint.Coefficients[
                variableIndex - 1
            ] = 1;

            constraint.Relation = relation;
            constraint.Rhs = rhs;

            return constraint;
        }

        // =============================================================
        // TABLEAU
        // =============================================================

        private void AppendSubProblemTableau(
            Solution mainSolution,
            SubProblem current)
        {
            if (current.Solution == null)
                return;

            if (current.Solution.IterationTableaux == null)
                return;

            if (current.Solution.IterationTableaux.Count == 0)
                return;

            int last =
                current.Solution.IterationTableaux.Count - 1;

            var headers =
                last <
                current.Solution.IterationColumnHeaders.Count
                    ? current.Solution.IterationColumnHeaders[last]
                    : null;

            mainSolution.AddIteration(
                current.Solution.IterationTableaux[last],
                "Final Pivot Table",
                -1,
                -1,
                headers
            );
        }

        // =============================================================
        // VARIABLE VALUES
        // =============================================================

        private void AppendVariableValues(
            Solution mainSolution,
            Solution solution)
        {
            if (solution.VariableValues == null ||
                solution.VariableValues.Count == 0)
            {
                return;
            }

            mainSolution.AddMessage(
                "Variable values:"
            );

            var decisionVariables =
                GetDecisionVariables(solution);

            foreach (var variable in decisionVariables)
            {
                mainSolution.AddMessage(
                    $"  {variable.Key} = " +
                    $"{NumberFormatter.Format(variable.Value)}"
                );
            }

            mainSolution.AddMessage("");
        }

        private List<KeyValuePair<string, double>>
            GetDecisionVariables(Solution solution)
        {
            return solution.VariableValues
                .Where(kvp =>
                    kvp.Key.StartsWith("x",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(kvp =>
                    GetVariableIndex(kvp.Key))
                .ToList();
        }

        // =============================================================
        // INFEASIBILITY
        // =============================================================

        private bool IsInfeasible(Solution solution)
        {
            if (solution == null)
                return true;

            if (solution.Messages == null)
                return false;

            return solution.Messages.Any(
                m => m.IndexOf(
                    "infeasible",
                    StringComparison.OrdinalIgnoreCase) >= 0
            );
        }

        // =============================================================
        // UNBOUNDED
        // =============================================================

        private bool IsUnbounded(Solution solution)
        {
            if (solution == null)
                return false;

            if (solution.Messages == null)
                return false;

            return solution.Messages.Any(
                m => m.IndexOf(
                    "unbounded",
                    StringComparison.OrdinalIgnoreCase) >= 0
            );
        }

        // =============================================================
        // BOUND
        // =============================================================

        private bool ShouldPrune(
            double currentValue,
            double bestKnownValue,
            bool isMaximization)
        {
            if (isMaximization)
            {
                return currentValue <=
                       bestKnownValue + TOLERANCE;
            }

            return currentValue >=
                   bestKnownValue - TOLERANCE;
        }

        // =============================================================
        // FIND FRACTIONAL VARIABLE
        // =============================================================

        private Tuple<string, double>
            FindMostFractionalVariable(
                Solution solution,
                LinearProgram program)
        {
            if (solution == null ||
                solution.VariableValues == null)
            {
                return null;
            }

            string selectedVariable = null;
            double selectedValue = 0;

            double closestToHalf =
                double.MaxValue;

            int lowestVariableIndex =
                int.MaxValue;

            // Only branch on variables actually declared Integer or Binary.
            // Some formulations (e.g. the piecewise-linear MIP used for
            // non-linear problems) deliberately mix continuous "weight"
            // variables with binary "selector" variables in the same
            // problem - only the selectors should ever be forced integer.
            // Any variable index that doesn't map cleanly falls back to the
            // old behaviour (branch on it) so nothing existing breaks.
            var decisionVariables =
                solution.VariableValues
                    .Where(kvp =>
                        kvp.Key.StartsWith(
                            "x",
                            StringComparison.OrdinalIgnoreCase))
                    .Where(kvp =>
                    {
                        int idx = GetVariableIndex(kvp.Key) - 1;
                        if (idx < 0 || idx >= program.Variables.Count)
                            return true;

                        var type = program.Variables[idx].Type;
                        return type == LinearProgram.VariableType.Integer ||
                               type == LinearProgram.VariableType.Binary;
                    })
                    .OrderBy(kvp =>
                        GetVariableIndex(kvp.Key))
                    .ToList();

            foreach (var variable in decisionVariables)
            {
                double value = variable.Value;

                double fractionalPart =
                    value - Math.Floor(value);

                // Ignore values which are effectively integers.
                if (fractionalPart <= TOLERANCE ||
                    fractionalPart >= 1 - TOLERANCE)
                {
                    continue;
                }

                double distanceFromHalf =
                    Math.Abs(
                        fractionalPart - 0.5
                    );

                int variableIndex =
                    GetVariableIndex(variable.Key);

                /*
                 * Most fractional variable rule:
                 *
                 * Choose the variable whose fractional part
                 * is closest to 0.5.
                 *
                 * If tied, choose the lowest variable number.
                 */
                if (
                    distanceFromHalf < closestToHalf ||
                    (
                        Math.Abs(
                            distanceFromHalf -
                            closestToHalf
                        ) < TOLERANCE &&
                        variableIndex <
                        lowestVariableIndex
                    )
                )
                {
                    closestToHalf =
                        distanceFromHalf;

                    selectedVariable =
                        variable.Key;

                    selectedValue =
                        value;

                    lowestVariableIndex =
                        variableIndex;
                }
            }

            if (selectedVariable == null)
                return null;

            return Tuple.Create(
                selectedVariable,
                selectedValue
            );
        }

        // =============================================================
        // VARIABLE INDEX
        // =============================================================

        private int GetVariableIndex(
            string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
                return int.MaxValue;

            return int.Parse(
                variableName.Substring(1)
            );
        }

        // =============================================================
        // COMPARE SOLUTIONS
        // =============================================================

        private bool IsBetterSolution(
            double newValue,
            double currentBest,
            bool isMaximization)
        {
            if (isMaximization)
                return newValue >
                       currentBest + TOLERANCE;

            return newValue <
                   currentBest - TOLERANCE;
        }

        // =============================================================
        // CLONE DUAL SIMPLEX
        // =============================================================

        private DualSimplex CloneDualSimplexState(
            DualSimplex original)
        {
            return original.Clone();
        }

        // =============================================================
        // CANONICAL FORM
        // =============================================================

        public string FormatCanonicalForm(
            LinearProgram program)
        {
            var sb =
                new StringBuilder();

            sb.AppendLine(
                "Canonical Form (with slack variables):"
            );

            sb.Append("z");

            for (int i = 0;
                 i < program.Variables.Count;
                 i++)
            {
                double coefficient =
                    program.IsMaximization
                        ? -program.Variables[i].Coefficient
                        : program.Variables[i].Coefficient;

                if (coefficient >= 0)
                {
                    sb.Append(
                        $" + " +
                        $"{NumberFormatter.Format(coefficient)}x{i + 1}"
                    );
                }
                else
                {
                    sb.Append(
                        " - " +
                        NumberFormatter.Format(Math.Abs(coefficient)) +
                        "x" + (i + 1)
                    );
                }
            }

            sb.AppendLine(" = 0");

            int slackIndex = 1;

            for (int i = 0;
                 i < program.Constraints.Count;
                 i++)
            {
                var constraint =
                    program.Constraints[i];

                bool first = true;

                for (int j = 0;
                     j < constraint.Coefficients.Count &&
                     j < program.Variables.Count;
                     j++)
                {
                    double coefficient =
                        constraint.Coefficients[j];

                    if (Math.Abs(coefficient) <
                        TOLERANCE)
                    {
                        continue;
                    }

                    if (first)
                    {
                        sb.Append(
                            " - " +
                            NumberFormatter.Format(Math.Abs(coefficient)) +
                            "x" + (i + 1)
                        );

                        first = false;
                    }
                    else
                    {
                        if (coefficient >= 0)
                        {
                            sb.Append(
                                " + " +
                                NumberFormatter.Format(coefficient) +
                                "x" + (j + 1)
                            );
                        }
                        else
                        {
                            sb.Append(
                                " - " +
                                NumberFormatter.Format(Math.Abs(coefficient)) +
                                "x" + (j + 1)
                            );
                        }
                    }
                }

                if (
                    constraint.Relation ==
                    LinearProgram.Relation.LessThanOrEqual
                )
                {
                    sb.Append(
                        $" + s{slackIndex}"
                    );

                    slackIndex++;
                }
                else if (
                    constraint.Relation ==
                    LinearProgram.Relation.GreaterThanOrEqual
                )
                {
                    sb.Append(
                        $" - s{slackIndex}"
                    );

                    slackIndex++;
                }

                sb.AppendLine(
                    " = " +
                    NumberFormatter.Format(constraint.Rhs)
                );
            }

            sb.AppendLine(
                "All variables >= 0"
            );

            return sb.ToString();
        }

        // =============================================================
        // SUB-PROBLEM
        // =============================================================

        private class SubProblem
        {
            public DualSimplex DualSimplex { get; set; }

            public Solution Solution { get; set; }

            public string Path { get; set; }

            public int Level { get; set; }

            public string BranchConstraint { get; set; }

            public string ParentBranchVariable { get; set; }

            public double? ParentBranchValue { get; set; }

            /// <summary>Path of this sub-problem's sibling (the other branch created
            /// from the same parent), so its header can reference where its pair
            /// went even though DFS means the sibling won't print nearby.</summary>
            public string SiblingPath { get; set; }

            /// <summary>The sibling's branch constraint, shown alongside SiblingPath.</summary>
            public string SiblingBranchConstraint { get; set; }
        }
    }
}