namespace SuperNodes.Tests.Common.Models;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class SuperNodeTest {
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

    superNode.ShouldBeOfType<SuperNode>();
    superNode.FilenamePrefix.ShouldBe("global::Tests.SuperNode");
  }

  [Fact]
  public void FilenamePrefixHandlesEmptyNamespace() {
    var superNode = new SuperNode(
      Namespace: "",
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

    superNode.ShouldBeOfType<SuperNode>();
    superNode.FilenamePrefix.ShouldBe("SuperNode");
  }
}
