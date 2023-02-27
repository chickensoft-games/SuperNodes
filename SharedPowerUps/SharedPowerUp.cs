using Godot;

namespace SharedPowerUps;

[PowerUp]
public partial class SharedPowerUp : Node
{
#pragma warning disable CA1822 // annoying lint about making this static :P
  public void OnSharedPowerUp(int _) { }
#pragma warning restore CA1822
}
