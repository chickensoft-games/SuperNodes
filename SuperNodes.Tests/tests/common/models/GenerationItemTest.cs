namespace SuperNodes.Tests.Common.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class GenerationItemTest {
  [Fact]
  public void Initializes() {
    var superNode = new SuperNode(
      Namespace: "global::Tests",
      Name: "SuperNode",
      NameWithoutGenerics: "SuperNode",
      Location: new Mock<Location>().Object,
      BaseClasses: new string[] { "global::Godot.Object" }.ToImmutableArray(),
      LifecycleHooks: ImmutableArray<IGodotNodeLifecycleHook>.Empty,
      PowerUpHooksByFullName: ImmutableDictionary<string, PowerUpHook>.Empty,
      NotificationHandlers: ImmutableArray<string>.Empty,
      HasPartialNotificationMethod: false,
      HasOnNotificationMethodHandler: false,
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: ImmutableHashSet<string>.Empty,
      ContainingTypes: ImmutableArray<ContainingType>.Empty
    );

    var superObject = new SuperObject(
      Namespace: "global::Tests",
      Name: "SuperObject",
      NameWithoutGenerics: "SuperObject",
      Location: new Mock<Location>().Object,
      BaseClasses: new string[] { "global::Godot.Object" }.ToImmutableArray(),
      LifecycleHooks: ImmutableArray<IGodotNodeLifecycleHook>.Empty,
      PowerUpHooksByFullName: ImmutableDictionary<string, PowerUpHook>.Empty,
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: ImmutableHashSet<string>.Empty,
      IsRecord: false,
      ContainingTypes: ImmutableArray<ContainingType>.Empty
    );
    var powerUps = ImmutableDictionary<string, PowerUp>.Empty;

    var superNodeGenerationItem =
      new SuperNodeGenerationItem(superNode, powerUps);

    var superObjectGenerationItem = new SuperObjectGenerationItem(
      superObject,
      powerUps
    );

    superNodeGenerationItem.ShouldBeOfType<SuperNodeGenerationItem>();
    superNodeGenerationItem.SuperNode.ShouldBe(superNode);
    superNodeGenerationItem.PowerUps.ShouldBe(powerUps);

    superObjectGenerationItem.ShouldBeOfType<SuperObjectGenerationItem>();
    superObjectGenerationItem.SuperObject.ShouldBe(superObject);
    superObjectGenerationItem.PowerUps.ShouldBe(powerUps);
  }
}
