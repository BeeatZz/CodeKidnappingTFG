using System.Collections.Generic;
using NUnit.Framework;

namespace CodeRunner.Tests
{
    public class CheckerTests
    {
        private TestWorld world;

        [SetUp]
        public void SetUp()
        {
            world = new TestWorld();
        }

        private CodeError FirstError(string source, RunMode mode = RunMode.WorkOrder)
        {
            List<CodeError> errors = world.Check(source, mode);
            Assert.IsNotEmpty(errors, "Expected an error for: " + source);
            return errors[0];
        }

        [Test]
        public void UnknownMember_SuggestsTheClosestName()
        {
            string source =
                "foreach (var thing in depot.ScanAll())\n" +
                "{\n" +
                "    if (thing is ICargo c)\n" +
                "    {\n" +
                "        int w = c.Wieght;\n" +
                "    }\n" +
                "}";

            CodeError error = FirstError(source);

            Assert.AreEqual("CS1061", error.Code);
            Assert.AreEqual(5, error.Line);
            Assert.AreEqual("'ICargo' does not contain a definition for 'Wieght'", error.Message);
            Assert.AreEqual("Did you mean 'Weight'?", error.Hint);
        }

        [Test]
        public void UnknownMember_OnAClass_SuggestsTheClosestName()
        {
            CodeError error = FirstError("foreach (var wagon in depot.Wagons) { int c = wagon.Capcity; }");

            Assert.AreEqual("Did you mean 'Capacity'?", error.Hint);
        }

        [Test]
        public void UnknownMember_WithNothingClose_HasNoHint()
        {
            CodeError error = FirstError("var x = depot.Banana;");

            Assert.IsNull(error.Hint);
        }

        [Test]
        public void UnknownVariable_SuggestsTheClosestName()
        {
            CodeError error = FirstError("int stillNeeded = 1;\nint y = stillNeded;");

            Assert.AreEqual("CS0103", error.Code);
            Assert.AreEqual(2, error.Line);
            Assert.AreEqual("Did you mean 'stillNeeded'?", error.Hint);
        }

        [Test]
        public void UnscannedHandle_IsAnError()
        {
            CodeError error = FirstError("imp.MoveTo(crate07);");

            Assert.AreEqual("CS0103", error.Code);
            StringAssert.Contains("'crate07' does not exist", error.Message);
            StringAssert.Contains("scanned", error.Hint);
        }

        [Test]
        public void WrongArgumentCount_IsAnError()
        {
            CodeError error = FirstError("imp.Load(depot);");

            Assert.AreEqual("CS1501", error.Code);
            StringAssert.Contains("takes 1 arguments", error.Message);
            StringAssert.Contains("void Load(ICargo cargo, Wagon wagon)", error.Hint);
        }

        [Test]
        public void WrongArgumentType_IsAnError()
        {
            CodeError error = FirstError("imp.Say(5);");

            Assert.AreEqual("CS1503", error.Code);
            Assert.AreEqual("Argument 1: cannot convert from 'int' to 'string'", error.Message);
        }

        [Test]
        public void TypeMismatchInDeclaration_IsAnError()
        {
            Assert.AreEqual("Cannot implicitly convert type 'string' to 'int'", FirstError("int x = \"hello\";").Message);
            Assert.AreEqual("Cannot implicitly convert type 'int' to 'string'", FirstError("string s = 5;").Message);
        }

        [Test]
        public void TypeMismatchInAssignment_IsAnError()
        {
            CodeError error = FirstError("int x = 1;\nx = true;");

            Assert.AreEqual("CS0029", error.Code);
            Assert.AreEqual(2, error.Line);
        }

        [Test]
        public void ConditionThatIsNotBool_IsAnError()
        {
            CodeError error = FirstError("if (5) { }");

            Assert.AreEqual("Cannot implicitly convert type 'int' to 'bool'", error.Message);
        }

        [Test]
        public void BadOperatorOperands_AreAnError()
        {
            CodeError error = FirstError("var x = 1 - \"a\";");

            Assert.AreEqual("CS0019", error.Code);
            Assert.AreEqual("Operator '-' cannot be applied to operands of type 'int' and 'string'", error.Message);
        }

        [Test]
        public void DuplicateVariable_IsAnError()
        {
            CodeError error = FirstError("int a = 1;\nint a = 2;");

            Assert.AreEqual("CS0128", error.Code);
            Assert.AreEqual(2, error.Line);
        }

        [Test]
        public void BreakOutsideLoop_IsAnError()
        {
            Assert.AreEqual("CS0139", FirstError("break;").Code);
        }

        [Test]
        public void MethodWithoutParentheses_IsAnError()
        {
            CodeError error = FirstError("var x = depot.ScanAll;");

            Assert.AreEqual("CS0428", error.Code);
        }

        [Test]
        public void PropertyUsedAsMethod_IsAnError()
        {
            Assert.AreEqual("CS1955", FirstError("var x = depot.Ledger();").Code);
        }

        [Test]
        public void ForeachOverAnInt_IsAnError()
        {
            CodeError error = FirstError("int n = 3;\nforeach (var x in n) { }");

            Assert.AreEqual("CS1579", error.Code);
            Assert.AreEqual(2, error.Line);
        }

        [Test]
        public void AssigningToAScannedObject_IsAnError()
        {
            Assert.AreEqual("CS0131", FirstError("depot = null;").Code);
        }

        [Test]
        public void VoidResult_CannotBeStored()
        {
            Assert.AreEqual("CS1547", FirstError("var x = imp.Say(\"hi\");").Code);
        }

        [Test]
        public void NotSupportedMethodCall_IsAnError()
        {
            CodeError error = FirstError("DoSomething();");

            Assert.AreEqual("CS0103", error.Code);
        }

        [Test]
        public void ConsoleMode_RefusesWorldActions()
        {
            CodeError error = FirstError("imp.Say(\"hi\")", RunMode.Console);

            StringAssert.Contains("changes the world", error.Message);
            StringAssert.Contains("console", error.Message);
        }

        [Test]
        public void ConsoleMode_AllowsReadingTheWorld()
        {
            Assert.IsEmpty(world.Check("depot.Wagons.Count", RunMode.Console));
        }

        [Test]
        public void SeveralErrors_AreAllReported()
        {
            List<CodeError> errors = world.Check("int a = \"x\";\nint b = true;\nvar c = nothing;");

            Assert.AreEqual(3, errors.Count);
            Assert.AreEqual(3, errors[2].Line);
        }

        [Test]
        public void IsNotPattern_KeepsTheVariableForTheCodeAfterIt()
        {
            string source =
                "foreach (var thing in depot.ScanAll())\n" +
                "{\n" +
                "    if (thing is not ICargo cargo) continue;\n" +
                "    if (cargo is IPerishable food && food.DaysUntilSpoiled < 2) continue;\n" +
                "    int w = cargo.Weight;\n" +
                "}";

            Assert.IsEmpty(world.Check(source));
        }

        [Test]
        public void UnknownTypeInPattern_SuggestsAType()
        {
            CodeError error = FirstError("var thing = depot.Ledger;\nif (thing is ICargoo c) { }");

            Assert.AreEqual("CS0246", error.Code);
            Assert.AreEqual("Did you mean 'ICargo'?", error.Hint);
        }
    }
}
