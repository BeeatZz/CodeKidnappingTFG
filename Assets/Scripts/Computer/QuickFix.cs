using CodeRunner;

namespace Computer
{
    // Turns a hint like "Did you mean 'Weight'?" into a name the editor can type in for the player
    public static class QuickFix
    {
        // The suggested name, or null if the error has no suggestion
        public static string NameFor(CodeError error)
        {
            if (error == null || string.IsNullOrEmpty(error.Hint) || !error.Hint.StartsWith("Did you mean"))
                return null;

            int start = error.Hint.IndexOf('\'');
            int end = start < 0 ? -1 : error.Hint.IndexOf('\'', start + 1);
            if (start < 0 || end < 0)
                return null;

            return error.Hint.Substring(start + 1, end - start - 1);
        }

        // The text of an error without the "Line 4:" start, which the editor shows on its own
        public static string MessageOf(CodeError error)
        {
            string text = error.Message;
            if (!string.IsNullOrEmpty(error.Code))
                text = "error " + error.Code + ": " + text;
            if (!string.IsNullOrEmpty(error.Hint))
                text += " " + error.Hint;
            return text;
        }
    }
}
