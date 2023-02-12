namespace SuperNodes;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
using SuperNodes.PowerUpsFeature;
using SuperNodes.SuperNodesFeature;

[Generator]
public partial class SuperNodesGenerator
  : ChickensoftGenerator, IIncrementalGenerator {
  public IPowerUpsRepo PowerUpsRepo { get; }
  public ISuperNodesRepo SuperNodesRepo { get; }
  public ICodeService CodeService { get; }

  private static Log Log { get; } = new Log();
  private static bool _logsFlushed;

  /// <summary>
  /// Parameterless constructor used by the .NET SDK tooling.
  /// </summary>
  public SuperNodesGenerator() {
    CodeService = new CodeService();
    PowerUpsRepo = new PowerUpsRepo(CodeService);
    SuperNodesRepo = new SuperNodesRepo(CodeService);
  }

  /// <summary>
  /// Constructor used for testing.
  /// </summary>
  /// <param name="codeService">Contains common code operations for syntax
  /// nodes and semantic model symbols.</param>
  /// <param name="powerUpsRepo">PowerUps repository to use.</param>
  /// <param name="superNodesRepo">SuperNodes repository to use.</param>
  public SuperNodesGenerator(
    ICodeService codeService,
    IPowerUpsRepo powerUpsRepo,
    ISuperNodesRepo superNodesRepo
  ) {
    CodeService = codeService;
    PowerUpsRepo = powerUpsRepo;
    SuperNodesRepo = superNodesRepo;
  }

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
    Log.Clear();
    Log.Print("Initializing source generation...");
    Log.Print("Injecting attributes");

    // Inject attributes and other utility sources.
    context.RegisterPostInitializationOutput(
      (context) => context.AddSource(
        $"{Constants.SUPER_NODE_ATTRIBUTE_NAME_FULL}.g.cs",
        SourceText.From(Format(Constants.SUPER_NODE_ATTRIBUTE_SOURCE), Encoding.UTF8)
      )
    );

    context.RegisterPostInitializationOutput(
      (context) => context.AddSource(
        $"{Constants.POWER_UP_ATTRIBUTE_NAME_FULL}.g.cs",
        SourceText.From(Format(Constants.POWER_UP_ATTRIBUTE_SOURCE), Encoding.UTF8)
      )
    );

    context.RegisterPostInitializationOutput(
      (context) => context.AddSource(
        $"{Constants.STATIC_REFLECTION_NAME}.g.cs",
        SourceText.From(
          Format(Constants.STATIC_REFLECTION_SOURCE), Encoding.UTF8
        )
      )
    );

    var godotNodeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: SuperNodesRepo.IsSuperNodeSyntaxCandidate,
      transform: (GeneratorSyntaxContext context, CancellationToken _) =>
        SuperNodesRepo.GetSuperNode(
          context.SemanticModel,
          (ClassDeclarationSyntax)context.Node
        )
    );

    var powerUpCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: PowerUpsRepo.IsPowerUpSyntaxCandidate,
      transform: (GeneratorSyntaxContext context, CancellationToken _) =>
        PowerUpsRepo.GetPowerUp(
          context.SemanticModel,
          (ClassDeclarationSyntax)context.Node
        )
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
            keySelector: (powerUp) => powerUp.FullName,
            elementSelector: (powerUp) => powerUp
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
          "LOG", SourceText.From(Log.Contents, Encoding.UTF8)
        );
        _logsFlushed = true;
      }
    );
#endif
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
            id: Constants.SUPER_NODE_MISSING_NOTIFICATION_METHOD,
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
    var appliedPowerUps = new List<PowerUp>();

    // See if the node has any power-ups.
    foreach (var lifecycleHook in node.LifecycleHooks) {
      if (
        lifecycleHook is not PowerUpHook powerUpHook ||
        !item.PowerUps.ContainsKey(powerUpHook.FullName)
      ) {
        continue;
      }

      // Look up power up hook in the dictionary of all power ups. Fully
      // resolved power up names are used as the keys.
      var powerUp = item.PowerUps[powerUpHook.FullName];

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
              id: Constants.SUPER_NODE_INVALID_POWER_UP,
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

    var lifecycleInvocations = node.LifecycleHooks.Where(
      hook => hook is not PowerUpHook powerUpHook ||
        powerUps[powerUpHook.FullName].HasOnPowerUpMethod
    ).Select(
      hook => {
        if (hook is PowerUpHook powerUpHook) {
          return $"On{powerUps[powerUpHook.FullName].Name}(what);";
        }
        var lifecycleMethodHook = (LifecycleMethodHook)hook;
        return $"{lifecycleMethodHook.Name}(what);";
      }
    ).ToImmutableList();

    // Create a switch/case for every implemented notification handler, such
    // as OnReady, OnPhysicsProcess, etc.
    var handlers = new List<string>();
    if (node.NotificationHandlers.Length > 0) {
      handlers.Add("switch (what) {");
      foreach (var handler in node.NotificationHandlers) {
        var method = Constants.LifecycleMethods[handler];
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
    GenerationItem item, IList<PowerUp> appliedPowerUps
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
    SuperNode node, PowerUp powerUp
  ) {
    // Edit the pieces of the user's power-up needed to make it suitable to be
    // a partial class of the specific node script it's applied to.

    var tree = CSharpSyntaxTree.ParseText(powerUp.Source);

    var typeParameterSubstitutions = new Dictionary<string, string>();
    for (var i = 0; i < powerUp.TypeParameters.Length; i++) {
      var typeParameter = powerUp.TypeParameters[i];
      var correspondingPowerUpHook = node.PowerUpHooksByFullName[powerUp.FullName];
      typeParameterSubstitutions[typeParameter] = correspondingPowerUpHook.TypeArguments[i];
    }

    var root = (CompilationUnitSyntax)tree.GetRoot();
    var classDeclaration = (ClassDeclarationSyntax)root.Members.First();
    var interfaces = powerUp.Interfaces;

    // Strip [PowerUp] attribute off the class declaration
    var newClassDeclaration = classDeclaration.WithAttributeLists(
      SyntaxFactory.List(
        classDeclaration.AttributeLists.Where(
          attributeList => attributeList.Attributes.All(
            attribute => attribute.Name.ToString() != Constants.POWER_UP_ATTRIBUTE_NAME
          )
        )
      )
    )
    // Change power up name to the node's name.
    .WithIdentifier(SyntaxFactory.Identifier(node.Name))
    .WithTypeParameterList(null)
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

    var powerUpRewriter = new PowerUpRewriter(
      typeParameters: typeParameterSubstitutions.ToImmutableDictionary(),
      powerUpClassName: powerUp.Name,
      superNodeClassName: node.Name
    );

    // Rewrite the user's power up class tree to substitute type parameters
    // and static references to the power-up class with the specific node class
    tree = tree.WithRootAndOptions(
      powerUpRewriter.Visit(tree.GetRoot()), tree.Options
    );

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
}
