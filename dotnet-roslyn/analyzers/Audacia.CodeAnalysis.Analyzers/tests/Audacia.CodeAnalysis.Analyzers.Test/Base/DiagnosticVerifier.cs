using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers.AttributeShadows.MethodLength;
using Audacia.CodeAnalysis.Analyzers.Test.Helpers.AttributeShadows.ParameterCount;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Audacia.CodeAnalysis.Analyzers.Test.Base
{
    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        private static readonly string AssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        private static readonly MetadataReference CorLib =
            MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));
        private static readonly MetadataReference SystemRef =
            MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.dll"));
        private static readonly MetadataReference SystemCore =
            MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));
        private static readonly MetadataReference SystemRuntime =
            MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.Runtime.dll"));
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference AspNetCoreMvcReference = MetadataReference.CreateFromFile(typeof(ProducesResponseTypeAttribute).Assembly.Location);
        private static readonly MetadataReference IActionResultReference = MetadataReference.CreateFromFile(typeof(IActionResult).Assembly.Location);
        private static readonly MetadataReference TypedResultsReference = MetadataReference.CreateFromFile(typeof(Results).Assembly.Location);
        private static readonly MetadataReference TaskReference = MetadataReference.CreateFromFile(typeof(Task).Assembly.Location);
        private static readonly MetadataReference ControllerBaseReference = MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location);
        private static readonly MetadataReference HttpGetReference = MetadataReference.CreateFromFile(typeof(HttpGetAttribute).Assembly.Location);
        private static readonly MetadataReference SystemConsoleReference = MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
        private static readonly MetadataReference StatusCodesReference = MetadataReference.CreateFromFile(typeof(StatusCodes).Assembly.Location);
        private static readonly MetadataReference LoggerReference = MetadataReference.CreateFromFile(typeof(ILogger<>).Assembly.Location);
        private static readonly MetadataReference LoggerFactoryReference = MetadataReference.CreateFromFile(typeof(LoggerFactory).Assembly.Location);
        private static readonly MetadataReference ControllerReference = MetadataReference.CreateFromFile(typeof(Controller).Assembly.Location);
        private static readonly MetadataReference CancellationTokenReference = MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location);
        private static readonly MetadataReference MaxMethodLengthReference = MetadataReference.CreateFromFile(typeof(MaxMethodLengthAttribute).Assembly.Location);
        private static readonly MetadataReference MaxParameterCountReference = MetadataReference.CreateFromFile(typeof(MaxParameterCountAttribute).Assembly.Location);
        private static readonly MetadataReference SuppressMessageReference = MetadataReference.CreateFromFile(typeof(SuppressMessageAttribute).Assembly.Location);
        
        internal static CompilationOptions CompilationOptions = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default, allowUnsafe: true);
        internal static CSharpParseOptions ParseOptions = CSharpParseOptions.Default;
        internal static string DefaultFilePathPrefix = "Test";
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string TestProjectName = "TestProject";

        #region To be implemented by Test classes
        /// <summary>
        /// Get the CSharp analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected virtual DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return null;
        }

        /// <summary>
        /// Get the Visual Basic analyzer being tested (C#) - to be implemented in non-abstract class
        /// </summary>
        protected virtual DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return null;
        }
        #endregion

        #region Verifier wrappers

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        protected void VerifyNoDiagnostic(string source)
        {
            VerifyDiagnostics(new[] { source }, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer());
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
        protected void VerifyDiagnostic(string source, params DiagnosticResult[] expected)
        {
            VerifyDiagnostics(new[] { source }, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), expected);
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        protected void VerifyDiagnostic(string[] sources, params DiagnosticResult[] expected)
        {
            VerifyDiagnostics(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), expected);
        }

        /// <summary>
        /// General method that gets a collection of actual diagnostics found in the source after the analyzer is run,
        /// then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="language">The language of the classes represented by the source strings</param>
        /// <param name="analyzer">The analyzer to be run on the source code</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        private void VerifyDiagnostics(string[] sources, string language, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expected)
        {
            var diagnostics = GetSortedDiagnostics(sources, language, analyzer);
            VerifyDiagnosticResults(diagnostics, analyzer, expected);
        }

        #endregion

        #region Actual comparisons and verifications
        /// <summary>
        /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
        private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
        {
            var diagnosticsList = actualResults.ToList();
            var diagnosticErrors = diagnosticsList.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (diagnosticErrors.Any())
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Compilation errors found in test code:");
                foreach (var error in diagnosticErrors)
                {
                    stringBuilder.AppendLine(error.ToString());
                }
                Assert.Fail(stringBuilder.ToString());
            }
            else
            {
                var expectedCount = expectedResults.Length;
                var actualCount = diagnosticsList.Count;
    
                if (expectedCount != actualCount)
                {
                    string diagnosticsOutput = diagnosticsList.Any() ? FormatDiagnostics(analyzer, diagnosticsList.ToArray()) : "    NONE.";
    
                    Assert.IsTrue(false,
                        string.Format("Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput));
                }
    
                for (int i = 0; i < expectedResults.Length; i++)
                {
                    var actual = diagnosticsList.ElementAt(i);
                    var expected = expectedResults[i];
    
                    if (expected.Line == -1 && expected.Column == -1)
                    {
                        if (actual.Location != Location.None)
                        {
                            Assert.IsTrue(false,
                                string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}",
                                FormatDiagnostics(analyzer, actual)));
                        }
                    }
                    else
                    {
                        VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());
                        var additionalLocations = actual.AdditionalLocations.ToArray();
    
                        if (additionalLocations.Length != expected.Locations.Length - 1)
                        {
                            Assert.IsTrue(false,
                                string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                    expected.Locations.Length - 1, additionalLocations.Length,
                                    FormatDiagnostics(analyzer, actual)));
                        }
    
                        for (int j = 0; j < additionalLocations.Length; ++j)
                        {
                            VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
                        }
                    }
    
                    if (actual.Id != expected.Id)
                    {
                        Assert.IsTrue(false,
                            string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)));
                    }
    
                    if (actual.Severity != expected.Severity)
                    {
                        Assert.IsTrue(false,
                            string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)));
                    }
    
                    if (actual.GetMessage() != expected.Message)
                    {
                        Assert.IsTrue(false,
                            string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)));
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
        /// </summary>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="diagnostic">The diagnostic that was found in the code</param>
        /// <param name="actual">The Location of the Diagnostic found in the code</param>
        /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.IsTrue(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                    expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.Line)
                {
                    Assert.IsTrue(false,
                        string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Line, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.Column)
                {
                    Assert.IsTrue(false,
                        string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                            expected.Column, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
                }
            }
        }
        #endregion

        #region Formatting Diagnostics
        /// <summary>
        /// Helper method to format a Diagnostic into an easily readable string
        /// </summary>
        /// <param name="analyzer">The analyzer that this verifier tests</param>
        /// <param name="diagnostics">The Diagnostics to be formatted</param>
        /// <returns>The Diagnostics formatted as a string</returns>
        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i].ToString());

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules)
                {
                    if (rule != null && rule.Id == diagnostics[i].Id)
                    {
                        var location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                        }
                        else
                        {
                            Assert.IsTrue(location.IsInSource,
                                $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                            string resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
                            var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id);
                        }

                        if (i != diagnostics.Length - 1)
                        {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }
            return builder.ToString();
        }
        #endregion

        #region  Get Diagnostics

        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source classes are in</param>
        /// <param name="analyzer">The analyzer to be run on the sources</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        private static Diagnostic[] GetSortedDiagnostics(string[] sources, string language, DiagnosticAnalyzer analyzer)
        {
            return GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(sources, language));
        }

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
        protected static Diagnostic[] GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilationWithAnalyzers = project.GetCompilationAsync().Result.WithAnalyzers(ImmutableArray.Create(analyzer));
                var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = document.GetSyntaxTreeAsync().Result;
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }

                var diagnosticErrors = compilationWithAnalyzers.GetAllDiagnosticsAsync().Result
                    .Where(d => d.Severity == DiagnosticSeverity.Error);
                diagnostics.AddRange(diagnosticErrors);
            }

            var results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        /// <summary>
        /// Sort diagnostics by location in source document
        /// </summary>
        /// <param name="diagnostics">The list of Diagnostics to be sorted</param>
        /// <returns>An IEnumerable containing the Diagnostics in order of Location</returns>
        private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        #endregion

        #region Set up compilation and documents
        /// <summary>
        /// Given an array of strings as sources and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Tuple containing the Documents produced from the sources and their TextSpans if relevant</returns>
        private static Document[] GetDocuments(string[] sources, string language)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new ArgumentException("Unsupported Language");
            }

            var project = CreateProject(sources, language);

            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new InvalidOperationException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        /// <summary>
        /// Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Document created from the source string</returns>
        protected static Document CreateDocument(string source, string language = LanguageNames.CSharp)
        {
            return CreateProject(new[] { source }, language).Documents.First();
        }

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Project created out of the Documents created from the source strings</returns>
        private static Project CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, language)
                .WithProjectCompilationOptions(projectId, CompilationOptions)
                .WithProjectParseOptions(projectId, ParseOptions)
                .AddMetadataReference(projectId, CorLib)
                .AddMetadataReference(projectId, SystemRef)
                .AddMetadataReference(projectId, SystemCore)
                .AddMetadataReference(projectId, SystemRuntime)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference)
                .AddMetadataReference(projectId, AspNetCoreMvcReference)
                .AddMetadataReference(projectId, IActionResultReference)
                .AddMetadataReference(projectId, TypedResultsReference)
                .AddMetadataReference(projectId, TaskReference)
                .AddMetadataReference(projectId, ControllerBaseReference)
                .AddMetadataReference(projectId, HttpGetReference)
                .AddMetadataReference(projectId, SystemConsoleReference)
                .AddMetadataReference(projectId, StatusCodesReference)
                .AddMetadataReference(projectId, LoggerReference)
                .AddMetadataReference(projectId, LoggerFactoryReference)
                .AddMetadataReference(projectId, ControllerReference)
                .AddMetadataReference(projectId, CancellationTokenReference)
                .AddMetadataReference(projectId, MaxMethodLengthReference)
                .AddMetadataReference(projectId, MaxParameterCountReference)
                .AddMetadataReference(projectId, SuppressMessageReference);

            int count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }
            return solution.GetProject(projectId);
        }
        #endregion
    }
}
