using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using UnityEngine;

namespace ReLog.RoslynAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DebugFixProvider))]
public class DebugFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("LOG001");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
        ClassDeclarationSyntax? classDeclaration =
            root?.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
        if (classDeclaration == null)
        {
            Debug.LogError("Null class declaration!");
            return;
        }
        context.RegisterCodeFix(
            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(title: "Transform Debug.Log to Logger.Log",
                createChangedDocument: c => TransformToLoggerAsync(context.Document, classDeclaration, c),
                equivalenceKey: "TransformToLogger"), diagnostic);
    }

    private async Task<Document> TransformToLoggerAsync(Document document, ClassDeclarationSyntax originalClassDeclaration, CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
        if (!(root is CompilationUnitSyntax compilationUnit))
        {
            Debug.LogError("Root is not a CompilationUnitSyntax.");
            return document;
        }
        ClassDeclarationSyntax? classDeclaration = compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(cd => cd.Identifier.Text == originalClassDeclaration.Identifier.Text);
        if (classDeclaration == null)
        {
            Debug.LogError("Class declaration could not be found in the syntax tree.");
            return document;
        }
        FieldDeclarationSyntax loggerField = SyntaxFactory
            .FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("CoreLogger"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator("Logger"))))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        CompilationUnitSyntax updatedRoot = compilationUnit.ReplaceNodes(new[] { classDeclaration }, (_, currentNode) =>
        {
            ClassDeclarationSyntax updatedClass = currentNode;
            if (updatedClass != null)
            {
                updatedClass = updatedClass.Members.Any()
                    ? updatedClass.WithMembers(updatedClass.Members.Insert(0, loggerField))
                    : updatedClass.AddMembers(loggerField);
                DebugToLoggerRewriter rewriter = new DebugToLoggerRewriter();
                updatedClass = (ClassDeclarationSyntax)rewriter.Visit(updatedClass);
                return updatedClass;
            }
            return currentNode;
        });
        if (!compilationUnit.Usings.Any(u => u.Name.ToString() == "ReLog"))
        {
            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("ReLog"));
            updatedRoot = updatedRoot.WithUsings(compilationUnit.Usings.Add(usingDirective));
        }
        return document.WithSyntaxRoot(updatedRoot);
    }
}

public class DebugToLoggerRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess &&
            (memberAccess.Expression.ToString() is "Debug" or "UnityEngine.Debug") &&
            (memberAccess.Name.ToString() == "Log" || memberAccess.Name.ToString() == "LogWarning" || memberAccess.Name.ToString() == "LogError"))
        {
            return node.WithExpression(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Logger"),
                memberAccess.Name));
        }
        return base.VisitInvocationExpression(node);
    }
}