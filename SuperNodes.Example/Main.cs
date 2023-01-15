namespace SuperNodes.Example;

using Godot;

[SuperNode(nameof(MyPowerUp))]
public partial class MyNode : Node {
  public override partial void _Notification(long what);

  public void OnReady() { }

  public void OnProcess(double _) { }
}

[PowerUp]
public partial class MyPowerUp : Node {
  public string AddedProperty { get; set; } = "";
  public void OnMyPowerUp(long _) { }
}
