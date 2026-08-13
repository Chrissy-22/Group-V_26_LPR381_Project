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

        public double Evaluate(double x) => _root.Evaluate(x);

        public override string ToString() => _expression;

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

        private abstract class Node
        {
            public abstract double Evaluate(double x);
        }

        private class ConstantNode : Node
        {
            private readonly double _value;
            public ConstantNode(double value) { _value = value; }
            public override double Evaluate(double x) => _value;
        }

        private class VariableNode : Node
        {
            public override double Evaluate(double x) => x;
        }

        private class NegateNode : Node
        {
            private readonly Node _inner;
            public NegateNode(Node inner) { _inner = inner; }
            public override double Evaluate(double x) => -_inner.Evaluate(x);
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
        }
    }
}