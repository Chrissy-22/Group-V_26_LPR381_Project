using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Group_V_26_LPR381_Project.Models
{
    /// <summary>
    /// Parses and evaluates an expression in n variables (x1, x2, ..., xn), and
    /// supports exact symbolic partial differentiation with respect to any one
    /// variable (treating the others as constants) - the building block for the
    /// multi-variable Hessian matrix.
    ///
    /// A bare "x" (no digit) is treated as x1, so single-variable expressions
    /// parse here too if needed, but the project's single-variable path
    /// (NonLinearFunction / GoldenSectionSearch) should still be preferred for
    /// those, since it's what's wired into the "nlp <max|min> <expr>" + bounds
    /// input format.
    /// </summary>
    public class MultiVariableNonLinearFunction
    {
        private readonly string _expression;
        private readonly Node _root;

        /// <summary>Highest variable index (x_n) referenced anywhere in the expression.</summary>
        public int VariableCount { get; }

        public MultiVariableNonLinearFunction(string expression)
        {
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            var tokens = Tokenize(_expression);
            int pos = 0;
            _root = ParseExpression(tokens, ref pos);
            if (pos != tokens.Count)
                throw new ArgumentException($"Unexpected token '{tokens[pos]}' in expression '{_expression}'");

            var found = new HashSet<int>();
            _root.CollectVariableIndices(found);
            VariableCount = found.Count == 0 ? 0 : found.Max();
        }

        private MultiVariableNonLinearFunction(Node root, int variableCount)
        {
            _root = root;
            _expression = null;
            VariableCount = variableCount;
        }

        /// <summary>x must have length >= VariableCount; x[i] supplies x_(i+1).</summary>
        public double Evaluate(double[] x) => _root.Evaluate(x);

        /// <summary>Exact symbolic partial derivative w.r.t. x_(variableIndex) (1-based).</summary>
        public MultiVariableNonLinearFunction PartialDerivative(int variableIndex) =>
            new MultiVariableNonLinearFunction(_root.Differentiate(variableIndex).Simplify(), VariableCount);

        /// <summary>
        /// Attempts to expand this expression into a fully-collected
        /// MultivariatePolynomial (e.g. -(x1-3)^2 becomes -x1^2+6x1-9). Only
        /// succeeds for genuine polynomial expressions (+, -, *, / by a constant,
        /// ^ by a non-negative integer constant); returns null for anything
        /// involving sin/cos/sqrt/exp/ln or division/exponentiation by a
        /// non-constant, since those can't be represented as a finite polynomial.
        /// </summary>
        public MultivariatePolynomial TryExpandPolynomial() => _root.TryExpandPolynomial(Math.Max(VariableCount, 1));

        public override string ToString() => _expression ?? _root.ToDisplayString();

        /// <summary>True if the expression references x1, x2, ... (i.e. is genuinely multi-variable).</summary>
        public static bool LooksMultiVariable(string expression) =>
            Regex.IsMatch(expression, @"\bx\d+\b");

        // ===================== Tokenizer =====================
        private static List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            int i = 0;
            while (i < expr.Length)
            {
                char c = expr[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }

                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.')) i++;
                    tokens.Add(expr.Substring(start, i - start));
                    if (i < expr.Length && (char.IsLetter(expr[i]) || expr[i] == '('))
                        tokens.Add("*");
                    continue;
                }

                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < expr.Length && char.IsLetter(expr[i])) i++;
                    string word = expr.Substring(start, i - start).ToLowerInvariant();

                    if (word == "x")
                    {
                        // Variable: "x" optionally followed immediately by digits (x1, x2, ...).
                        // A bare "x" with no digits is treated as x1.
                        int digitStart = i;
                        while (i < expr.Length && char.IsDigit(expr[i])) i++;
                        string suffix = expr.Substring(digitStart, i - digitStart);
                        string varToken = suffix.Length > 0 ? "x" + suffix : "x1";
                        tokens.Add(varToken);
                        if (i < expr.Length && expr[i] == '(')
                            tokens.Add("*");
                        continue;
                    }

                    if (word == "sin" || word == "cos" || word == "tan" ||
                        word == "sqrt" || word == "exp" || word == "ln")
                    {
                        tokens.Add(word);
                        continue;
                    }

                    throw new ArgumentException($"Unknown identifier '{word}' in expression '{expr}'");
                }

                if ("+-*/^()".IndexOf(c) >= 0)
                {
                    tokens.Add(c.ToString());
                    i++;
                    continue;
                }

                throw new ArgumentException($"Unexpected character '{c}' in expression");
            }
            return tokens;
        }

        // ===================== Recursive-descent parser =====================
        private static Node ParseExpression(List<string> tokens, ref int pos)
        {
            var node = ParseTerm(tokens, ref pos);
            while (pos < tokens.Count && (tokens[pos] == "+" || tokens[pos] == "-"))
            {
                string op = tokens[pos++];
                var right = ParseTerm(tokens, ref pos);
                node = new BinaryNode(op, node, right);
            }
            return node;
        }

        private static Node ParseTerm(List<string> tokens, ref int pos)
        {
            var node = ParsePower(tokens, ref pos);
            while (pos < tokens.Count && (tokens[pos] == "*" || tokens[pos] == "/"))
            {
                string op = tokens[pos++];
                var right = ParsePower(tokens, ref pos);
                node = new BinaryNode(op, node, right);
            }
            return node;
        }

        private static Node ParsePower(List<string> tokens, ref int pos)
        {
            var node = ParseUnary(tokens, ref pos);
            if (pos < tokens.Count && tokens[pos] == "^")
            {
                pos++;
                var right = ParsePower(tokens, ref pos); // right-associative
                node = new BinaryNode("^", node, right);
            }
            return node;
        }

        private static Node ParseUnary(List<string> tokens, ref int pos)
        {
            if (pos < tokens.Count && tokens[pos] == "-")
            {
                pos++;
                return new NegateNode(ParseUnary(tokens, ref pos));
            }
            return ParsePrimary(tokens, ref pos);
        }

        private static Node ParsePrimary(List<string> tokens, ref int pos)
        {
            if (pos >= tokens.Count)
                throw new ArgumentException("Unexpected end of expression");

            string token = tokens[pos];

            if (token == "(")
            {
                pos++;
                var inner = ParseExpression(tokens, ref pos);
                if (pos >= tokens.Count || tokens[pos] != ")")
                    throw new ArgumentException("Missing closing parenthesis");
                pos++;
                return inner;
            }

            if (Regex.IsMatch(token, @"^x\d+$"))
            {
                pos++;
                int index = int.Parse(token.Substring(1), CultureInfo.InvariantCulture);
                return new VariableNode(index);
            }

            if (token == "sin" || token == "cos" || token == "tan" ||
                token == "sqrt" || token == "exp" || token == "ln")
            {
                pos++;
                if (pos >= tokens.Count || tokens[pos] != "(")
                    throw new ArgumentException($"Expected '(' after '{token}'");
                pos++;
                var argument = ParseExpression(tokens, ref pos);
                if (pos >= tokens.Count || tokens[pos] != ")")
                    throw new ArgumentException($"Missing closing parenthesis for '{token}'");
                pos++;
                return new FunctionNode(token, argument);
            }

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                pos++;
                return new ConstantNode(value);
            }

            throw new ArgumentException($"Unexpected token '{token}'");
        }

        // ===================== Expression tree =====================
        private static bool IsConstantValue(Node n, double value) =>
            n is ConstantNode c && Math.Abs(c.Evaluate(null) - value) < 1e-12;

        private abstract class Node
        {
            public abstract double Evaluate(double[] x);
            public abstract Node Differentiate(int wrtIndex);
            public abstract Node Simplify();
            public abstract string ToDisplayString();
            public abstract void CollectVariableIndices(HashSet<int> found);

            /// <summary>Attempts to expand this subtree into a MultivariatePolynomial
            /// over nVars variables; returns null if not polynomial-representable
            /// (e.g. contains sin/cos/sqrt/exp/ln, or division/power by a non-constant).</summary>
            public abstract MultivariatePolynomial TryExpandPolynomial(int nVars);
        }

        private class ConstantNode : Node
        {
            private readonly double _value;
            public ConstantNode(double value) { _value = value; }
            public override double Evaluate(double[] x) => _value;
            public override Node Differentiate(int wrtIndex) => new ConstantNode(0);
            public override Node Simplify() => this;
            public override string ToDisplayString() => _value.ToString(CultureInfo.InvariantCulture);
            public override void CollectVariableIndices(HashSet<int> found) { }
            public override MultivariatePolynomial TryExpandPolynomial(int nVars) => MultivariatePolynomial.Constant(_value, nVars);
        }

        private class VariableNode : Node
        {
            public int Index { get; }
            public VariableNode(int index) { Index = index; }
            public override double Evaluate(double[] x) => x[Index - 1];
            public override Node Differentiate(int wrtIndex) => new ConstantNode(Index == wrtIndex ? 1 : 0);
            public override Node Simplify() => this;
            public override string ToDisplayString() => "x" + Index;
            public override void CollectVariableIndices(HashSet<int> found) => found.Add(Index);
            public override MultivariatePolynomial TryExpandPolynomial(int nVars) => MultivariatePolynomial.Variable(Index, nVars);
        }

        private class NegateNode : Node
        {
            private readonly Node _inner;
            public NegateNode(Node inner) { _inner = inner; }
            public Node Inner => _inner;
            public override double Evaluate(double[] x) => -_inner.Evaluate(x);
            public override Node Differentiate(int wrtIndex) => new NegateNode(_inner.Differentiate(wrtIndex));
            public override string ToDisplayString() => $"-({_inner.ToDisplayString()})";
            public override void CollectVariableIndices(HashSet<int> found) => _inner.CollectVariableIndices(found);

            public override MultivariatePolynomial TryExpandPolynomial(int nVars)
            {
                var inner = _inner.TryExpandPolynomial(nVars);
                return inner?.Negate();
            }

            public override Node Simplify()
            {
                var inner = _inner.Simplify();
                if (inner is ConstantNode c) return new ConstantNode(-c.Evaluate(null));
                if (inner is NegateNode nn) return nn.Inner; // double negation cancels
                return new NegateNode(inner);
            }
        }

        private class FunctionNode : Node
        {
            private readonly string _name;
            private readonly Node _argument;
            public FunctionNode(string name, Node argument) { _name = name; _argument = argument; }

            public override double Evaluate(double[] x)
            {
                double arg = _argument.Evaluate(x);
                switch (_name)
                {
                    case "sin": return Math.Sin(arg);
                    case "cos": return Math.Cos(arg);
                    case "tan": return Math.Tan(arg);
                    case "sqrt": return Math.Sqrt(arg);
                    case "exp": return Math.Exp(arg);
                    case "ln": return Math.Log(arg);
                    default: throw new InvalidOperationException($"Unknown function '{_name}'");
                }
            }

            public override Node Differentiate(int wrtIndex)
            {
                Node uPrime = _argument.Differentiate(wrtIndex);
                Node outerDerivative;
                switch (_name)
                {
                    case "sin":
                        outerDerivative = new FunctionNode("cos", _argument);
                        break;
                    case "cos":
                        outerDerivative = new NegateNode(new FunctionNode("sin", _argument));
                        break;
                    case "tan":
                        outerDerivative = new BinaryNode("/", new ConstantNode(1),
                            new BinaryNode("^", new FunctionNode("cos", _argument), new ConstantNode(2)));
                        break;
                    case "sqrt":
                        outerDerivative = new BinaryNode("/", new ConstantNode(1),
                            new BinaryNode("*", new ConstantNode(2), new FunctionNode("sqrt", _argument)));
                        break;
                    case "exp":
                        outerDerivative = new FunctionNode("exp", _argument);
                        break;
                    case "ln":
                        outerDerivative = new BinaryNode("/", new ConstantNode(1), _argument);
                        break;
                    default:
                        throw new InvalidOperationException($"Cannot differentiate unknown function '{_name}'");
                }
                return new BinaryNode("*", outerDerivative, uPrime);
            }

            // sin/cos/tan/sqrt/exp/ln of a variable expression are never polynomial.
            public override MultivariatePolynomial TryExpandPolynomial(int nVars) => null;

            public override Node Simplify()
            {
                var arg = _argument.Simplify();
                if (arg is ConstantNode c)
                {
                    double x = c.Evaluate(null);
                    double val;
                    switch (_name)
                    {
                        case "sin": val = Math.Sin(x); break;
                        case "cos": val = Math.Cos(x); break;
                        case "tan": val = Math.Tan(x); break;
                        case "sqrt": val = Math.Sqrt(x); break;
                        case "exp": val = Math.Exp(x); break;
                        case "ln": val = Math.Log(x); break;
                        default: throw new InvalidOperationException($"Unknown function '{_name}'");
                    }
                    return new ConstantNode(val);
                }
                return new FunctionNode(_name, arg);
            }

            public override string ToDisplayString() => $"{_name}({_argument.ToDisplayString()})";
            public override void CollectVariableIndices(HashSet<int> found) => _argument.CollectVariableIndices(found);
        }

        private class BinaryNode : Node
        {
            private readonly string _op;
            private readonly Node _left, _right;
            public BinaryNode(string op, Node left, Node right) { _op = op; _left = left; _right = right; }

            public override double Evaluate(double[] x)
            {
                double l = _left.Evaluate(x);
                double r = _right.Evaluate(x);
                switch (_op)
                {
                    case "+": return l + r;
                    case "-": return l - r;
                    case "*": return l * r;
                    case "/": return l / r;
                    case "^": return Math.Pow(l, r);
                    default: throw new InvalidOperationException($"Unknown operator '{_op}'");
                }
            }

            public override Node Differentiate(int wrtIndex)
            {
                switch (_op)
                {
                    case "+":
                        return new BinaryNode("+", _left.Differentiate(wrtIndex), _right.Differentiate(wrtIndex));
                    case "-":
                        return new BinaryNode("-", _left.Differentiate(wrtIndex), _right.Differentiate(wrtIndex));
                    case "*":
                        // Product rule
                        return new BinaryNode("+",
                            new BinaryNode("*", _left.Differentiate(wrtIndex), _right),
                            new BinaryNode("*", _left, _right.Differentiate(wrtIndex)));
                    case "/":
                        // Quotient rule
                        return new BinaryNode("/",
                            new BinaryNode("-",
                                new BinaryNode("*", _left.Differentiate(wrtIndex), _right),
                                new BinaryNode("*", _left, _right.Differentiate(wrtIndex))),
                            new BinaryNode("^", _right, new ConstantNode(2)));
                    case "^":
                        return DifferentiatePower(wrtIndex);
                    default:
                        throw new InvalidOperationException($"Cannot differentiate unknown operator '{_op}'");
                }
            }

            private Node DifferentiatePower(int wrtIndex)
            {
                bool exponentIsConstant = _right is ConstantNode;
                bool baseIsConstant = _left is ConstantNode;

                if (exponentIsConstant)
                {
                    double n = ((ConstantNode)_right).Evaluate(null);
                    Node nMinus1 = new BinaryNode("^", _left, new ConstantNode(n - 1));
                    return new BinaryNode("*",
                        new BinaryNode("*", new ConstantNode(n), nMinus1),
                        _left.Differentiate(wrtIndex));
                }

                if (baseIsConstant)
                {
                    Node lnBase = new FunctionNode("ln", _left);
                    return new BinaryNode("*", new BinaryNode("*", this, lnBase), _right.Differentiate(wrtIndex));
                }

                Node term1 = new BinaryNode("*", _right.Differentiate(wrtIndex), new FunctionNode("ln", _left));
                Node term2 = new BinaryNode("*", _right, new BinaryNode("/", _left.Differentiate(wrtIndex), _left));
                return new BinaryNode("*", this, new BinaryNode("+", term1, term2));
            }

            public override MultivariatePolynomial TryExpandPolynomial(int nVars)
            {
                var l = _left.TryExpandPolynomial(nVars);
                var r = _right.TryExpandPolynomial(nVars);
                if (l == null || r == null) return null;

                switch (_op)
                {
                    case "+": return l.Add(r);
                    case "-": return l.Subtract(r);
                    case "*": return l.Multiply(r);
                    case "/":
                        if (!r.IsConstant || Math.Abs(r.ConstantValue) < 1e-12) return null;
                        return l.Scale(1.0 / r.ConstantValue);
                    case "^":
                        if (!r.IsConstant) return null;
                        double expVal = r.ConstantValue;
                        int expInt = (int)Math.Round(expVal);
                        if (Math.Abs(expVal - expInt) > 1e-9 || expInt < 0 || expInt > 20) return null;
                        return l.Pow(expInt);
                    default:
                        return null;
                }
            }

            public override Node Simplify()
            {
                Node l = _left.Simplify();
                Node r = _right.Simplify();

                switch (_op)
                {
                    case "+":
                        if (IsConstantValue(l, 0)) return r;
                        if (IsConstantValue(r, 0)) return l;
                        break;
                    case "-":
                        if (IsConstantValue(r, 0)) return l;
                        if (IsConstantValue(l, 0)) return new NegateNode(r).Simplify();
                        break;
                    case "*":
                        if (IsConstantValue(l, 0) || IsConstantValue(r, 0)) return new ConstantNode(0);
                        if (IsConstantValue(l, 1)) return r;
                        if (IsConstantValue(r, 1)) return l;
                        break;
                    case "/":
                        if (IsConstantValue(r, 1)) return l;
                        if (IsConstantValue(l, 0)) return new ConstantNode(0);
                        break;
                    case "^":
                        if (IsConstantValue(r, 1)) return l;
                        if (IsConstantValue(r, 0)) return new ConstantNode(1);
                        break;
                }

                if (l is ConstantNode lc && r is ConstantNode rc)
                {
                    double lv = lc.Evaluate(null), rv = rc.Evaluate(null);
                    double result;
                    switch (_op)
                    {
                        case "+": result = lv + rv; break;
                        case "-": result = lv - rv; break;
                        case "*": result = lv * rv; break;
                        case "/": result = lv / rv; break;
                        case "^": result = Math.Pow(lv, rv); break;
                        default: throw new InvalidOperationException($"Unknown operator '{_op}'");
                    }
                    return new ConstantNode(result);
                }

                return new BinaryNode(_op, l, r);
            }

            public override string ToDisplayString() => $"({_left.ToDisplayString()} {_op} {_right.ToDisplayString()})";
            public override void CollectVariableIndices(HashSet<int> found)
            {
                _left.CollectVariableIndices(found);
                _right.CollectVariableIndices(found);
            }
        }
    }
}