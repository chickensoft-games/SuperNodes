namespace ImplicitInterfacePropertyGetter;

using Godot;
using SuperNodes.Types;

[SuperNode]
public partial class ExampleNode : Node, IExampleNode {
  public override partial void _Notification(int what);

  public void OnReady() { }

  public double Value => 0.0;
}

public interface IExampleNode {
  public double Value { get; }
}
