using CodeRunner.Bridge;
using CodeRunner.Interpreting;
using Mission;
using NUnit.Framework;

namespace CodeRunner.Tests
{
    public class KitchenTests
    {
        private Kitchen kitchen;
        private TypeRegistry types;
        private CodeSession session;

        [SetUp]
        public void SetUp()
        {
            kitchen = new Kitchen();

            types = new TypeRegistry();
            types.RegisterAssembly(typeof(Imp).Assembly);

            ScopeRegistry handles = new ScopeRegistry(kitchen.Imp);
            for (int i = 0; i < kitchen.Sacks.Count; i++)
                handles.Add("sack" + (i + 1).ToString("D2"), kitchen.Sacks[i]);
            handles.Add("hound", kitchen.Hound);

            session = new CodeSession(types, handles, new InstantActionPlayer());
            kitchen.WatchSession(session);
        }

        private RunResult Run(string source, RunMode mode = RunMode.WorkOrder)
        {
            return session.RunBlocking(source, mode);
        }

        private const string SolutionA = "imp.Give(sack01, hound);\nimp.Give(sack02, hound);\nimp.Give(sack03, hound);";
        private const string SolutionB = "imp.Give(sack01, hound);\nimp.Give(sack02, hound);\nimp.Give(sack04, hound);";

        [Test]
        public void StartingValues_MatchTheDescription()
        {
            Assert.AreEqual("Meat", kitchen.Sacks[0].Label);
            Assert.AreEqual("Turnips", kitchen.Sacks[2].Contents);
            Assert.AreEqual("Meat", kitchen.Sacks[2].Label);
            Assert.AreEqual("", kitchen.Sacks[3].Label);
            Assert.AreEqual("Meat", kitchen.Sacks[3].Contents);
            Assert.AreEqual("Bread", kitchen.Sacks[4].Contents);
            Assert.AreEqual(3, kitchen.Hound.Hunger);
            Assert.AreEqual("Hangry", kitchen.Hound.Mood);
            Assert.IsFalse(kitchen.IsMissionComplete);
        }

        [Test]
        public void Moods_FollowTheHunger()
        {
            Run("imp.Give(sack01, hound);");
            Assert.AreEqual("Grumpy", kitchen.Hound.Mood);
            Run("imp.Give(sack02, hound);");
            Assert.AreEqual("Peckish", kitchen.Hound.Mood);
            Run("imp.Give(sack04, hound);");
            Assert.AreEqual("Asleep", kitchen.Hound.Mood);
        }

        [Test]
        public void SolutionA_StopsOnLine3_WithHoundRefused()
        {
            RunResult result = Run(SolutionA);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("HoundRefusedException", result.FirstError.ExceptionType);
            Assert.AreEqual(3, result.FirstError.Line);
            StringAssert.Contains("Turnips", result.FirstError.Message);
            Assert.AreEqual(1, kitchen.Hound.Hunger);
            Assert.IsFalse(kitchen.IsMissionComplete);
            Assert.IsTrue(kitchen.Imp.IsCoveredInFood);
            Assert.IsFalse(kitchen.Sacks[2].IsEaten);
        }

        [Test]
        public void SolutionB_WinsWithTheOptionalGoal()
        {
            RunResult result = Run(SolutionB);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, kitchen.Hound.Hunger);
            Assert.IsTrue(kitchen.IsMissionComplete);
            Assert.IsTrue(kitchen.OptionalGoalMet);
        }

        [Test]
        public void ResetAfterA_RestoresTheStartingState_ThenBWins()
        {
            Run(SolutionA);
            kitchen.Reset();

            Assert.AreEqual(3, kitchen.Hound.Hunger);
            Assert.IsFalse(kitchen.Imp.IsCoveredInFood);
            foreach (FoodSack sack in kitchen.Sacks)
                Assert.IsFalse(sack.IsEaten);
            Assert.AreEqual("Meat", kitchen.Sacks[0].Contents);
            Assert.AreEqual("", kitchen.Sacks[3].Label);

            Run(SolutionB);
            Assert.IsTrue(kitchen.IsMissionComplete);
            Assert.IsTrue(kitchen.OptionalGoalMet);
        }

        [Test]
        public void OptionalGoal_IsLost_WhenAnEarlierRunWasNotAfterTheReset()
        {
            Run("imp.Give(sack01, hound);");
            Run("imp.Give(sack02, hound);\nimp.Give(sack04, hound);");

            Assert.IsTrue(kitchen.IsMissionComplete);
            Assert.IsFalse(kitchen.OptionalGoalMet);
        }

        [Test]
        public void OptionalGoal_IsLost_WhenARefusalHappenedInTheSameRun()
        {
            Run("imp.Give(sack03, hound);");
            kitchen.Reset();
            Run("imp.Give(sack01, hound);\nimp.Give(sack02, hound);\nimp.Give(sack04, hound);");

            // The refusal was in a run before the reset, so it does not count
            Assert.IsTrue(kitchen.OptionalGoalMet);
        }

        [Test]
        public void GivingASackThatWasEaten_IsAnObjectDisposedError()
        {
            Run("imp.Give(sack01, hound);");
            RunResult result = Run("imp.Give(sack01, hound);");

            Assert.AreEqual("ObjectDisposedException", result.FirstError.ExceptionType);
            Assert.AreEqual(2, kitchen.Hound.Hunger);
        }

        [Test]
        public void SleepingHound_DoesNothing_AndIsNotAnError()
        {
            Run(SolutionB);
            RunResult result = Run("imp.Give(sack05, hound);");

            Assert.IsTrue(result.Success);
            Assert.AreEqual(GiveOutcome.HoundAsleep, kitchen.Imp.LastOutcome);
            Assert.IsFalse(kitchen.Sacks[4].IsEaten);
        }

        [Test]
        public void Give_IsRefusedInTheConsole()
        {
            RunResult result = Run("imp.Give(sack01, hound);", RunMode.Console);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(3, kitchen.Hound.Hunger);
        }

        [Test]
        public void Pet_IsHiddenUntilTheImpLearnsIt()
        {
            Assert.IsNull(types.FindMember(typeof(Imp), "Pet"));
            Assert.IsNotNull(types.FindMember(typeof(Imp), "Give"));

            types.Learn("Pet");

            MemberDescriptor pet = types.FindMember(typeof(Imp), "Pet");
            Assert.IsNotNull(pet);
            Assert.IsTrue(pet.IsWorldAction);
            Assert.IsTrue(Run("imp.Pet(hound);").Success);
        }

        [Test]
        public void SackMembers_AreOnlyLabelAndContents()
        {
            ObjectDescription description = types.Describe(kitchen.Sacks[0]);

            Assert.AreEqual(2, description.Properties.Count);
            Assert.AreEqual(0, description.Methods.Count);
        }
    }
}
