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
  /// <param name="typeDeclaration">PowerUp class declaration syntax node.
  /// </param>
  /// <param name="symbol">Named type symbol representing the class declaration
  /// syntax node, if any.</param>
  /// <returns>A PowerUp model.</returns>
  PowerUp GetPowerUp(
    TypeDeclarationSyntax typeDeclaration,
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
  ) => node is RecordDeclarationSyntax or ClassDeclarationSyntax &&
      node is TypeDeclarationSyntax typeDeclaration && typeDeclaration
      .AttributeLists
      .SelectMany(list => list.Attributes)
      .Any(
        attribute
          => attribute.Name.ToString() == Constants.POWER_UP_ATTRIBUTE_NAME
      );

  public PowerUp GetPowerUp(
    TypeDeclarationSyntax typeDeclaration,
    INamedTypeSymbol? symbol
  ) {
    var name = typeDeclaration.Identifier.ValueText;
    var fullName = CodeService.GetNameFullyQualified(symbol, name);
    var baseType =
      CodeService.GetBaseTypeFullyQualified(symbol, Constants.BaseClass);
    var typeParameters = CodeService.GetTypeParameters(typeDeclaration);
    var interfaces = CodeService.GetVisibleInterfacesFullyQualified(
      typeDeclaration, symbol
    );
    var @namespace = CodeService.GetContainingNamespace(symbol);

    var usings = CodeService.GetUsings(symbol);

    var hasOnPowerUpMethod = typeDeclaration.Members.Any(
      member => member is MethodDeclarationSyntax method
        && method.Identifier.ValueText == $"On{name}"
    );

    var members = CodeService.GetMembers(symbol);

    return new PowerUp(
      Namespace: @namespace,
      Name: name,
      FullName: fullName,
      Location: typeDeclaration.GetLocation(),
      BaseClass: baseType,
      TypeParameters: typeParameters,
      Interfaces: interfaces,
      Source: typeDeclaration.ToString(),
      PropsAndFields: CodeService.GetPropsAndFields(members),
      Usings: usings,
      HasOnPowerUpMethod: hasOnPowerUpMethod
    );
  }
}
