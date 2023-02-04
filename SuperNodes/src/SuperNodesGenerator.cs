namespace SuperNodes;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

public record GenerationItem(
  GodotNode Node,
  ImmutableDictionary<string, PowerUpDescription> PowerUps
);

public record AttributeDescription(
  string Name,
  string Type,
  string[] ArgumentExpressions
);

/// <summary>
/// Field or property information. Script properties are captured into a static
/// table for reflection-style lookups. This allows power-ups to be more, uh,
/// powerful.
/// </summary>
/// <param name="Name">Property or field name.</param>
/// <param name="Type">Property or field type.</param>
/// <param name="Attributes">Basic attribute information.</param>
/// <param name="IsField">True if a field, false if a property.</param>
public record PropOrField(
  string Name,
  string Type,
  IImmutableSet<AttributeDescription> Attributes,
  bool IsField
);

public record GodotNode(
  string? Namespace,
  string Name,
  Location Location,
  string[] BaseClasses,
  IList<string> LifecycleMethods,
  IList<string> NotificationHandlers,
  bool HasPartialNotificationMethod,
  bool HasNotificationMethodHandler,
  IList<PropOrField> PropsAndFields,
  IImmutableSet<string> Usings
) {
  public string FilenamePrefix
    => Namespace is not null and not "" ? $"{Namespace}.{Name}" : Name;
}

public record PowerUpDescription(
  string? Namespace,
  string Name,
  Location Location,
  string BaseClass,
  string[] Interfaces,
  string Source,
  IList<PropOrField> PropsAndFields,
  IImmutableSet<string> Usings,
  bool HasOnPowerUpMethod
);

public readonly record struct LifecycleMethod(
  string GodotNotification,
  string ReturnType,
  string[] Args
);

[Generator]
public partial class SuperNodesGenerator
  : ChickensoftGenerator, IIncrementalGenerator {
  private static Log _log { get; } = new Log();
  private static bool _logsFlushed;

  public static readonly ImmutableHashSet<string>
    BlacklistedStaticPowerUpProperties = new HashSet<string> {
    "PropertiesAndFields"
  }.ToImmutableHashSet();

  public static readonly ImmutableHashSet<string>
    BlacklistedStaticPowerUpMethods = new HashSet<string> {
    "GetScriptPropertyOrFieldType"
  }.ToImmutableHashSet();

  public void Initialize(IncrementalGeneratorInitializationContext context) {
    // Debugger.Launch();

    _logsFlushed = false;
    _log.Clear();
    _log.Print("Initializing source generation...");
    _log.Print("Injecting attributes");

    // Inject attributes and other utility sources.
    context.RegisterPostInitializationOutput(
      (context) => context.AddSource(
        $"{SUPER_NODE_ATTRIBUTE_NAME_FULL}.g.cs",
        SourceText.From(Format(SUPER_NODE_ATTRIBUTE_SOURCE), Encoding.UTF8)
      )
    );

    context.RegisterPostInitializationOutput(
      (context) => context.AddSource(
        $"{POWER_UP_ATTRIBUTE_NAME_FULL}.g.cs",
        SourceText.From(Format(POWER_UP_ATTRIBUTE_SOURCE), Encoding.UTF8)
      )
    );

    context.RegisterPostInitializationOutput(
      (context) => context.AddSource(
        $"{STATIC_REFLECTION_NAME}.g.cs",
        SourceText.From(
          Format(STATIC_REFLECTION_SOURCE), Encoding.UTF8
        )
      )
    );

    var godotNodeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: IsGodotNodeSyntaxCandidate,
      transform: GetGodotNodeSyntaxCandidate
    );

    var powerUpCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: IsPowerUpSyntaxCandidate,
      transform: GetPowerUpSyntaxCandidate
    );

    // Combine each godot node candidate with the list of power ups and the
    // compilation.
    //
    // This is absolutely disgusting (because combining results in tuples,
    // among other things), but it allows for performance (supposedly, if
    // you use cache-friendly values). I'm not really sure how cache-friendly
    // this generator is yet, but we'll get there.
    var generationItems = godotNodeCandidates
      .Combine(
        powerUpCandidates.Collect().Select(
          (s, _) => s.ToImmutableDictionary(
            keySelector: (item) => item.Name,
            elementSelector: (item) => item
          )
        )
      ).Select(
        (item, _) => new GenerationItem(
          Node: item.Left,
          PowerUps: item.Right
        )
      );

    context.RegisterSourceOutput(
      source: generationItems,
      action: Execute
    );

#if DEBUG
    // Very hacky way of only printing out one log file.
    var syntax = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: (syntaxNode, _) => syntaxNode is CompilationUnitSyntax,
      transform: (syntaxContext, _) => syntaxContext.Node
    );
    context.RegisterImplementationSourceOutput(
      syntax,
      (ctx, _) => {
        if (_logsFlushed) { return; }
        ctx.AddSource(
          "LOG", SourceText.From(_log.Contents, Encoding.UTF8)
        );
        _logsFlushed = true;
      }
    );
#endif
  }

  public static bool IsPowerUpSyntaxCandidate(
    SyntaxNode node, CancellationToken _
  ) => node is ClassDeclarationSyntax classDeclaration && classDeclaration
    .AttributeLists
    .SelectMany(list => list.Attributes)
    .Any(attribute => attribute.Name.ToString() == POWER_UP_ATTRIBUTE_NAME);

  public static PowerUpDescription GetPowerUpSyntaxCandidate(
    GeneratorSyntaxContext context, CancellationToken _
  ) {
    var node = (ClassDeclarationSyntax)context.Node;
    var name = node.Identifier.Text;
    var model = context.SemanticModel;
    var symbol = model.GetDeclaredSymbol(node);
    var baseType = symbol?.BaseType?.ToDisplayString(
      SymbolDisplayFormat.FullyQualifiedFormat
    );
    var baseClass = baseType ?? "Godot.Node";

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
      .ToArray();

    var @namespace = symbol?.ContainingNamespace.ToString();

    var usings = symbol is not null
      ? GetUsings(symbol)
      : ImmutableHashSet<string>.Empty;

    var hasOnPowerUpMethod = node.Members.Any(
      member => member is MethodDeclarationSyntax method
        && method.Identifier.Text == $"On{name}"
    );

    var members = symbol is not null
      ? symbol.GetMembers()
      : new ImmutableArray<ISymbol>();

    return new PowerUpDescription(
      Namespace: @namespace,
      Name: name,
      Location: node.GetLocation(),
      BaseClass: baseClass,
      Interfaces: interfaces,
      Source: node.ToString(),
      PropsAndFields: GetPropsAndFields(members),
      Usings: usings,
      HasOnPowerUpMethod: hasOnPowerUpMethod
    );
  }

  public static bool IsGodotNodeSyntaxCandidate(
    SyntaxNode node, CancellationToken _
  ) => node is ClassDeclarationSyntax classDeclaration &&
    classDeclaration.AttributeLists.SelectMany(
      list => list.Attributes
    ).Any(
      // Returns true for classes that have a partial method for _Notification
      attribute => attribute.Name.ToString() == SUPER_NODE_ATTRIBUTE_NAME
    );

  public static GodotNode GetGodotNodeSyntaxCandidate(
    GeneratorSyntaxContext context, CancellationToken _
  ) => GetGodotNode(
    context.SemanticModel,
    (ClassDeclarationSyntax)context.Node
  );

  public static GodotNode GetGodotNode(
    SemanticModel model,
    ClassDeclarationSyntax classDeclaration
  ) {
    var symbol = model.GetDeclaredSymbol(classDeclaration);

    var name = symbol?.Name ?? classDeclaration.Identifier.Text;
    var @namespace = symbol?.ContainingNamespace.IsGlobalNamespace != false
      ? string.Empty
      : symbol.ContainingNamespace.ToString();

    // Make sure the SuperNode declares the following method:
    // `public override partial void _Notification(long what);`
    var hasPartialNotificationMethod = classDeclaration.Members.Any(
      member => member is MethodDeclarationSyntax method &&
        method.Modifiers.Any(modifier => modifier.Text == "public") &&
        method.Modifiers.Any(modifier => modifier.Text == "override") &&
        method.Modifiers.Any(modifier => modifier.Text == "partial") &&
        method.ReturnType.ToString() == "void" &&
        method.Identifier.Text == "_Notification" &&
        method.ParameterList.Parameters.Count == 1 &&
        method.ParameterList.Parameters.First()!.Type?.ToString() == "long" &&
        method.ParameterList.Parameters.First()!.Identifier.Text == "what"
    );

    // We want to see if the script implements OnNotification(long). It's
    // a special case since it has to be called on any notification.
    var hasNotificationMethodHandler = classDeclaration.Members.Any(
      member => member is MethodDeclarationSyntax method &&
        method.ReturnType.ToString() == "void" &&
        method.Identifier.Text == "OnNotification" &&
        method.ParameterList.Parameters.Count == 1 &&
        method.ParameterList.Parameters.First()!.Type?.ToString() == "long"
    );

    // Find the [SuperNode] attribute on the class.
    var lifecycleMethods = new List<string>();
    var attributes
      = symbol?.GetAttributes() ?? ImmutableArray<AttributeData>.Empty;
    var superNodeAttribute = attributes.FirstOrDefault(
      attribute =>
        attribute.AttributeClass?.Name == SUPER_NODE_ATTRIBUTE_NAME_FULL
    );

    if (superNodeAttribute is AttributeData attribute) {
      var args = attribute.ConstructorArguments;
      _log.Print($"Found attribute {attribute} {args} {args.Length}");
      if (args.Length == 1) {
        // SuperNode attribute technically only requires 1 argument which
        // should be an array of strings.
        var arg = args[0];
        // if (arg.Kind != TypedConstantKind.Array) {
        //   goto skipSuperNodeAttribute;
        // }
        foreach (var constant in arg.Values) {
          if (constant.Type?.Name != "String") { continue; }
          lifecycleMethods.Add((string)constant.Value!);
        }
      }
    }
    // skipSuperNodeAttribute:

    // Find any On[Notification] method handlers.
    var notificationHandlers = classDeclaration.Members.Where(
      member => member is MethodDeclarationSyntax method
        && LifecycleMethods.ContainsKey(method.Identifier.Text)
    )
    .Cast<MethodDeclarationSyntax>()
    .Select(method => method.Identifier.Text)
    .ToList();

    var members = symbol is not null
      ? symbol.GetMembers()
      : new ImmutableArray<ISymbol>();

    var propsAndFields = GetPropsAndFields(members);

    var usings = symbol is not null
      ? GetUsings(symbol)
      : ImmutableHashSet<string>.Empty;

    return new GodotNode(
      Namespace: @namespace,
      Name: name,
      Location: classDeclaration.GetLocation(),
      BaseClasses: symbol is null
        ? Array.Empty<string>()
        : GetBaseClassHierarchy(symbol),
      LifecycleMethods: lifecycleMethods,
      NotificationHandlers: notificationHandlers,
      HasPartialNotificationMethod: hasPartialNotificationMethod,
      HasNotificationMethodHandler: hasNotificationMethodHandler,
      PropsAndFields: propsAndFields,
      Usings: usings
    );
  }

  public static void Execute(
    SourceProductionContext context,
    GenerationItem item
  ) {
    var node = item.Node;

    if (!node.HasPartialNotificationMethod) {
      context.ReportDiagnostic(
        Diagnostic.Create(
          new DiagnosticDescriptor(
            id: SUPER_NODE_MISSING_NOTIFICATION_METHOD,
            title: "Missing partial `_Notification` method signature.",
            messageFormat: "The SuperNode '{0}' is missing a partial " +
              "signature for `_Notification(long what)`. Please add the " +
              "following method signature to your class: " +
              "`public override partial void _Notification(long what);`",
            category: "SuperNode",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
          ),
          location: node.Location,
          node.Name
        )
      );
    }

    context.AddSource(
      $"{node.FilenamePrefix}.g.cs",
      SourceText.From(GenerateSuperNode(item), Encoding.UTF8)
    );

    var powerUps = item.PowerUps;
    var appliedPowerUps = new List<PowerUpDescription>();

    // See if the node has any power-ups.
    foreach (var lifecycleMethod in node.LifecycleMethods) {
      if (!powerUps.ContainsKey(lifecycleMethod)) { continue; }
      var powerUp = powerUps[lifecycleMethod];

      // make sure the node's base class hierarchy includes the power-up's
      // base class
      var canApplyPowerUp = node.BaseClasses.Contains(powerUp.BaseClass);

      if (!canApplyPowerUp) {
        // Log a source generator error so the user knows they can't apply
        // a power up on this node script since it doesn't extend the right
        // class.
        context.ReportDiagnostic(
          Diagnostic.Create(
            descriptor: new DiagnosticDescriptor(
              id: SUPER_NODE_INVALID_POWER_UP,
              title: "Invalid power-up on Godot node script class",
              messageFormat: "Power-up '{0}' cannot be applied to node '{1}' " +
                "because '{1}' does not extend '{2}'",
              category: "SuperNode",
              defaultSeverity: DiagnosticSeverity.Error,
              isEnabledByDefault: true
            ),
            location: node.Location,
            powerUp.Name,
            node.Name,
            powerUp.BaseClass
          )
        );
        continue;
      }

      appliedPowerUps.Add(powerUp);

      context.AddSource(
        $"{node.FilenamePrefix}_{powerUp.Name}.g.cs",
        SourceText.From(
          GeneratePowerUpImplementation(node, powerUp),
          Encoding.UTF8
        )
      );
    }

    context.AddSource(
      $"{node.FilenamePrefix}_Static.g.cs",
      SourceText.From(
        GenerateSuperNodeStatic(item, appliedPowerUps), Encoding.UTF8
      )
    );
  }

  public static string GenerateSuperNode(GenerationItem item) {
    var node = item.Node;
    var powerUps = item.PowerUps;

    var lifecycleInvocations = node.LifecycleMethods.Where(
      method => (
        powerUps.ContainsKey(method) && powerUps[method].HasOnPowerUpMethod
      ) || (!powerUps.ContainsKey(method))
    ).Select(
      method => {
        if (powerUps.ContainsKey(method)) {
          return $"On{method}(what);";
        }
        return $"{method}(what);";
      }
    ).ToList();

    // Create a switch/case for every implemented notification handler, such
    // as OnReady, OnPhysicsProcess, etc.
    var handlers = new List<string>();
    if (node.NotificationHandlers.Count > 0) {
      handlers.Add("switch (what) {");
      foreach (var handler in node.NotificationHandlers) {
        var method = LifecycleMethods[handler];
        handlers.Add(
          $"case {method.GodotNotification}:"
        );
        handlers.Add($"{handler}({string.Join(", ", method.Args)});");
        handlers.Add("break;");
      }
      handlers.Add("default:");
      handlers.Add("break;");
      handlers.Add("}");
    }

    // A sad substitute for templates since I couldn't be bothered.
    // This reads decently (I hope).
    var code = $$"""
    #nullable enable
    using Godot;

    {{If(
      node.Namespace != "",
      $$"""namespace {{node.Namespace}} {"""
    )}}
      partial class {{node.Name}} {
        public override partial void _Notification(long what) {
          {{If(
          lifecycleInvocations.Count > 0,
          "// Invoke declared lifecycle method handlers."
          )}}
          {{If(lifecycleInvocations.Count > 0, Lines(lifecycleInvocations))}}
          {{If(
            node.HasNotificationMethodHandler,
            """
            // Invoke the notification handler in the script.
            OnNotification(what);
            """
          )}}
          {{If(
          handlers.Count > 0,
          "// Invoke any notification handlers declared in the script."
          )}}
          {{If(handlers.Count > 0, Lines(handlers))}}
        }
      }
    {{If(
      node.Namespace != "",
      "}"
    )}}
    #nullable disable
    """;
    return Format(code);
  }

  public static string GenerateSuperNodeStatic(
    GenerationItem item, IList<PowerUpDescription> appliedPowerUps
  ) {
    var node = item.Node;

    // Combine properties and fields from the node script and all of its
    // applied power-ups.
    var propsAndFields = node.PropsAndFields.Concat(
      appliedPowerUps.SelectMany(powerUp => powerUp.PropsAndFields)
    ).OrderBy(propOrField => propOrField.Name).ToList();

    var usings = node.Usings.Union(new string[] {
        "System",
        "System.Collections.Generic",
        "System.Collections.Immutable"
    }).OrderBy(@using => @using).Select(
      @using => $"using {@using};"
    ).ToImmutableArray();

    var fields = new List<string>() {
      "/// <summary>",
      "/// A list of all properties and fields on this node script, along with",
      "/// basic information about the member and its attributes.",
      "/// This is provided to allow PowerUps to access script member data",
      "/// without having to resort to reflection.",
      "/// </summary>",
      "internal static ScriptPropertyOrField[] PropertiesAndFields { get; }",
      $"{Tab(1)}= new ScriptPropertyOrField[] {{"
    };

    for (var propI = 0; propI < propsAndFields.Count; propI++) {
      var propOrField = propsAndFields[propI];
      var propComma
        = propOrField == propsAndFields[propsAndFields.Count - 1] ? "" : ",";
      fields.Add($"{Tab(1)}new ScriptPropertyOrField(");
      fields.Add($"{Tab(2)}\"{propOrField.Name}\",");
      fields.Add($"{Tab(2)}typeof({propOrField.Type}),");
      fields.Add($"{Tab(2)}{propOrField.IsField.ToString().ToLower()},");
      var attributes = propOrField.Attributes;
      if (attributes.Count > 0) {
        fields.Add(
          $"{Tab(2)}new Dictionary<string, ScriptAttributeDescription>() {{"
        );
        foreach (var attribute in attributes) {
          var attrComma = attribute == attributes.Last() ? "" : ",";
          fields.Add($"{Tab(3)}[\"{attribute.Type}\"] =");
          fields.Add($"{Tab(4)}new ScriptAttributeDescription(");
          fields.Add($"{Tab(5)}\"{attribute.Name}\",");
          fields.Add($"{Tab(5)}typeof({attribute.Type}),");
          var args = attribute.ArgumentExpressions;
          if (args.Length > 0) {
            fields.Add($"{Tab(5)}ImmutableArray.Create<dynamic>(");
            foreach (var arg in args) {
              var argComma
                = arg == attribute.ArgumentExpressions.Last() ? "" : ",";
              fields.Add(Tab(6) + arg + argComma);
            }
            fields.Add($"{Tab(5)})");
          }
          else {
            fields.Add($"{Tab(5)}Array.Empty<dynamic>().ToImmutableArray()");
          }
          fields.Add($"{Tab(4)}){attrComma}");
        }
        fields.Add($"{Tab(2)}}}.ToImmutableDictionary()");
      }
      else {
        fields.Add(
          $"{Tab(2)}ImmutableDictionary<string, " +
            "ScriptAttributeDescription>.Empty"
        );
      }
      fields.Add($"{Tab(1)}){propComma}");
    }

    fields.Add("};");

    IEnumerable<string> getTypeFn = new List<string> {
      "/// <summary>",
      "/// Calls the given type receiver with the generic type of the given",
      "/// script property or field. Generated by SuperNodes.",
      "/// </summary>",
      "/// <typeparam name=\"TResult\">The return type of the type receiver's",
      "/// receive method.</typeparam>",
      "/// <param name=\"scriptProperty\">The name of the script property or " +
        "field",
      "/// to get the type of.</param>",
      "/// <param name=\"receiver\">The type receiver to call with the type",
      "/// of the script property or field.</param>",
      "/// <returns>The result of the type receiver's receive method." +
        "</returns>",
      "/// <exception cref=\"System.ArgumentException\">Thrown if the given " +
        "script",
      "/// property or field does not exist.</exception>",
      "internal static TResult GetScriptPropertyOrFieldType<TResult>(",
      $"{Tab(1)}string scriptProperty, ITypeReceiver<TResult> receiver",
      ") {",
      $"{Tab(1)}switch (scriptProperty) {{",
    };

    if (propsAndFields.Count > 0) {
      foreach (var fieldOrProp in propsAndFields) {
        getTypeFn = getTypeFn.Concat(new string[] {
          $"{Tab(2)}case \"{fieldOrProp.Name}\":",
          $"{Tab(3)}return receiver.Receive<{fieldOrProp.Type}>();"
        });
      }
    }

    getTypeFn = getTypeFn.Concat(new string[] {
      $"{Tab(2)}default:",
      $"{Tab(3)}throw new System.ArgumentException(",
      $"{Tab(4)}$\"No field or property named '{{scriptProperty}}' was " +
        $"found on {node.Name}.\"",
      $"{Tab(3)});",
      $"{Tab(1)}}}",
      "}"
    });

    return $$"""
    #nullable enable
    {{Lines(usings)}}

    {{If(
      node.Namespace != "",
      $$"""namespace {{node.Namespace}} {"""
    )}}
      partial class {{node.Name}} {
        {{Lines(2, fields)}}

        {{Lines(2, getTypeFn)}}
      }
    {{If(
      node.Namespace != "",
      "}"
    )}}
    #nullable disable
    """;
  }

  public static string GeneratePowerUpImplementation(
    GodotNode node, PowerUpDescription powerUp
  ) {
    // Edit the pieces of the user's power-up needed to make it suitable to be
    // a partial class of the specific node script it's applied to.

    var tree = CSharpSyntaxTree.ParseText(powerUp.Source);
    var root = (CompilationUnitSyntax)tree.GetRoot();
    var classDeclaration = (ClassDeclarationSyntax)root.Members.First();
    var interfaces = powerUp.Interfaces;

    // Strip [PowerUp] attribute off the class declaration
    var newClassDeclaration = classDeclaration.WithAttributeLists(
      SyntaxFactory.List(
        classDeclaration.AttributeLists.Where(
          attributeList => attributeList.Attributes.All(
            attribute => attribute.Name.ToString() != POWER_UP_ATTRIBUTE_NAME
          )
        )
      )
    )
    // Change power up name to the node's name.
    .WithIdentifier(SyntaxFactory.Identifier(node.Name))
    // Strip modifiers, add partial modifier back.
    .WithModifiers(new SyntaxTokenList())
    .AddModifiers(
      new[] {
        SyntaxFactory
          .Token(SyntaxKind.PartialKeyword)
          .WithTrailingTrivia(SyntaxFactory.Whitespace(" "))
      }
    )
    // Remove power-up constructors. Since PowerUps can constrain which classes
    // they are applicable on, PowerUp authors will sometimes have to provide
    // a parameterless constructor to satisfy Godot's node requirements.
    .WithMembers(
      SyntaxFactory.List(
        classDeclaration.Members.Where(
          member => member is not ConstructorDeclarationSyntax
        )
      )
    )
    // Remove base list
    .WithBaseList(null);
    // Remove static stubs with the same names as the static reflection tables
    // generated by SuperNodes. PowerUps that use static reflection will need
    // to supply stubs of the static reflection tables themselves since the
    // PowerUp must also be able to compile without being applied to a
    // SuperNode.
    //
    // Once static inheritance is implemented in .NET 7, it will not be
    // necessary for power-ups to declare stubs for the static reflection
    // tables.
    newClassDeclaration = newClassDeclaration.WithMembers(
      SyntaxFactory.List(
        newClassDeclaration.Members.Where(
          member => (member is not MethodDeclarationSyntax method || !(
            BlacklistedStaticPowerUpMethods.Contains(
              method.Identifier.ToString()
            ) &&
            method.Modifiers.Any(
              modifier => modifier.IsKind(SyntaxKind.StaticKeyword)
            )
          ))
          && (member is not PropertyDeclarationSyntax prop || !(
            BlacklistedStaticPowerUpProperties.Contains(
              prop.Identifier.ToString()
            ) &&
            prop.Modifiers.Any(
              modifier => modifier.IsKind(SyntaxKind.StaticKeyword)
            )
          ))
        )
      )
    );

    if (interfaces.Length > 0) {
      // Add only interfaces back to the base list.
      newClassDeclaration = newClassDeclaration.WithBaseList(
        SyntaxFactory.BaseList(
          SyntaxFactory.SeparatedList<BaseTypeSyntax>(
            interfaces.Select(
              @interface => SyntaxFactory.SimpleBaseType(
                SyntaxFactory.ParseTypeName(@interface)
              )
            )
          )
        )
      );
    }

    // Edit the user's power up class tree based on the above changes.
    root = root.ReplaceNode(classDeclaration, newClassDeclaration);
    tree = tree.WithRootAndOptions(root, tree.Options);

    var usings = powerUp.Usings.Union(new string[] { "Godot" })
      .OrderBy(@using => @using)
      .Select(@using => $"using {@using};").ToImmutableArray();

    // get the source code of the class itself
    var source = tree.GetRoot().ToFullString();

    var code = $$"""
    #nullable enable
    {{Lines(usings)}}

    {{If(
      node.Namespace != "",
      $$"""namespace {{node.Namespace}} {"""
    )}}
    {{source}}
    {{If(
      node.Namespace != "",
      "}"
    )}}
    #nullable disable
    """;
    return Format(code);
  }

  internal static List<PropOrField> GetPropsAndFields(
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
      .ToList();
  }

  internal static ImmutableHashSet<AttributeDescription>
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
