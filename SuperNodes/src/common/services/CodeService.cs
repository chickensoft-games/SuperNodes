namespace SuperNodes.Common.Services;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
  ImmutableArray<string> GetVisibleInterfacesFullyQualified(
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
  ImmutableArray<string> GetVisibleGenericInterfaces(
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
  string? GetContainingNamespace(ISymbol? symbol);

  /// <summary>
  /// Computes the "using" imports of the syntax tree that the given named type
  /// symbol resides in.
  /// </summary>
  /// <param name="symbol">Named type to inspect.</param>
  /// <returns>String array of "using" imports.</returns>
  ImmutableHashSet<string> GetUsings(INamedTypeSymbol? symbol);

  /// <summary>
  /// Recursively computes the base classes of a named type symbol.
  /// </summary>
  /// <param name="symbol">Named type to inspect.</param>
  /// <returns>String array of fully qualified base classes, or an empty array
  /// if no base classes.</returns>
  ImmutableArray<string> GetBaseClassHierarchy(INamedTypeSymbol? symbol);

  /// <summary>
  /// Computes the list of symbols representing properties or fields based
  /// on a given array of symbols from the semantic model.
  /// </summary>
  /// <param name="members">Array of symbols from the semantic model.</param>
  /// <returns>List of symbols that are properties or fields.</returns>
  ImmutableArray<PropOrField> GetPropsAndFields(
    ImmutableArray<ISymbol> members
  );

  /// <summary>
  /// Given an optional named type symbol, returns the fully qualified name of
  /// the base type, or null if the symbol is null or has no known base type.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fallbackClass">Default class name to return if there is no
  /// base class.</param>
  /// <returns>Fully qualified name (or null).</returns>
  string GetBaseTypeFullyQualified(
    INamedTypeSymbol? symbol, string fallbackClass = "object"
  );

  /// <summary>
  /// Determines the fully qualified name of a named type symbol.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fallbackName">Default symbol name to return if there is no
  /// symbol name.</param>
  /// <returns>The fully qualified name of the symbol (or null).</returns>
  string GetNameFullyQualified(INamedTypeSymbol? symbol, string fallbackName);

  /// <summary>
  /// Returns the array of members on the named type symbol, if any.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <returns>Array of members.</returns>
  ImmutableArray<ISymbol> GetMembers(INamedTypeSymbol? symbol);

  /// <summary>
  /// Returns the name of the symbol, or the name of the identifier for the
  /// given type declaration syntax node if the symbol is null.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fallbackType">Fallback type declaration syntax node.</param>
  string GetName(
    INamedTypeSymbol? symbol, TypeDeclarationSyntax fallbackType
  );

  /// <summary>
  /// Gets the name of the symbol, or null if the symbol is null.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <returns>Name of the symbol.</returns>
  string? GetName(ISymbol? symbol);

  /// <summary>
  /// Finds the attribute on the symbol with the given full name, such as
  /// <c>SuperNodeAttribute</c> or <c>ExportAttribute</c>.
  /// </summary>
  /// <param name="symbol">Named type symbol.</param>
  /// <param name="fullName">Full name of the attribute.</param>
  /// <returns></returns>
  AttributeData? GetAttribute(
    INamedTypeSymbol? symbol, string fullName
  );

  /// <summary>
  /// Examines the SuperNode attribute data for any mentioned lifecycle hooks.
  /// </summary>
  /// <param name="attribute">SuperNode attribute data found on a class symbol.
  /// </param>
  /// <returns>Lifecycle hook information.</returns>
  LifecycleHooksResponse GetLifecycleHooks(
    AttributeData? attribute
  );

  /// <summary>
  /// Returns true if the <paramref name="members"/> contains the partial
  /// <c>_Notification(int what)</c> method stub needed to intercept the Godot
  /// node lifecycle notifications.
  /// </summary>
  /// <param name="members">Type members.</param>
  /// <returns>
  /// True if the <paramref name="members"/> contains a partial notification
  /// method.
  /// </returns>
  bool HasPartialNotificationMethod(
    SyntaxList<MemberDeclarationSyntax> members
  );

  /// <summary>
  /// Determines whether or not <paramref name="members"/> contains an
  /// <c>OnNotification</c> method handler that should be called from the
  /// generated notification handlers.
  /// </summary>
  /// <param name="members">Type members</param>
  bool HasOnNotificationMethodHandler(
    SyntaxList<MemberDeclarationSyntax> members
  );
}

/// <summary>
/// Common code operations for syntax nodes and semantic model symbols.
/// </summary>
public class CodeService : ICodeService {
  public ImmutableArray<string> GetVisibleInterfacesFullyQualified(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  ) {
    var nonGenericInterfaces = GetVisibleInterfaces(classDeclaration);
    var genericInterfaces = GetVisibleGenericInterfaces(classDeclaration);
    var visibleInterfaces = nonGenericInterfaces
      .Union(genericInterfaces)
      .ToImmutableArray();

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
      .OrderBy(@interface => @interface)
      .ToImmutableArray();
  }

  public ImmutableArray<string> GetTypeParameters(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.TypeParameterList?.Parameters
      .Select(parameter => parameter.Identifier.ValueText)
      .ToImmutableArray()
    ) ?? ImmutableArray<string>.Empty;

  public ImmutableArray<string> GetVisibleInterfaces(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.BaseList?.Types
      .Select(type => type.Type)
      .OfType<IdentifierNameSyntax>()
      .Select(type => type.Identifier.ValueText)
      .OrderBy(@interface => @interface)
      .ToImmutableArray()
    ) ?? ImmutableArray<string>.Empty;

  public ImmutableArray<string> GetVisibleGenericInterfaces(
    ClassDeclarationSyntax classDeclaration
  ) => (
    classDeclaration.BaseList?.Types
      .Select(type => type.Type)
      .OfType<GenericNameSyntax>()
      .Select(type => type.Identifier.ValueText)
      .ToImmutableArray()
    ) ?? ImmutableArray<string>.Empty;

  public string? GetContainingNamespace(ISymbol? symbol)
    => symbol?.ContainingNamespace.IsGlobalNamespace == true
      ? null
      : symbol?.ContainingNamespace.ToDisplayString(
          SymbolDisplayFormat.FullyQualifiedFormat
        ).Replace("global::", "");

  public ImmutableArray<string> GetBaseClassHierarchy(INamedTypeSymbol? symbol)
    => symbol?.BaseType is INamedTypeSymbol baseSymbol
      ? new[] {
            baseSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        }.Concat(GetBaseClassHierarchy(baseSymbol)).ToImmutableArray()
      : ImmutableArray<string>.Empty;

  public ImmutableHashSet<string> GetUsings(INamedTypeSymbol? symbol) {
    if (symbol is null) {
      return ImmutableHashSet<string>.Empty;
    }
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
        // only get properties and fields
        (member is not IFieldSymbol and not IPropertySymbol) ||
        member.IsStatic || // no static members
        member.IsImplicitlyDeclared // no backing fields, etc
      ) { continue; }

      var name = member.Name;
      var isMutable = false;
      var isReadable = true;
      var type = "";
      var nameParts = ImmutableArray<SimpleSymbolDisplayPart>.Empty;
      var typeParts = ImmutableArray<SimpleSymbolDisplayPart>.Empty;
      if (
        member is IPropertySymbol property
      ) {
        isMutable = !property.IsReadOnly;
        isReadable = !property.IsWriteOnly;
        var hasExplicitInterfaceImplementations = property
          .ExplicitInterfaceImplementations
          .Any();
        var correspondingInterfaceMembers = member
          .ExplicitOrImplicitInterfaceImplementations();
        var declaredInInterface = correspondingInterfaceMembers.Any();
        if (declaredInInterface) {
          // All members originally declared in interfaces are referred to by
          // IInterface.Name to help avoid naming collisions

          var interfaceMember = correspondingInterfaceMembers[0];
          var interfaceName = interfaceMember.ContainingType.Name;

          name = interfaceMember.Name;

          nameParts = ExtractRelevantIdentifierParts(
            interfaceName, interfaceMember.ToDisplayParts()
          );
        }
        type = property.Type.ToString().TrimEnd('?');
        typeParts = property.Type.ToDisplayParts()
          .Select(
            part => new SimpleSymbolDisplayPart(part.Kind, part.ToString())
          ).ToImmutableArray();
      }
      else if (member is IFieldSymbol field) {
        type = field.Type.ToString().TrimEnd('?');
        isMutable = !field.IsReadOnly;
      }

      var attributes = GetAttributesForPropOrField(member.GetAttributes());

      var propOrField = new PropOrField(
        Name: name,
        Reference: name,
        Type: type,
        Attributes: attributes,
        IsField: member is IFieldSymbol,
        IsMutable: isMutable,
        IsReadable: isReadable,
        NameParts: nameParts,
        TypeParts: typeParts
      );

      propsAndFields.Add(propOrField);
    }

    return propsAndFields
      .OrderBy(propOrField => propOrField.Name)
      .ToImmutableArray();
  }

  public ImmutableArray<SimpleSymbolDisplayPart> ExtractRelevantIdentifierParts(
    string beginningIdentifier, ImmutableArray<SymbolDisplayPart> parts
  ) {
    // Searches for the first part that matches the beginning identifier and
    // captures the rest of the parts. This is used to chop off extraneous
    // leading namespaces.
    var relevantNameParts = new List<SimpleSymbolDisplayPart>();
    var shouldCaptureParts = false;
    var genericStackDepth = 0;
    for (var i = 0; i < parts.Length; i++) {
      var part = parts[i];
      if (part.Kind == SymbolDisplayPartKind.Punctuation) {
        if (part.ToString() == "<") {
          genericStackDepth++;
        }
        else if (part.ToString() == ">") {
          genericStackDepth--;
        }
      }
      if (
        genericStackDepth == 0 &&
        part.ToString() == beginningIdentifier &&
        !shouldCaptureParts
      ) {
        shouldCaptureParts = true;
      }
      if (shouldCaptureParts) {
        relevantNameParts.Add(
          new SimpleSymbolDisplayPart(part.Kind, part.ToString())
        );
      }
    }
    return relevantNameParts.ToImmutableArray();
  }

  public AttributeData? GetAttribute(
    INamedTypeSymbol? symbol, string fullName
  ) {
    var attributes = symbol?.GetAttributes()
      ?? ImmutableArray<AttributeData>.Empty;
    return attributes.FirstOrDefault(
      attribute => attribute.AttributeClass?.Name == fullName
    );
  }

  public ImmutableArray<AttributeDescription>
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
    ).ToImmutableArray();

  public string GetBaseTypeFullyQualified(
    INamedTypeSymbol? symbol, string fallbackClass = "object"
  )
    => symbol?.BaseType?.ToDisplayString(
      SymbolDisplayFormat.FullyQualifiedFormat
    ) ?? fallbackClass;

  public string GetNameFullyQualified(
    INamedTypeSymbol? symbol, string fallbackName
  )
    => symbol?.ToDisplayString(
      SymbolDisplayFormat.FullyQualifiedFormat
    ) ?? fallbackName;

  public string GetName(
    INamedTypeSymbol? symbol, TypeDeclarationSyntax fallbackType
  ) => symbol?.Name ?? fallbackType.Identifier.ValueText;

  public string? GetName(ISymbol? symbol) => symbol?.Name;

  public ImmutableArray<ISymbol> GetMembers(INamedTypeSymbol? symbol)
    => symbol?.GetMembers() ?? ImmutableArray<ISymbol>.Empty;

  public bool HasOnNotificationMethodHandler(
    SyntaxList<MemberDeclarationSyntax> members
  ) => members.Any(
    member => member is MethodDeclarationSyntax method &&
      method.ReturnType.ToString() == "void" &&
      method.Identifier.ValueText == "OnNotification" &&
      method.ParameterList.Parameters.Count == 1 &&
      method.ParameterList.Parameters.First().Type?.ToString()
        == "int"
  );

  public bool HasPartialNotificationMethod(
    SyntaxList<MemberDeclarationSyntax> members
  ) => members.Any(
    member => member is MethodDeclarationSyntax method &&
      method.Modifiers.Any(modifier => modifier.ValueText == "public") &&
      method.Modifiers.Any(modifier => modifier.ValueText == "override") &&
      method.Modifiers.Any(modifier => modifier.ValueText == "partial") &&
      method.ReturnType.ToString() == "void" &&
      method.Identifier.ValueText == "_Notification" &&
      method.ParameterList.Parameters.Count == 1 &&
      method.ParameterList.Parameters.First().Type?.ToString() == "int" &&
      method.ParameterList.Parameters.First().Identifier.ValueText == "what"
  );

  public LifecycleHooksResponse GetLifecycleHooks(AttributeData? attribute) {
    if (attribute is null) {
      return LifecycleHooksResponse.Empty;
    }

    var lifecycleHooks = new List<IGodotNodeLifecycleHook>();
    var powerUpHooksByFullName = new Dictionary<string, PowerUpHook>();

    var args = attribute.ConstructorArguments;
    if (args.Length > 0) {
      // SuperNode attribute technically only requires 1 argument which
      // should be an array of compile-time constants. We only support
      // two kinds of parameters: strings and types (via typeof).
      var arg = args[0];
      foreach (var constant in arg.Values) {
        var constantType = constant.Type;
        var name = GetName(constantType);
        if (name == "String") {
          // Found a lifecycle method. This can be the name of a method
          // to call from another generator or a method from a PowerUp.
          var stringValue = (string)constant.Value!;
          lifecycleHooks.Add(new LifecycleMethodHook(stringValue));
        }
        else if (name == "Type") {
          // We found a typeof(SomePowerUp<a, b, ...>) expression. It may
          // or may not have generic args. The important part is that we know
          // this must be a specific PowerUp (possibly with generics) that
          // needs to be applied to the node script.
          var typeValue = (INamedTypeSymbol)constant.Value!;
          // convert from PowerUp<bool, string> to the less concrete type
          // parameters like PowerUp<TA, TB>.
          var typeWithGenericParams = typeValue.ConstructedFrom;
          var fullName = typeWithGenericParams.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
          );
          var powerUpHook = new PowerUpHook(
            fullName,
            typeValue.TypeArguments.Select(arg => arg.ToDisplayString(
              SymbolDisplayFormat.FullyQualifiedFormat
            )).ToImmutableArray()
          );
          lifecycleHooks.Add(powerUpHook);
          powerUpHooksByFullName[fullName] = powerUpHook;
        }
      }
    }

    return new LifecycleHooksResponse(
      lifecycleHooks.ToImmutableArray(),
      powerUpHooksByFullName.ToImmutableDictionary()
    );
  }
}
