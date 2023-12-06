namespace ImplementingGenericInterfaceFromPowerUpTest;

using System.Diagnostics;
using Chickensoft.GoDotTest;
using Godot;
using SuperNodes.Types;


public interface IInterface<T> {
  T Value { get; }
}

[PowerUp]
public class PowerUp<T> : IInterface<T> {
  public T Value { get; set; }
}

[SuperNode(typeof(PowerUp<string>))]
public partial class MyObject : GodotObject {
  public override partial void _Notification(int what);
}
