namespace SuperNodes.SuperNodesFeature;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
using SuperNodes.SuperNodesFeature.Models;
using SuperNodes.SuperNodesFeature.Services;

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
  /// <returns>True if the syntax node is a class declaration with a SuperNode
  /// attribute.</returns>
  bool IsSuperNodeSyntaxCandidate(SyntaxNode node);

  /// <summary>
  /// Returns a model that represents a SuperNode based on the SuperNode syntax
  /// node candidate provided by the generation context.
  /// </summary>
  /// <param name="classDeclaration">SuperNode class declaration syntax node.
  /// </param>
  /// <param name="symbol">Named type symbol representing the class declaration
  /// syntax node, if any.</param>
  /// <returns>A SuperNode model.</returns>
  SuperNode GetSuperNode(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  );
}

/// <summary>
/// Handles logic for generating SuperNodes.
/// </summary>
public class SuperNodesRepo : ISuperNodesRepo {
  public ICodeService CodeService { get; }
  public ISuperNodesCodeService SuperNodesCodeService { get; }

  /// <summary>
  /// Create a new PowerUpsRepo.
  /// </summary>
  /// <param name="codeService">Common code operations for syntax nodes and
  /// semantic model symbols.</param>
  /// <param name="superNodesCodeService">SuperNodes code service.</param>
  public SuperNodesRepo(
    ICodeService codeService,
    ISuperNodesCodeService superNodesCodeService
  ) {
    CodeService = codeService;
    SuperNodesCodeService = superNodesCodeService;
  }

  public bool IsSuperNodeSyntaxCandidate(SyntaxNode node)
    => node is ClassDeclarationSyntax classDeclaration &&
      classDeclaration.AttributeLists.SelectMany(
        list => list.Attributes
      ).Any(
      attribute => attribute.Name.ToString() == Constants.SUPER_NODE_ATTRIBUTE_NAME
    );

  public SuperNode GetSuperNode(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  ) {
    var name = symbol?.Name ?? classDeclaration.Identifier.ValueText;
    var @namespace = symbol is not null
      ? CodeService.GetContainingNamespace(symbol)
      : "";

    var baseClasses = symbol is null
        ? ImmutableArray<string>.Empty
        : CodeService.GetBaseClassHierarchy(symbol);

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
    var attributes
      = symbol?.GetAttributes() ?? ImmutableArray<AttributeData>.Empty;
    var superNodeAttribute = attributes.FirstOrDefault(
      attribute => attribute.AttributeClass?.Name ==
          Constants.SUPER_NODE_ATTRIBUTE_NAME_FULL
    );

    var lifecycleHooksResponse = superNodeAttribute is not null
      ? SuperNodesCodeService.GetLifecycleHooks(superNodeAttribute)
      : LifecycleHooksResponse.Empty;

    // skipSuperNodeAttribute:

    // Find any On[Notification] method handlers.
    var notificationHandlers = classDeclaration.Members.Where(
      member => member is MethodDeclarationSyntax method
        && Constants.LifecycleMethods.ContainsKey(method.Identifier.Text)
    )
    .Cast<MethodDeclarationSyntax>()
    .Select(method => method.Identifier.Text)
    .ToList();

    var members = symbol?.GetMembers() ?? ImmutableArray<ISymbol>.Empty;

    var usings = symbol is not null
      ? CodeService.GetUsings(symbol)
      : ImmutableHashSet<string>.Empty;

    return new SuperNode(
      Namespace: @namespace,
      Name: name,
      Location: classDeclaration.GetLocation(),
      BaseClasses: baseClasses,
      LifecycleHooks: lifecycleHooksResponse.LifecycleHooks,
      PowerUpHooksByFullName: lifecycleHooksResponse.PowerUpHooksByFullName,
      NotificationHandlers: notificationHandlers.ToImmutableArray(),
      HasPartialNotificationMethod: hasPartialNotificationMethod,
      HasNotificationMethodHandler: hasNotificationMethodHandler,
      PropsAndFields: CodeService.GetPropsAndFields(members),
      Usings: usings
    );
  }
}
