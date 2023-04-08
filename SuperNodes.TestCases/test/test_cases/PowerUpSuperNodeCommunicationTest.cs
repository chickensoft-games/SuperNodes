namespace PowerUpSuperNodeCommunication;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp), typeof(MyGenericPowerUp<string>))]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);

  public void OnReady() { }
}

#pragma warning disable IDE0002
[PowerUp]
public partial class MyPowerUp : Node {
  [PowerUpIgnore]
  public static string NameToGreet { get; set; } = default!;

  public void OnMyPowerUp(int what) {
    if (what == NotificationReady) {
      GD.Print($"Hello, {MyPowerUp.NameToGreet}!");
    }
  }
}

[PowerUp]
public partial class MyGenericPowerUp<T> : Node {
#pragma warning disable CA1000
  public static T Thing { get; set; } = default!;
#pragma warning restore CA1000

  public void OnMyGenericPowerUp(int what) {
    if (what == NotificationReady) {
      GD.Print($"I have been given a thing: {MyGenericPowerUp<T>.Thing}");
    }
  }
}
#pragma warning restore IDE0002
