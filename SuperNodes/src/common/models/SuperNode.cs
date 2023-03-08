namespace SuperNodes.Common.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// Represents a SuperNode. A SuperNode is a Godot node script class defined by
/// a game developer. SuperNodes generate static tables containing information
/// about their fields and properties so that reflection is not needed to
/// access that information, in addition to calling declared lifecycle method
/// handlers that may be provided by other source generators or partial class
/// implementations. SuperNodes also allow PowerUps to be applied to them,
/// similar to mixins in other languages (and somewhat like templates in C++).
/// </summary>
/// <param name="Namespace">Fully qualified namespace containing the node
/// (without the <c>global::</c> prefix).</param>
/// <param name="Name">Name of the Godot Node (not fully qualified). Combine
/// with <paramref name="Namespace" /> to determine the fully resolved name.
/// </param>
/// <param name="Location">The location of the class declaration syntax node
/// that corresponds to the SuperNode.</param>
/// <param name="BaseClasses">Array of fully qualified base types (base class
/// and implemented interfaces).</param>
/// <param name="LifecycleHooks">Lifecycle hooks that need to be invoked from
/// the SuperNode's generated <c>_Notification(int what)</c> method.
/// Lifecycle hooks can either be the string names of declared methods to invoke
/// (as specified in the SuperNode attribute) or a PowerUp to apply to the
/// SuperNode.</param>
/// <param name="PowerUpHooksByFullName">Applied PowerUps (as specified in the
/// SuperNode attribute), keyed by the fully resolved name of the PowerUp.
/// </param>
/// <param name="NotificationHandlers">Notification handlers found in the
/// script class, such as <c>OnEnterTree</c>, <c>OnProcess(double delta)</c>,
/// <c>OnReady</c>, etc.</param>
/// <param name="HasPartialNotificationMethod">True if the SuperNode has a stub
/// for <c>public override partial void _Notification(int what)</c>. Since
/// SuperNodes cannot work without this stub, we track whether or not it exists
/// so we can issue a warning at generation time if it is missing.</param>
/// <param name="HasOnNotificationMethodHandler">SuperNode scripts can implement
/// a special notification handler, <c>OnNotification(int what)</c> that will
/// be called whenever any notification is received. Since this is a special
/// case, we track it separately.</param>
/// <param name="PropsAndFields">Information about properties and fields
/// declared in the SuperNode. We track these fields so that static reflection
/// tables can be generated at build time, allowing scripts and PowerUps to
/// introspect their properties and fields (and the attributes applied to them)
/// without having to use reflection.</param>
/// <param name="Usings">All of the using imports defined for the SuperNode
/// script.</param>
public record SuperNode(
  string? Namespace,
  string Name,
  Location Location,
  ImmutableArray<string> BaseClasses,
  ImmutableArray<IGodotNodeLifecycleHook> LifecycleHooks,
  ImmutableDictionary<string, PowerUpHook> PowerUpHooksByFullName,
  ImmutableArray<string> NotificationHandlers,
  bool HasPartialNotificationMethod,
  bool HasOnNotificationMethodHandler,
  ImmutableArray<PropOrField> PropsAndFields,
  IImmutableSet<string> Usings
) {
  /// <summary>
  /// Filename prefix to use when generating the SuperNode's related
  /// implementation files.
  /// </summary>
  public string FilenamePrefix
    => Namespace is not "" ? $"{Namespace}.{Name}" : Name;
}
