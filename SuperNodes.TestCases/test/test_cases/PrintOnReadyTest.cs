namespace SuperNodes.TestCases;

using Godot;
using SuperNodes.Types;

[SuperNode("PrintOnReady", typeof(MyPowerUp), "OtherGenerator")]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);

  public void OnReady()
    => GD.Print("PrintOnReady will have already printed out that I'm ready.");
}

// Pretend this is the implementation created by PrintOnReady source generator
public partial class MySuperNode {
  public void PrintOnReady(int what) {
    if (what == NotificationReady) {
      GD.Print($"{Name} is ready.");
    }
  }
}

public partial class MySuperNode {
  public void OtherGenerator(int what) { }
}

[PowerUp]
public partial class MyPowerUp {
  public void OnMyPowerUp(int what) { }
}
