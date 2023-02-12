namespace SuperNodes;

using System.Collections.Generic;
using System.Collections.Immutable;

public partial class SuperNodesGenerator {
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
      /// Source generator lifecycle methods and/or PowerUps to invoke from
      /// <see cref="Godot.Object._Notification(long)" />.
      /// </summary>
      internal object[] Args { get; }

      /// <summary>
      /// SuperNode attribute. Add this to a Godot node script class to use
      /// functionality from other compatible source generators.
      /// </summary>
      internal SuperNodeAttribute() => Args = Array.Empty<object>();

      /// <summary>
      /// SuperNode attribute. Add this to a Godot node script class to use
      /// functionality from other compatible source generators.
      /// <br />
      /// Compatible source generator lifecycle methods or PowerUps that will
      /// be invoked from
      /// <see cref="Godot.Object._Notification(long)"/> in the order specified
      /// here.
      /// </summary>
      /// <param name="args">Compatible source generator lifecycle method
      /// names or the types of PowerUps.</param>
      internal SuperNodeAttribute(params object[] args) => Args = args;
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

  public const string STATIC_REFLECTION_SOURCE = """
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    /// <summary>
    /// Object containing a single method that can receive a generic type argument.
    /// Sometimes, it is desirable for a PowerUp to be able to access the type of a
    /// field or property on the node it is applied to. This interface allows
    /// PowerUps to create a receiver object for the type in a generic context,
    /// which would otherwise require a source generator or reflection to obtain.
    /// <br />
    /// This interface is supplied by SuperNodes.
    /// </summary>
    /// <typeparam name="TResult">Return value of the receive method.</typeparam>
    internal interface ITypeReceiver<TResult> {
      /// <summary>
      /// Generic method which receives the type of the field or property.
      /// </summary>
      /// <typeparam name="T">Type of the field or property, as a generic type
      /// variable.</typeparam>
      /// <returns>A value of type <typeparamref name="TResult" />.</returns>
      TResult Receive<T>();
    }

    /// <summary>
    /// A description of an attribute used within a node script. This model is
    /// supplied by SuperNodes.
    /// </summary>
    /// <param name="Name">Name of the attribute, without the "Attribute" suffix.
    /// </param>
    /// <param name="NameFullyQualified">Fully qualified name of the attribute
    /// class.</param>
    /// <param name="ArgumentExpressions">Expressions (as plain text) given for each
    /// argument.</param>
    internal record ScriptAttributeDescription(
      string Name,
      Type Type,
      ImmutableArray<dynamic> ArgumentExpressions
    );

    /// <summary>
    /// A description of a property or field used within a node script. This model
    /// is supplied by SuperNodes.
    /// </summary>
    /// <param name="Name">Name of the field or property.</param>
    /// <param name="Type">Type of the field or property.</param>
    /// <param name="Attributes">Descriptions of the attributes applied to the field
    /// or property.</param>
    internal record struct ScriptPropertyOrField(
      string Name,
      Type Type,
      bool IsField,
      IDictionary<string, ScriptAttributeDescription> Attributes
    );
    """;

  public const string STATIC_REFLECTION_NAME = "StaticReflection";
}
