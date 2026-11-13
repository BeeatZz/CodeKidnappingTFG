using System.Collections.Generic;
using System.Text;

namespace CodeRunner.Lexing
{
    // Turns source text into a list of tokens
    public class Lexer
    {
        // Every C# keyword is lexed as a keyword. The parser decides which ones it supports.
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "var", "virtual", "void", "volatile", "while"
        };

        private static readonly string[] TwoCharSymbols =
        {
            "==", "!=", "<=", ">=", "&&", "||", "++", "--", "+=", "-=", "*=", "/=", "%=", "=>", "??"
        };

        private const string OneCharSymbols = "+-*/%=<>!(){}[];,.?:&|^~";

        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private int position;
        private int line = 1;
        private int column = 1;

        public Lexer(string source)
        {
            this.source = source ?? "";
        }

        public List<Token> Tokenize()
        {
            while (true)
            {
                SkipWhitespaceAndComments();
                if (position >= source.Length)
                    break;
                ReadToken();
            }

            tokens.Add(new Token(TokenType.EndOfFile, "", line, column));
            return tokens;
        }

        private char Peek(int offset = 0)
        {
            int index = position + offset;
            return index < source.Length ? source[index] : '\0';
        }

        private char Advance()
        {
            char c = source[position];
            position++;
            if (c == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            return c;
        }

        private CodeErrorException Error(string code, string message, int errorLine, int errorColumn)
        {
            return new CodeErrorException(new CodeError(ErrorPhase.Syntax, code, message, errorLine, errorColumn));
        }

        private CodeErrorException NotSupported(string what, int errorLine, int errorColumn)
        {
            return Error("", what + " is not supported yet", errorLine, errorColumn);
        }

        private void SkipWhitespaceAndComments()
        {
            while (position < source.Length)
            {
                char c = Peek();

                if (char.IsWhiteSpace(c))
                {
                    Advance();
                }
                else if (c == '/' && Peek(1) == '/')
                {
                    while (position < source.Length && Peek() != '\n')
                        Advance();
                }
                else if (c == '/' && Peek(1) == '*')
                {
                    int startLine = line;
                    int startColumn = column;
                    Advance();
                    Advance();
                    while (!(Peek() == '*' && Peek(1) == '/'))
                    {
                        if (position >= source.Length)
                            throw Error("CS1035", "End-of-file found, '*/' expected", startLine, startColumn);
                        Advance();
                    }
                    Advance();
                    Advance();
                }
                else
                {
                    break;
                }
            }
        }

        private void ReadToken()
        {
            char c = Peek();

            if (char.IsLetter(c) || c == '_')
                ReadWord();
            else if (char.IsDigit(c))
                ReadNumber();
            else if (c == '"')
                ReadString();
            else if (c == '\'')
                throw NotSupported("Using character literals", line, column);
            else if ((c == '$' || c == '@') && Peek(1) == '"')
                throw NotSupported("Using interpolated or verbatim strings", line, column);
            else
                ReadSymbol();
        }

        private void ReadWord()
        {
            int startLine = line;
            int startColumn = column;
            int start = position;

            while (char.IsLetterOrDigit(Peek()) || Peek() == '_')
                Advance();

            string text = source.Substring(start, position - start);
            TokenType type = Keywords.Contains(text) ? TokenType.Keyword : TokenType.Identifier;
            tokens.Add(new Token(type, text, startLine, startColumn));
        }

        private void ReadNumber()
        {
            int startLine = line;
            int startColumn = column;
            int start = position;

            while (char.IsDigit(Peek()))
                Advance();

            // 1.5, 10f, 3L and similar are not part of version 1
            char next = Peek();
            if (next == '.' && char.IsDigit(Peek(1)))
                throw NotSupported("Using decimal numbers", startLine, startColumn);
            if (char.IsLetter(next) || next == '_')
                throw Error("CS1003", "Invalid number, numbers can only contain digits", startLine, startColumn);

            string text = source.Substring(start, position - start);
            int value;
            if (!int.TryParse(text, out value))
                throw Error("CS1021", "The number " + text + " is too large for an int", startLine, startColumn);

            tokens.Add(new Token(TokenType.IntLiteral, text, startLine, startColumn, value));
        }

        private void ReadString()
        {
            int startLine = line;
            int startColumn = column;
            int start = position;
            StringBuilder value = new StringBuilder();

            Advance(); // opening quote

            while (true)
            {
                if (position >= source.Length)
                    throw Error("CS1039", "Unterminated string literal", startLine, startColumn);

                char c = Peek();

                if (c == '\n')
                    throw Error("CS1010", "Newline in constant, a string must end on the same line", startLine, startColumn);

                if (c == '"')
                {
                    Advance();
                    break;
                }

                if (c == '\\')
                {
                    int escapeLine = line;
                    int escapeColumn = column;
                    Advance();
                    char escaped = position < source.Length ? Advance() : '\0';
                    switch (escaped)
                    {
                        case 'n': value.Append('\n'); break;
                        case 't': value.Append('\t'); break;
                        case 'r': value.Append('\r'); break;
                        case '0': value.Append('\0'); break;
                        case '\\': value.Append('\\'); break;
                        case '"': value.Append('"'); break;
                        case '\'': value.Append('\''); break;
                        default:
                            throw Error("CS1009", "Unrecognized escape sequence", escapeLine, escapeColumn);
                    }
                }
                else
                {
                    value.Append(Advance());
                }
            }

            string text = source.Substring(start, position - start);
            tokens.Add(new Token(TokenType.StringLiteral, text, startLine, startColumn, value.ToString()));
        }

        private void ReadSymbol()
        {
            int startLine = line;
            int startColumn = column;

            foreach (string symbol in TwoCharSymbols)
            {
                if (Peek() == symbol[0] && Peek(1) == symbol[1])
                {
                    Advance();
                    Advance();
                    tokens.Add(new Token(TokenType.Symbol, symbol, startLine, startColumn));
                    return;
                }
            }

            char c = Peek();
            if (OneCharSymbols.IndexOf(c) >= 0)
            {
                Advance();
                tokens.Add(new Token(TokenType.Symbol, c.ToString(), startLine, startColumn));
                return;
            }

            throw Error("CS1056", "Unexpected character '" + c + "'", startLine, startColumn);
        }
    }
}
