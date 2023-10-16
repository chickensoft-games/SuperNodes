namespace NullableReferenceTypeMember;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp))]
public partial class ExampleNode : Node {
  public override partial void _Notification(int what);
}

public class MyObject { }


[PowerUp]
public partial class MyPowerUp : Node {
  public MyObject? SomeObj { get; set; }
}
