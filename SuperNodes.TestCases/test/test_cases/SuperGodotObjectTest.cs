namespace SuperGodotObject;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp))]
public partial class SuperGodotObject : GodotObject {
  public override partial void _Notification(int what);
}

[PowerUp]
public partial class MyPowerUp : GodotObject { }
