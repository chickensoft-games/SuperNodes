namespace SuperNodes.PowerUpsFeature;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;

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
  /// <param name="_">Cancellation token (unused).</param>
  /// <returns>True if the syntax node is a class declaration with the
  /// PowerUp attribute.</returns>
  bool IsPowerUpSyntaxCandidate(SyntaxNode node, CancellationToken _);

  /// <summary>
  /// Returns a model that represents a PowerUp based on the PowerUp syntax
  /// node candidate provided by the generation context.
  /// context.
  /// </summary>
  /// <param name="context">Generation context containing a PowerUp syntax node
  /// candidate.</param>
  /// <param name="_">Cancellation token (unused).</param>
  PowerUp GetPowerUpSyntaxCandidate(
    GeneratorSyntaxContext context, CancellationToken _
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
  /// <param name="syntaxOps">Common operations needed for syntax nodes.</param>
  public PowerUpsRepo(ICodeService syntaxOps) {
    CodeService = syntaxOps;
  }

  public bool IsPowerUpSyntaxCandidate(
    SyntaxNode node, CancellationToken _
  ) => node is ClassDeclarationSyntax classDeclaration && classDeclaration
    .AttributeLists
    .SelectMany(list => list.Attributes)
    .Any(
      attribute
        => attribute.Name.ToString() == Constants.POWER_UP_ATTRIBUTE_NAME
    );

  public PowerUp GetPowerUpSyntaxCandidate(
    GeneratorSyntaxContext context, CancellationToken _
  ) {
    var node = (ClassDeclarationSyntax)context.Node;
    var name = node.Identifier.Text;
    var model = context.SemanticModel;
    var symbol = model.GetDeclaredSymbol(node);
    var fullName = symbol?.ToDisplayString(
      SymbolDisplayFormat.FullyQualifiedFormat
    ) ?? name;
    var baseType = symbol?.BaseType?.ToDisplayString(
      SymbolDisplayFormat.FullyQualifiedFormat
    );
    var baseClass = baseType ?? "global::Godot.Node";

    var typeParameters = node.TypeParameterList?.Parameters
      .Select(parameter => parameter.Identifier.Text)
      .ToImmutableArray() ?? ImmutableArray<string>.Empty;

    // get only the interfaces shown in the power-up's source code
    var plainInterfaces = (
      node.BaseList?.Types
      .Where(type => type.Type is IdentifierNameSyntax)
      .Select(type => (type.Type as IdentifierNameSyntax)!.Identifier.Text)
      .ToImmutableHashSet()
    ) ?? new HashSet<string>().ToImmutableHashSet();

    var genericInterfaces = (
      node.BaseList?.Types
      .Where(type => type.Type is GenericNameSyntax)
      .Select(type => (type.Type as GenericNameSyntax)!.Identifier.Text)
      .ToImmutableHashSet()
    ) ?? new HashSet<string>().ToImmutableHashSet();

    var powerUpInterfaces = plainInterfaces.Union(genericInterfaces);

    var allInterfaces = symbol?.AllInterfaces ??
      new ImmutableArray<INamedTypeSymbol>();

    var interfaces = allInterfaces
      .Where(@interface => powerUpInterfaces.Contains(@interface.Name))
      .Select(
        @interface => @interface.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        )
      )
      .ToImmutableArray();

    var @namespace = symbol is not null
      ? CodeService.GetContainingNamespace(symbol)
      : "";

    var usings = symbol is not null
      ? CodeService.GetUsings(symbol)
      : ImmutableHashSet<string>.Empty;

    var hasOnPowerUpMethod = node.Members.Any(
      member => member is MethodDeclarationSyntax method
        && method.Identifier.Text == $"On{name}"
    );

    var members = symbol is not null
      ? symbol.GetMembers()
      : new ImmutableArray<ISymbol>();

    return new PowerUp(
      Namespace: @namespace,
      Name: name,
      FullName: fullName,
      Location: node.GetLocation(),
      BaseClass: baseClass,
      TypeParameters: typeParameters,
      Interfaces: interfaces,
      Source: node.ToString(),
      PropsAndFields: CodeService.GetPropsAndFields(members),
      Usings: usings,
      HasOnPowerUpMethod: hasOnPowerUpMethod
    );
  }
}
