namespace SuperNodes;

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

  public PowerUpRewriter(
    ImmutableDictionary<string, string> typeParameters
  ) {
    TypeParameters = typeParameters;
  }

  //! TODO: Also replace references to the static name of the power up class
  //! with the name of the script class it is being applied to.

  public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) {
    // Rewrite any and all identifiers that match the type parameters we know
    // about with their corresponding argument.
    //
    // As long as power-ups don't have conflicting identifiers (e.g., a variable
    // named "T" and a type parameter named "T"), this should be fine.
    if (TypeParameters.TryGetValue(node.Identifier.Text, out var replacement)) {
      // return SyntaxFactory.ParseExpression(replacement);
      return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(replacement)).WithTriviaFrom(node);
    }

    return base.VisitIdentifierName(node);
  }
}
