namespace NamingConflictWorkaround;

using Godot;
using SuperNodes.Types;

[SuperNode(typeof(PowerUpA), typeof(PowerUpB))]
public partial class MySuperNode : Node {
  public override partial void _Notification(int what);
}

public interface IPowerUpA {
  string MyName { get; }
}

[PowerUp]
public class PowerUpA : IPowerUpA {
  string IPowerUpA.MyName { get; } = nameof(PowerUpA);
}

public interface IPowerUpB {
  string MyName { get; }
}

[PowerUp]
public class PowerUpB : IPowerUpB {
  string IPowerUpB.MyName { get; } = nameof(PowerUpB);
}
