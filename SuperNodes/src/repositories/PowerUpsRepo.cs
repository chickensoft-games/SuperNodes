namespace SuperNodes.Repositories;

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public interface IPowerUpsRepo {
  /// <summary>
  /// Determines if the given syntax node is a power up syntax candidate.
  /// </summary>
  /// <param name="node">Syntax node to check.</param>
  /// <param name="_">Cancellation token (unused).</param>
  /// <returns>True if the syntax node is a class declaration with the
  /// PowerUp attribute.</returns>
  bool IsPowerUpSyntaxCandidate(SyntaxNode node, CancellationToken _);
}

/// <summary>
/// Handles logic for generating PowerUps.
/// </summary>
public class PowerUpsRepo : IPowerUpsRepo {
  public bool IsPowerUpSyntaxCandidate(
    SyntaxNode node, CancellationToken _
  ) => node is ClassDeclarationSyntax classDeclaration && classDeclaration
    .AttributeLists
    .SelectMany(list => list.Attributes)
    .Any(
      attribute
        => attribute.Name.ToString() == Constants.POWER_UP_ATTRIBUTE_NAME
    );
}
