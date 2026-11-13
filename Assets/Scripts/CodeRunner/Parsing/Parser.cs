using System.Collections.Generic;
using CodeRunner.Lexing;

namespace CodeRunner.Parsing
{
    // Hand-written recursive descent parser. Stops at the first error.
    public class Parser
    {
        // Keywords that the parser understands. Any other keyword gives a "not supported yet" error.
        private static readonly HashSet<string> SupportedKeywords = new HashSet<string>
        {
            "var", "int", "string", "bool", "if", "else", "while", "for", "foreach", "in",
            "break", "continue", "true", "false", "null", "is"
        };

        private static readonly HashSet<string> UnsupportedOperators = new HashSet<string>
        {
            "++", "--", "?", "??", "=>", "&", "|", "^", "~", ":"
        };

        private readonly List<Token> tokens;
        private readonly bool allowBareExpressions;
        private int current;

        // allowBareExpressions is for console mode, where "wagon.Capacity" is a valid line
        public Parser(List<Token> tokens, bool allowBareExpressions = false)
        {
            this.tokens = tokens;
            this.allowBareExpressions = allowBareExpressions;
        }

        public ProgramNode ParseProgram()
        {
            ProgramNode program = new ProgramNode();
            program.Line = 1;
            program.Column = 1;

            while (!IsAtEnd())
                program.Statements.Add(ParseStatement());

            return program;
        }

        // ---------- Token helpers ----------

        private Token Peek(int offset = 0)
        {
            int index = current + offset;
            if (index >= tokens.Count)
                index = tokens.Count - 1;
            return tokens[index];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EndOfFile;
        }

        private Token Advance()
        {
            Token token = Peek();
            if (!IsAtEnd())
                current++;
            return token;
        }

        private bool CheckSymbol(string symbol)
        {
            return Peek().IsSymbol(symbol);
        }

        private bool CheckKeyword(string keyword)
        {
            return Peek().IsKeyword(keyword);
        }

        private bool MatchSymbol(string symbol)
        {
            if (!CheckSymbol(symbol))
                return false;
            Advance();
            return true;
        }

        // ---------- Errors ----------

        private CodeErrorException ErrorAt(Token token, string code, string message)
        {
            return new CodeErrorException(new CodeError(ErrorPhase.Syntax, code, message, token.Line, token.Column));
        }

        // Like C#, a missing ; or ) is reported right after the last token we did read
        private CodeErrorException ErrorAfterPrevious(string code, string message)
        {
            if (current == 0)
                return ErrorAt(Peek(), code, message);

            Token last = Previous();
            int length = last.Text.Length;
            return new CodeErrorException(new CodeError(ErrorPhase.Syntax, code, message, last.Line, last.Column + length));
        }

        private CodeErrorException NotSupported(Token token, string what)
        {
            return ErrorAt(token, "", what + " is not supported yet");
        }

        private Token ExpectSymbol(string symbol, string code)
        {
            if (CheckSymbol(symbol))
                return Advance();
            throw ErrorAfterPrevious(code, symbol + " expected");
        }

        private Token ExpectIdentifier(string what)
        {
            Token token = Peek();
            if (token.Type == TokenType.Identifier)
                return Advance();
            throw ErrorAt(token, "CS1001", "Identifier expected, " + what + " should be a name");
        }

        // Called after an expression. Syntax we know about but don't run yet gets a clear message.
        private void RejectUnsupportedOperator()
        {
            Token token = Peek();
            if (token.Type != TokenType.Symbol || !UnsupportedOperators.Contains(token.Text))
                return;

            switch (token.Text)
            {
                case "++":
                case "--":
                    throw NotSupported(token, "Using '" + token.Text + "' inside a bigger expression");
                case "?":
                case "??":
                    throw NotSupported(token, "The '" + token.Text + "' operator");
                case "=>":
                    throw NotSupported(token, "Using lambda expressions");
                default:
                    throw NotSupported(token, "The '" + token.Text + "' operator");
            }
        }

        // ---------- Statements ----------

        private Stmt ParseStatement()
        {
            Token token = Peek();

            if (token.IsSymbol("{"))
                return ParseBlock();

            if (token.Type == TokenType.Keyword)
            {
                switch (token.Text)
                {
                    case "if": return ParseIf();
                    case "while": return ParseWhile();
                    case "for": return ParseFor();
                    case "foreach": return ParseForeach();
                    case "break":
                        Advance();
                        ExpectSymbol(";", "CS1002");
                        return Located(new BreakStmt(), token);
                    case "continue":
                        Advance();
                        ExpectSymbol(";", "CS1002");
                        return Located(new ContinueStmt(), token);
                }
            }

            Stmt statement = ParseSimpleStatement();

            // In the console the last line does not need its semicolon
            if (!(allowBareExpressions && IsAtEnd()))
                ExpectSymbol(";", "CS1002");

            return statement;
        }

        private T Located<T>(T node, Token token) where T : Node
        {
            node.Line = token.Line;
            node.Column = token.Column;
            return node;
        }

        private BlockStmt ParseBlock()
        {
            Token open = ExpectSymbol("{", "CS1514");
            BlockStmt block = Located(new BlockStmt(), open);

            while (!CheckSymbol("}"))
            {
                if (IsAtEnd())
                    throw ErrorAfterPrevious("CS1513", "} expected");
                block.Statements.Add(ParseStatement());
            }

            Advance(); // the closing brace
            return block;
        }

        private Stmt ParseIf()
        {
            Token keyword = Advance();
            ExpectSymbol("(", "CS1003");
            IfStmt statement = Located(new IfStmt(), keyword);
            statement.Condition = ParseExpression();
            ExpectSymbol(")", "CS1026");
            statement.Then = ParseStatement();

            if (CheckKeyword("else"))
            {
                Advance();
                statement.Else = ParseStatement();
            }
            return statement;
        }

        private Stmt ParseWhile()
        {
            Token keyword = Advance();
            ExpectSymbol("(", "CS1003");
            WhileStmt statement = Located(new WhileStmt(), keyword);
            statement.Condition = ParseExpression();
            ExpectSymbol(")", "CS1026");
            statement.Body = ParseStatement();
            return statement;
        }

        private Stmt ParseFor()
        {
            Token keyword = Advance();
            ExpectSymbol("(", "CS1003");
            ForStmt statement = Located(new ForStmt(), keyword);

            if (!CheckSymbol(";"))
                statement.Initializer = ParseSimpleStatement();
            ExpectSymbol(";", "CS1002");

            if (!CheckSymbol(";"))
                statement.Condition = ParseExpression();
            ExpectSymbol(";", "CS1002");

            if (!CheckSymbol(")"))
            {
                statement.Updates.Add(ParseSimpleStatement());
                while (MatchSymbol(","))
                    statement.Updates.Add(ParseSimpleStatement());
            }
            ExpectSymbol(")", "CS1026");

            statement.Body = ParseStatement();
            return statement;
        }

        private Stmt ParseForeach()
        {
            Token keyword = Advance();
            ExpectSymbol("(", "CS1003");
            ForeachStmt statement = Located(new ForeachStmt(), keyword);

            Token typeToken = Advance();
            if (typeToken.IsKeyword("var"))
                statement.TypeName = null;
            else if (IsTypeToken(typeToken))
                statement.TypeName = typeToken.Text;
            else
                throw ErrorAt(typeToken, "CS1525", "Invalid expression term '" + typeToken.Text + "', expected a type or 'var'");

            statement.VariableName = ExpectIdentifier("the loop variable").Text;

            if (!CheckKeyword("in"))
                throw ErrorAfterPrevious("CS1515", "'in' expected");
            Advance();

            statement.Collection = ParseExpression();
            ExpectSymbol(")", "CS1026");
            statement.Body = ParseStatement();
            return statement;
        }

        // A declaration, assignment, ++/-- or an expression. The caller reads the semicolon.
        private Stmt ParseSimpleStatement()
        {
            Token token = Peek();

            if (token.Type == TokenType.Keyword)
            {
                if (token.Text == "var" || token.Text == "int" || token.Text == "string" || token.Text == "bool")
                    return ParseDeclaration();

                if (!SupportedKeywords.Contains(token.Text))
                    throw NotSupported(token, "'" + token.Text + "'");
            }

            // Wagon w = ...;  is an identifier followed by another identifier
            if (token.Type == TokenType.Identifier && Peek(1).Type == TokenType.Identifier)
                return ParseDeclaration();

            Expr expression = ParseOr();
            Token next = Peek();

            if (next.IsSymbol("=") || next.IsSymbol("+=") || next.IsSymbol("-="))
            {
                Advance();
                AssignStmt assign = Located(new AssignStmt(), token);
                assign.Target = expression;
                assign.Operator = next.Text;
                assign.Value = ParseExpression();
                return assign;
            }

            if (next.IsSymbol("*=") || next.IsSymbol("/=") || next.IsSymbol("%="))
                throw NotSupported(next, "The '" + next.Text + "' operator");

            if (next.IsSymbol("++") || next.IsSymbol("--"))
            {
                Advance();
                IncDecStmt incDec = Located(new IncDecStmt(), token);
                incDec.Target = expression;
                incDec.IsIncrement = next.Text == "++";
                return incDec;
            }

            RejectUnsupportedOperator();

            if (!allowBareExpressions && !(expression is CallExpr))
            {
                throw new CodeErrorException(new CodeError(ErrorPhase.Syntax, "CS0201",
                    "Only assignment, method calls, ++ and -- can be used as a statement",
                    expression.Line, expression.Column));
            }

            ExprStmt statement = Located(new ExprStmt(), token);
            statement.Expression = expression;
            return statement;
        }

        private Stmt ParseDeclaration()
        {
            Token typeToken = Advance();
            VarDeclStmt declaration = Located(new VarDeclStmt(), typeToken);
            declaration.TypeName = typeToken.IsKeyword("var") ? null : typeToken.Text;

            if (CheckSymbol("["))
                throw NotSupported(Peek(), "Using arrays");

            Token name = ExpectIdentifier("the variable");
            declaration.Name = name.Text;

            if (MatchSymbol("="))
            {
                declaration.Initializer = ParseExpression();
            }
            else if (declaration.TypeName == null)
            {
                throw new CodeErrorException(new CodeError(ErrorPhase.Syntax, "CS0818",
                    "Implicitly-typed variables must be initialized, write var " + name.Text + " = ...;",
                    name.Line, name.Column));
            }

            return declaration;
        }

        private static bool IsTypeToken(Token token)
        {
            return token.Type == TokenType.Identifier
                || token.IsKeyword("int") || token.IsKeyword("string") || token.IsKeyword("bool");
        }

        // ---------- Expressions ----------
        // From the loosest to the tightest: ||  &&  == !=  < > <= >= is  + -  * / %  ! -  . ( )

        private Expr ParseExpression()
        {
            Expr expression = ParseOr();
            RejectUnsupportedOperator();
            return expression;
        }

        private Expr ParseOr()
        {
            Expr left = ParseAnd();
            while (CheckSymbol("||"))
            {
                Token op = Advance();
                left = MakeBinary(op, left, ParseAnd());
            }
            return left;
        }

        private Expr ParseAnd()
        {
            Expr left = ParseEquality();
            while (CheckSymbol("&&"))
            {
                Token op = Advance();
                left = MakeBinary(op, left, ParseEquality());
            }
            return left;
        }

        private Expr ParseEquality()
        {
            Expr left = ParseRelational();
            while (CheckSymbol("==") || CheckSymbol("!="))
            {
                Token op = Advance();
                left = MakeBinary(op, left, ParseRelational());
            }
            return left;
        }

        private Expr ParseRelational()
        {
            Expr left = ParseAdditive();
            while (true)
            {
                if (CheckSymbol("<") || CheckSymbol(">") || CheckSymbol("<=") || CheckSymbol(">="))
                {
                    Token op = Advance();
                    left = MakeBinary(op, left, ParseAdditive());
                }
                else if (CheckKeyword("is"))
                {
                    left = ParseIs(left);
                }
                else
                {
                    return left;
                }
            }
        }

        private Expr ParseIs(Expr operand)
        {
            Token keyword = Advance();
            IsExpr expression = Located(new IsExpr(), keyword);
            expression.Operand = operand;

            if (Peek().Type == TokenType.Identifier && Peek().Text == "not")
            {
                Advance();
                expression.Negated = true;
            }

            Token typeToken = Peek();
            if (typeToken.IsKeyword("null"))
                throw NotSupported(typeToken, "Checking for null with 'is'");
            if (!IsTypeToken(typeToken))
                throw ErrorAt(typeToken, "CS1525", "Invalid expression term '" + typeToken.Text + "', expected a type after 'is'");
            Advance();
            expression.TypeName = typeToken.Text;

            // An optional variable name: x is Type name
            if (Peek().Type == TokenType.Identifier)
                expression.VariableName = Advance().Text;

            return expression;
        }

        private Expr ParseAdditive()
        {
            Expr left = ParseMultiplicative();
            while (CheckSymbol("+") || CheckSymbol("-"))
            {
                Token op = Advance();
                left = MakeBinary(op, left, ParseMultiplicative());
            }
            return left;
        }

        private Expr ParseMultiplicative()
        {
            Expr left = ParseUnary();
            while (CheckSymbol("*") || CheckSymbol("/") || CheckSymbol("%"))
            {
                Token op = Advance();
                left = MakeBinary(op, left, ParseUnary());
            }
            return left;
        }

        private Expr MakeBinary(Token op, Expr left, Expr right)
        {
            BinaryExpr binary = Located(new BinaryExpr(), op);
            binary.Operator = op.Text;
            binary.Left = left;
            binary.Right = right;
            return binary;
        }

        private Expr ParseUnary()
        {
            if (CheckSymbol("!") || CheckSymbol("-"))
            {
                Token op = Advance();
                UnaryExpr unary = Located(new UnaryExpr(), op);
                unary.Operator = op.Text;
                unary.Operand = ParseUnary();
                return unary;
            }
            return ParsePostfix();
        }

        // Member access and calls: a.b.c(d)
        private Expr ParsePostfix()
        {
            Expr expression = ParsePrimary();

            while (true)
            {
                if (CheckSymbol("."))
                {
                    Advance();
                    Token name = ExpectIdentifier("the member");
                    MemberExpr member = Located(new MemberExpr(), name);
                    member.Target = expression;
                    member.Name = name.Text;
                    expression = member;
                }
                else if (CheckSymbol("("))
                {
                    Token open = Advance();
                    CallExpr call = new CallExpr();
                    call.Callee = expression;
                    call.Line = expression.Line;
                    call.Column = expression.Column;

                    if (!CheckSymbol(")"))
                    {
                        call.Arguments.Add(ParseExpression());
                        while (MatchSymbol(","))
                            call.Arguments.Add(ParseExpression());
                    }
                    ExpectSymbol(")", "CS1026");
                    expression = call;
                }
                else if (CheckSymbol("["))
                {
                    throw NotSupported(Peek(), "Indexing with [ ]");
                }
                else
                {
                    return expression;
                }
            }
        }

        private Expr ParsePrimary()
        {
            Token token = Peek();

            switch (token.Type)
            {
                case TokenType.IntLiteral:
                case TokenType.StringLiteral:
                    Advance();
                    return Located(new LiteralExpr { Value = token.Value }, token);

                case TokenType.Identifier:
                    Advance();
                    return Located(new NameExpr { Name = token.Text }, token);

                case TokenType.Keyword:
                    if (token.Text == "true" || token.Text == "false")
                    {
                        Advance();
                        return Located(new LiteralExpr { Value = token.Text == "true" }, token);
                    }
                    if (token.Text == "null")
                    {
                        Advance();
                        return Located(new LiteralExpr { Value = null }, token);
                    }
                    if (!SupportedKeywords.Contains(token.Text))
                        throw NotSupported(token, "'" + token.Text + "'");
                    throw ErrorAt(token, "CS1525", "Invalid expression term '" + token.Text + "'");

                case TokenType.Symbol:
                    if (token.Text == "(")
                    {
                        Advance();
                        Expr inner = ParseExpression();
                        ExpectSymbol(")", "CS1026");
                        return inner;
                    }
                    throw ErrorAt(token, "CS1525", "Invalid expression term '" + token.Text + "'");

                default:
                    throw ErrorAfterPrevious("CS1733", "Expected an expression");
            }
        }
    }
}
