namespace ExplicitInterfaceImplementationExample;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp<int>))]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);

  public void OnReady() { }
}

[PowerUp]
public partial class MyPowerUp<T> : Node, IMyPowerUp<T> {
  T IMyPowerUp<T>.Value { get; } = default!;
}

public interface IMyPowerUp<T> {
  T Value { get; }
}
