namespace SuperNodes.SuperNodesFeature.Models;

using System.Collections.Immutable;
using SuperNodes.Common.Models;

/// <summary>
/// Represents lifecycle hooks determined from a SuperNode attribute while the
/// semantic model is available.
/// </summary>
/// <param name="LifecycleHooks">Lifecycle hooks, in order of their appearance.
/// </param>
/// <param name="PowerUpHooksByFullName">PowerUp hooks, keyed by their fully
/// resolved name.</param>
public record LifecycleHooksResponse(
  ImmutableArray<IGodotNodeLifecycleHook> LifecycleHooks,
  ImmutableDictionary<string, PowerUpHook> PowerUpHooksByFullName
) {
  public static LifecycleHooksResponse Empty = new(
    ImmutableArray<IGodotNodeLifecycleHook>.Empty,
    ImmutableDictionary<string, PowerUpHook>.Empty
  );
}
