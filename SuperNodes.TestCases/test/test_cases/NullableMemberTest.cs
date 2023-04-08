namespace NullableMemberTest;

using System.Collections.Generic;
using Godot;
using GoDotTest;
using Shouldly;
using SuperNodes.Types;

[SuperNode]
public partial class NullableMemberNode : Node {
  public override partial void _Notification(int what);

  public string? Value { get; set; } = "";
  public List<string?> Items { get; set; } = new();
  public List<string?>? Names { get; set; }

  private string? _value = "";
  private List<string?> _items = new();
  private List<string?>? _names = new();
}

public class NullableMemberTest : TestClass {
  public NullableMemberTest(Node testScene) : base(testScene) { }

  [Test]
  public void NullableMemberIsCompiled() {
    var node = new NullableMemberNode();
    node.PropertiesAndFields["Value"].Type.ShouldBe(typeof(string));
    node.PropertiesAndFields["Items"].Type.ShouldBe(typeof(List<string?>));
    node.PropertiesAndFields["Names"].Type.ShouldBe(typeof(List<string?>));

    node.PropertiesAndFields["_value"].Type.ShouldBe(typeof(string));
    node.PropertiesAndFields["_items"].Type.ShouldBe(typeof(List<string?>));
    node.PropertiesAndFields["_names"].Type.ShouldBe(typeof(List<string?>));
  }
}
