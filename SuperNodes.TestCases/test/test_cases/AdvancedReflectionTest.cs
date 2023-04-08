namespace AdvancedReflection;

using System;
using System.Collections.Immutable;
using Chickensoft.GoDotTest;
using Godot;
using SuperNodes.Types;

[SuperNode(typeof(MyPowerUp))]
public partial class MySuperNode : Node2D {
  public override partial void _Notification(int what);

  [Export(PropertyHint.Range, "0, 100")]
  public int Probability { get; set; } = 50;

  public void OnReady() {
    foreach (var property in PropertiesAndFields.Keys) {
      GD.Print($"{property} = {GetScriptPropertyOrField(property)}");
    }
    // Change probability to 100
    SetScriptPropertyOrField("Probability", 100);
  }
}

[PowerUp]
public abstract partial class MyPowerUp : Node2D {
  [Obsolete("MyName is obsolete — please use Identifier instead.")]
  public string MyName { get; set; } = nameof(MyPowerUp);

  public string Identifier { get; set; } = nameof(MyPowerUp);

  #region StaticReflectionStubs

  [PowerUpIgnore]
  public abstract ImmutableDictionary<string, ScriptPropertyOrField> PropertiesAndFields { get; }

  [PowerUpIgnore]
  public abstract dynamic GetScriptPropertyOrField(string name);

  [PowerUpIgnore]
  public abstract void SetScriptPropertyOrField(string name, dynamic value);

  #endregion StaticReflectionStubs

  public void OnMyPowerUp(int what) {
    foreach (var property in PropertiesAndFields.Keys) {
      GD.Print($"{property} = {GetScriptPropertyOrField(property)}");
    }
    // Change identifier
    SetScriptPropertyOrField("Identifier", "AnotherIdentifier");
  }
}

public class AdvancedReflectionTest : TestClass {
  public AdvancedReflectionTest(Node testScene) : base(testScene) { }

  [Test]
  public void Test() {
    var mySuperNode = new MySuperNode();
    mySuperNode._Notification((int)Node.NotificationReady);
  }
}
