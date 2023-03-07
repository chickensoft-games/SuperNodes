namespace SimpleExample;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(ExamplePowerUp))]
public partial class ExampleNode : Node {
  public override partial void _Notification(int what);

  public void OnReady() => SomeMethod();

  public void OnProcess(double delta) => SomeMethod();

  public void SomeMethod() {
    var d = GetProcessDeltaTime();
    if (LastNotification == NotificationReady) {
      GD.Print("We were getting ready.");
    }
    else if (LastNotification == NotificationProcess) {
      GD.Print("We were processing a frame.");
    }
  }
}

// A PowerUp that logs some of the main lifecycle events of a node.
[PowerUp]
public partial class ExamplePowerUp : Node {
  public long LastNotification { get; private set; }

  public void OnExamplePowerUp(int what) {
    switch ((long)what) {
      case NotificationReady:
        GD.Print("PowerUp is ready!");
        break;
      case NotificationEnterTree:
        GD.Print("I'm in the tree!");
        break;
      case NotificationExitTree:
        GD.Print("I'm out of the tree!");
        break;
      default:
        break;
    }
    LastNotification = what;
  }
}
