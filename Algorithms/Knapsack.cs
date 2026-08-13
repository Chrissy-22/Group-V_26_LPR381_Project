using Group_V_26_LPR381_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinearProgrammingSolver.Algorithms
{
    public class Knapsack
    {
        private class Item
        {
            public int OriginalIndex; // 1-based index
            public double Value;
            public double Weight;
            public double Ratio => Weight > 0 ? Value / Weight : double.PositiveInfinity;
        }

        private class Node
        {
            public double Weight;
            public double Value;
            public double Bound;
            public bool?[] Decisions; // null=undecided, true=take, false=reject
            public string Id;
            public List<(int Index, bool Take)> DecisionsMade = new List<(int, bool)>();
            public int Level; // Current decision level
        }

        private List<Item> items;
        private double capacity;

        public Solution Solve(LinearProgram program)
        {
            if (!program.isKnapsackProblem || !program.WeightConstraints.Any())
                throw new ArgumentException("Not a knapsack problem or weight constraints missing.");

            int n = program.Variables.Count;
            var values = program.Variables.Select(v => v.Coefficient).ToArray();
            var weights = program.WeightConstraints[0].Coefficients.ToArray();
            capacity = program.WeightConstraints[0].Capacity;

            Solution sol = new Solution();
            sol.AddMessage("Running Branch and Bound Knapsack algorithm...");
            sol.AddMessage("");

            items = new List<Item>(n);
            for (int i = 0; i < n; i++)
                items.Add(new Item { OriginalIndex = i + 1, Value = values[i], Weight = weights[i] });

            // Ratio test
            sol.AddGroupHeader("Ratio Test", 0);
            var sortedItems = items.OrderByDescending(i => i.Ratio).ToList();
            int rank = 1;
            foreach (var item in sortedItems)
                sol.AddMessage($"x{item.OriginalIndex}: {NumberFormatter.Format(item.Value)} / {NumberFormatter.Format(item.Weight)} = {NumberFormatter.Format(item.Ratio)}  (Rank {rank++})");

            // Root relaxation
            var rootRelaxation = ComputeLPRelaxation(capacity, 0, 0, new bool?[n]);
            sol.AddGroupHeader("Sub-problem 0", 0);
            DisplayLPRelaxation(sol, new bool?[n], capacity, 0, 0, new List<(int, bool)>(), false);

            if (rootRelaxation.fractionalItem != null)
            {
                sol.AddMessage("");
                sol.AddMessage($"This sub-problem will be branched on x{rootRelaxation.fractionalItem.OriginalIndex}:");
                sol.AddMessage($"  Branch 1: x{rootRelaxation.fractionalItem.OriginalIndex} = 0");
                sol.AddMessage($"  Branch 2: x{rootRelaxation.fractionalItem.OriginalIndex} = 1");
            }

            var queue = new Queue<Node>();
            var root = new Node { Weight = 0, Value = 0, Decisions = new bool?[n], Id = "0", Level = 0 };
            queue.Enqueue(root);

            double bestValue = double.NegativeInfinity;
            bool[] bestSolution = new bool[n];
            var candidates = new List<(string Id, double Value, bool[] Decisions, string Label)>();
            char candidateLabel = 'A';

            while (queue.Count > 0)
            {
                Node node = queue.Dequeue();

                if (node.Id == "0")
                {
                    if (rootRelaxation.fractionalItem != null)
                    {
                        int branchIndex = items.FindIndex(it => it.OriginalIndex == rootRelaxation.fractionalItem.OriginalIndex);

                        var skipNode = new Node { Weight = 0, Value = 0, Decisions = new bool?[n], Id = "1", Level = 1 };
                        skipNode.Decisions[branchIndex] = false;
                        skipNode.DecisionsMade.Add((branchIndex, false));
                        queue.Enqueue(skipNode);

                        var takeNode = new Node { Weight = rootRelaxation.fractionalItem.Weight, Value = rootRelaxation.fractionalItem.Value, Decisions = new bool?[n], Id = "2", Level = 1 };
                        takeNode.Decisions[branchIndex] = true;
                        takeNode.DecisionsMade.Add((branchIndex, true));
                        queue.Enqueue(takeNode);
                    }
                    continue;
                }

                int depth = node.Id.Count(c => c == '.') + 1;

                if (node.Weight > capacity)
                {
                    sol.AddGroupHeader($"Sub-problem {node.Id}", depth);
                    sol.AddMessage("Result: INFEASIBLE (exceeds capacity). Pruned.");
                    continue;
                }

                var relaxation = ComputeLPRelaxation(capacity - node.Weight, node.Weight, node.Value, node.Decisions);
                node.Bound = node.Value + relaxation.addedValue;

                string decisionsStr = string.Join(", ", node.DecisionsMade.Select(d => $"x{items[d.Index].OriginalIndex} = {(d.Take ? 1 : 0)}"));
                sol.AddGroupHeader(string.IsNullOrEmpty(decisionsStr) ? $"Sub-problem {node.Id}" : $"Sub-problem {node.Id}: {decisionsStr}", depth);

                DisplayLPRelaxation(sol, node.Decisions, capacity, node.Weight, node.Value, node.DecisionsMade, true);

                if (!relaxation.hasFraction)
                {
                    double totalValue = node.Value + relaxation.takenItems.Sum(i => i.Value);
                    bool[] fullDecisions = new bool[n];
                    for (int i = 0; i < n; i++) fullDecisions[i] = node.Decisions[i] ?? false;
                    foreach (var item in relaxation.takenItems)
                    {
                        int idx = items.FindIndex(it => it.OriginalIndex == item.OriginalIndex);
                        if (idx >= 0) fullDecisions[idx] = true;
                    }

                    var takenItems = items.Where((item, j) => fullDecisions[j]).OrderBy(item => item.OriginalIndex);
                    string valueStr = string.Join(" + ", takenItems.Select(item => NumberFormatter.Format(item.Value)));

                    sol.AddMessage("");
                    sol.AddMessage($"Result: INTEGER SOLUTION - z = {valueStr} = {NumberFormatter.Format(totalValue)}");
                    sol.AddMessage($"Candidate {candidateLabel}");

                    candidates.Add((node.Id, totalValue, (bool[])fullDecisions.Clone(), candidateLabel.ToString()));
                    candidateLabel++;

                    if (totalValue > bestValue)
                    {
                        bestValue = totalValue;
                        bestSolution = (bool[])fullDecisions.Clone();
                    }
                    continue;
                }

                if (relaxation.fractionalItem != null && node.Level < n)
                {
                    int branchIndex = items.FindIndex(it => it.OriginalIndex == relaxation.fractionalItem.OriginalIndex);
                    if (branchIndex < 0 || node.Decisions[branchIndex].HasValue) continue;

                    string skipId = $"{node.Id}.1";
                    string takeId = $"{node.Id}.2";

                    sol.AddMessage("");
                    sol.AddMessage($"This sub-problem will be branched on x{relaxation.fractionalItem.OriginalIndex}:");
                    sol.AddMessage($"  Branch {skipId}: x{relaxation.fractionalItem.OriginalIndex} = 0");
                    sol.AddMessage($"  Branch {takeId}: x{relaxation.fractionalItem.OriginalIndex} = 1");

                    var skipNode = new Node { Weight = node.Weight, Value = node.Value, Decisions = (bool?[])node.Decisions.Clone(), Id = skipId, DecisionsMade = new List<(int, bool)>(node.DecisionsMade), Level = node.Level + 1 };
                    skipNode.DecisionsMade.Add((branchIndex, false));
                    skipNode.Decisions[branchIndex] = false;
                    queue.Enqueue(skipNode);

                    var takeNode = new Node { Weight = node.Weight + relaxation.fractionalItem.Weight, Value = node.Value + relaxation.fractionalItem.Value, Decisions = (bool?[])node.Decisions.Clone(), Id = takeId, DecisionsMade = new List<(int, bool)>(node.DecisionsMade), Level = node.Level + 1 };
                    takeNode.DecisionsMade.Add((branchIndex, true));
                    takeNode.Decisions[branchIndex] = true;
                    queue.Enqueue(takeNode);
                }
            }

            sol.AddGroupHeader("Comparison of Candidates", 0);
            foreach (var candidate in candidates)
            {
                var takenItems = items.Where((item, j) => candidate.Decisions[j]).OrderBy(item => item.OriginalIndex);
                string valueStr = string.Join(" + ", takenItems.Select(item => NumberFormatter.Format(item.Value)));
                sol.AddMessage($"Candidate {candidate.Label}: z = {valueStr} = {NumberFormatter.Format(candidate.Value)}");
            }

            if (candidates.Any())
            {
                var bestCandidate = candidates.OrderByDescending(c => c.Value).First();
                sol.AddMessage("");
                sol.AddMessage($"Candidate {bestCandidate.Label} is the best candidate.");
            }

            sol.OptimalValue = bestValue;
            sol.VariableValues = items.ToDictionary(i => $"x{i.OriginalIndex}", i => bestSolution[items.IndexOf(i)] ? 1.0 : 0.0);

            return sol;
        }

        private void DisplayLPRelaxation(Solution sol, bool?[] decisions, double totalCapacity, double usedWeight, double currentValue, List<(int, bool)> decisionsMade, bool showFixed)
        {
            double remainingCapacity = totalCapacity - usedWeight;

            if (showFixed)
            {
                foreach (var decision in decisionsMade)
                {
                    Item item = items[decision.Item1];
                    if (decision.Item2)
                    {
                        sol.AddMessage($"  x{item.OriginalIndex} = 1   (capacity: {NumberFormatter.Format(totalCapacity)} - {NumberFormatter.Format(item.Weight)} = {NumberFormatter.Format(totalCapacity - item.Weight)})");
                        remainingCapacity = totalCapacity - item.Weight;
                        totalCapacity = remainingCapacity;
                    }
                    else
                    {
                        sol.AddMessage($"  x{item.OriginalIndex} = 0   (capacity unchanged: {NumberFormatter.Format(totalCapacity)})");
                    }
                }
            }

            var undecided = items.Where((item, index) => !decisions[index].HasValue).OrderByDescending(i => i.Ratio).ToList();
            foreach (var item in undecided)
            {
                if (remainingCapacity <= 0)
                {
                    sol.AddMessage($"  x{item.OriginalIndex} = 0   (no capacity remaining)");
                }
                else if (item.Weight <= remainingCapacity)
                {
                    sol.AddMessage($"  x{item.OriginalIndex} = 1   (capacity: {NumberFormatter.Format(remainingCapacity)} - {NumberFormatter.Format(item.Weight)} = {NumberFormatter.Format(remainingCapacity - item.Weight)})");
                    remainingCapacity -= item.Weight;
                }
                else
                {
                    double fraction = remainingCapacity / item.Weight;
                    sol.AddMessage($"  x{item.OriginalIndex} = {NumberFormatter.Format(fraction)}   (fractional: {NumberFormatter.Format(remainingCapacity)} / {NumberFormatter.Format(item.Weight)})");
                    remainingCapacity = 0;
                }
            }
        }

        private (Item fractionalItem, double addedValue, List<Item> takenItems, bool hasFraction) ComputeLPRelaxation(double remainingCapacity, double curWeight, double curValue, bool?[] decisions)
        {
            var undecided = items.Where((item, index) => !decisions[index].HasValue).OrderByDescending(i => i.Ratio).ToList();
            double addedValue = 0;
            double tempCapacity = remainingCapacity;
            List<Item> takenItems = new List<Item>();
            Item fractionalItem = null;
            bool hasFraction = false;

            foreach (var item in undecided)
            {
                if (item.Weight <= tempCapacity)
                {
                    tempCapacity -= item.Weight;
                    addedValue += item.Value;
                    takenItems.Add(item);
                }
                else if (tempCapacity > 0)
                {
                    // Fractional assignment
                    addedValue += item.Value * (tempCapacity / item.Weight);
                    fractionalItem = item;
                    hasFraction = true;
                    break;
                }
            }

            return (fractionalItem, addedValue, takenItems, hasFraction);
        }
    }
}