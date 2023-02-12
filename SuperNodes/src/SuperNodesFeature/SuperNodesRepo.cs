namespace SuperNodes.SuperNodesFeature;

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
/// Handles logic for generating SuperNodes.
/// </summary>
public interface ISuperNodesRepo {
  /// <summary>Common operations needed for syntax nodes.</summary>
  ICodeService CodeService { get; }

  /// <summary>
  /// Determines whether or not a syntax node is a SuperNode.
  /// </summary>
  /// <param name="node">Syntax node to check.</param>
  /// <param name="_">Cancellation token (unused).</param>
  /// <returns>True if the syntax node is a class declaration with a SuperNode
  /// attribute.</returns>
  bool IsSuperNodeSyntaxCandidate(SyntaxNode node, CancellationToken _);

  /// <summary>
  /// Returns a model that represents a SuperNode based on the SuperNode syntax
  /// node candidate provided by the generation context.
  /// </summary>
  /// <param name="model">Semantic model.</param>
  /// <param name="classDeclaration">SuperNode class declaration syntax node.
  /// </param>
  /// <returns>A SuperNode model.</returns>
  SuperNode GetSuperNode(
    SemanticModel model,
    ClassDeclarationSyntax classDeclaration
  );
}

/// <summary>
/// Handles logic for generating SuperNodes.
/// </summary>
public class SuperNodesRepo : ISuperNodesRepo {
  public ICodeService CodeService { get; }

  /// <summary>
  /// Create a new PowerUpsRepo.
  /// </summary>
  /// <param name="syntaxOps">Common operations needed for syntax nodes.</param>
  public SuperNodesRepo(ICodeService syntaxOps) {
    CodeService = syntaxOps;
  }

  public bool IsSuperNodeSyntaxCandidate(
    SyntaxNode node, CancellationToken _
  ) =>
    node is ClassDeclarationSyntax classDeclaration &&
      classDeclaration.AttributeLists.SelectMany(
        list => list.Attributes
      ).Any(
      attribute => attribute.Name.ToString() == Constants.SUPER_NODE_ATTRIBUTE_NAME
    );

  public SuperNode GetSuperNode(
    SemanticModel model,
    ClassDeclarationSyntax classDeclaration
  ) {
    var symbol = model.GetDeclaredSymbol(classDeclaration);

    var name = symbol?.Name ?? classDeclaration.Identifier.ValueText;
    var @namespace = symbol is not null
      ? CodeService.GetContainingNamespace(symbol)
      : "";

    var baseClasses = symbol is null
        ? ImmutableArray<string>.Empty
        : CodeService.GetBaseClassHierarchy(symbol).ToImmutableArray();

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
    var lifecycleHooks = new List<IGodotNodeLifecycleHook>();
    var powerUpHooksByFullName = new Dictionary<string, PowerUpHook>();

    var attributes
      = symbol?.GetAttributes() ?? ImmutableArray<AttributeData>.Empty;
    var superNodeAttribute = attributes.FirstOrDefault(
      attribute =>
        attribute.AttributeClass?.Name == Constants.SUPER_NODE_ATTRIBUTE_NAME_FULL
    );

    if (superNodeAttribute is AttributeData attribute) {
      var args = attribute.ConstructorArguments;
      if (args.Length == 1) {
        // SuperNode attribute technically only requires 1 argument which
        // should be an array of strings.
        var arg = args[0];
        foreach (var constant in arg.Values) {
          var constantType = constant.Type;
          if (constantType?.Name == "String") {
            // Found a lifecycle method. This can be the name of a method
            // to call from another generator or a method from a PowerUp.
            var stringValue = (string)constant.Value!;
            lifecycleHooks.Add(new LifecycleMethodHook(stringValue));
          }
          else if (constantType?.Name == "Type") {
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
    }

    // skipSuperNodeAttribute:

    // Find any On[Notification] method handlers.
    var notificationHandlers = classDeclaration.Members.Where(
      member => member is MethodDeclarationSyntax method
        && Constants.LifecycleMethods.ContainsKey(method.Identifier.Text)
    )
    .Cast<MethodDeclarationSyntax>()
    .Select(method => method.Identifier.Text)
    .ToList();

    var members = symbol is not null
      ? symbol.GetMembers()
      : new ImmutableArray<ISymbol>();

    var propsAndFields = CodeService.GetPropsAndFields(members);

    var usings = symbol is not null
      ? CodeService.GetUsings(symbol)
      : ImmutableHashSet<string>.Empty;

    return new SuperNode(
      Namespace: @namespace,
      Name: name,
      Location: classDeclaration.GetLocation(),
      BaseClasses: baseClasses,
      LifecycleHooks: lifecycleHooks.ToImmutableArray(),
      PowerUpHooksByFullName: powerUpHooksByFullName.ToImmutableDictionary(),
      NotificationHandlers: notificationHandlers.ToImmutableArray(),
      HasPartialNotificationMethod: hasPartialNotificationMethod,
      HasNotificationMethodHandler: hasNotificationMethodHandler,
      PropsAndFields: propsAndFields,
      Usings: usings
    );
  }
}
