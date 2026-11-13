namespace CodeRunner.Lexing
{
    public enum TokenType
    {
        Identifier,
        Keyword,
        IntLiteral,
        StringLiteral,
        Symbol,     // operators and punctuation like + == ( ) ;
        EndOfFile
    }

    public class Token
    {
        public TokenType Type;
        public string Text;     // the text as written in the source
        public object Value;    // the int or string value for literals
        public int Line;        // 1-based
        public int Column;      // 1-based

        public Token(TokenType type, string text, int line, int column, object value = null)
        {
            Type = type;
            Text = text;
            Line = line;
            Column = column;
            Value = value;
        }

        public bool IsSymbol(string symbol)
        {
            return Type == TokenType.Symbol && Text == symbol;
        }

        public bool IsKeyword(string keyword)
        {
            return Type == TokenType.Keyword && Text == keyword;
        }

        public override string ToString()
        {
            return Type + " '" + Text + "' (" + Line + ":" + Column + ")";
        }
    }
}
