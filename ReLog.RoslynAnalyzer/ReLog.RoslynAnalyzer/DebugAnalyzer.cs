using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReLog.RoslynAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DebugAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new("LOG001", "Replace Debug.Log with ReLog",
        "Debug.Log should be replaced with ReLog", "Logging", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeInvocation,
        Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
    
    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Parent is ArrowExpressionClauseSyntax)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                IMethodSymbol? symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
                if (symbol?.ContainingType?.ToString() == "UnityEngine.Debug")
                    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
            }
            return;
        }
        if (invocation.Expression is MemberAccessExpressionSyntax normalMemberAccess)
        {
            IMethodSymbol? symbol = context.SemanticModel.GetSymbolInfo(normalMemberAccess).Symbol as IMethodSymbol;
            if (symbol?.ContainingType?.ToString() == "UnityEngine.Debug")
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
        }
    }
}