namespace SuperNodes.Common.Services;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Models;

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public interface ICodeService {
  /// <summary>
  /// Determines the list of visible type parameters shown on a class
  /// declaration syntax node.
  /// </summary>
  /// <param name="classDeclaration">Class declaration syntax node.</param>
  /// <returns>Visible list of type parameters shown on that particular class
  /// declaration syntax node.</returns>
  ImmutableArray<string> GetTypeParameters(
    ClassDeclarationSyntax classDeclaration
  );

  /// <summary>
  /// Determines the list of visible interfaces shown on a class declaration
  /// syntax node and returns the set of the fully qualified interface names.
  /// </summary>
  /// <param name="classDeclaration">Class declaration syntax node.</param>
  /// <param name="symbol">Named type symbol corresponding to the class
  /// </param>
  /// <returns>Visible list of interfaces shown on that particular class
  /// declaration syntax node.</returns>
  ImmutableHashSet<string> GetVisibleInterfacesFullyQualified(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  );

  /// <summary>
  /// Determines the list of visible generic interfaces shown on a class
  /// declaration syntax node.
  /// </summary>
  /// <param name="classDeclaration">Class declaration syntax node.</param>
  /// <returns>Visible list of generic interfaces shown on that particular class
  /// declaration syntax node.</returns>
  ImmutableHashSet<string> GetVisibleGenericInterfaces(
    ClassDeclarationSyntax classDeclaration
  );

  /// <summary>
  /// Determines the fully resolved containing namespace of a symbol, if the
  /// symbol is non-null and has a containing namespace. Otherwise, returns a
  /// blank string.
  /// </summary>
  /// <param name="symbol">A potential symbol whose containing namespace
  /// should be determined.</param>
  /// <returns>The fully resolved containing namespace of the symbol, or the
  /// empty string.</returns>
  string GetContainingNamespace(ISymbol symbol);

  /// <summary>
  /// Computes the "using" imports of the syntax tree that the given named type
  /// symbol resides in.
  /// </summary>
  /// <param name="symbol">Named type to inspect.</param>
  /// <returns>String array of "using" imports.</returns>
  ImmutableHashSet<string> GetUsings(INamedTypeSymbol symbol);

  /// <summary>
  /// Recursively computes the base classes of a named type symbol.
  /// </summary>
  /// <param name="symbol">Named type to inspect.</param>
  /// <returns>String array of fully qualified base classes, or an empty array
  /// if no base classes.</returns>
  ImmutableArray<string> GetBaseClassHierarchy(INamedTypeSymbol symbol);

  /// <summary>
  /// Computes the list of symbols representing properties or fields based
  /// on a given array of symbols from the semantic model.
  /// </summary>
  /// <param name="members">Array of symbols from the semantic model.</param>
  /// <returns>List of symbols that are properties or fields.</returns>
  ImmutableArray<PropOrField> GetPropsAndFields(
    ImmutableArray<ISymbol> members
  );
}

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public class CodeService : ICodeService {
  public ImmutableHashSet<string> GetVisibleInterfacesFullyQualified(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  ) {
    var nonGenericInterfaces = GetVisibleInterfaces(classDeclaration);
    var genericInterfaces = GetVisibleGenericInterfaces(classDeclaration);
    var visibleInterfaces = nonGenericInterfaces.Union(genericInterfaces);

    var allKnownInterfaces = symbol?.AllInterfaces ??
      ImmutableArray<INamedTypeSymbol>.Empty;

    if (allKnownInterfaces.IsEmpty) {
      // Symbol doesn't have any information (probably because the code isn't
      // fully valid while being edited), so just return the non-fully-qualified
      // names of the interfaces (since that's the best we can do).
      return visibleInterfaces;
    }

    // Find the fully qualified names of only the interfaces that are directly
    // listed on the class declaration syntax node we are given.
    // allKnownInterfaces is computed based on the semantic model, so it
    // actually can contain interfaces that may be implemented by other
    // partial class implementations elsewhere.
    return allKnownInterfaces
      .Where(@interface => visibleInterfaces.Contains(@interface.Name))
      .Select(
        @interface => @interface.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        )
      )
      .ToImmutableHashSet();
  }

  public ImmutableArray<string> GetTypeParameters(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.TypeParameterList?.Parameters
      .Select(parameter => parameter.Identifier.ValueText)
      .ToImmutableArray()
    ) ?? ImmutableArray<string>.Empty;

  public ImmutableHashSet<string> GetVisibleInterfaces(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.BaseList?.Types
      .Select(type => type.Type)
      .OfType<IdentifierNameSyntax>()
      .Select(type => type.Identifier.ValueText)
      .ToImmutableHashSet()
    ) ?? ImmutableHashSet<string>.Empty;

  public ImmutableHashSet<string> GetVisibleGenericInterfaces(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.BaseList?.Types
      .Select(type => type.Type)
      .OfType<GenericNameSyntax>()
      .Select(type => type.Identifier.ValueText)
      .ToImmutableHashSet()
    ) ?? ImmutableHashSet<string>.Empty;

  public string GetContainingNamespace(ISymbol symbol)
    => symbol.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : symbol.ContainingNamespace.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        ).Replace("global::", "");

  public ImmutableArray<string> GetBaseClassHierarchy(INamedTypeSymbol symbol) =>
  symbol.BaseType is INamedTypeSymbol baseSymbol
    ? new[] {
          baseSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
      }.Concat(GetBaseClassHierarchy(baseSymbol)).ToImmutableArray()
    : ImmutableArray<string>.Empty;

  public ImmutableHashSet<string> GetUsings(INamedTypeSymbol symbol) {
    var allUsings = SyntaxFactory.List<UsingDirectiveSyntax>();
    foreach (var syntaxRef in symbol.DeclaringSyntaxReferences) {
      foreach (var parent in syntaxRef.GetSyntax().Ancestors(false)) {
        if (parent is BaseNamespaceDeclarationSyntax ns) {
          allUsings = allUsings.AddRange(ns.Usings);
        }
        else if (parent is CompilationUnitSyntax comp) {
          allUsings = allUsings.AddRange(comp.Usings);
        }
      }
    }
    return allUsings
      .Select(@using => @using.Name.ToString())
      .ToImmutableHashSet();
  }

  public ImmutableArray<PropOrField> GetPropsAndFields(
    ImmutableArray<ISymbol> members
  ) {
    var propsAndFields = new List<PropOrField>();

    foreach (var member in members) {
      if (
        (member is not IFieldSymbol and not IPropertySymbol) ||
        member.IsStatic || !member.CanBeReferencedByName
      ) {
        continue;
      }

      var name = member.Name;
      var type = "";

      if (member is IPropertySymbol property) {
        type = property.Type.ToString();
      }
      if (member is IFieldSymbol field) {
        type = field.Type.ToString();
      }

      var attributes = GetAttributesForPropOrField(member.GetAttributes());

      var propOrField = new PropOrField(
        Name: name,
        Type: type,
        Attributes: attributes,
        IsField: member is IFieldSymbol
      );

      propsAndFields.Add(propOrField);
    }

    return propsAndFields
      .OrderBy(propOrField => propOrField.Name)
      .ToImmutableArray();
  }

  private ImmutableHashSet<AttributeDescription>
    GetAttributesForPropOrField(ImmutableArray<AttributeData> attributes
  ) => attributes.Select(
      attribute => new AttributeDescription(
        Name: attribute.AttributeClass?.Name ?? string.Empty,
        Type: attribute.AttributeClass?.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        ) ?? string.Empty,
        ArgumentExpressions: attribute.ConstructorArguments.Select(
          arg => arg.ToCSharpString()
        ).ToImmutableArray()
      )
    ).ToImmutableHashSet();
}
