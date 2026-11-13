using System;
using System.Collections.Generic;
using CodeRunner.Bridge;

namespace CodeRunner.Parsing
{
    // Every node remembers where it was written, so errors can point at the right line
    public abstract class Node
    {
        public int Line;
        public int Column;
    }

    // ---------- Expressions ----------

    public abstract class Expr : Node
    {
        public Type StaticType;     // filled in by the Checker
    }

    public class LiteralExpr : Expr
    {
        public object Value;        // an int, a string, a bool or null
    }

    public class NameExpr : Expr
    {
        public string Name;
    }

    public class MemberExpr : Expr
    {
        public Expr Target;
        public string Name;
        public MemberDescriptor Member;     // filled in by the Checker
    }

    public class CallExpr : Expr
    {
        public Expr Callee;                 // only a MemberExpr like imp.Say is supported
        public List<Expr> Arguments = new List<Expr>();
        public MemberDescriptor Method;     // filled in by the Checker
    }

    public class UnaryExpr : Expr
    {
        public string Operator;             // "!" or "-"
        public Expr Operand;
    }

    public class BinaryExpr : Expr
    {
        public string Operator;
        public Expr Left;
        public Expr Right;
    }

    // x is Type, x is Type name, x is not Type name
    public class IsExpr : Expr
    {
        public Expr Operand;
        public string TypeName;
        public string VariableName;         // null when no variable is declared
        public bool Negated;
        public Type ResolvedType;           // filled in by the Checker
    }

    // ---------- Statements ----------

    public abstract class Stmt : Node
    {
    }

    public class VarDeclStmt : Stmt
    {
        public string TypeName;             // null means "var"
        public string Name;
        public Expr Initializer;            // can be null for typed declarations
        public Type ResolvedType;           // filled in by the Checker
    }

    public class AssignStmt : Stmt
    {
        public Expr Target;
        public string Operator;             // "=", "+=" or "-="
        public Expr Value;
    }

    public class IncDecStmt : Stmt
    {
        public Expr Target;
        public bool IsIncrement;
    }

    public class ExprStmt : Stmt
    {
        public Expr Expression;
    }

    public class BlockStmt : Stmt
    {
        public List<Stmt> Statements = new List<Stmt>();
    }

    public class IfStmt : Stmt
    {
        public Expr Condition;
        public Stmt Then;
        public Stmt Else;                   // can be null
    }

    public class WhileStmt : Stmt
    {
        public Expr Condition;
        public Stmt Body;
    }

    public class ForStmt : Stmt
    {
        public Stmt Initializer;            // can be null
        public Expr Condition;              // can be null
        public List<Stmt> Updates = new List<Stmt>();
        public Stmt Body;
    }

    public class ForeachStmt : Stmt
    {
        public string TypeName;             // null means "var"
        public string VariableName;
        public Expr Collection;
        public Stmt Body;
        public Type ElementType;            // type of the loop variable, filled in by the Checker
    }

    public class BreakStmt : Stmt
    {
    }

    public class ContinueStmt : Stmt
    {
    }

    public class ProgramNode : Node
    {
        public List<Stmt> Statements = new List<Stmt>();
    }
}
