using System;
using System.Collections.Generic;
using CodeRunner.Bridge;
using CodeRunner.Parsing;

namespace CodeRunner.Checking
{
    // Walks the AST before it runs and reports compile-style errors.
    // It also fills in the types and members on the AST so the interpreter does not have to look them up.
    public class Checker
    {
        private readonly TypeRegistry types;
        private readonly ScopeRegistry handles;
        private readonly Dictionary<string, Type> sessionVariables;
        private readonly RunMode mode;
        private readonly List<CodeError> errors = new List<CodeError>();

        // One dictionary per block we are inside of: variable name -> type
        private readonly List<Dictionary<string, Type>> scopes = new List<Dictionary<string, Type>>();
        private int loopDepth;

        public Checker(TypeRegistry types, ScopeRegistry handles, Dictionary<string, Type> sessionVariables, RunMode mode)
        {
            this.types = types;
            this.handles = handles;
            this.sessionVariables = sessionVariables;
            this.mode = mode;
        }

        public List<CodeError> Check(ProgramNode program)
        {
            PushScope();
            foreach (Stmt statement in program.Statements)
                CheckStatement(statement);
            PopScope();
            return errors;
        }

        // ---------- Helpers ----------

        private void PushScope()
        {
            scopes.Add(new Dictionary<string, Type>());
        }

        private void PopScope()
        {
            scopes.RemoveAt(scopes.Count - 1);
        }

        private void Error(Node node, string code, string message, string hint = null)
        {
            errors.Add(new CodeError(ErrorPhase.Check, code, message, node.Line, node.Column, hint));
        }

        private void Declare(string name, Type type, Node node)
        {
            Dictionary<string, Type> current = scopes[scopes.Count - 1];
            if (current.ContainsKey(name))
            {
                Error(node, "CS0128", "A local variable named '" + name + "' is already defined in this scope");
                return;
            }

            for (int i = scopes.Count - 2; i >= 0; i--)
            {
                if (scopes[i].ContainsKey(name))
                {
                    Error(node, "CS0136", "A local variable named '" + name + "' cannot be declared here because the name is already used in an enclosing scope");
                    return;
                }
            }

            current[name] = type ?? typeof(UnknownType);
        }

        // Looks for a variable, first in the blocks we are in, then in earlier runs, then in the scanned handles
        private Type LookupName(string name, out bool isHandle)
        {
            isHandle = false;

            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                Type type;
                if (scopes[i].TryGetValue(name, out type))
                    return type;
            }

            Type sessionType;
            if (sessionVariables.TryGetValue(name, out sessionType))
                return sessionType;

            object handle;
            if (handles.TryGet(name, out handle))
            {
                isHandle = true;
                return handle.GetType();
            }

            return null;
        }

        private List<string> VisibleNames()
        {
            List<string> names = new List<string>();
            foreach (Dictionary<string, Type> scope in scopes)
                names.AddRange(scope.Keys);
            names.AddRange(sessionVariables.Keys);
            names.AddRange(handles.Names);
            return names;
        }

        private Type ResolveType(string name, Node node)
        {
            Type type;
            if (types.TryGetType(name, out type))
                return type;

            Error(node, "CS0246", "The type or namespace name '" + name + "' could not be found",
                EditDistance.Suggest(name, types.TypeNameList));
            return null;
        }

        // Can a value of type 'from' be stored in something of type 'to'?
        // A null on either side means an error was already reported, so we stay quiet.
        private static bool IsAssignable(Type to, Type from)
        {
            if (to == null || from == null)
                return true;
            if (from == typeof(NullType))
                return !to.IsValueType;
            return to.IsAssignableFrom(from);
        }

        private static string Name(Type type)
        {
            return TypeNames.Of(type);
        }

        // ---------- Statements ----------

        private void CheckStatement(Stmt statement)
        {
            switch (statement)
            {
                case VarDeclStmt s:
                    CheckVarDecl(s);
                    break;

                case AssignStmt s:
                    CheckAssign(s);
                    break;

                case IncDecStmt s:
                    CheckIncDec(s);
                    break;

                case ExprStmt s:
                    CheckExpr(s.Expression);
                    break;

                case BlockStmt s:
                    PushScope();
                    foreach (Stmt inner in s.Statements)
                        CheckStatement(inner);
                    PopScope();
                    break;

                case IfStmt s:
                    RequireBool(s.Condition, CheckValueExpr(s.Condition));
                    CheckStatement(s.Then);
                    if (s.Else != null)
                        CheckStatement(s.Else);
                    break;

                case WhileStmt s:
                    RequireBool(s.Condition, CheckValueExpr(s.Condition));
                    CheckLoopBody(s.Body);
                    break;

                case ForStmt s:
                    CheckFor(s);
                    break;

                case ForeachStmt s:
                    CheckForeach(s);
                    break;

                case BreakStmt s:
                    if (loopDepth == 0)
                        Error(s, "CS0139", "No enclosing loop out of which to break");
                    break;

                case ContinueStmt s:
                    if (loopDepth == 0)
                        Error(s, "CS0139", "No enclosing loop to continue");
                    break;
            }
        }

        private void CheckLoopBody(Stmt body)
        {
            loopDepth++;
            CheckStatement(body);
            loopDepth--;
        }

        private void RequireBool(Expr expression, Type type)
        {
            if (type != null && type != typeof(bool))
                Error(expression, "CS0029", "Cannot implicitly convert type '" + Name(type) + "' to 'bool'");
        }

        private void CheckVarDecl(VarDeclStmt s)
        {
            Type initializerType = null;
            if (s.Initializer != null)
                initializerType = CheckValueExpr(s.Initializer);

            Type declared;
            if (s.TypeName == null)
            {
                if (initializerType == typeof(NullType))
                {
                    Error(s, "CS0815", "Cannot assign <null> to an implicitly-typed variable");
                    initializerType = null;
                }
                declared = initializerType;
            }
            else
            {
                declared = ResolveType(s.TypeName, s);
                if (s.Initializer != null && !IsAssignable(declared, initializerType))
                {
                    Error(s.Initializer, "CS0029", "Cannot implicitly convert type '" + Name(initializerType) + "' to '" + Name(declared) + "'");
                }
            }

            s.ResolvedType = declared;
            Declare(s.Name, declared, s);
        }

        private void CheckAssign(AssignStmt s)
        {
            Type variableType = CheckAssignTarget(s.Target);
            Type valueType = CheckValueExpr(s.Value);
            if (variableType == null || valueType == null)
                return;

            switch (s.Operator)
            {
                case "=":
                    if (!IsAssignable(variableType, valueType))
                        Error(s.Value, "CS0029", "Cannot implicitly convert type '" + Name(valueType) + "' to '" + Name(variableType) + "'");
                    break;

                case "+=":
                    if (variableType == typeof(int) && valueType == typeof(int))
                        break;
                    if (variableType == typeof(string))
                        break;
                    Error(s, "CS0019", "Operator '+=' cannot be applied to operands of type '" + Name(variableType) + "' and '" + Name(valueType) + "'");
                    break;

                case "-=":
                    if (variableType != typeof(int) || valueType != typeof(int))
                        Error(s, "CS0019", "Operator '-=' cannot be applied to operands of type '" + Name(variableType) + "' and '" + Name(valueType) + "'");
                    break;
            }
        }

        // The left side of an assignment has to be a variable
        private Type CheckAssignTarget(Expr target)
        {
            NameExpr name = target as NameExpr;
            if (name == null)
            {
                CheckExpr(target);
                Error(target, "CS0131", "The left-hand side of an assignment must be a variable");
                return null;
            }

            bool isHandle;
            Type type = LookupName(name.Name, out isHandle);
            if (type == null)
            {
                ReportUnknownName(name);
                return null;
            }
            if (isHandle)
            {
                Error(name, "CS0131", "'" + name.Name + "' is a scanned object, you can't assign a new value to it");
                return null;
            }

            name.StaticType = type;
            return type == typeof(UnknownType) ? null : type;
        }

        private void CheckIncDec(IncDecStmt s)
        {
            Type type = CheckAssignTarget(s.Target);
            if (type != null && type != typeof(int))
            {
                string op = s.IsIncrement ? "++" : "--";
                Error(s, "CS0023", "Operator '" + op + "' cannot be applied to operand of type '" + Name(type) + "'");
            }
        }

        private void CheckFor(ForStmt s)
        {
            PushScope();

            if (s.Initializer != null)
                CheckStatement(s.Initializer);
            if (s.Condition != null)
                RequireBool(s.Condition, CheckValueExpr(s.Condition));
            foreach (Stmt update in s.Updates)
                CheckStatement(update);

            CheckLoopBody(s.Body);
            PopScope();
        }

        private void CheckForeach(ForeachStmt s)
        {
            Type collectionType = CheckValueExpr(s.Collection);
            Type elementType = null;

            if (collectionType != null)
            {
                elementType = CollectionTypes.GetElementType(collectionType);
                if (elementType == null)
                {
                    Error(s.Collection, "CS1579", "foreach cannot loop over a '" + Name(collectionType) + "'",
                        "You can loop over IEnumerable<T>, IReadOnlyList<T> and IReadOnlyDictionary<K,V>.");
                }
            }

            PushScope();

            Type variableType = elementType;
            if (s.TypeName != null)
            {
                variableType = ResolveType(s.TypeName, s);
                if (!IsAssignable(variableType, elementType))
                    Error(s, "CS0266", "Cannot implicitly convert type '" + Name(elementType) + "' to '" + Name(variableType) + "'");
            }

            s.ElementType = variableType;
            Declare(s.VariableName, variableType, s);

            CheckLoopBody(s.Body);
            PopScope();
        }

        // ---------- Expressions ----------

        // Checks an expression and returns its type. Returns null if there was an error.
        private Type CheckExpr(Expr expression)
        {
            Type result = null;

            switch (expression)
            {
                case LiteralExpr e:
                    result = CheckLiteral(e);
                    break;
                case NameExpr e:
                    result = CheckName(e);
                    break;
                case MemberExpr e:
                    result = CheckMember(e);
                    break;
                case CallExpr e:
                    result = CheckCall(e);
                    break;
                case UnaryExpr e:
                    result = CheckUnary(e);
                    break;
                case BinaryExpr e:
                    result = CheckBinary(e);
                    break;
                case IsExpr e:
                    result = CheckIs(e);
                    break;
            }

            expression.StaticType = result;
            return result;
        }

        // Same as CheckExpr, but the expression has to produce a value (not void)
        private Type CheckValueExpr(Expr expression)
        {
            Type type = CheckExpr(expression);
            if (type == typeof(void))
            {
                Error(expression, "CS1547", "Keyword 'void' cannot be used in this context, this method does not return a value");
                return null;
            }
            return type;
        }

        private Type CheckLiteral(LiteralExpr e)
        {
            if (e.Value == null) return typeof(NullType);
            if (e.Value is int) return typeof(int);
            if (e.Value is bool) return typeof(bool);
            return typeof(string);
        }

        private Type CheckName(NameExpr e)
        {
            bool isHandle;
            Type type = LookupName(e.Name, out isHandle);
            if (type == null)
            {
                ReportUnknownName(e);
                return null;
            }
            return type == typeof(UnknownType) ? null : type;
        }

        private void ReportUnknownName(NameExpr e)
        {
            string hint = EditDistance.Suggest(e.Name, VisibleNames());
            if (hint == null)
                hint = "Objects have to be scanned before your code can use them.";
            Error(e, "CS0103", "The name '" + e.Name + "' does not exist in the current context", hint);
        }

        private Type CheckMember(MemberExpr e)
        {
            Type targetType = CheckValueExpr(e.Target);
            if (targetType == null)
                return null;

            MemberDescriptor member = FindMemberOrReport(e, targetType);
            if (member == null)
                return null;

            if (member.IsMethod)
            {
                Error(e, "CS0428", "Cannot use the method '" + e.Name + "' as a value",
                    "Did you forget the parentheses ( )?");
                return null;
            }

            e.Member = member;
            return member.ValueType;
        }

        private MemberDescriptor FindMemberOrReport(MemberExpr e, Type targetType)
        {
            if (targetType == typeof(NullType))
            {
                Error(e, "CS0023", "Cannot use '.' on null");
                return null;
            }

            MemberDescriptor member = types.FindMember(targetType, e.Name);
            if (member != null)
                return member;

            List<string> names = new List<string>();
            foreach (MemberDescriptor candidate in types.GetMembers(targetType))
                names.Add(candidate.Name);

            Error(e, "CS1061", "'" + Name(targetType) + "' does not contain a definition for '" + e.Name + "'",
                EditDistance.Suggest(e.Name, names));
            return null;
        }

        private Type CheckCall(CallExpr call)
        {
            MemberExpr callee = call.Callee as MemberExpr;

            if (callee == null)
            {
                foreach (Expr argument in call.Arguments)
                    CheckValueExpr(argument);

                NameExpr name = call.Callee as NameExpr;
                if (name != null)
                {
                    Error(name, "CS0103", "The name '" + name.Name + "' does not exist in the current context",
                        "You can't write your own methods yet, call a method of an object like imp.Say(...)");
                }
                else
                {
                    Error(call, "CS1955", "Only methods of objects can be called");
                }
                return null;
            }

            Type targetType = CheckValueExpr(callee.Target);
            List<Type> argumentTypes = new List<Type>();
            foreach (Expr argument in call.Arguments)
                argumentTypes.Add(CheckValueExpr(argument));

            if (targetType == null)
                return null;

            MemberDescriptor method = FindMemberOrReport(callee, targetType);
            if (method == null)
                return null;

            if (!method.IsMethod)
            {
                Error(callee, "CS1955", "Non-invocable member '" + callee.Name + "' cannot be used like a method");
                return null;
            }

            callee.Member = method;
            call.Method = method;

            if (call.Arguments.Count != method.ParameterTypes.Length)
            {
                Error(callee, "CS1501", "No overload for method '" + method.Name + "' takes " + call.Arguments.Count + " arguments",
                    "It is declared as: " + method.Signature);
            }
            else
            {
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    if (!IsAssignable(method.ParameterTypes[i], argumentTypes[i]))
                    {
                        Error(call.Arguments[i], "CS1503", "Argument " + (i + 1) + ": cannot convert from '" + Name(argumentTypes[i]) + "' to '" + Name(method.ParameterTypes[i]) + "'");
                    }
                }
            }

            if (mode == RunMode.Console && method.IsWorldAction)
            {
                Error(callee, "", "'" + method.Name + "' changes the world, so it can't be used in the console. Put it in a work order instead.");
            }

            return method.ValueType;
        }

        private Type CheckUnary(UnaryExpr e)
        {
            Type operand = CheckValueExpr(e.Operand);
            Type expected = e.Operator == "!" ? typeof(bool) : typeof(int);

            if (operand == null)
                return expected;
            if (operand != expected)
            {
                Error(e, "CS0023", "Operator '" + e.Operator + "' cannot be applied to operand of type '" + Name(operand) + "'");
                return null;
            }
            return expected;
        }

        private Type CheckBinary(BinaryExpr e)
        {
            Type left = CheckValueExpr(e.Left);
            Type right = CheckValueExpr(e.Right);

            bool isComparison = e.Operator == "==" || e.Operator == "!=" || e.Operator == "<"
                || e.Operator == ">" || e.Operator == "<=" || e.Operator == ">="
                || e.Operator == "&&" || e.Operator == "||";

            // An earlier error already happened. We still know what a comparison gives.
            if (left == null || right == null)
                return isComparison ? typeof(bool) : null;

            bool ok = false;
            Type result = null;

            switch (e.Operator)
            {
                case "+":
                    if (left == typeof(int) && right == typeof(int))
                    {
                        ok = true;
                        result = typeof(int);
                    }
                    else if (left == typeof(string) || right == typeof(string))
                    {
                        ok = true;
                        result = typeof(string);
                    }
                    break;

                case "-":
                case "*":
                case "/":
                case "%":
                    ok = left == typeof(int) && right == typeof(int);
                    result = typeof(int);
                    break;

                case "<":
                case ">":
                case "<=":
                case ">=":
                    ok = left == typeof(int) && right == typeof(int);
                    result = typeof(bool);
                    break;

                case "==":
                case "!=":
                    ok = IsAssignable(left, right) || IsAssignable(right, left);
                    result = typeof(bool);
                    break;

                case "&&":
                case "||":
                    ok = left == typeof(bool) && right == typeof(bool);
                    result = typeof(bool);
                    break;
            }

            if (!ok)
            {
                Error(e, "CS0019", "Operator '" + e.Operator + "' cannot be applied to operands of type '" + Name(left) + "' and '" + Name(right) + "'");
                return isComparison ? typeof(bool) : null;
            }
            return result;
        }

        private Type CheckIs(IsExpr e)
        {
            CheckValueExpr(e.Operand);

            Type type = ResolveType(e.TypeName, e);
            e.ResolvedType = type;

            if (e.VariableName != null)
                Declare(e.VariableName, type, e);

            return typeof(bool);
        }
    }
}
