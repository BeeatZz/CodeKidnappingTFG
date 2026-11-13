using System.Collections.Generic;
using CodeRunner.Lexing;
using NUnit.Framework;

namespace CodeRunner.Tests
{
    public class LexerTests
    {
        private static List<Token> Lex(string source)
        {
            return new Lexer(source).Tokenize();
        }

        private static CodeError LexError(string source)
        {
            CodeErrorException e = Assert.Throws<CodeErrorException>(() => Lex(source));
            return e.Error;
        }

        [Test]
        public void SimpleDeclaration_GivesTokensWithPositions()
        {
            List<Token> tokens = Lex("int x = 5;");

            Assert.AreEqual(6, tokens.Count);
            Assert.AreEqual(TokenType.Keyword, tokens[0].Type);
            Assert.AreEqual("int", tokens[0].Text);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual(5, tokens[1].Column);
            Assert.IsTrue(tokens[2].IsSymbol("="));
            Assert.AreEqual(TokenType.IntLiteral, tokens[3].Type);
            Assert.AreEqual(5, tokens[3].Value);
            Assert.IsTrue(tokens[4].IsSymbol(";"));
            Assert.AreEqual(TokenType.EndOfFile, tokens[5].Type);
        }

        [Test]
        public void NewLines_UpdateLineAndColumn()
        {
            List<Token> tokens = Lex("a\n  b");

            Assert.AreEqual(1, tokens[0].Line);
            Assert.AreEqual(2, tokens[1].Line);
            Assert.AreEqual(3, tokens[1].Column);
        }

        [Test]
        public void Comments_AreSkipped_AndLinesStillCount()
        {
            List<Token> tokens = Lex("// first line\nx /* spans\n two lines */ y");

            Assert.AreEqual("x", tokens[0].Text);
            Assert.AreEqual(2, tokens[0].Line);
            Assert.AreEqual("y", tokens[1].Text);
            Assert.AreEqual(3, tokens[1].Line);
        }

        [Test]
        public void StringLiteral_DecodesEscapes()
        {
            List<Token> tokens = Lex("\"a\\nb \\\"q\\\"\"");

            Assert.AreEqual(TokenType.StringLiteral, tokens[0].Type);
            Assert.AreEqual("a\nb \"q\"", tokens[0].Value);
        }

        [Test]
        public void TwoCharacterOperators_AreSingleTokens()
        {
            List<Token> tokens = Lex("a<=b && c!=d; i++; n += 1; x *= 2;");

            Assert.IsTrue(tokens[1].IsSymbol("<="));
            Assert.IsTrue(tokens[3].IsSymbol("&&"));
            Assert.IsTrue(tokens[5].IsSymbol("!="));
            Assert.IsTrue(tokens[9].IsSymbol("++"));
            Assert.IsTrue(tokens[12].IsSymbol("+="));
            Assert.IsTrue(tokens[16].IsSymbol("*="));
        }

        [Test]
        public void Keywords_AreNotConfusedWithLongerNames()
        {
            List<Token> tokens = Lex("foreach foreachCount var variable is isolated");

            Assert.AreEqual(TokenType.Keyword, tokens[0].Type);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual(TokenType.Keyword, tokens[2].Type);
            Assert.AreEqual(TokenType.Identifier, tokens[3].Type);
            Assert.AreEqual(TokenType.Keyword, tokens[4].Type);
            Assert.AreEqual(TokenType.Identifier, tokens[5].Type);
        }

        [Test]
        public void UnterminatedString_ReportsTheLineItStartedOn()
        {
            CodeError error = LexError("x = 1;\ny = \"abc");

            Assert.AreEqual(2, error.Line);
            Assert.AreEqual(5, error.Column);
            StringAssert.Contains("Unterminated", error.Message);
        }

        [Test]
        public void NewLineInsideString_IsAnError()
        {
            CodeError error = LexError("\"abc\ndef\"");

            Assert.AreEqual(1, error.Line);
            StringAssert.Contains("Newline", error.Message);
        }

        [Test]
        public void UnexpectedCharacter_IsAnError()
        {
            CodeError error = LexError("int a = 1;\nint b = #;");

            Assert.AreEqual(2, error.Line);
            StringAssert.Contains("Unexpected character '#'", error.Message);
        }

        [Test]
        public void UnterminatedComment_IsAnError()
        {
            CodeError error = LexError("x\n/* never closed");

            Assert.AreEqual(2, error.Line);
        }

        [Test]
        public void NumberTooBigForInt_IsAnError()
        {
            CodeError error = LexError("int x = 99999999999;");

            StringAssert.Contains("too large", error.Message);
        }

        [Test]
        public void DecimalNumber_SaysNotSupportedYet()
        {
            CodeError error = LexError("var x = 1.5;");

            StringAssert.Contains("not supported yet", error.Message);
        }

        [Test]
        public void EmptySource_GivesOnlyEndOfFile()
        {
            List<Token> tokens = Lex("");

            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual(TokenType.EndOfFile, tokens[0].Type);
        }
    }
}
