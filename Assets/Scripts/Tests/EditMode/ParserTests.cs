using CodeRunner.Lexing;
using CodeRunner.Parsing;
using NUnit.Framework;

namespace CodeRunner.Tests
{
    public class ParserTests
    {
        private static ProgramNode Parse(string source, bool bare = false)
        {
            return new Parser(new Lexer(source).Tokenize(), bare).ParseProgram();
        }

        private static CodeError ParseError(string source, bool bare = false)
        {
            CodeErrorException e = Assert.Throws<CodeErrorException>(() => Parse(source, bare));
            return e.Error;
        }

        [Test]
        public void Declarations_AreParsed()
        {
            ProgramNode program = Parse("var a = 1; int b = 2; string c = \"x\"; bool d; Wagon w = null;");

            Assert.AreEqual(5, program.Statements.Count);
            VarDeclStmt first = (VarDeclStmt)program.Statements[0];
            Assert.IsNull(first.TypeName);
            Assert.AreEqual("a", first.Name);
            Assert.AreEqual("int", ((VarDeclStmt)program.Statements[1]).TypeName);
            Assert.IsNull(((VarDeclStmt)program.Statements[3]).Initializer);
            Assert.AreEqual("Wagon", ((VarDeclStmt)program.Statements[4]).TypeName);
        }

        [Test]
        public void ControlFlow_IsParsed()
        {
            ProgramNode program = Parse(
                "if (a) { b = 1; } else { b = 2; }\n" +
                "while (a < 3) a++;\n" +
                "for (int i = 0; i < 3; i++) { continue; }\n" +
                "foreach (var x in things) { break; }");

            Assert.IsInstanceOf<IfStmt>(program.Statements[0]);
            Assert.IsNotNull(((IfStmt)program.Statements[0]).Else);
            Assert.IsInstanceOf<WhileStmt>(program.Statements[1]);
            Assert.IsInstanceOf<ForStmt>(program.Statements[2]);
            Assert.IsInstanceOf<ForeachStmt>(program.Statements[3]);
            Assert.AreEqual(4, program.Statements[3].Line);
        }

        [Test]
        public void Multiplication_BindsTighterThanAddition()
        {
            ProgramNode program = Parse("1 + 2 * 3", true);

            ExprStmt statement = (ExprStmt)program.Statements[0];
            BinaryExpr plus = (BinaryExpr)statement.Expression;
            Assert.AreEqual("+", plus.Operator);
            Assert.IsInstanceOf<LiteralExpr>(plus.Left);
            Assert.AreEqual("*", ((BinaryExpr)plus.Right).Operator);
        }

        [Test]
        public void IsPatterns_AreParsed()
        {
            ProgramNode program = Parse("if (thing is not ICargo cargo) continue; var t = x is IPerishable;");

            IfStmt ifStatement = (IfStmt)program.Statements[0];
            IsExpr negated = (IsExpr)ifStatement.Condition;
            Assert.IsTrue(negated.Negated);
            Assert.AreEqual("ICargo", negated.TypeName);
            Assert.AreEqual("cargo", negated.VariableName);

            IsExpr plain = (IsExpr)((VarDeclStmt)program.Statements[1]).Initializer;
            Assert.IsFalse(plain.Negated);
            Assert.IsNull(plain.VariableName);
        }

        [Test]
        public void MemberAccessAndCalls_AreChained()
        {
            ProgramNode program = Parse("imp.Load(cargo, depot.Wagons);");

            CallExpr call = (CallExpr)((ExprStmt)program.Statements[0]).Expression;
            Assert.AreEqual(2, call.Arguments.Count);
            Assert.AreEqual("Load", ((MemberExpr)call.Callee).Name);
            Assert.IsInstanceOf<MemberExpr>(call.Arguments[1]);
        }

        [Test]
        public void MissingSemicolon_IsReportedOnTheLineBefore()
        {
            CodeError error = ParseError("int x = 5\nint y = 6;");

            Assert.AreEqual(1, error.Line);
            Assert.AreEqual(10, error.Column);
            StringAssert.Contains("; expected", error.Message);
        }

        [Test]
        public void MissingClosingBrace_IsAnError()
        {
            CodeError error = ParseError("while (true)\n{\n    x++;\n");

            StringAssert.Contains("} expected", error.Message);
            Assert.AreEqual(3, error.Line);
        }

        [Test]
        public void MissingClosingParenthesis_IsAnError()
        {
            CodeError error = ParseError("int a = 1;\nif (a == 1 {\n}");

            Assert.AreEqual(2, error.Line);
            StringAssert.Contains(") expected", error.Message);
        }

        [Test]
        public void BadExpression_ReportsItsLine()
        {
            CodeError error = ParseError("int a = 1;\nint b = 2;\nint c = a + ;");

            Assert.AreEqual(3, error.Line);
            StringAssert.Contains("Invalid expression term", error.Message);
        }

        [Test]
        public void VarWithoutInitializer_IsAnError()
        {
            CodeError error = ParseError("var x;");

            StringAssert.Contains("must be initialized", error.Message);
        }

        [Test]
        public void ExpressionAsStatement_IsAnErrorInWorkOrders_ButFineInConsole()
        {
            CodeError error = ParseError("5 + 3;");
            StringAssert.Contains("Only assignment", error.Message);

            Assert.DoesNotThrow(() => Parse("5 + 3", true));
        }

        [Test]
        public void UnsupportedSyntax_SaysNotSupportedYet()
        {
            string[] samples =
            {
                "return 5;",
                "class Foo { }",
                "var f = x => x;",
                "var o = new Foo();",
                "int[] numbers;",
                "x *= 2;",
                "var y = a ? b : c;",
                "var z = list[0];",
                "switch (x) { }"
            };

            foreach (string sample in samples)
            {
                CodeError error = ParseError(sample);
                StringAssert.Contains("not supported yet", error.Message, sample);
            }
        }

        [Test]
        public void IncrementInsideExpression_SaysNotSupportedYet()
        {
            CodeError error = ParseError("int a = 1; int b = a++;");

            StringAssert.Contains("not supported yet", error.Message);
        }
    }
}
