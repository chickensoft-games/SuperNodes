namespace Test;

using Godot;

[SuperNode(typeof(GeneralFeaturePowerUp<int>), "OtherGenerator")]
public partial class GeneralFeatureSuperNode : Node {
  public override partial void _Notification(int what);

  public void OnReady() { }
}

[PowerUp]
public partial class GeneralFeaturePowerUp<T>
  : Node, IGeneralFeaturePowerUp<T> {
  T IGeneralFeaturePowerUp<T>.TestValue { get; } = default!;
}

public interface IGeneralFeaturePowerUp<T> {
  T TestValue { get; }
}

public partial class GeneralFeatureSuperNode {
  public void OtherGenerator(int what) { }
}
