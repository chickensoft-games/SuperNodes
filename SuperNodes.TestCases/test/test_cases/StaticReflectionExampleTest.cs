namespace StaticReflectionExample;

using System;
using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp))]
public partial class MySuperNode : Node2D {
  [Export(PropertyHint.Range, "0, 100")]
  public int Probability { get; set; } = 50;

  public override partial void _Notification(int what);
}

[PowerUp]
public partial class MyPowerUp : Node2D {
  [Obsolete("MyName is obsolete — please use Identifier instead.")]
  public string MyName { get; set; } = nameof(MyPowerUp);

  public string Identifier { get; set; } = nameof(MyPowerUp);
}
