using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using CodeRunner.Bridge;
using CodeRunner.Parsing;

namespace CodeRunner.Interpreting
{
    // Tree-walking interpreter. Statements are coroutines so the game can animate between them.
    // Expressions are evaluated at once. A world action changes the world when it is called, and
    // is queued; the animation plays before the next statement starts.
    public class Interpreter
    {
        private enum Flow
        {
            None,
            Break,
            Continue
        }

        private readonly CodeSession session;
        private readonly RunMode mode;
        private readonly RunResult result;
        private readonly List<WorldActionInfo> pendingActions = new List<WorldActionInfo>();
        private readonly Stack<int> loopLines = new Stack<int>();

        private Scope scope;
        private Flow flow = Flow.None;
        private int steps;

        public Interpreter(CodeSession session, RunMode mode, RunResult result)
        {
            this.session = session;
            this.mode = mode;
            this.result = result;
        }

        public int Steps
        {
            get { return steps; }
        }

        public IEnumerable<object> ExecuteProgram(ProgramNode program, Scope runScope)
        {
            scope = runScope;

            foreach (Stmt statement in program.Statements)
            {
                foreach (object step in Execute(statement))
                    yield return step;
            }
        }

        // ---------- Statements ----------

        private IEnumerable<object> Execute(Stmt statement)
        {
            if (!(statement is BlockStmt))
            {
                session.SetCurrentLine(statement.Line);
                CountStep(statement.Line);
            }

            switch (statement)
            {
                case BlockStmt s:
                    foreach (object step in ExecuteBlock(s))
                        yield return step;
                    break;

                case IfStmt s:
                    foreach (object step in ExecuteIf(s))
                        yield return step;
                    break;

                case WhileStmt s:
                    foreach (object step in ExecuteWhile(s))
                        yield return step;
                    break;

                case ForStmt s:
                    foreach (object step in ExecuteFor(s))
                        yield return step;
                    break;

                case ForeachStmt s:
                    foreach (object step in ExecuteForeach(s))
                        yield return step;
                    break;

                default:
                    ExecuteSimple(statement);
                    break;
            }

            // Wait for the animations of the world actions this statement started
            foreach (object step in PlayPendingActions())
                yield return step;
        }

        private IEnumerable<object> PlayPendingActions()
        {
            while (pendingActions.Count > 0)
            {
                WorldActionInfo action = pendingActions[0];
                pendingActions.RemoveAt(0);
                yield return session.ActionPlayer.Play(action);
            }
        }

        private IEnumerable<object> ExecuteBlock(BlockStmt block)
        {
            Scope outer = scope;
            scope = new Scope(outer);
            try
            {
                foreach (Stmt inner in block.Statements)
                {
                    foreach (object step in Execute(inner))
                        yield return step;

                    if (flow != Flow.None)
                        break;
                }
            }
            finally
            {
                scope = outer;
            }
        }

        private IEnumerable<object> ExecuteIf(IfStmt statement)
        {
            bool condition = (bool)Eval(statement.Condition);
            foreach (object step in PlayPendingActions())
                yield return step;

            if (condition)
            {
                foreach (object step in Execute(statement.Then))
                    yield return step;
            }
            else if (statement.Else != null)
            {
                foreach (object step in Execute(statement.Else))
                    yield return step;
            }
        }

        private IEnumerable<object> ExecuteWhile(WhileStmt statement)
        {
            loopLines.Push(statement.Line);
            try
            {
                while (true)
                {
                    CountStep(statement.Line);

                    bool condition = (bool)Eval(statement.Condition);
                    foreach (object step in PlayPendingActions())
                        yield return step;
                    if (!condition)
                        break;

                    foreach (object step in Execute(statement.Body))
                        yield return step;

                    if (LoopShouldStop())
                        break;
                }
            }
            finally
            {
                loopLines.Pop();
            }
        }

        private IEnumerable<object> ExecuteFor(ForStmt statement)
        {
            Scope outer = scope;
            scope = new Scope(outer);
            loopLines.Push(statement.Line);
            try
            {
                if (statement.Initializer != null)
                {
                    foreach (object step in Execute(statement.Initializer))
                        yield return step;
                }

                while (true)
                {
                    CountStep(statement.Line);

                    if (statement.Condition != null)
                    {
                        bool condition = (bool)Eval(statement.Condition);
                        foreach (object step in PlayPendingActions())
                            yield return step;
                        if (!condition)
                            break;
                    }

                    foreach (object step in Execute(statement.Body))
                        yield return step;

                    if (LoopShouldStop())
                        break;

                    foreach (Stmt update in statement.Updates)
                    {
                        foreach (object step in Execute(update))
                            yield return step;
                    }
                }
            }
            finally
            {
                loopLines.Pop();
                scope = outer;
            }
        }

        private IEnumerable<object> ExecuteForeach(ForeachStmt statement)
        {
            object collection = Eval(statement.Collection);
            foreach (object step in PlayPendingActions())
                yield return step;

            if (collection == null)
                throw NullReference(statement.Line);

            IEnumerator items = ((IEnumerable)collection).GetEnumerator();
            Scope outer = scope;
            loopLines.Push(statement.Line);
            try
            {
                while (MoveNext(items, statement.Line))
                {
                    CountStep(statement.Line);

                    // Every iteration gets a fresh scope with the loop variable in it
                    scope = new Scope(outer);
                    scope.Declare(statement.VariableName, statement.ElementType, items.Current);

                    foreach (object step in Execute(statement.Body))
                        yield return step;

                    if (LoopShouldStop())
                        break;
                }
            }
            finally
            {
                loopLines.Pop();
                scope = outer;
                IDisposable disposable = items as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        // Called after a loop body. Handles break and continue, and returns true if the loop must end.
        private bool LoopShouldStop()
        {
            if (flow == Flow.Break)
            {
                flow = Flow.None;
                return true;
            }
            if (flow == Flow.Continue)
                flow = Flow.None;
            return false;
        }

        private bool MoveNext(IEnumerator items, int line)
        {
            try
            {
                return items.MoveNext();
            }
            catch (Exception e)
            {
                throw FromException(e, line);
            }
        }

        // Statements that finish without waiting for anything
        private void ExecuteSimple(Stmt statement)
        {
            switch (statement)
            {
                case VarDeclStmt s:
                {
                    object value = s.Initializer != null ? Eval(s.Initializer) : DefaultValue(s.ResolvedType);
                    scope.Declare(s.Name, s.ResolvedType, value);
                    break;
                }

                case AssignStmt s:
                {
                    Variable variable = scope.Find(((NameExpr)s.Target).Name);
                    object value = Eval(s.Value);
                    if (s.Operator == "=")
                        variable.Value = value;
                    else if (s.Operator == "+=")
                        variable.Value = Add(variable.Value, value);
                    else
                        variable.Value = (int)variable.Value - (int)value;
                    break;
                }

                case IncDecStmt s:
                {
                    Variable variable = scope.Find(((NameExpr)s.Target).Name);
                    variable.Value = (int)variable.Value + (s.IsIncrement ? 1 : -1);
                    break;
                }

                case ExprStmt s:
                {
                    object value = Eval(s.Expression);
                    if (mode == RunMode.Console && s.Expression.StaticType != typeof(void))
                    {
                        result.ValueText = ValueFormatter.Format(value);
                        session.Log.Add(s.Line, AstPrinter.Print(s.Expression), result.ValueText);
                    }
                    break;
                }

                case BreakStmt s:
                    flow = Flow.Break;
                    break;

                case ContinueStmt s:
                    flow = Flow.Continue;
                    break;
            }
        }

        // ---------- Step budget ----------

        private void CountStep(int line)
        {
            steps++;
            if (steps <= session.Options.MaxSteps)
                return;

            string message = "The code used more than " + session.Options.MaxSteps + " steps and was stopped.";
            int errorLine = line;
            if (loopLines.Count > 0)
            {
                errorLine = loopLines.Peek();
                message += " Does the loop on line " + errorLine + " ever end?";
            }
            throw new ScriptRuntimeException("StepLimitExceeded", message, errorLine);
        }

        // ---------- Expressions ----------

        private object Eval(Expr expression)
        {
            switch (expression)
            {
                case LiteralExpr e:
                    return e.Value;
                case NameExpr e:
                    return EvalName(e);
                case MemberExpr e:
                    return EvalMember(e);
                case CallExpr e:
                    return EvalCall(e);
                case UnaryExpr e:
                    return EvalUnary(e);
                case BinaryExpr e:
                    return EvalBinary(e);
                case IsExpr e:
                    return EvalIs(e);
            }
            throw new ScriptRuntimeException("InvalidOperationException", "Unknown kind of expression", expression.Line);
        }

        private object EvalName(NameExpr e)
        {
            Variable variable = scope.Find(e.Name);
            if (variable != null)
                return variable.Value;

            object handle;
            if (session.Handles.TryGet(e.Name, out handle))
                return handle;

            throw new ScriptRuntimeException("InvalidOperationException", "The name '" + e.Name + "' does not exist", e.Line);
        }

        private object EvalMember(MemberExpr e)
        {
            object target = Eval(e.Target);
            if (target == null)
                throw NullReference(e.Line);

            try
            {
                return e.Member.GetValue(target);
            }
            catch (Exception ex)
            {
                throw FromException(ex, e.Line);
            }
        }

        private object EvalCall(CallExpr call)
        {
            MemberExpr callee = (MemberExpr)call.Callee;
            MemberDescriptor method = call.Method;

            object target = Eval(callee.Target);
            if (target == null)
                throw NullReference(callee.Line);

            object[] arguments = new object[call.Arguments.Count];
            for (int i = 0; i < arguments.Length; i++)
                arguments[i] = Eval(call.Arguments[i]);

            if (mode == RunMode.Console && method.IsWorldAction)
            {
                throw new ScriptRuntimeException("ConsoleModeException",
                    "'" + method.Name + "' changes the world, so it can't be used in the console", callee.Line);
            }

            object value;
            try
            {
                value = method.Call(target, arguments);
            }
            catch (Exception ex)
            {
                throw FromException(ex, callee.Line);
            }

            if (method.IsWorldAction)
            {
                WorldActionInfo action = new WorldActionInfo();
                action.Line = callee.Line;
                action.Target = target;
                action.Method = method.Name;
                action.Arguments = arguments;
                action.Text = AstPrinter.Print(call);
                pendingActions.Add(action);
                session.Log.Add(action.Line, action.Text, "done");
            }

            return value;
        }

        private object EvalUnary(UnaryExpr e)
        {
            object operand = Eval(e.Operand);
            if (e.Operator == "!")
                return !(bool)operand;
            return -(int)operand;
        }

        private object EvalBinary(BinaryExpr e)
        {
            // && and || only evaluate the right side when they have to
            if (e.Operator == "&&")
                return (bool)Eval(e.Left) && (bool)Eval(e.Right);
            if (e.Operator == "||")
                return (bool)Eval(e.Left) || (bool)Eval(e.Right);

            object left = Eval(e.Left);
            object right = Eval(e.Right);

            switch (e.Operator)
            {
                case "+": return Add(left, right);
                case "-": return (int)left - (int)right;
                case "*": return (int)left * (int)right;
                case "/":
                    if ((int)right == 0)
                        throw new ScriptRuntimeException("DivideByZeroException", "Attempted to divide by zero.", e.Line);
                    return (int)left / (int)right;
                case "%":
                    if ((int)right == 0)
                        throw new ScriptRuntimeException("DivideByZeroException", "Attempted to divide by zero.", e.Line);
                    return (int)left % (int)right;
                case "<": return (int)left < (int)right;
                case ">": return (int)left > (int)right;
                case "<=": return (int)left <= (int)right;
                case ">=": return (int)left >= (int)right;
                case "==": return ValuesEqual(left, right);
                case "!=": return !ValuesEqual(left, right);
            }
            throw new ScriptRuntimeException("InvalidOperationException", "Unknown operator " + e.Operator, e.Line);
        }

        private object EvalIs(IsExpr e)
        {
            object value = Eval(e.Operand);
            bool matches = value != null && e.ResolvedType.IsInstanceOfType(value);

            // The variable lives in the surrounding block, like in C#
            if (e.VariableName != null)
                scope.Declare(e.VariableName, e.ResolvedType, matches ? value : null);

            return e.Negated ? !matches : matches;
        }

        // ---------- Helpers ----------

        private static object Add(object left, object right)
        {
            if (left is int && right is int)
                return (int)left + (int)right;
            return ToText(left) + ToText(right);
        }

        private static string ToText(object value)
        {
            if (value == null) return "";
            if (value is int) return ((int)value).ToString(CultureInfo.InvariantCulture);
            return value.ToString();
        }

        private static bool ValuesEqual(object a, object b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a is string && b is string) return (string)a == (string)b;
            if (a.GetType().IsValueType) return a.Equals(b);
            return false;
        }

        private static object DefaultValue(Type type)
        {
            if (type == typeof(int)) return 0;
            if (type == typeof(bool)) return false;
            return null;
        }

        private static ScriptRuntimeException NullReference(int line)
        {
            return new ScriptRuntimeException("NullReferenceException", "Object reference not set to an instance of an object.", line);
        }

        // Game code threw something. Keep its name and message so the player can read them.
        private static ScriptRuntimeException FromException(Exception e, int line)
        {
            ScriptRuntimeException already = e as ScriptRuntimeException;
            if (already != null)
                return already;
            return new ScriptRuntimeException(e.GetType().Name, e.Message, line);
        }
    }
}
