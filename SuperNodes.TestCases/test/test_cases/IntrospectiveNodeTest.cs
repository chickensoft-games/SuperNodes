namespace IntrospectiveExample;

using System.Collections.Immutable;
using System.Linq;
using Godot;
using SuperNodes.Types;

[SuperNode(typeof(IntrospectivePowerUp))]
public partial class IntrospectiveNode : Node2D {
  public override partial void _Notification(int what);

  [Export(PropertyHint.MultilineText)]
  public string MyDescription { get; set; } = nameof(IntrospectiveNode);
}

[PowerUp]
public abstract partial class IntrospectivePowerUp : Node {
  // These stubs won't be copied over into SuperNode implementations.

  [PowerUpIgnore]
  public ImmutableDictionary<string, ScriptPropertyOrField>
    PropertiesAndFields { get; } =
      ImmutableDictionary<string, ScriptPropertyOrField>.Empty;

  [PowerUpIgnore]
  public static TResult ReceiveScriptPropertyOrFieldType<TResult>(
    string scriptProperty, ITypeReceiver<TResult> receiver
  ) => default!;

  [PowerUpIgnore]
  public abstract TResult GetScriptPropertyOrFieldType<TResult>(
    string scriptProperty, ITypeReceiver<TResult> receiver
  );

  [PowerUpIgnore]
  public abstract dynamic GetScriptPropertyOrField(string scriptProperty);

  [PowerUpIgnore]
  public abstract void SetScriptPropertyOrField(
    string scriptProperty, dynamic value
  );

  // A type receiver which checks the type of a value when the reified type
  // is given to its Receive method.
  private class CheckValueType : ITypeReceiver<bool> {
    public dynamic Value { get; }

    public CheckValueType(dynamic value) {
      Value = value;
    }

    public bool Receive<T>() => Value is T;
  }

  public void OnIntrospectivePowerUp(int what) {
    if (what == NotificationReady) {
      var numberOfPropsAndFields = PropertiesAndFields.Count;
      GD.Print($"I have {numberOfPropsAndFields} properties and fields.");

      if (numberOfPropsAndFields > 0) {
        var prop = PropertiesAndFields.First();
        var myData = "hello, world"!;
        var myDataIsSameTypeAsProp = GetScriptPropertyOrFieldType(
          prop.Key, new CheckValueType(myData)
        );
      }
    }
  }
}
