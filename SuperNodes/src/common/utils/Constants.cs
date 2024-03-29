namespace SuperNodes.Common.Utils;

using System.Collections.Generic;
using System.Collections.Immutable;
using SuperNodes.Common.Models;

public class Constants {
  /// <summary>Spaces per tab. Adjust to your generator's liking.</summary>
  public static int SPACES_PER_INDENT = 2;

  public const string DEFAULT_BASE_CLASS = "global::Godot.Object";

  public static string BaseClass { get; set; } = DEFAULT_BASE_CLASS;

  /// <summary>
  /// Represents the void return type for a lifecycle method.
  /// </summary>
  public const string VOID = "void";

  /// <summary>
  /// Creates an immutable array of arguments for a lifecycle method. A
  /// convenience method to avoid having to create an array literal every time.
  /// </summary>
  /// <param name="args">Lifecycle method arguments.</param>
  public static ImmutableArray<string> Args(params string[] args)
    => args.ToImmutableArray();

  /// <summary>
  /// Represents the lack of arguments for a lifecycle method.
  /// </summary>
  public static readonly ImmutableArray<string> NoArgs
    = ImmutableArray<string>.Empty;

  /// <summary>
  /// Map of lifecycle method handlers that script classes can implement to the
  /// corresponding Godot.Node / Godot.Object notification.
  /// </summary>
  public static readonly Dictionary<string, LifecycleMethod>
    LifecycleMethods = new() {
      // Godot.Object Notifications
      ["OnPostinitialize"] = new LifecycleMethod(
        "NotificationPostinitialize", VOID, NoArgs
      ),
      ["OnPredelete"] = new LifecycleMethod(
        "NotificationPredelete", VOID, NoArgs
      ),
      // Godot.Object Notifications
      ["OnEnterTree"] = new LifecycleMethod(
        "NotificationEnterTree", VOID, NoArgs
      ),
      ["OnWMWindowFocusIn"] = new LifecycleMethod(
        "NotificationWMWindowFocusIn", VOID, NoArgs
      ),
      ["OnWMWindowFocusOut"] = new LifecycleMethod(
        "NotificationWMWindowFocusOut", VOID, NoArgs
      ),
      ["OnWMCloseRequest"] = new LifecycleMethod(
        "NotificationWMCloseRequest", VOID, NoArgs
      ),
      ["OnWMSizeChanged"] = new LifecycleMethod(
        "NotificationWMSizeChanged", VOID, NoArgs
      ),
      ["OnWMDpiChange"] = new LifecycleMethod(
        "NotificationWMDpiChange", VOID, NoArgs
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
      ["OnWMAbout"] = new LifecycleMethod(
        "NotificationWMAbout", VOID, NoArgs
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
      ["OnWMMouseExit"] = new LifecycleMethod(
        "NotificationWMMouseExit", VOID, NoArgs
      ),
      ["OnWMMouseEnter"] = new LifecycleMethod(
        "NotificationWMMouseEnter", VOID, NoArgs
      ),
      ["OnWMGoBackRequest"] = new LifecycleMethod(
        "NotificationWMGoBackRequest", VOID, NoArgs
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
  public const string SUPER_OBJECT_ATTRIBUTE_NAME = "SuperObject";
  public const string SUPER_OBJECT_ATTRIBUTE_NAME_FULL = "SuperObjectAttribute";

  public const string POWER_UP_ATTRIBUTE_NAME
    = "PowerUp";
  public const string POWER_UP_ATTRIBUTE_NAME_FULL
    = "PowerUpAttribute";

  public const string POWER_UP_IGNORE_ATTRIBUTE_NAME
    = "PowerUpIgnore";
  public const string POWER_UP_IGNORE_ATTRIBUTE_NAME_FULL
    = "PowerUpIgnoreAttribute";
  public const string POWER_UP_IGNORE_ATTRIBUTE_NAME_FULLY_QUALIFIED
    = "global::SuperNodes.Types.PowerUpIgnoreAttribute";

  public const string SUPER_NODE_MISSING_NOTIFICATION_METHOD
    = "SUPER_NODE_MISSING_NOTIFICATION_METHOD";

  public const string SUPER_NODE_INVALID_POWER_UP
    = "SUPER_NODE_INVALID_POWER_UP";

  /// <summary>
  /// A dictionary of source code that must be injected into the compilation
  /// regardless of whether or not the user has taken advantage of any of the
  /// other features of this source generator.
  /// </summary>
  public static readonly ImmutableDictionary<string, string>
    PostInitializationSources = new Dictionary<string, string>()
      .ToImmutableDictionary();
}
