using Audacia.CodeAnalysis.Analyzers.Rules.DoNotUseNumberInIdentifierName;
using Audacia.CodeAnalysis.Analyzers.Settings;
using Audacia.CodeAnalysis.Analyzers.Test.Base;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Audacia.CodeAnalysis.Analyzers.Test.Rules;

[TestClass]
public class DoNotUseNumberInIdentifierNameAnalyzerTests : DiagnosticVerifier
{
    private readonly Mock<ISettingsReader> _mockSettingsReader = new();

    private DiagnosticResult BuildExpectedResult(int lineNumber, int column, string kind, string identifierName)
    {
        return new DiagnosticResult
        {
            Id = DoNotUseNumberInIdentifierNameAnalyzer.Id,
            Message = $"{kind} '{identifierName}' contains one or more digits in its name",
            Severity = DiagnosticSeverity.Warning,
            Locations =
                new[] {
                    new DiagnosticResultLocation("Test0.cs", lineNumber, column)
                }
        };
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new DoNotUseNumberInIdentifierNameAnalyzer(_mockSettingsReader.Object);
    }

    [TestMethod]
    public void No_Diagnostic_If_No_Numbers_Used()
    {
        var test = @"
    namespace ConsoleApplication
    {
        class TypeName
        {
            private void MethodName()
            {
                var noNumber = 1.0;
            }
        }
    }";

        VerifyNoDiagnostic(test);
    }

    [TestMethod]
    public void Diagnostic_If_Numbers_Used_For_Variable_Name()
    {
        var test = @"
    namespace ConsoleApplication
    {
        class TypeName
        {
            private void MethodName()
            {
                var variable1 = 1.0;
            }
        }
    }";

        var expected = BuildExpectedResult(8, 21, "Variable", "variable1");

        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void Diagnostic_If_Numbers_Used_For_Parameter_Name()
    {
        var test = @"
    namespace ConsoleApplication
    {
        class TypeName
        {
            private void MethodName(string parameter1)
            {
            }
        }
    }";

        var expected = BuildExpectedResult(6, 44, "Parameter", "parameter1");

        VerifyDiagnostic(test, expected);
    }

    [TestMethod]
    public void Diagnostic_If_Numbers_Used_For_Type_Name()
    {
        var test = @"
    namespace ConsoleApplication
    {
        class TypeName1
        {
            private void MethodName()
            {
            }
        }
    }";

        var expected = BuildExpectedResult(4, 15, "Class", "TypeName1");

        VerifyDiagnostic(test, expected);
    }
}