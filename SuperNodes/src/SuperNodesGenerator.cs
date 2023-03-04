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
using SuperNodes.Common.Utils;
using SuperNodes.PowerUpsFeature;
using SuperNodes.SuperNodesFeature;

[Generator]
public partial class SuperNodesGenerator
  : ChickensoftGenerator, IIncrementalGenerator {
  public ICodeService CodeService { get; }
  public IPowerUpGeneratorService PowerUpGeneratorService { get; }
  public IPowerUpsRepo PowerUpsRepo { get; }
  public ISuperNodesRepo SuperNodesRepo { get; }
  public ISuperNodeGeneratorService SuperNodeGeneratorService { get; }
  public ISuperNodeGenerator SuperNodeGenerator { get; }
  public IPowerUpGenerator PowerUpGenerator { get; }
  public static Log Log { get; } = new Log();

#pragma warning disable IDE0052
  private static bool _logsFlushed;
#pragma warning restore IDE0052

  /// <summary>
  /// Parameterless constructor used by the .NET SDK tooling.
  /// </summary>
  public SuperNodesGenerator() {
    CodeService = new CodeService();
    PowerUpGeneratorService = new PowerUpGeneratorService();
    PowerUpsRepo = new PowerUpsRepo(CodeService);
    SuperNodesRepo = new SuperNodesRepo(CodeService);
    SuperNodeGeneratorService = new SuperNodeGeneratorService();
    SuperNodeGenerator = new SuperNodeGenerator(SuperNodeGeneratorService);
    PowerUpGenerator = new PowerUpGenerator(PowerUpGeneratorService);
  }

  public void Initialize(IncrementalGeneratorInitializationContext context) {
    // Debugger.Launch();

    _logsFlushed = false;
    Log.Clear();
    Log.Print("Initializing source generation");

    // We no longer need to output source code for attributes or property types.
    // It is expected the user will have a PackageReference to SuperNodes.Types.
    // Providing types in a separate package allows for SuperNodes to be
    // inspected across assemblies (something that could help with mods, dynamic
    // loading of scripts, level editors, etc).
    //
    // foreach (var postInitSource in Constants.PostInitializationSources) {
    //   context.RegisterPostInitializationOutput(
    //     (context) => context.AddSource(
    //       $"{postInitSource.Key}.g.cs",
    //       SourceText.From(
    //         postInitSource.Value.NormalizeLineEndings(),
    //         Encoding.UTF8
    //       )
    //     )
    //   );
    // }

    var superNodeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: (SyntaxNode node, CancellationToken _)
        => SuperNodesRepo.IsSuperNodeSyntaxCandidate(node),
      transform: (GeneratorSyntaxContext context, CancellationToken _) => {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        return SuperNodesRepo.GetSuperNode(
          classDeclaration,
          context.SemanticModel.GetDeclaredSymbol(classDeclaration)
        );
      }
    );

    var powerUpCandidates = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: (SyntaxNode node, CancellationToken _)
        => PowerUpsRepo.IsPowerUpSyntaxCandidate(node),
      transform: (GeneratorSyntaxContext context, CancellationToken _) => {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        return PowerUpsRepo.GetPowerUp(
          classDeclaration,
          context.SemanticModel.GetDeclaredSymbol(classDeclaration)
        );
      }
    );

    // Combine each godot node candidate with the list of power ups and the
    // compilation.
    //
    // This is absolutely disgusting (because combining results in tuples,
    // among other things), but it allows for performance (supposedly, if
    // you use cache-friendly values). I'm not really sure how cache-friendly
    // this generator is yet, but we'll get there.
    var generationItems = superNodeCandidates
      .Combine(
        powerUpCandidates.Collect().Select(
          (s, _) => s.ToImmutableDictionary(
            keySelector: (powerUp) => powerUp.FullName,
            elementSelector: (powerUp) => powerUp
          )
        )
      ).Select(
        (item, _) => new GenerationItem(
          SuperNode: item.Left,
          PowerUps: item.Right
        )
      );

    context.RegisterSourceOutput(
      source: generationItems,
      action: Execute
    );

    // When debugging SuperNodes, it can be nice to output a log file. Uncomment
    // the code below to allow for logging output.
    //
    // #if DEBUG
    //     // Very hacky way of only printing out one log file.
    //     var syntax = context.SyntaxProvider.CreateSyntaxProvider(
    //       predicate: (syntaxNode, _) => syntaxNode is CompilationUnitSyntax,
    //       transform: (syntaxContext, _) => syntaxContext.Node
    //     );
    //     context.RegisterImplementationSourceOutput(
    //       syntax,
    //       (ctx, _) => {
    //         if (_logsFlushed) { return; }
    //         ctx.AddSource(
    //           "LOG", SourceText.From(Log.Contents, Encoding.UTF8)
    //         );
    //         _logsFlushed = true;
    //       }
    //     );
    // #endif
  }

  public void Execute(
    SourceProductionContext context,
    GenerationItem item
  ) {
    var superNode = item.SuperNode;

    if (!superNode.HasPartialNotificationMethod) {
      context.ReportDiagnostic(
        Diagnostic.Create(
          new DiagnosticDescriptor(
            id: Constants.SUPER_NODE_MISSING_NOTIFICATION_METHOD,
            title: "Missing partial `_Notification` method signature.",
            messageFormat: "The SuperNode '{0}' is missing a partial " +
              "signature for `_Notification(int what)`. Please add the " +
              "following method signature to your class: " +
              "`public override partial void _Notification(int what);`",
            category: "SuperNode",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
          ),
          location: superNode.Location,
          superNode.Name
        )
      );
    }

    context.AddSource(
      $"{superNode.FilenamePrefix}.g.cs",
      SourceText.From(
        SuperNodeGenerator.GenerateSuperNode(item),
        Encoding.UTF8
      )
    );

    var powerUps = item.PowerUps;
    var appliedPowerUps = new List<PowerUp>();

    // See if the node has any power-ups.
    foreach (var lifecycleHook in superNode.LifecycleHooks) {
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
      var canApplyPowerUp = superNode.BaseClasses.Contains(powerUp.BaseClass);

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
            location: superNode.Location,
            powerUp.Name,
            superNode.Name,
            powerUp.BaseClass
          )
        );
        continue;
      }

      appliedPowerUps.Add(powerUp);

      context.AddSource(
        $"{superNode.FilenamePrefix}_{powerUp.Name}.g.cs",
        SourceText.From(
          PowerUpGenerator.GeneratePowerUp(powerUp, superNode),
          Encoding.UTF8
        )
      );
    }

    context.AddSource(
      $"{superNode.FilenamePrefix}_Reflection.g.cs",
      SourceText.From(
        SuperNodeGenerator.GenerateSuperNodeStatic(
          item, appliedPowerUps.ToImmutableArray()
        ),
        Encoding.UTF8
      )
    );
  }
}
