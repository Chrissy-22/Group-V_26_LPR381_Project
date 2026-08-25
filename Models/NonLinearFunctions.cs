using System;
using System.Collections.Generic;
using System.Globalization;

namespace Group_V_26_LPR381_Project.Models
{
    public class NonLinearFunction
    {
        private readonly string _expression;
        private readonly Node _root;

        public NonLinearFunction(string expression)
        {
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            var tokens = Tokenize(_expression);
            int pos = 0;
            _root = ParseExpression(tokens, ref pos);
            if (pos != tokens.Count)
                throw new ArgumentException($"Unexpected token '{tokens[pos]}' in expression '{_expression}'");
        }

        private NonLinearFunction(Node root)
        {
            _root = root;
            _expression = null;
        }

        public double Evaluate(double x) => _root.Evaluate(x);

        /// <summary>
        /// Returns a new NonLinearFunction representing the exact symbolic derivative
        /// of this function (not a numerical approximation), automatically simplified
        /// to fold away dead 0/1 terms introduced by mechanical differentiation.
        /// </summary>
        public NonLinearFunction Derivative() => new NonLinearFunction(_root.Differentiate().Simplify());

        public override string ToString() => _expression ?? _root.ToDisplayString();

        // =============================================================
        // TOKENIZER
        // =============================================================

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

                    // Implicit multiplication: "4x" -> "4","*","x"; "4sin(x)" -> "4","*","sin",...
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
                        tokens.Add("x");
                        // Implicit multiplication: "x(" -> "x","*","("
                        if (i < expr.Length && expr[i] == '(')
                            tokens.Add("*");
                    }
                    else if (word == "sin" || word == "cos" || word == "tan" ||
                             word == "sqrt" || word == "exp" || word == "ln")
                    {
                        tokens.Add(word);
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown identifier '{word}' in expression '{expr}'");
                    }

                    continue;
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

        // =============================================================
        // RECURSIVE-DESCENT PARSER
        //
        // Grammar (lowest to highest precedence):
        //   expression := term (('+'|'-') term)*
        //   term       := power (('*'|'/') power)*
        //   power      := unary ('^' power)?        (right-associative)
        //   unary      := '-' unary | primary
        //   primary    := number | 'x' | '(' expression ')'
        // =============================================================

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

            if (token == "x")
            {
                pos++;
                return new VariableNode();
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

        // =============================================================
        // EXPRESSION TREE
        // =============================================================

        private static bool IsConstantValue(Node n, double value) =>
            n is ConstantNode c && Math.Abs(c.Evaluate(0) - value) < 1e-12;

        private abstract class Node
        {
            public abstract double Evaluate(double x);
            public abstract Node Differentiate();
            public abstract Node Simplify();
            public abstract string ToDisplayString();
        }

        private class ConstantNode : Node
        {
            private readonly double _value;
            public ConstantNode(double value) { _value = value; }
            public override double Evaluate(double x) => _value;
            public override Node Differentiate() => new ConstantNode(0);
            public override Node Simplify() => this;
            public override string ToDisplayString() => _value.ToString(CultureInfo.InvariantCulture);
        }

        private class VariableNode : Node
        {
            public override double Evaluate(double x) => x;
            public override Node Differentiate() => new ConstantNode(1);
            public override Node Simplify() => this;
            public override string ToDisplayString() => "x";
        }

        private class NegateNode : Node
        {
            private readonly Node _inner;
            public NegateNode(Node inner) { _inner = inner; }
            public Node Inner => _inner;
            public override double Evaluate(double x) => -_inner.Evaluate(x);
            public override Node Differentiate() => new NegateNode(_inner.Differentiate());
            public override string ToDisplayString() => $"-({_inner.ToDisplayString()})";

            public override Node Simplify()
            {
                var inner = _inner.Simplify();
                if (inner is ConstantNode c) return new ConstantNode(-c.Evaluate(0));
                if (inner is NegateNode nn) return nn.Inner; // double negation cancels
                return new NegateNode(inner);
            }
        }

        private class FunctionNode : Node
        {
            private readonly string _name;
            private readonly Node _argument;

            public FunctionNode(string name, Node argument)
            {
                _name = name;
                _argument = argument;
            }

            public override double Evaluate(double x)
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

            public override Node Differentiate()
            {
                Node uPrime = _argument.Differentiate();
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
                        // 1 / cos(u)^2
                        outerDerivative = new BinaryNode("/", new ConstantNode(1),
                            new BinaryNode("^", new FunctionNode("cos", _argument), new ConstantNode(2)));
                        break;
                    case "sqrt":
                        // 1 / (2*sqrt(u))
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

            public override Node Simplify()
            {
                var arg = _argument.Simplify();
                if (arg is ConstantNode c)
                {
                    double x = c.Evaluate(0);
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
        }

        private class BinaryNode : Node
        {
            private readonly string _op;
            private readonly Node _left, _right;

            public BinaryNode(string op, Node left, Node right)
            {
                _op = op;
                _left = left;
                _right = right;
            }

            public override double Evaluate(double x)
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

            public override Node Differentiate()
            {
                switch (_op)
                {
                    case "+":
                        return new BinaryNode("+", _left.Differentiate(), _right.Differentiate());
                    case "-":
                        return new BinaryNode("-", _left.Differentiate(), _right.Differentiate());
                    case "*":
                        // Product rule: (l*r)' = l'*r + l*r'
                        return new BinaryNode("+",
                            new BinaryNode("*", _left.Differentiate(), _right),
                            new BinaryNode("*", _left, _right.Differentiate()));
                    case "/":
                        // Quotient rule: (l/r)' = (l'*r - l*r') / r^2
                        return new BinaryNode("/",
                            new BinaryNode("-",
                                new BinaryNode("*", _left.Differentiate(), _right),
                                new BinaryNode("*", _left, _right.Differentiate())),
                            new BinaryNode("^", _right, new ConstantNode(2)));
                    case "^":
                        return DifferentiatePower();
                    default:
                        throw new InvalidOperationException($"Cannot differentiate unknown operator '{_op}'");
                }
            }

            private Node DifferentiatePower()
            {
                bool exponentIsConstant = _right is ConstantNode;
                bool baseIsConstant = _left is ConstantNode;

                if (exponentIsConstant)
                {
                    // Power rule: (u^n)' = n * u^(n-1) * u'
                    double n = ((ConstantNode)_right).Evaluate(0);
                    Node nMinus1 = new BinaryNode("^", _left, new ConstantNode(n - 1));
                    return new BinaryNode("*",
                        new BinaryNode("*", new ConstantNode(n), nMinus1),
                        _left.Differentiate());
                }

                if (baseIsConstant)
                {
                    // (a^v)' = a^v * ln(a) * v'
                    Node lnBase = new FunctionNode("ln", _left);
                    return new BinaryNode("*", new BinaryNode("*", this, lnBase), _right.Differentiate());
                }

                // General case: (u^v)' = u^v * (v'*ln(u) + v*u'/u)
                Node term1 = new BinaryNode("*", _right.Differentiate(), new FunctionNode("ln", _left));
                Node term2 = new BinaryNode("*", _right, new BinaryNode("/", _left.Differentiate(), _left));
                return new BinaryNode("*", this, new BinaryNode("+", term1, term2));
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
                    double lv = lc.Evaluate(0), rv = rc.Evaluate(0);
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
        }
    }
}