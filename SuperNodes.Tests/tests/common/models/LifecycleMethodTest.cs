namespace SuperNodes.Tests.Common.Models;

using System.Collections.Immutable;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class LifecycleMethodTest {
  [Fact]
  public void Initializes() {
    var lifecycleMethod = new LifecycleMethod(
      GodotNotification: "ready",
      ReturnType: "void",
      Args: ImmutableArray<string>.Empty
    );

    lifecycleMethod.ShouldBeOfType<LifecycleMethod>();
  }
}
