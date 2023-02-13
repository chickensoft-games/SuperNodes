namespace SuperNodes.PowerUpsFeature;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class PowerUpRewriter : CSharpSyntaxRewriter {
  /// <summary>
  /// Keys represent the name of the type parameters. Values represent the
  /// argument given to the type parameter.
  /// <br />
  /// i.e., keys are replaced with values in the code.
  /// </summary>
  public ImmutableDictionary<string, string> TypeParameters { get; }

  /// <summary>
  /// Name of the PowerUp class that's being rewritten.
  /// </summary>
  public string PowerUpClassName { get; }

  /// <summary>
  /// Name of the SuperNode class that is applying the PowerUp.
  /// </summary>
  public string SuperNodeClassName { get; }

  public PowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters,
    string powerUpClassName,
    string superNodeClassName
  ) {
    TypeParameters = typeParameters;
    PowerUpClassName = powerUpClassName;
    SuperNodeClassName = superNodeClassName;
  }

  public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
    => node.Identifier.ValueText == PowerUpClassName
      ? SyntaxFactory
        .IdentifierName(SyntaxFactory.Identifier(SuperNodeClassName))
        .WithTriviaFrom(node)
      : base.VisitGenericName(node);

  /// <summary>
  /// Rewrite any and all identifiers that match the type parameters we know
  /// about with their corresponding argument.
  /// <br />
  /// As long as power-ups don't have conflicting identifiers (e.g., a variable
  /// named "T" and a type parameter named "T"), this should be fine.
  /// </summary>
  /// <param name="node">Identifier name syntax node.</param>
  public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
    TypeParameters.TryGetValue(node.Identifier.ValueText, out var replacement)
      ? SyntaxFactory
        .IdentifierName(SyntaxFactory.Identifier(replacement))
        .WithTriviaFrom(node)
      : node.Identifier.ValueText == PowerUpClassName
      ? SyntaxFactory
        .IdentifierName(SyntaxFactory.Identifier(SuperNodeClassName))
        .WithTriviaFrom(node)
      : base.VisitIdentifierName(node);
}
