using System;
using Godot;
using SuperNodes.Types;

public class MyDisposable : IDisposable {
  public void Dispose() { }
}

[SuperNode(typeof(GenericWithWhereClause<MyDisposable>))]
public partial class TestNode : Node {
  public override partial void _Notification(int what);
}

[PowerUp]
public class GenericWithWhereClause<T> where T : IDisposable {
  public void OnMyPowerUp(int what) { }
}

//we only need to make sure that this code compiles, no runtime check is required.
