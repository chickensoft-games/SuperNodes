namespace SuperNodes.Tests.Common.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class PowerUpTest {
  [Fact]
  public void Initializes() {
    var powerUp = new PowerUp(
      Namespace: "global::Tests",
      Name: "PowerUp",
      FullName: "global::Tests.PowerUp",
      Location: new Mock<Location>().Object,
      BaseClass: "global::Godot.Node",
      TypeParameters: ImmutableArray<string>.Empty,
      Interfaces: ImmutableArray<string>.Empty,
      Source: "namespace Tests { [PowerUp] public class PowerUp {} }",
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: ImmutableHashSet<string>.Empty,
      HasOnPowerUpMethod: false
    );
    powerUp.ShouldBeOfType<PowerUp>();
    powerUp.IsGeneric.ShouldBeFalse();
  }

  [Fact]
  public void IsGenericRecognizes() {
    var powerUp = new PowerUp(
      Namespace: "global::Tests",
      Name: "PowerUp",
      FullName: "global::Tests.PowerUp",
      Location: new Mock<Location>().Object,
      BaseClass: "global::Godot.Node",
      TypeParameters: ImmutableArray.Create("T"),
      Interfaces: ImmutableArray<string>.Empty,
      Source: "namespace Tests { [PowerUp] public class PowerUp<T> {} }",
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: ImmutableHashSet<string>.Empty,
      HasOnPowerUpMethod: false
    );
    powerUp.ShouldBeOfType<PowerUp>();
    powerUp.IsGeneric.ShouldBeTrue();
  }
}
