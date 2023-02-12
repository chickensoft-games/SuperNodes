namespace SuperNodes.Models;

using System.Collections.Immutable;

/// <summary>
/// Represents a lifecycle method on a node script (i.e., a method that should
/// be invoked when a Godot node or object receives a notification from the
/// Godot engine).
/// </summary>
/// <param name="GodotNotification">The name of the Godot node or object
/// notification.</param>
/// <param name="ReturnType">The return type of the method that handles the
/// Godot node or object notification.</param>
/// <param name="Args">Arguments that should be given to the notification
/// handler.</param>
public readonly record struct LifecycleMethod(
  string GodotNotification,
  string ReturnType,
  ImmutableArray<string> Args
);
