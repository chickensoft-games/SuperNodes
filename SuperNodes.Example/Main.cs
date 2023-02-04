namespace SuperNodes.Example;

using System;
using Godot;
using SharedPowerUps;

[SuperNode(nameof(MyPowerUp), nameof(SharedPowerUp))]
public partial class MyNode : Node {
  public string ScriptProperty { get; set; } = "hello";

  public override partial void _Notification(long what);

  public void OnReady() { }

  public void OnProcess(double _) { }
}

public interface IMyPowerUpGeneric<TSomethingA, TSomethingB> { }
public interface IMyPowerUp { }

[PowerUp]
public abstract partial class MyPowerUp : Node, IMyPowerUp {
  private readonly struct MyTypeReceiver : ITypeReceiver<bool> {
    public Node Node { get; }

    public MyTypeReceiver(Node node) {
      Node = node;
    }

    public bool Receive<T>() => Node is IEquatable<T>;
  }

  public string AddedProperty { get; set; } = "";

  public void OnMyPowerUp(long what) {
    if (what == Node.NotificationReady) {
      var receiver = new MyTypeReceiver(this);
      GD.Print(
        "is IEquatable<string>? " +
        GetScriptPropertyOrFieldType("AddedProperty", receiver)
      );
    }
  }

  // Stubs for the generated static reflection tables generated on SuperNodes

  internal static ScriptPropertyOrField[] PropertiesAndFields { get; }
    = default!;
  internal static TResult GetScriptPropertyOrFieldType<TResult>(
    string scriptProperty, ITypeReceiver<TResult> receiver
  ) => default!;
}
