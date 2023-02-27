namespace SuperNodes.PowerUpsFeature;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

public class PowerUpGeneratorServiceTest {
  [Fact]
  public void CreatesPowerUpRewriter() {
    var service = new PowerUpGeneratorService();
    var rewriter = service.CreatePowerUpRewriter(
      typeParameters: ImmutableDictionary<string, string>.Empty,
      powerUpClassName: "PowerUp",
      superNodeClassName: "SuperNode"
    );
    rewriter.ShouldBeAssignableTo<PowerUpRewriter>();
  }
}
