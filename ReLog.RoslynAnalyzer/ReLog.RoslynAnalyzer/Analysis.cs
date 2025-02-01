using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using UnityEngine;

namespace ReLog.RoslynAnalyzer;

public static class Analysis
{
    public const string IGNORED_DIRECTORY_NAME = "IgnoreReLog";
    
    public static readonly DiagnosticAnalyzer DefaultAnalyzer = new DebugAnalyzer();
    public static readonly CodeFixProvider DefaultProvider = new DebugFixProvider();

    private static readonly string[] IgnoredDirectories = {"ReLog", IGNORED_DIRECTORY_NAME};
    
    // TODO: Fix Assembly Resolution
    /// <summary>
    /// Checks if the given file is in a directory or subdirectory with one of the specified names.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <param name="ignoredDirectories">The names of directories to ignore (e.g., "ReLog", "IgnoreReLog").</param>
    /// <returns>True if the file is in an ignored directory, otherwise false.</returns>
    public static bool IsInIgnoredDirectory(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        while (!string.IsNullOrEmpty(directory))
        {
            string? dirName = Path.GetFileName(directory);
            if (IgnoredDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                return true;
            directory = Path.GetDirectoryName(directory);
        }
        return false;
    }
    
    /// <summary>
    /// Finds all files and lines where Debug.Log (and variants) are used.
    /// </summary>
    /// <param name="directoryPath">The directory to analyze.</param>
    /// <param name="analyzer">The Roslyn analyzer instance.</param>
    /// <param name="extraCompilations">A list of additional assemblies' file paths.</param>
    /// <returns>A list of affected files and lines of code.</returns>
    public static List<AffectedFile> GetAffectedFilesAndLines(string directoryPath, DiagnosticAnalyzer analyzer, params string[] extraCompilations)
    {
        List<AffectedFile> affectedFiles = new List<AffectedFile>();
        string[] csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
        foreach (string file in csFiles)
        {
            if(IsInIgnoredDirectory(file)) continue;
            string code = File.ReadAllText(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilation compilation = CSharpCompilation.Create("Analysis");
            foreach (string assemblyPath in extraCompilations)
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(assemblyPath));
            compilation = compilation.AddSyntaxTrees(tree);
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            diagnostics.AddRange(compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result);
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Location.IsInSource)
                {
                    FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
                    int startLine = lineSpan.StartLinePosition.Line;
                    string? lineText = File.ReadAllLines(file)[startLine];
                    affectedFiles.Add(new (file, startLine + 1, lineText));
                }
            }
        }
        return affectedFiles;
    }

    /// <summary>
    /// Applies Roslyn fixes to all files in a directory.
    /// </summary>
    /// <param name="directoryPath">The directory to analyze and fix.</param>
    /// <param name="analyzer">The Roslyn analyzer instance.</param>
    /// <param name="codeFixProvider">The Roslyn code fix provider instance.</param>
    /// <param name="extraCompilations">A list of additional assemblies' file paths.</param>
    /// <returns>A task representing the completion of the fixes.</returns>
    public static async Task ApplyRoslynFixesAsync(string directoryPath, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, params string[] extraCompilations)
    {
        string[] csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
        foreach (string file in csFiles)
        {
            if (IsInIgnoredDirectory(file)) continue;
            string code = File.ReadAllText(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilation compilation = CSharpCompilation.Create("Fixer");
            foreach (string assemblyPath in extraCompilations)
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(assemblyPath));
            compilation = compilation.AddSyntaxTrees(tree);
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            diagnostics.AddRange(compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result);
            if (diagnostics.Count == 0) continue;
            AdhocWorkspace workspace = new AdhocWorkspace();
            Project project = workspace
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                .AddProject("FixProject", "FixProject", LanguageNames.CSharp);
            foreach (string assemblyPath in extraCompilations)
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(assemblyPath));
            Document document = project.AddDocument("FixDocument", code);
            foreach (Diagnostic diagnostic in diagnostics)
            {
                List<CodeAction> actions = new List<CodeAction>();
                CodeFixContext context = new CodeFixContext(document, diagnostic, (a, _) => actions.Add(a), default);
                await codeFixProvider.RegisterCodeFixesAsync(context);
                foreach (CodeAction action in actions)
                {
                    ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(default);
                    foreach (CodeActionOperation operation in operations)
                        operation.Apply(workspace, default);
                }
            }
            Document? updatedDocument = workspace.CurrentSolution.GetDocument(document.Id);
            if (updatedDocument == null)
            {
                Debug.LogError("Updated document is null.");
                continue;
            }
            SourceText fixedCode = await updatedDocument.GetTextAsync();
            File.WriteAllText(file, fixedCode.ToString());
        }
    }
}