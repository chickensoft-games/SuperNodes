namespace GenericScript;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp))]
public abstract partial class MySuperNodeBase<T> : Node2D {
  public override partial void _Notification(int what);

  public bool IsReady { get; private set; }

  public T? Item { get; set; }

  public void OnReady() => IsReady = true;
}

public partial class MySuperNode : MySuperNodeBase<string> { }

[PowerUp]
public abstract partial class MyPowerUp : Node2D {
  public void OnMyPowerUp(int what) { }
}

public class GenericScriptTest : TestClass {
  public GenericScriptTest(Node testScene) : base(testScene) { }

  [Test]
  public void Test() {
    var mySuperNode = new MySuperNode();
    mySuperNode._Notification((int)Node.NotificationReady);
    mySuperNode.IsReady.ShouldBeTrue();
  }
}
