namespace SuperNodes.PowerUpsFeature;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
using SuperNodes.Common.Utils;

/// <summary>
/// Handles logic for generating PowerUps.
/// </summary>
public interface IPowerUpsRepo {
  /// <summary>Common operations needed for syntax nodes.</summary>
  ICodeService CodeService { get; }

  /// <summary>
  /// Determines if the given syntax node is a power up syntax candidate.
  /// </summary>
  /// <param name="node">Syntax node to check.</param>
  /// <returns>True if the syntax node is a class declaration with the
  /// PowerUp attribute.</returns>
  bool IsPowerUpSyntaxCandidate(SyntaxNode node);

  /// <summary>
  /// Returns a model that represents a PowerUp based on the PowerUp syntax
  /// node candidate provided by the generation context.
  /// context.
  /// </summary>
  /// <param name="classDeclaration">PowerUp class declaration syntax node.
  /// </param>
  /// <param name="symbol">Named type symbol representing the class declaration
  /// syntax node, if any.</param>
  /// <returns>A PowerUp model.</returns>
  PowerUp GetPowerUp(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  );
}

/// <summary>
/// Handles logic for generating PowerUps.
/// </summary>
public class PowerUpsRepo : IPowerUpsRepo {
  public ICodeService CodeService { get; }

  /// <summary>
  /// Create a new PowerUpsRepo.
  /// </summary>
  /// <param name="codeService">Common code operations for syntax nodes and
  /// semantic model symbols.</param>
  public PowerUpsRepo(ICodeService codeService) {
    CodeService = codeService;
  }

  public bool IsPowerUpSyntaxCandidate(
    SyntaxNode node
  ) => node is ClassDeclarationSyntax classDeclaration && classDeclaration
    .AttributeLists
    .SelectMany(list => list.Attributes)
    .Any(
      attribute
        => attribute.Name.ToString() == Constants.POWER_UP_ATTRIBUTE_NAME
    );

  public PowerUp GetPowerUp(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  ) {
    var name = classDeclaration.Identifier.ValueText;
    var fullName = CodeService.GetNameFullyQualified(symbol, name);
    var baseClass =
      CodeService.GetBaseTypeFullyQualified(symbol, Constants.BaseClass);
    var typeParameters = CodeService.GetTypeParameters(classDeclaration);
    var interfaces = CodeService.GetVisibleInterfacesFullyQualified(
      classDeclaration, symbol
    );
    var @namespace = CodeService.GetContainingNamespace(symbol);

    var usings = CodeService.GetUsings(symbol);

    var hasOnPowerUpMethod = classDeclaration.Members.Any(
      member => member is MethodDeclarationSyntax method
        && method.Identifier.ValueText == $"On{name}"
    );

    var members = CodeService.GetMembers(symbol);

    return new PowerUp(
      Namespace: @namespace,
      Name: name,
      FullName: fullName,
      Location: classDeclaration.GetLocation(),
      BaseClass: baseClass,
      TypeParameters: typeParameters,
      Interfaces: interfaces,
      Source: classDeclaration.ToString(),
      PropsAndFields: CodeService.GetPropsAndFields(members),
      Usings: usings,
      HasOnPowerUpMethod: hasOnPowerUpMethod
    );
  }
}
