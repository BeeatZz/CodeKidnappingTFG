using System.Collections.Generic;
using CodeRunner.Bridge;
using CodeRunner.Interpreting;
using Mission;
using NUnit.Framework;

namespace CodeRunner.Tests
{
    public class MissionTests
    {
        private TestWorld world;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
        }

        // ---------- Level data ----------

        [Test]
        public void LevelData_MatchesTheDescription()
        {
            List<IScannable> things = new List<IScannable>(world.Level.Depot.ScanAll());
            // 30 crates, 3 wagons and the ledger
            Assert.AreEqual(34, things.Count);

            Assert.AreEqual(6, world.Level.Ledger.Order["Grain"]);
            Assert.AreEqual(2, world.Level.Ledger.Order["Lamp oil"]);

            Assert.AreEqual(20, world.Level.Depot.Wagons[0].Capacity);
            Assert.AreEqual(30, world.Level.Depot.Wagons[1].Capacity);
            Assert.AreEqual(35, world.Level.Depot.Wagons[2].Capacity);
            Assert.AreEqual("War wagon", world.Level.Depot.Wagons[2].Name);
        }

        [Test]
        public void IsMissionComplete_IsFalseAtTheStart_AndDoesNotChangeAnything()
        {
            Assert.IsFalse(MissionRules.IsMissionComplete(world.Level.Ledger, world.Level.Camp));
            Assert.IsFalse(MissionRules.IsMissionComplete(world.Level.Ledger, world.Level.Camp));
            Assert.AreEqual(0, world.Level.Camp.Accepted.Count);
            Assert.AreEqual(0, world.Level.Camp.Rejected.Count);
        }

        [Test]
        public void Camp_RejectsSpoilingFood_Rocks_AndStaysIncomplete()
        {
            world.ScanEverything();

            // crate00 spoils tomorrow, crate02 is rocks labelled as grain
            world.Run("imp.Load(crate00, smallCart);\nimp.Load(crate02, smallCart);\nsmallCart.Dispatch();");

            Assert.AreEqual(0, world.Level.Camp.Accepted.Count);
            Assert.AreEqual(2, world.Level.Camp.Rejected.Count);
            StringAssert.Contains("spoil", world.Level.Camp.Rejected[0].Reason);
            StringAssert.Contains("Rocks", world.Level.Camp.Rejected[1].Reason);
            Assert.IsFalse(MissionRules.IsMissionComplete(world.Level.Ledger, world.Level.Camp));
        }

        [Test]
        public void IsMissionComplete_IsFalse_WhenOneItemIsShort()
        {
            world.ScanEverything();

            // Everything but the bandages
            world.Run(FullPlanLines(false).ToText());

            Assert.IsFalse(MissionRules.IsMissionComplete(world.Level.Ledger, world.Level.Camp));
        }

        [Test]
        public void LoadingTheSameCrateTwice_IsARuntimeError()
        {
            world.ScanEverything();

            RunResult result = world.Run("imp.Load(crate03, smallCart);\nimp.Load(crate03, smallCart);");

            Assert.IsFalse(result.Success);
            Assert.AreEqual("CargoAlreadyLoadedException", result.FirstError.ExceptionType);
            Assert.AreEqual(2, result.FirstError.Line);
            Assert.AreEqual(4, world.Level.Depot.Wagons[0].CurrentLoad);
        }

        [Test]
        public void LoadingACrateOntoTwoWagons_IsARuntimeError()
        {
            world.ScanEverything();

            RunResult result = world.Run("imp.Load(crate03, smallCart);\nimp.Load(crate03, farmWagon);");

            Assert.AreEqual("CargoAlreadyLoadedException", result.FirstError.ExceptionType);
            Assert.AreEqual(0, world.Level.Depot.Wagons[1].CurrentLoad);
        }

        // ---------- Acceptance test 1: the loop based solution ----------

        [Test]
        public void Acceptance_LoopSolution_FillsTheWholeOrder()
        {
            string script =
                "foreach (var entry in depot.Ledger.Order)\n" +
                "{\n" +
                "    int stillNeeded = entry.Value;\n" +
                "    foreach (var thing in depot.ScanAll())\n" +
                "    {\n" +
                "        if (stillNeeded == 0) break;\n" +
                "        if (thing is not ICargo cargo) continue;\n" +
                "        if (cargo.Contents != entry.Key) continue;\n" +
                "        if (cargo is IPerishable food && food.DaysUntilSpoiled < 2) continue;\n" +
                "        if (cargo is IVolatile oil && oil.IsLeaking) continue;\n" +
                "        foreach (var wagon in depot.Wagons)\n" +
                "        {\n" +
                "            if (wagon.CurrentLoad + cargo.Weight <= wagon.Capacity)\n" +
                "            {\n" +
                "                imp.Load(cargo, wagon);\n" +
                "                stillNeeded--;\n" +
                "                break;\n" +
                "            }\n" +
                "        }\n" +
                "    }\n" +
                "}\n" +
                "foreach (var wagon in depot.Wagons)\n" +
                "{\n" +
                "    if (wagon.Cargo.Count > 0) wagon.Dispatch();\n" +
                "}";

            RunResult result = world.Run(script);

            Assert.IsEmpty(result.Errors, result.FirstError == null ? "" : result.FirstError.ToString());
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, world.Level.Camp.Rejected.Count);
            Assert.AreEqual(20, world.Level.Camp.Accepted.Count);
            Assert.IsTrue(MissionRules.IsMissionComplete(world.Level.Ledger, world.Level.Camp));

            // 20 loads and 3 dispatches all went through the action player
            Assert.AreEqual(23, world.Player.Played.Count);
        }

        // ---------- Acceptance test 2: named handles, several partial runs ----------

        // Builds the script and remembers which lines belong to which step
        private class Plan
        {
            public List<string> Lines = new List<string>();
            public int GrainEnd;
            public int FarmEnd;
            public int CartEnd;

            public string ToText()
            {
                return string.Join("\n", Lines);
            }
        }

        private static void AddLoads(Plan plan, string wagon, params int[] crates)
        {
            foreach (int crate in crates)
                plan.Lines.Add("imp.Load(crate" + crate.ToString("D2") + ", " + wagon + ");");
        }

        // grain 3-8 -> war wagon, meat 11-14 and arrows 16-20 -> farm wagon,
        // lamp oil 23-24 and bandages 26-28 -> small cart, then dispatch everything
        private static Plan FullPlanLines(bool includeBandages)
        {
            Plan plan = new Plan();

            plan.Lines.Add("var grainWagon = warWagon;");
            plan.Lines.Add("imp.MoveTo(crate03);");
            AddLoads(plan, "grainWagon", 3, 4, 5, 6, 7, 8);
            plan.GrainEnd = plan.Lines.Count;

            AddLoads(plan, "farmWagon", 11, 12, 13, 14, 16, 17, 18, 19, 20);
            plan.FarmEnd = plan.Lines.Count;

            AddLoads(plan, "smallCart", 23, 24);
            if (includeBandages)
                AddLoads(plan, "smallCart", 26, 27, 28);
            plan.CartEnd = plan.Lines.Count;

            plan.Lines.Add("grainWagon.Dispatch();");
            plan.Lines.Add("farmWagon.Dispatch();");
            plan.Lines.Add("smallCart.Dispatch();");
            return plan;
        }

        [Test]
        public void Acceptance_NamedHandles_SplitAcrossPartialRuns()
        {
            world.ScanEverything();
            Plan plan = FullPlanLines(true);
            string source = plan.ToText();
            int last = plan.Lines.Count;

            Wagon smallCart = world.Level.Depot.Wagons[0];
            Wagon farmWagon = world.Level.Depot.Wagons[1];
            Wagon warWagon = world.Level.Depot.Wagons[2];

            // Run 1: the grain
            RunResult grain = world.Session.RunBlocking(source, RunMode.WorkOrder, 1, plan.GrainEnd);
            Assert.IsTrue(grain.Success, grain.FirstError == null ? "" : grain.FirstError.ToString());
            Assert.AreEqual(24, warWagon.CurrentLoad);
            Assert.AreEqual(0, farmWagon.CurrentLoad);

            // Run 2: meat and arrows. The wagons from run 1 are still loaded.
            RunResult farm = world.Session.RunBlocking(source, RunMode.WorkOrder, plan.GrainEnd + 1, plan.FarmEnd);
            Assert.IsTrue(farm.Success, farm.FirstError == null ? "" : farm.FirstError.ToString());
            Assert.AreEqual(24, warWagon.CurrentLoad);
            Assert.AreEqual(22, farmWagon.CurrentLoad);

            // Run 3: lamp oil and bandages
            RunResult cart = world.Session.RunBlocking(source, RunMode.WorkOrder, plan.FarmEnd + 1, plan.CartEnd);
            Assert.IsTrue(cart.Success, cart.FirstError == null ? "" : cart.FirstError.ToString());
            Assert.AreEqual(13, smallCart.CurrentLoad);
            Assert.IsFalse(MissionRules.IsMissionComplete(world.Level.Ledger, world.Level.Camp), "Nothing is dispatched yet");

            // Run 4: dispatch. It uses the grainWagon variable declared in run 1.
            RunResult dispatch = world.Session.RunBlocking(source, RunMode.WorkOrder, plan.CartEnd + 1, last);
            Assert.IsTrue(dispatch.Success, dispatch.FirstError == null ? "" : dispatch.FirstError.ToString());

            Assert.AreEqual(0, world.Level.Camp.Rejected.Count);
            Assert.AreEqual(20, world.Level.Camp.Accepted.Count);
            Assert.IsTrue(MissionRules.IsMissionComplete(world.Level.Ledger, world.Level.Camp));
        }
    }
}
