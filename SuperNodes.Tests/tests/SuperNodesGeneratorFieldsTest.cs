namespace SuperNodes.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
using Shouldly;
using Xunit;

public class SuperNodesGeneratorFieldsTest {
  [Fact]
  public void Fields() {
    SuperNodesGenerator.VOID.ShouldBeOfType<string>();
    SuperNodesGenerator.Args().ShouldBeOfType<ImmutableArray<string>>();
    SuperNodesGenerator.NoArgs.ShouldBeOfType<ImmutableArray<string>>();
    SuperNodesGenerator.LifecycleMethods
      .ShouldBeAssignableTo<IDictionary<string, LifecycleMethod>>();
    SuperNodesGenerator.SUPER_NODE_ATTRIBUTE_NAME
      .ShouldBeOfType<string>();
    SuperNodesGenerator.SUPER_NODE_ATTRIBUTE_NAME_FULL
      .ShouldBeOfType<string>();
    SuperNodesGenerator.SUPER_NODE_ATTRIBUTE_SOURCE
      .ShouldBeOfType<string>();
    SuperNodesGenerator.POWER_UP_ATTRIBUTE_NAME
      .ShouldBeOfType<string>();
    SuperNodesGenerator.POWER_UP_ATTRIBUTE_NAME_FULL
      .ShouldBeOfType<string>();
    SuperNodesGenerator.POWER_UP_ATTRIBUTE_SOURCE
      .ShouldBeOfType<string>();
  }
}
