namespace PowerUpConstraints;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp))]
public partial class MySuperNode : Node2D {
  public override partial void _Notification(int what);
}

[PowerUp]
public partial class MyPowerUp : Node2D { }
