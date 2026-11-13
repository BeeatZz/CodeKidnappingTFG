using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CodeRunner.Bridge;
using CodeRunner.Checking;
using CodeRunner.Lexing;
using CodeRunner.Parsing;

namespace CodeRunner.Interpreting
{
    // Everything that lives between runs: the scanned handles, the variables the player
    // declared, the run log and the settings. Each Run goes through the whole pipeline:
    // lexer, parser, checker, interpreter.
    public class CodeSession
    {
        public TypeRegistry Types { get; private set; }
        public ScopeRegistry Handles { get; private set; }
        public IActionPlayer ActionPlayer { get; private set; }
        public RunOptions Options { get; private set; }
        public RunLog Log { get; private set; }

        // Variables from earlier work order runs, so a later run can keep using them
        public Scope Variables { get; private set; }

        // The line being executed, or 0 when nothing runs. The editor highlights this line.
        public int CurrentLine { get; private set; }
        public event Action<int> LineChanged;

        // Raised when a run starts, before anything executes
        public event Action<RunMode> RunStarted;

        public CodeSession(TypeRegistry types, ScopeRegistry handles, IActionPlayer actionPlayer, RunOptions options = null)
        {
            Types = types;
            Handles = handles;
            ActionPlayer = actionPlayer;
            Options = options ?? new RunOptions();
            Log = new RunLog();
            Variables = new Scope();
        }

        public void SetCurrentLine(int line)
        {
            CurrentLine = line;
            if (LineChanged != null)
                LineChanged(line);
        }

        // Runs the code as a coroutine. Only the lines from firstLine to lastLine run, which is how
        // the editor runs a selection. The result is filled in as the run goes on.
        public IEnumerator Run(string source, RunMode mode, RunResult result, int firstLine = 1, int lastLine = int.MaxValue)
        {
            result.Mode = mode;
            result.IsFinished = false;
            result.Errors.Clear();
            result.ValueText = null;

            if (RunStarted != null)
                RunStarted(mode);

            ProgramNode program = Compile(source, mode, firstLine, lastLine, result);
            if (program != null)
            {
                Scope runScope = new Scope(Variables);
                Interpreter interpreter = new Interpreter(this, mode, result);
                IEnumerator routine = interpreter.ExecuteProgram(program, runScope).GetEnumerator();

                while (true)
                {
                    object step = null;
                    try
                    {
                        if (!routine.MoveNext())
                            break;
                        step = routine.Current;
                    }
                    catch (ScriptRuntimeException e)
                    {
                        ReportRuntimeError(result, e.ExceptionType, e.Message, e.Line);
                        break;
                    }
                    catch (Exception e)
                    {
                        ReportRuntimeError(result, "InternalError", e.Message, CurrentLine);
                        break;
                    }
                    yield return step;
                }

                result.StepsUsed = interpreter.Steps;

                // Console snippets leave nothing behind, work orders keep their variables
                if (mode == RunMode.WorkOrder)
                    Variables.CopyFrom(runScope);
            }

            SetCurrentLine(0);
            result.IsFinished = true;
        }

        // Finds the errors in the code without running it and without touching the run log.
        // The editor uses this to underline mistakes while the player types.
        public List<CodeError> FindErrors(string source, RunMode mode)
        {
            List<CodeError> errors = new List<CodeError>();
            ProgramNode program;

            try
            {
                var tokens = new Lexer(source).Tokenize();
                program = new Parser(tokens, mode == RunMode.Console).ParseProgram();
            }
            catch (CodeErrorException e)
            {
                errors.Add(e.Error);
                return errors;
            }

            Checker checker = new Checker(Types, Handles, Variables.GetTypes(), mode);
            errors.AddRange(checker.Check(program));
            return errors;
        }

        // Runs everything at once, without Unity. Handy for tests and the console.
        public RunResult RunBlocking(string source, RunMode mode, int firstLine = 1, int lastLine = int.MaxValue)
        {
            RunResult result = new RunResult();
            CoroutineRunner.RunToCompletion(Run(source, mode, result, firstLine, lastLine));
            return result;
        }

        // Lexer, parser and checker. Returns null if the code has errors.
        private ProgramNode Compile(string source, RunMode mode, int firstLine, int lastLine, RunResult result)
        {
            string text = KeepOnlyLines(source, firstLine, lastLine);
            ProgramNode program;

            try
            {
                var tokens = new Lexer(text).Tokenize();
                program = new Parser(tokens, mode == RunMode.Console).ParseProgram();
            }
            catch (CodeErrorException e)
            {
                ReportCompileErrors(result, new[] { e.Error });
                return null;
            }

            Checker checker = new Checker(Types, Handles, Variables.GetTypes(), mode);
            var errors = checker.Check(program);
            if (errors.Count > 0)
            {
                ReportCompileErrors(result, errors);
                return null;
            }

            return program;
        }

        private void ReportCompileErrors(RunResult result, System.Collections.Generic.IEnumerable<CodeError> errors)
        {
            foreach (CodeError error in errors)
            {
                result.Errors.Add(error);
                Log.AddError(error.Line, "error", error.ToString());
            }
        }

        private void ReportRuntimeError(RunResult result, string exceptionType, string message, int line)
        {
            CodeError error = new CodeError(ErrorPhase.Runtime, "", message, line, 0);
            error.ExceptionType = exceptionType;
            result.Errors.Add(error);
            Log.AddError(line, exceptionType, message);
        }

        // Blanks the lines outside the range but keeps the line breaks,
        // so the line numbers in errors still match the editor.
        private static string KeepOnlyLines(string source, int firstLine, int lastLine)
        {
            if (source == null)
                return "";
            if (firstLine <= 1 && lastLine == int.MaxValue)
                return source;

            StringBuilder kept = new StringBuilder();
            int line = 1;
            foreach (char c in source)
            {
                if (c == '\n')
                {
                    kept.Append(c);
                    line++;
                }
                else if (line >= firstLine && line <= lastLine)
                {
                    kept.Append(c);
                }
            }
            return kept.ToString();
        }
    }
}
