using System.Collections.Generic;
using CodeRunner.Bridge;

namespace CodeRunner.Parsing
{
    // Turns an expression back into text, for the run log
    public static class AstPrinter
    {
        public static string Print(Expr expression)
        {
            switch (expression)
            {
                case LiteralExpr e:
                    return ValueFormatter.Format(e.Value);

                case NameExpr e:
                    return e.Name;

                case MemberExpr e:
                    return Print(e.Target) + "." + e.Name;

                case CallExpr e:
                    List<string> arguments = new List<string>();
                    foreach (Expr argument in e.Arguments)
                        arguments.Add(Print(argument));
                    return Print(e.Callee) + "(" + string.Join(", ", arguments) + ")";

                case UnaryExpr e:
                    return e.Operator + Wrap(e.Operand);

                case BinaryExpr e:
                    return Wrap(e.Left) + " " + e.Operator + " " + Wrap(e.Right);

                case IsExpr e:
                    string text = Wrap(e.Operand) + " is " + (e.Negated ? "not " : "") + e.TypeName;
                    if (e.VariableName != null)
                        text += " " + e.VariableName;
                    return text;
            }
            return "?";
        }

        private static string Wrap(Expr expression)
        {
            string text = Print(expression);
            if (expression is BinaryExpr || expression is IsExpr)
                return "(" + text + ")";
            return text;
        }
    }
}
