namespace SuperNodes.TestCases;

using Godot;

[SuperNode(typeof(SimplePowerUp))]
public partial class SimpleExampleNode : Node {
  public override partial void _Notification(int what);

  public void OnReady() => SomeMethod();

  public void OnProcess(double delta) => SomeMethod();

  public void SomeMethod() {
    var d = GetProcessDeltaTime();
    if (LastNotification == NotificationReady) {
      GD.Print("We are getting ready.");
    }
    else if (LastNotification == NotificationProcess) {
      GD.Print("We are processing a frame.");
    }
  }
}

// A PowerUp that logs some of the main lifecycle events of a node.
[PowerUp]
public partial class SimplePowerUp : Node {
  public long LastNotification { get; private set; }

  public void OnSimplePowerUp(int what) {
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
