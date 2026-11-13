using System.Collections.Generic;
using CodeRunner.Bridge;
using CodeRunner.Checking;
using CodeRunner.Interpreting;
using CodeRunner.Lexing;
using CodeRunner.Parsing;
using Mission;

namespace CodeRunner.Tests
{
    // The first test mission wired up to a session, without any scene
    public class TestWorld
    {
        public FirstMission Level;
        public TypeRegistry Types;
        public InstantActionPlayer Player;
        public ScopeRegistry Handles;
        public CodeSession Session;

        // The scanner would register these handles. Here the tests do it.
        public TestWorld(int maxSteps = 10000)
        {
            Level = new FirstMission();

            Types = new TypeRegistry();
            Types.RegisterAssembly(typeof(Imp).Assembly);

            Player = new InstantActionPlayer();

            Handles = new ScopeRegistry(Level.Imp);
            Handles.Add("depot", Level.Depot);
            Handles.Add("ledger", Level.Ledger);

            RunOptions options = new RunOptions();
            options.MaxSteps = maxSteps;
            Session = new CodeSession(Types, Handles, Player, options);
        }

        // Registers crate00 to crate29 (in the order they were added to the depot)
        // and the three wagons, like a player who scanned everything.
        public void ScanEverything()
        {
            List<IScannable> things = new List<IScannable>(Level.Depot.ScanAll());
            for (int i = 0; i < 30; i++)
                Handles.Add("crate" + i.ToString("D2"), things[i]);

            Handles.Add("smallCart", Level.Depot.Wagons[0]);
            Handles.Add("farmWagon", Level.Depot.Wagons[1]);
            Handles.Add("warWagon", Level.Depot.Wagons[2]);
        }

        // Runs only the checker, so tests can look at the errors without anything running
        public List<CodeError> Check(string source, RunMode mode = RunMode.WorkOrder)
        {
            List<Token> tokens = new Lexer(source).Tokenize();
            ProgramNode program = new Parser(tokens, mode == RunMode.Console).ParseProgram();
            Checker checker = new Checker(Types, Handles, Session.Variables.GetTypes(), mode);
            return checker.Check(program);
        }

        public RunResult Run(string source, RunMode mode = RunMode.WorkOrder)
        {
            return Session.RunBlocking(source, mode);
        }
    }
}
