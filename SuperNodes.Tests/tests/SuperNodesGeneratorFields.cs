namespace SuperNodes.Tests;

using System.Collections.Generic;
using Shouldly;
using Xunit;

public class SuperNodesGeneratorFieldsTest {
  [Fact]
  public void Fields() {
    SuperNodesGenerator.VOID.ShouldBeOfType<string>();
    SuperNodesGenerator.Args().ShouldBeOfType<string[]>();
    SuperNodesGenerator.NoArgs.ShouldBeOfType<string[]>();
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
