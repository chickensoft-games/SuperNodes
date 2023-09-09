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
      BaseClasses: new string[] { "global::Godot.Node" }.ToImmutableArray(),
      LifecycleHooks: ImmutableArray<IGodotNodeLifecycleHook>.Empty,
      PowerUpHooksByFullName: ImmutableDictionary<string, PowerUpHook>.Empty,
      NotificationHandlers: ImmutableArray<string>.Empty,
      HasPartialNotificationMethod: false,
      HasOnNotificationMethodHandler: false,
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: ImmutableHashSet<string>.Empty
    );
    var powerUps = ImmutableDictionary<string, PowerUp>.Empty;

    var generationItem = new GenerationItem(superNode, powerUps);

    generationItem.ShouldBeOfType<GenerationItem>();
    generationItem.SuperNode.ShouldBe(superNode);
    generationItem.PowerUps.ShouldBe(powerUps);
  }
}
