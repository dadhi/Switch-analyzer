using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace SwitchAnalyzer.Test
{
    [TestClass]
    public class NestedInterfaceStructSwitchAnalyzerTests : CodeFixVerifier
    {
        private readonly string codeStart = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        public class OuterClass 
        {
            public interface union {}

            public struct NestedStructA : union {}
            public struct NestedStructB : union {}
        }

        public class Program 
        {
            public static void Main() 
            {
            ";

        private readonly string codeEnd = @"
            }
        }
    }
    ";

        [TestMethod]
        public void SimpleValid()
        {
            var switchStatement = @"
                var x = 0;
                OuterClass.union test = new OuterClass.NestedStructA();
                switch (test)
                {
                    case OuterClass.NestedStructA a: x = 1; break;
                    case OuterClass.NestedStructB b: x = 2; break;
                    default: throw new NotImplementedException();
                }
            ";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void SimpleInvalid()
        {
            var switchStatement = @"
                var x = 0;
                OuterClass.union test = new OuterClass.NestedStructA();
                switch (test)
                {
                    case OuterClass.NestedStructA a: x = 1; break;
                    default: throw new NotImplementedException();
                }
            ";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test, GetDiagnostic("ConsoleApplication1.OuterClass.NestedStructB"));
        }

        [TestMethod]
        [Ignore("review me")]
        public void CheckWithThrowInBlock()
        {
            var switchStatement = @"
            union test = new TestClass();
            switch (test)
            {
                case TestClass a: return TestEnum.Case1;
                case IChildInterface i: return TestEnum.Case2;
                default: default:{
                        var s = GetEnum(testValue);
                        throw new NotImplementedException();
                        }
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test, GetDiagnostic("OneMoreInheritor"));
        }

        [TestMethod]
        [Ignore("review me")]
        public void NoChecksWithoutThrowInDefault()
        {
            var switchStatement = @"
            union test = new TestClass();
            switch (test)
            {
                case TestClass a: return TestEnum.Case1;
                default: default:{
                        var s = GetEnum(testValue);
                        break;
                        }
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        [Ignore("review me")]
        public void MultipleValuesReturnedInDiagnostic()
        {
            var switchStatement = @"
            union test = new TestClass();
            switch (test)
            {
                default: default:{
                        var s = GetEnum(testValue);
                        throw new NotImplementedException();
                        }
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test, GetDiagnostic("IChildInterface", "OneMoreInheritor", "TestClass"));
        }

        [TestMethod]
        [Ignore("review me")]
        public void ArgumentAsTypeConversionValid()
        {
            var switchStatement = @"
            union test = new TestClass();
            switch (new TestClass() as union)
            {
                case TestClass a: return TestEnum.Case1;
                case IChildInterface i: return TestEnum.Case2;
                default: throw new NotImplementedException();
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test, GetDiagnostic("OneMoreInheritor"));
        }

        [TestMethod][Ignore("review me")]
        public void EmptyExpressionValid()
        {
            var switchStatement = @"
            union test = new TestClass();
            switch (test)
            {
                case TestClass a:
                case OneMoreInheritor a: return TestEnum.Case2;
                case IChildInterface i:
                default: throw new NotImplementedException();
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        [Ignore("review me")]
        public void DontCheckFromOtherNamespace()
        {
            var switchStatement = @"
            OtherNamespace.union test = new OtherNamespace.TestClass();
            switch (test)
            {
                case OtherNamespace.OneMoreInheritor o: return TestEnum.Case1;
                default: throw new NotImplementedException();
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            // No check for items from other namespaces not referenced in current place.
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        [Ignore("review me")]
        public void FixSimple()
        {
            var switchStatement = @"
            union test = new TestClass();
            switch (test)
            {
                case TestClass a: return TestEnum.Case2;
                case IChildInterface i: return TestEnum.Case1;
                default: throw new NotImplementedException();
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test, GetDiagnostic("OneMoreInheritor"));


            var expectedFixSwitch = @"
            union test = new TestClass();
            switch (test)
            {
                case TestClass a: return TestEnum.Case2;
                case IChildInterface i: return TestEnum.Case1;
                case OneMoreInheritor _:
                default: throw new NotImplementedException();
            }";
            var expectedResult = $@"{codeStart}
                          {expectedFixSwitch}
                          {codeEnd}";

            VerifyCSharpFix(test, expectedResult);
        }

        [TestMethod]
        [Ignore("review me")]
        public void FixInterface()
        {
            var switchStatement = @"
            union test = new TestClass();
            switch (test)
            {
                case TestClass a: return TestEnum.Case2;
                case OneMoreInheritor o: return TestEnum.Case1;
                default: throw new NotImplementedException();
            }";
            var test = $@"{codeStart}
                          {switchStatement}
                          {codeEnd}";

            VerifyCSharpDiagnostic(test, GetDiagnostic("IChildInterface"));

            var expectedFixSwitch = @"
            union test = new TestClass();
            switch (test)
            {
                case TestClass a: return TestEnum.Case2;
                case OneMoreInheritor o: return TestEnum.Case1;
                case IChildInterface _:
                default: throw new NotImplementedException();
            }";
            var expectedResult = $@"{codeStart}
                          {expectedFixSwitch}
                          {codeEnd}";

            VerifyCSharpFix(test, expectedResult);
        }

        private DiagnosticResult GetDiagnostic(params string[] expectedTypes)
        {
            return new DiagnosticResult
            {
                Id = "SA002",
                Message = string.Format("Switch case should check interface implementation of type(s): {0}", string.Join(", ", expectedTypes)),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 27, 17)
                    }
            };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SwitchAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new SwitchAnalyzerCodeFixProvider();
        }
    }
}
