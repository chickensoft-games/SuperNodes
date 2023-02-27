namespace SuperNodes.PowerUpsFeature;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public abstract class PowerUpRewriter : CSharpSyntaxRewriter {
  /// <summary>
  /// Map of the PowerUp's type parameters to the actual type arguments.
  /// The type parameter identifiers in the PowerUp's code will be replaced with
  /// the type arguments.
  /// </summary>
  public ImmutableDictionary<string, string> TypeParameters { get; }

  /// <summary>Name of the PowerUp class that's being rewritten.</summary>
  public string PowerUpClassName { get; }

  /// <summary>
  /// Name of the SuperNode class that is applying the PowerUp.
  /// </summary>
  public string SuperNodeClassName { get; }

  public PowerUpRewriter() {
    TypeParameters = default!;
    PowerUpClassName = default!;
    SuperNodeClassName = default!;
    // Only used by tests.
  }

  /// <summary>
  /// Creates an abstract PowerUpRewriter.
  /// </summary>
  /// <param name="typeParameters">Map of the PowerUp's type parameters to the
  /// actual type arguments.</param>
  /// <param name="powerUpClassName">PowerUp class name.</param>
  /// <param name="superNodeClassName">SuperNode class name.</param>
  public PowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters,
    string powerUpClassName,
    string superNodeClassName
  ) {
    TypeParameters = typeParameters;
    PowerUpClassName = powerUpClassName;
    SuperNodeClassName = superNodeClassName;
  }
}

public class DefaultPowerUpRewriter : PowerUpRewriter {
  public DefaultPowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters,
    string powerUpClassName,
    string superNodeClassName
  ) : base(
    typeParameters: typeParameters,
    powerUpClassName: powerUpClassName,
    superNodeClassName: superNodeClassName
  ) { }

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
