namespace MultipleStubs;

using System;
using System.Collections.Immutable;
using Godot;
using SuperNodes.Types;

[PowerUp]
public partial class PowerUpA : Node {
  [PowerUpIgnore]
  public ImmutableDictionary<string, ScriptPropertyOrField> PropertiesAndFields
    => throw new NotImplementedException();
}

[PowerUp]
public partial class PowerUpB : Node {
  [PowerUpIgnore]
  public ImmutableDictionary<string, ScriptPropertyOrField> PropertiesAndFields
    => throw new NotImplementedException();
}

[SuperNode(typeof(PowerUpA), typeof(PowerUpB))]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);
}
