using System.Collections.Generic;
using CodeRunner.Bridge;
using CodeRunner.Checking;
using Mission;
using NUnit.Framework;

namespace CodeRunner.Tests
{
    public class BridgeTests
    {
        private TestWorld world;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
            world.ScanEverything();
        }

        [Test]
        public void Describe_ShowsTypeInterfacesAndProperties()
        {
            // crate03 is a good crate of grain, so it is cargo and perishable
            object crate = world.Level.Depot.ScanAllList()[3];
            ObjectDescription description = world.Types.Describe(crate);

            Assert.AreEqual("PerishableCargo", description.TypeName);
            CollectionAssert.AreEquivalent(new[] { "ICargo", "IPerishable" }, description.Interfaces);

            PropertyDescription days = description.Properties.Find(p => p.Name == "DaysUntilSpoiled");
            Assert.AreEqual("int", days.TypeName);
            Assert.AreEqual("9", days.Value);

            PropertyDescription contents = description.Properties.Find(p => p.Name == "Contents");
            Assert.AreEqual("\"Grain\"", contents.Value);
        }

        [Test]
        public void Describe_ShowsMethodsWithSignaturesAndWhetherTheyChangeTheWorld()
        {
            ObjectDescription description = world.Types.Describe(world.Level.Imp);

            MethodDescription load = description.Methods.Find(m => m.Signature == "void Load(ICargo cargo, Wagon wagon)");
            Assert.IsNotNull(load);
            Assert.IsTrue(load.ChangesWorld);
            Assert.AreEqual("Picks up the cargo and puts it on the wagon", load.Description);

            ObjectDescription depot = world.Types.Describe(world.Level.Depot);
            MethodDescription scan = depot.Methods.Find(m => m.Signature == "IEnumerable<IScannable> ScanAll()");
            Assert.IsNotNull(scan);
            Assert.IsFalse(scan.ChangesWorld);
        }

        [Test]
        public void Describe_ShowsGenericPropertyTypesAndValues()
        {
            ObjectDescription description = world.Types.Describe(world.Level.Depot.Wagons[0]);

            PropertyDescription cargo = description.Properties.Find(p => p.Name == "Cargo");
            Assert.AreEqual("IReadOnlyList<ICargo>", cargo.TypeName);
            Assert.AreEqual("ReadOnlyCollection<ICargo> (0 items)", cargo.Value);
        }

        [Test]
        public void Members_NotMarkedWithAttributes_AreHidden()
        {
            Assert.IsNull(world.Types.FindMember(typeof(Depot), "AddCargo"));
            Assert.IsNull(world.Types.FindMember(typeof(Wagon), "AddCargo"));
            Assert.IsNull(world.Types.FindMember(typeof(Imp), "Location"));
        }

        [Test]
        public void CollectionsAndPairs_GetBuiltInMembers()
        {
            Assert.IsNotNull(world.Types.FindMember(typeof(IReadOnlyList<ICargo>), "Count"));
            Assert.IsNotNull(world.Types.FindMember(typeof(IReadOnlyDictionary<string, int>), "Count"));
            Assert.IsNull(world.Types.FindMember(typeof(IEnumerable<ICargo>), "Count"));
            Assert.IsNotNull(world.Types.FindMember(typeof(KeyValuePair<string, int>), "Value"));
        }

        [Test]
        public void TypeNames_AreWrittenLikeCSharp()
        {
            Assert.AreEqual("int", TypeNames.Of(typeof(int)));
            Assert.AreEqual("IReadOnlyDictionary<string, int>", TypeNames.Of(typeof(IReadOnlyDictionary<string, int>)));
            Assert.AreEqual("KeyValuePair<string, int>", TypeNames.Of(typeof(KeyValuePair<string, int>)));
        }

        [Test]
        public void ScopeRegistry_RejectsBadNames_AndProtectsTheImp()
        {
            ScopeRegistry registry = new ScopeRegistry(world.Level.Imp);

            Assert.Throws<System.ArgumentException>(() => registry.Add("not valid", world.Level.Depot));
            Assert.Throws<System.ArgumentException>(() => registry.Add("imp", world.Level.Depot));
            Assert.IsTrue(registry.Contains("imp"));
        }

        [Test]
        public void EditDistance_FindsTyposAndIgnoresFarNames()
        {
            Assert.AreEqual("Did you mean 'Weight'?", EditDistance.Suggest("Wieght", new[] { "Label", "Weight" }));
            Assert.AreEqual("Did you mean 'Weight'?", EditDistance.Suggest("weight", new[] { "Weight" }));
            Assert.IsNull(EditDistance.Suggest("Banana", new[] { "Label", "Weight" }));
            Assert.AreEqual(1, EditDistance.Distance("ab", "ba"));
        }
    }

    // Test-only helper: the depot's scan list as a List so tests can pick items by position
    internal static class DepotTestExtensions
    {
        public static List<IScannable> ScanAllList(this Depot depot)
        {
            return new List<IScannable>(depot.ScanAll());
        }
    }
}
