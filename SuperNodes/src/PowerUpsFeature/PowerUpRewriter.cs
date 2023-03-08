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

  public PowerUpRewriter() {
    TypeParameters = default!;
    // Only used by tests.
  }

  /// <summary>
  /// Creates an abstract PowerUpRewriter.
  /// </summary>
  /// <param name="typeParameters">Map of the PowerUp's type parameters to the
  /// actual type arguments.</param>
  public PowerUpRewriter(ImmutableDictionary<string, string> typeParameters) {
    TypeParameters = typeParameters;
  }
}

public class DefaultPowerUpRewriter : PowerUpRewriter {
  public DefaultPowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters
  ) : base(typeParameters: typeParameters) { }

  /// <summary>
  /// Rewrite any and all identifiers that match the type parameters we know
  /// about with their corresponding argument.
  /// <br />
  /// As long as power-ups don't have conflicting identifiers (e.g., a variable
  /// named "T" and a type parameter named "T"), this should be fine.
  /// </summary>
  /// <param name="node">Identifier name syntax node.</param>
  public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) {
    if (TypeParameters.TryGetValue(
      node.Identifier.ValueText, out var replacement
    )) {
      return SyntaxFactory
        .IdentifierName(SyntaxFactory.Identifier(replacement))
        .WithTriviaFrom(node);
    }
    return base.VisitIdentifierName(node);
  }
}
