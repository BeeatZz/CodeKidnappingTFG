using System;

namespace CodeRunner
{
    public enum ErrorPhase
    {
        Syntax,
        Check,
        Runtime
    }

    // An error found in the player's code, at any stage of the pipeline
    public class CodeError
    {
        public ErrorPhase Phase;
        public string Code;           // compiler style code like "CS1061", empty if there is none
        public string Message;
        public string Hint;           // optional friendly hint, for example "Did you mean 'Weight'?"
        public int Line;
        public int Column;
        public string ExceptionType;  // only used for runtime errors

        public CodeError(ErrorPhase phase, string code, string message, int line, int column, string hint = null)
        {
            Phase = phase;
            Code = code;
            Message = message;
            Line = line;
            Column = column;
            Hint = hint;
        }

        public override string ToString()
        {
            string text = "Line " + Line + ": ";

            if (Phase == ErrorPhase.Runtime && ExceptionType != null)
                text += ExceptionType + ": " + Message;
            else if (!string.IsNullOrEmpty(Code))
                text += "error " + Code + ": " + Message;
            else
                text += "error: " + Message;

            if (!string.IsNullOrEmpty(Hint))
                text += " " + Hint;

            return text;
        }
    }

    // Thrown by the lexer and the parser, which stop at the first error
    public class CodeErrorException : Exception
    {
        public CodeError Error;

        public CodeErrorException(CodeError error) : base(error.Message)
        {
            Error = error;
        }
    }
}
