using System.Collections;
using System.Collections.Generic;
using CodeRunner.Interpreting;
using Mission;
using NUnit.Framework;

namespace CodeRunner.Tests
{
    public class InterpreterTests
    {
        private TestWorld world;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
        }

        private static string Lines(params string[] lines)
        {
            return string.Join("\n", lines);
        }

        [Test]
        public void Console_EvaluatesExpressions()
        {
            Assert.AreEqual("7", world.Run("1 + 2 * 3", RunMode.Console).ValueText);
            Assert.AreEqual("\"a1True\"", world.Run("\"a\" + 1 + true", RunMode.Console).ValueText);
            Assert.AreEqual("true", world.Run("10 % 3 == 1 && !(4 < 2)", RunMode.Console).ValueText);
        }

        [Test]
        public void Console_ReadsTheWorld()
        {
            Assert.AreEqual("3", world.Run("depot.Wagons.Count", RunMode.Console).ValueText);
            Assert.AreEqual("5", world.Run("ledger.Order.Count", RunMode.Console).ValueText);
        }

        [Test]
        public void Console_RefusesWorldActions_AndDoesNotRunThem()
        {
            RunResult result = world.Run("imp.Say(\"hello\")", RunMode.Console);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("console", result.FirstError.Message);
            Assert.IsNull(world.Level.Imp.LastSaid);
            Assert.AreEqual(0, world.Player.Played.Count);
        }

        [Test]
        public void Console_StillRefusesWorldActionsHiddenInsideStatements()
        {
            RunResult result = world.Run("if (true) { imp.Say(\"hello\"); }", RunMode.Console);

            Assert.IsFalse(result.Success);
            Assert.IsNull(world.Level.Imp.LastSaid);
        }

        [Test]
        public void Console_LeavesNoVariablesBehind()
        {
            world.Run("int hidden = 5;", RunMode.Console);

            RunResult result = world.Run("hidden", RunMode.Console);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("CS0103", result.FirstError.Code);
        }

        [Test]
        public void Loops_BreakAndContinue_Work()
        {
            RunResult result = world.Run(Lines(
                "int n = 0;",
                "int hits = 0;",
                "while (n < 10)",
                "{",
                "    n++;",
                "    if (n % 2 == 0) continue;",
                "    if (n > 7) break;",
                "    hits += n;",
                "}",
                "imp.Say(\"\" + hits);"));

            Assert.IsTrue(result.Success);
            Assert.AreEqual("16", world.Level.Imp.LastSaid);
        }

        [Test]
        public void ForLoop_AndCompoundAssignment_Work()
        {
            world.Run(Lines(
                "int sum = 0;",
                "for (int i = 1; i <= 4; i++) { sum += i; }",
                "sum -= 3;",
                "imp.Say(\"sum \" + sum);"));

            Assert.AreEqual("sum 7", world.Level.Imp.LastSaid);
        }

        [Test]
        public void Foreach_OverDictionary_GivesKeyAndValue()
        {
            world.Run(Lines(
                "int total = 0;",
                "string names = \"\";",
                "foreach (var entry in ledger.Order)",
                "{",
                "    total += entry.Value;",
                "    names += entry.Key + \";\";",
                "}",
                "imp.Say(total + \" \" + names);"));

            Assert.AreEqual("20 Grain;Salted meat;Arrows;Lamp oil;Bandages;", world.Level.Imp.LastSaid);
        }

        [Test]
        public void IfElse_ChoosesTheRightBranch()
        {
            world.Run(Lines(
                "int x = 3;",
                "if (x > 5) { imp.Say(\"big\"); }",
                "else if (x > 2) { imp.Say(\"medium\"); }",
                "else { imp.Say(\"small\"); }"));

            Assert.AreEqual("medium", world.Level.Imp.LastSaid);
        }

        [Test]
        public void ShortCircuit_DoesNotEvaluateTheRightSide()
        {
            RunResult result = world.Run("ICargo c = null;\nif (c != null && c.Weight > 1) { imp.Say(\"no\"); }");

            Assert.IsTrue(result.Success);
            Assert.IsNull(world.Level.Imp.LastSaid);
        }

        [Test]
        public void StepBudget_StopsAnEndlessLoop_AndNamesTheLine()
        {
            world = new TestWorld(100);

            RunResult result = world.Run(Lines(
                "imp.Say(\"a\");",
                "imp.Say(\"b\");",
                "while (true) { }",
                "imp.Say(\"never\");"));

            Assert.IsFalse(result.Success);
            Assert.AreEqual("StepLimitExceeded", result.FirstError.ExceptionType);
            Assert.AreEqual(3, result.FirstError.Line);
            StringAssert.Contains("line 3", result.FirstError.Message);
            Assert.AreEqual("b", world.Level.Imp.LastSaid);
        }

        [Test]
        public void StepBudget_IsConfigurable()
        {
            string script = "for (int i = 0; i < 50; i++) { }";

            Assert.IsTrue(new TestWorld(1000).Run(script).Success);
            Assert.IsFalse(new TestWorld(20).Run(script).Success);
        }

        [Test]
        public void RuntimeException_StopsTheRun_AtTheRightLine()
        {
            world.ScanEverything();
            world.Handles.Add("cart", world.Level.Depot.Wagons[0]);

            RunResult result = world.Run(Lines(
                "imp.Say(\"start\");",
                "foreach (var thing in depot.ScanAll())",
                "{",
                "    if (thing is ICargo cargo)",
                "    {",
                "        imp.Load(cargo, cart);",
                "    }",
                "}",
                "imp.Say(\"never\");"));

            Assert.IsFalse(result.Success);
            Assert.AreEqual("WagonOverloadedException", result.FirstError.ExceptionType);
            Assert.AreEqual(6, result.FirstError.Line);
            StringAssert.Contains("Small cart", result.FirstError.Message);

            // The world stays in the state it reached: five grain crates fit
            Assert.AreEqual("start", world.Level.Imp.LastSaid);
            Assert.AreEqual(20, world.Level.Depot.Wagons[0].CurrentLoad);
        }

        [Test]
        public void LeakingBarrel_ExplodesAndDestroysTheWagon()
        {
            world.ScanEverything();

            RunResult result = world.Run("imp.Load(crate22, smallCart);\nimp.Say(\"never\");");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("CargoExplodedException", result.FirstError.ExceptionType);
            Assert.AreEqual(1, result.FirstError.Line);
            Assert.IsTrue(world.Level.Depot.Wagons[0].IsDestroyed);
            Assert.IsNull(world.Level.Imp.LastSaid);
        }

        [Test]
        public void NullMemberAccess_IsARuntimeError()
        {
            RunResult result = world.Run("ICargo c = null;\nint w = c.Weight;");

            Assert.AreEqual("NullReferenceException", result.FirstError.ExceptionType);
            Assert.AreEqual(2, result.FirstError.Line);
        }

        [Test]
        public void DivisionByZero_IsARuntimeError()
        {
            RunResult result = world.Run("int a = 5;\nint b = 0;\nint c = a / b;");

            Assert.AreEqual("DivideByZeroException", result.FirstError.ExceptionType);
            Assert.AreEqual(3, result.FirstError.Line);
        }

        [Test]
        public void PartialRuns_KeepTheWorldAndTheVariables()
        {
            string source = Lines(
                "int count = 5;",
                "imp.Say(\"first\");",
                "count += 1;",
                "imp.Say(\"count \" + count);");

            RunResult first = world.Session.RunBlocking(source, RunMode.WorkOrder, 1, 2);
            Assert.IsTrue(first.Success);
            Assert.AreEqual("first", world.Level.Imp.LastSaid);

            RunResult second = world.Session.RunBlocking(source, RunMode.WorkOrder, 3, 4);
            Assert.IsTrue(second.Success);
            Assert.AreEqual("count 6", world.Level.Imp.LastSaid);
        }

        [Test]
        public void PartialRun_ErrorsKeepTheEditorLineNumbers()
        {
            string source = Lines("imp.Say(\"a\");", "imp.Say(\"b\");", "int x = \"oops\";");

            RunResult result = world.Session.RunBlocking(source, RunMode.WorkOrder, 2, 3);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(3, result.FirstError.Line);
            Assert.IsNull(world.Level.Imp.LastSaid);
        }

        [Test]
        public void RunningTheSameDeclarationAgain_IsFine()
        {
            Assert.IsTrue(world.Run("int x = 1;").Success);
            Assert.IsTrue(world.Run("int x = 2;\nimp.Say(\"\" + x);").Success);
            Assert.AreEqual("2", world.Level.Imp.LastSaid);
        }

        [Test]
        public void WorldActions_GoThroughTheYieldPath()
        {
            RunResult result = new RunResult();
            IEnumerator routine = world.Session.Run("imp.Say(\"a\");\nint x = 1;\nimp.Say(\"b\");", RunMode.WorkOrder, result);

            // The state changes as the action is called, and the interpreter then yields the
            // animation. Like Unity, the test plays it before asking for the next step.
            int yields = 0;
            while (routine.MoveNext())
            {
                Assert.IsInstanceOf<IEnumerator>(routine.Current);
                yields++;
                CoroutineRunner.RunToCompletion((IEnumerator)routine.Current);
            }

            Assert.AreEqual(2, yields);
            Assert.AreEqual(2, world.Player.Played.Count);
            Assert.AreEqual("imp.Say(\"a\")", world.Player.Played[0].Text);
            Assert.IsTrue(result.Success);
        }

        [Test]
        public void CurrentLine_IsReportedWhileRunning()
        {
            List<int> seen = new List<int>();
            world.Session.LineChanged += line => seen.Add(line);

            world.Run("int a = 1;\n\nint b = 2;\nimp.Say(\"x\");");

            CollectionAssert.AreEqual(new[] { 1, 3, 4, 0 }, seen);
            Assert.AreEqual(0, world.Session.CurrentLine);
        }

        [Test]
        public void RunLog_ListsWorldActionsAndErrors()
        {
            world.ScanEverything();

            world.Run("imp.Say(\"hi\");\nimp.Load(crate00, smallCart);");
            world.Run("imp.Load(crate22, smallCart);");

            List<RunLogEntry> entries = world.Session.Log.Entries;
            Assert.AreEqual(3, entries.Count);
            Assert.AreEqual(1, entries[0].Line);
            Assert.AreEqual("imp.Say(\"hi\")", entries[0].Action);
            Assert.AreEqual("imp.Load(crate00, smallCart)", entries[1].Action);
            Assert.AreEqual(2, entries[1].Line);
            Assert.IsTrue(entries[entries.Count - 1].IsError);
            StringAssert.Contains("CargoExplodedException", entries[entries.Count - 1].Action);
        }

        [Test]
        public void ScannedHandle_CanBeUsed_AfterItIsRegistered()
        {
            world.ScanEverything();
            world.Handles.Add("thing", world.Level.Depot.Wagons[1]);

            RunResult before = new TestWorld().Run("imp.MoveTo(thing);");
            Assert.IsFalse(before.Success);
            Assert.AreEqual("CS0103", before.FirstError.Code);

            RunResult after = world.Run("imp.MoveTo(thing);");
            Assert.IsTrue(after.Success);
            Assert.AreSame(world.Level.Depot.Wagons[1], world.Level.Imp.Location);
        }

        [Test]
        public void ImpHandle_IsAlwaysThere()
        {
            Assert.IsTrue(world.Run("imp.Say(\"hi\");").Success);
        }

        [Test]
        public void SyntaxError_IsReported_AndNothingRuns()
        {
            RunResult result = world.Run("imp.Say(\"a\");\nint x = ;");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(2, result.FirstError.Line);
            Assert.IsNull(world.Level.Imp.LastSaid);
        }
    }
}
