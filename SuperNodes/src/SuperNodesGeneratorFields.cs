namespace SuperNodes;

using System;
using System.Collections.Generic;

public partial class SuperNodesGenerator {
  public const string VOID = "void";
  public static string[] Args(params string[] args) => args;
  public static readonly string[] NoArgs = Array.Empty<string>();

  /// <summary>
  /// Map of lifecycle method handlers that script classes can implement to the
  /// corresponding Godot.Node / Godot.Object notification.
  /// </summary>
  /// <returns></returns>
  public static readonly Dictionary<string, LifecycleMethod> LifecycleMethods = new() {
    // Godot.Object Notifications
    ["OnPostinitialize"] = new LifecycleMethod(
      "NotificationPostinitialize", VOID, NoArgs
    ),
    ["OnPredelete"] = new LifecycleMethod(
      "NotificationPredelete", VOID, NoArgs
    ),
    // Godot.Node Notifications
    ["OnEnterTree"] = new LifecycleMethod(
      "NotificationEnterTree", VOID, NoArgs
    ),
    ["OnWmWindowFocusIn"] = new LifecycleMethod(
      "NotificationWmWindowFocusIn", VOID, NoArgs
    ),
    ["OnWmWindowFocusOut"] = new LifecycleMethod(
      "NotificationWmWindowFocusOut", VOID, NoArgs
    ),
    ["OnWmCloseRequest"] = new LifecycleMethod(
      "NotificationWmCloseRequest", VOID, NoArgs
    ),
    ["OnWmSizeChanged"] = new LifecycleMethod(
      "NotificationWmSizeChanged", VOID, NoArgs
    ),
    ["OnWmDpiChange"] = new LifecycleMethod(
      "NotificationWmDpiChange", VOID, NoArgs
    ),
    ["OnVpMouseEnter"] = new LifecycleMethod(
      "NotificationVpMouseEnter", VOID, NoArgs
    ),
    ["OnVpMouseExit"] = new LifecycleMethod(
      "NotificationVpMouseExit", VOID, NoArgs
    ),
    ["OnOsMemoryWarning"] = new LifecycleMethod(
      "NotificationOsMemoryWarning", VOID, NoArgs
    ),
    ["OnTranslationChanged"] = new LifecycleMethod(
      "NotificationTranslationChanged", VOID, NoArgs
    ),
    ["OnWmAbout"] = new LifecycleMethod(
      "NotificationWmAbout", VOID, NoArgs
    ),
    ["OnCrash"] = new LifecycleMethod(
      "NotificationCrash", VOID, NoArgs
    ),
    ["OnOsImeUpdate"] = new LifecycleMethod(
      "NotificationOsImeUpdate", VOID, NoArgs
    ),
    ["OnApplicationResumed"] = new LifecycleMethod(
      "NotificationApplicationResumed", VOID, NoArgs
    ),
    ["OnApplicationPaused"] = new LifecycleMethod(
      "NotificationApplicationPaused", VOID, NoArgs
    ),
    ["OnApplicationFocusIn"] = new LifecycleMethod(
      "NotificationApplicationFocusIn", VOID, NoArgs
    ),
    ["OnApplicationFocusOut"] = new LifecycleMethod(
      "NotificationApplicationFocusOut", VOID, NoArgs
    ),
    ["OnTextServerChanged"] = new LifecycleMethod(
      "NotificationTextServerChanged", VOID, NoArgs
    ),
    ["OnWmMouseExit"] = new LifecycleMethod(
      "NotificationWmMouseExit", VOID, NoArgs
    ),
    ["OnWmMouseEnter"] = new LifecycleMethod(
      "NotificationWmMouseEnter", VOID, NoArgs
    ),
    ["OnWmGoBackRequest"] = new LifecycleMethod(
      "NotificationWmGoBackRequest", VOID, NoArgs
    ),
    ["OnEditorPreSave"] = new LifecycleMethod(
      "NotificationEditorPreSave", VOID, NoArgs
    ),
    ["OnExitTree"] = new LifecycleMethod(
      "NotificationExitTree", VOID, NoArgs
    ),
    ["OnMovedInParent"] = new LifecycleMethod(
      "NotificationMovedInParent", VOID, NoArgs
    ),
    ["OnReady"] = new LifecycleMethod(
      "NotificationReady", VOID, NoArgs
    ),
    ["OnEditorPostSave"] = new LifecycleMethod(
      "NotificationEditorPostSave", VOID, NoArgs
    ),
    ["OnUnpaused"] = new LifecycleMethod(
      "NotificationUnpaused", VOID, NoArgs
    ),
    ["OnPhysicsProcess"] = new LifecycleMethod(
      "NotificationPhysicsProcess",
      VOID,
      Args("GetPhysicsProcessDeltaTime()")
    ),
    ["OnProcess"] = new LifecycleMethod(
      "NotificationProcess",
      VOID,
      Args("GetProcessDeltaTime()")
    ),
    ["OnParented"] = new LifecycleMethod(
      "NotificationParented", VOID, NoArgs
    ),
    ["OnUnparented"] = new LifecycleMethod(
      "NotificationUnparented", VOID, NoArgs
    ),
    ["OnPaused"] = new LifecycleMethod(
      "NotificationPaused", VOID, NoArgs
    ),
    ["OnDragBegin"] = new LifecycleMethod(
      "NotificationDragBegin", VOID, NoArgs
    ),
    ["OnDragEnd"] = new LifecycleMethod(
      "NotificationDragEnd", VOID, NoArgs
    ),
    ["OnPathRenamed"] = new LifecycleMethod(
      "NotificationPathRenamed", VOID, NoArgs
    ),
    ["OnInternalProcess"] = new LifecycleMethod(
      "NotificationInternalProcess", VOID, NoArgs
    ),
    ["OnInternalPhysicsProcess"] = new LifecycleMethod(
      "NotificationInternalPhysicsProcess", VOID, NoArgs
    ),
    ["OnPostEnterTree"] = new LifecycleMethod(
      "NotificationPostEnterTree", VOID, NoArgs
    ),
    ["OnDisabled"] = new LifecycleMethod(
      "NotificationDisabled", VOID, NoArgs
    ),
    ["OnEnabled"] = new LifecycleMethod(
      "NotificationEnabled", VOID, NoArgs
    ),
    ["OnSceneInstantiated"] = new LifecycleMethod(
      "NotificationSceneInstantiated", VOID, NoArgs
    ),
  };

  public const string SUPER_NODE_ATTRIBUTE_NAME = "SuperNode";
  public const string SUPER_NODE_ATTRIBUTE_NAME_FULL = "SuperNodeAttribute";
  public const string SUPER_NODE_ATTRIBUTE_SOURCE = """
  using System;

  /// <summary>
  /// SuperNode attribute. Add this to a Godot node script class to use
  /// functionality from other compatible source generators.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  internal class SuperNodeAttribute : Attribute {
    /// <summary>
    /// Source generator lifecycle methods to invoke from
    /// <see cref="Godot.Object._Notification(long)" />.
    /// </summary>
    internal string[] Methods { get; set; }

    /// <summary>
    /// SuperNode attribute. Add this to a Godot node script class to use
    /// functionality from other compatible source generators.
    /// </summary>
    internal SuperNodeAttribute() => Methods = Array.Empty<string>();

    /// <summary>
    /// SuperNode attribute. Add this to a Godot node script class to use
    /// functionality from other compatible source generators.
    /// <br />
    /// Compatible source generator lifecycle methods will be invoked from
    /// <see cref="Godot.Object._Notification(long)"/> in the order specified
    /// here.
    /// </summary>
    /// <param name="methods">Compatible source generator lifecycle method names.
    /// </param>
    internal SuperNodeAttribute(params string[] methods) => Methods = methods;
  }
  """;

  public const string POWER_UP_ATTRIBUTE_NAME = "PowerUp";
  public const string POWER_UP_ATTRIBUTE_NAME_FULL = "PowerUpAttribute";
  public const string POWER_UP_ATTRIBUTE_SOURCE = """
  using System;

  /// <summary>
  /// Power-up attribute. Add this to a class to create a mixin.
  /// </summary>
  internal class PowerUpAttribute : Attribute {
    /// <summary>PowerUp attribute. Add this to a class to create a mixin.
    /// </summary>
    internal PowerUpAttribute() { }
  }
  """;

  public const string SUPER_NODE_MISSING_NOTIFICATION_METHOD
    = "SUPER_NODE_MISSING_NOTIFICATION_METHOD";

  public const string SUPER_NODE_INVALID_POWER_UP
    = "SUPER_NODE_INVALID_POWER_UP";
}
