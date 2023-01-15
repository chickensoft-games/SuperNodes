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

public record GodotNode(
  string? Namespace,
  string Name,
  Location Location,
  string[] BaseClasses,
  IList<string> LifecycleMethods,
  IList<string> NotificationHandlers,
  bool HasPartialNotificationMethod,
  bool HasNotificationMethodHandler
);

public record PowerUpDescription(
  string? Namespace,
  string Name,
  Location Location,
  string BaseClass,
  string Source,
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
  private static bool _logsFlushed = false;

  public void Initialize(IncrementalGeneratorInitializationContext context) {
    // Debugger.Launch();

    _logsFlushed = false;
    _log.Clear();
    _log.Print("Initializing source generation...");
    _log.Print("Injecting attributes");

    // Inject [SuperNode] and [PowerUp] attributes.
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
      transform: (syntaxContext, ct) => syntaxContext.Node
    );
    context.RegisterImplementationSourceOutput(
      syntax,
      (ctx, _) => {
        if (_logsFlushed) { return; }
        ctx.AddSource(
          $"LOG", SourceText.From(_log.Contents, Encoding.UTF8)
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
    var @namespace = symbol?.ContainingNamespace.ToString();

    var usings = symbol is not null
      ? GetUsings(symbol)
      : ImmutableHashSet<string>.Empty;

    var hasOnPowerUpMethod = node.Members.Where(
      member => member is MethodDeclarationSyntax method
        && method.Identifier.Text == $"On{name}"
    ).Any();

    return new PowerUpDescription(
      Namespace: @namespace,
      Name: name,
      Location: node.GetLocation(),
      BaseClass: baseClass,
      Source: node.ToString(),
      Usings: usings,
      HasOnPowerUpMethod: hasOnPowerUpMethod
    );
  }

  // Returns true for classes that have a partial method for _Notification
  public static bool IsGodotNodeSyntaxCandidate(
    SyntaxNode node, CancellationToken _
  ) => node is ClassDeclarationSyntax classDeclaration &&
    classDeclaration.AttributeLists.SelectMany(
      list => list.Attributes
    ).Any(
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
    var hasPartialNotificationMethod = classDeclaration.Members.Where(
      member => member is MethodDeclarationSyntax method &&
        method.Modifiers.Any(modifier => modifier.Text == "public") &&
        method.Modifiers.Any(modifier => modifier.Text == "override") &&
        method.Modifiers.Any(modifier => modifier.Text == "partial") &&
        method.ReturnType.ToString() == "void" &&
        method.Identifier.Text == "_Notification" &&
        method.ParameterList.Parameters.Count == 1 &&
        method.ParameterList.Parameters.First()!.Type?.ToString() == "long" &&
        method.ParameterList.Parameters.First()!.Identifier.Text == "what"
    ).Any();

    // We want to see if the script implements OnNotification(long). It's
    // a special case since it has to be called on any notification.
    var hasNotificationMethodHandler = classDeclaration.Members.Where(
      member => member is MethodDeclarationSyntax method &&
        method.ReturnType.ToString() == "void" &&
        method.Identifier.Text == "OnNotification" &&
        method.ParameterList.Parameters.Count == 1 &&
        method.ParameterList.Parameters.First()!.Type?.ToString() == "long"
    ).Any();

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
        var arg = args.First();
        // if (arg.Kind != TypedConstantKind.Array) {
        //   goto skipSuperNodeAttribute;
        // }
        foreach (var constant in arg.Values) {
          if (constant.Type?.Name != "String") { continue; };
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
      HasNotificationMethodHandler: hasNotificationMethodHandler
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
      $"{node.Name}.g.cs",
      SourceText.From(
        GenerateSuperNode(item),
        Encoding.UTF8
      )
    );

    var powerUps = item.PowerUps;

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

      context.AddSource(
        $"{node.Name}.{powerUp.Name}.g.cs",
        SourceText.From(
          GeneratePowerUpImplementation(node, powerUp),
          Encoding.UTF8
        )
      );
    }
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
      public partial class {{node.Name}} {
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

  public static string GeneratePowerUpImplementation(
    GodotNode node, PowerUpDescription powerUp
  ) {
    // Edit the pieces of the user's power-up needed to make it suitable to be
    // a partial class of the specific node script it's applied to.

    var tree = CSharpSyntaxTree.ParseText(powerUp.Source);
    var root = (CompilationUnitSyntax)tree.GetRoot();
    var classDeclaration = (ClassDeclarationSyntax)root.Members.First();

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
    // Add partial modifier with correct spacing?
    .WithModifiers(
      new SyntaxTokenList(
        classDeclaration.Modifiers.Where(
          (modifier) => !modifier.IsKind(SyntaxKind.PartialKeyword)
        )
      )
    ).AddModifiers(
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

    // Edit the user's power up class tree based on the above changes.
    root = root.ReplaceNode(classDeclaration, newClassDeclaration);
    tree = tree.WithRootAndOptions(root, tree.Options);

    var usings = powerUp.Usings.Select(@using => $"using {@using};")
      .ToImmutableArray();

    // get the source code of the class itself
    var source = tree.GetRoot().ToFullString();

    var code = $$"""
    #nullable enable
    using Godot;
    {{If(
      usings.Length > 0,
      Lines(usings)
    )}}

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
}
