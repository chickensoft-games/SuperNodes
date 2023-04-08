namespace InitOnlyMember;

using Chickensoft.GoDotTest;
using Godot;
using Shouldly;
using SuperNodes.Types;

[SuperNode]
public partial class InitOnlyMemberNode : Node {
  public override partial void _Notification(int what);

  public string Value { get; init; } = "";
}

public class InitOnlyMemberTest : TestClass {
  public InitOnlyMemberTest(Node testScene) : base(testScene) { }

  [Test]
  public void Compiles() {
    var node = new InitOnlyMemberNode();
    node.PropertiesAndFields["Value"].IsMutable.ShouldBeFalse();
  }
}
