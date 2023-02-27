namespace SuperNodes.Tests.Common.Models;

using System.Collections.Immutable;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class LifecycleHooksTest {
  [Fact]
  public void Initializes() {
    var lifecycleMethodHook = new LifecycleMethodHook("LifecycleMethod");
    var powerUpHook = new PowerUpHook(
      "global::Tests.PowerUpMethod", ImmutableArray<string>.Empty
    );

    lifecycleMethodHook.ShouldBeOfType<LifecycleMethodHook>();
    powerUpHook.ShouldBeOfType<PowerUpHook>();
  }
}
