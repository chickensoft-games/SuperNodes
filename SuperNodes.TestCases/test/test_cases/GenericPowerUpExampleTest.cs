namespace GenericPowerUpExample;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp<string>))]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);

  public void OnReady() => System.Diagnostics.Debug.Assert(Value is not null);
}

[PowerUp]
public partial class MyPowerUp<T> : Node {
  public T Value { get; set; } = default!;

  public void OnMyPowerUp(int what) {
    if (what == NotificationReady) {
      if (Value is string) {
        GD.Print("You gave me a string!");
      }
      else if (Value is int) {
        GD.Print("You gave me an int!");
      }
      else {
        GD.Print("You gave me something else!");
      }
    }
  }
}
