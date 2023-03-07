namespace LifecycleExample;

using Godot;
using SuperNodes.Types;

[SuperNode(nameof(MySuperNode.MyLifecycleHook))]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);
}

// Pretend this implementation is created by another source generator
public partial class MySuperNode {
  public void MyLifecycleHook(int what) {
    if (what == NotificationReady) {
      GD.Print($"{Name} is ready.");
    }
  }
}
