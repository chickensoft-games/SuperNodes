namespace ImplementedInterfaceExample;

using System.Diagnostics;
using Godot;
using GoDotTest;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp))]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);

  public void OnReady()
    => Debug.Assert(
      this is IMyPowerUp, "MySuperNode should implement IMyPowerUp"
    );
}

public interface IMyPowerUp { }

[PowerUp]
public class MyPowerUp : IMyPowerUp { }

public class ImplementedInterfaceExampleTest : TestClass {
  public ImplementedInterfaceExampleTest(Node testScene) : base(testScene) { }

  [Test]
  public void TestImplementedInterface() {
    var mySuperNode = new MySuperNode();
    mySuperNode.OnReady();
  }
}
