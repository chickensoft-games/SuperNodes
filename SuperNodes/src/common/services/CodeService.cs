namespace SuperNodes.Common.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Models;

/// <summary>
/// Contains common code operations for syntax nodes and semantic model symbols.
/// </summary>
public interface ICodeService {
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
  string[] GetBaseClassHierarchy(INamedTypeSymbol symbol);

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
/// Contains common code operations for syntax nodes and semantic model symbols.
/// </summary>
public class CodeService : ICodeService {
  public string GetContainingNamespace(ISymbol symbol)
    => symbol.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : symbol.ContainingNamespace.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        ).Replace("global::", "");

  public string[] GetBaseClassHierarchy(INamedTypeSymbol symbol) =>
  symbol.BaseType is INamedTypeSymbol baseSymbol
    ? new[] {
          baseSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
      }.Concat(GetBaseClassHierarchy(baseSymbol)).ToArray()
    : Array.Empty<string>();

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
        ).ToArray()
      )
    ).ToImmutableHashSet();
}
