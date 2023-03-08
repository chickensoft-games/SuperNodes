namespace SuperNodes.SuperNodesFeature;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
using SuperNodes.Common.Utils;

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

  /// <summary>
  /// Create a new PowerUpsRepo.
  /// </summary>
  /// <param name="codeService">Common code operations for syntax nodes and
  /// semantic model symbols.</param>
  public SuperNodesRepo(
    ICodeService codeService
  ) {
    CodeService = codeService;
  }

  public bool IsSuperNodeSyntaxCandidate(SyntaxNode node)
    => node is ClassDeclarationSyntax classDeclaration &&
      classDeclaration.AttributeLists.SelectMany(
        list => list.Attributes
      ).Any(
      attribute => attribute.Name.ToString()
        == Constants.SUPER_NODE_ATTRIBUTE_NAME
    );

  public SuperNode GetSuperNode(
    ClassDeclarationSyntax classDeclaration,
    INamedTypeSymbol? symbol
  ) {
    var name = CodeService.GetName(symbol, classDeclaration);
    var @namespace = CodeService.GetContainingNamespace(symbol);
    var baseClasses = CodeService.GetBaseClassHierarchy(symbol);

    // Make sure the SuperNode declares the following method:
    // `public override partial void _Notification(int what);`
    var hasPartialNotificationMethod
      = CodeService.HasPartialNotificationMethod(
        classDeclaration.Members
      );

    // We want to see if the script implements OnNotification(int). It's
    // a special case since it has to be called on any notification.
    var hasOnNotificationMethodHandler
      = CodeService.HasOnNotificationMethodHandler(
        classDeclaration.Members
      );

    var superNodeAttribute = CodeService.GetAttribute(
      symbol,
      Constants.SUPER_NODE_ATTRIBUTE_NAME_FULL
    );

    var lifecycleHooksResponse = CodeService.GetLifecycleHooks(
      superNodeAttribute
    );

    // Find any On[Notification] method handlers.
    var notificationHandlers = classDeclaration.Members
      .OfType<MethodDeclarationSyntax>().Where(
      member => Constants.LifecycleMethods.ContainsKey(
        member.Identifier.ValueText
      )
    )
    .Select(method => method.Identifier.ValueText)
    .ToImmutableArray();

    var members = CodeService.GetMembers(symbol);
    var usings = CodeService.GetUsings(symbol);

    return new SuperNode(
      Namespace: @namespace,
      Name: name,
      Location: classDeclaration.GetLocation(),
      BaseClasses: baseClasses,
      LifecycleHooks: lifecycleHooksResponse.LifecycleHooks,
      PowerUpHooksByFullName: lifecycleHooksResponse.PowerUpHooksByFullName,
      NotificationHandlers: notificationHandlers,
      HasPartialNotificationMethod: hasPartialNotificationMethod,
      HasOnNotificationMethodHandler: hasOnNotificationMethodHandler,
      PropsAndFields: CodeService.GetPropsAndFields(members),
      Usings: usings
    );
  }
}
