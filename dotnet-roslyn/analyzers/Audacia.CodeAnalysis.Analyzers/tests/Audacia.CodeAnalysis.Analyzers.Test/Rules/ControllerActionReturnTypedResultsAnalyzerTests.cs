﻿using Audacia.CodeAnalysis.Analyzers.Rules.ControllerActionReturnTypedResults;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules
{
    [TestClass]
    public class ControllerActionReturnTypedResultsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
          => new ControllerActionReturnTypedResultsAnalyzer();

        private static DiagnosticResult BuildExpectedResult(string message, int lineNumber, int column)
        {
            var diagnosticResult = new DiagnosticResult
            {
                Id = ControllerActionReturnTypedResultsAnalyzer.Id,
                Severity = ControllerActionReturnTypedResultsAnalyzer.Severity,
                Message = message,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                    }
            };

            return diagnosticResult;
        }

        [TestMethod]
        public void No_Diagnostics_For_Empty_Code()
        {
            const string testCode = @"";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Non_Controller_Method()
        {
            const string testCode = @"
                namespace ConsoleApplication1
                {
                    class TestClass
                    {
                        void TestMethod()
                        {
                            Console.WriteLine(1234567890);
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_TypedResult_ReturnType_Not_Using_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        public Results<NotFound, Ok<string>> Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_IActionResults_ReturnType_Using_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        public IActionResult Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void No_Diagnostics_For_Controller_With_Async_Method_TypedResult_ReturnType_Not_Using_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        public async Task<Results<NotFound, Ok<string>>> Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            VerifyNoDiagnostic(testCode);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_With_TypedResults_ReturnType_And_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        public Results<NotFound, Ok<string>> Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            const string expectedMessage
                = "Controller action name 'Get' [ProducesResponseType] attribute should not be applied when using TypedResults";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 9, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Diagnostics_For_Controller_With_Async_Method_TypedResults_ReturnType_And_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        public async Task<Results<NotFound, Ok<string>>> Get()
                        {
                            return 'hello';
                        }
                    }
                }";

            const string expectedMessage
                = "Controller action name 'Get' [ProducesResponseType] attribute should not be applied when using TypedResults";

            var expectedDiagnostic = BuildExpectedResult(expectedMessage, 9, 25);

            VerifyDiagnostic(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public void Multiple_Diagnostics_For_Controllers_With_Multiple_Methods_With_TypedResults_ReturnType_And_ProducesResponseType_Attribute()
        {
            const string testCode = @"
                using System.Threading.Tasks;
                using Microsoft.AspNetCore.Mvc;

                namespace ConsoleApplication1
                {
                    public class TestController : ControllerBase
                    {
                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        public async Task<Results<NotFound, Ok<string>>> Get()
                        {
                            return 'hello';
                        }

                        [HttpGet]
                        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
                        public async Task<Results<NotFound, Ok<string>>> Get()
                        {
                            return 'hello';
                        }
                    }
                }";
            const string expectedMessage1
                = "Controller action name 'Get' [ProducesResponseType] attribute should not be applied when using TypedResults";

            const string expectedMessage2
                = "Controller action name 'Get' [ProducesResponseType] attribute should not be applied when using TypedResults";

            var expectedDiagnostics
                = new[]
                {
                    BuildExpectedResult(expectedMessage1, 9, 25),
                    BuildExpectedResult(expectedMessage2, 16, 25)
                };

            VerifyDiagnostic(testCode, expectedDiagnostics);
        }
    }
}
