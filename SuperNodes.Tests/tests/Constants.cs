namespace SuperNodes.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
using Shouldly;
using SuperNodes.Models;
using Xunit;

public class ConstantsTest {
  [Fact]
  public void Fields() {
    Constants.VOID.ShouldBeOfType<string>();
    Constants.Args().ShouldBeOfType<ImmutableArray<string>>();
    Constants.NoArgs.ShouldBeOfType<ImmutableArray<string>>();
    Constants.LifecycleMethods
      .ShouldBeAssignableTo<IDictionary<string, LifecycleMethod>>();
    Constants.SUPER_NODE_ATTRIBUTE_NAME
      .ShouldBeOfType<string>();
    Constants.SUPER_NODE_ATTRIBUTE_NAME_FULL
      .ShouldBeOfType<string>();
    Constants.SUPER_NODE_ATTRIBUTE_SOURCE
      .ShouldBeOfType<string>();
    Constants.POWER_UP_ATTRIBUTE_NAME
      .ShouldBeOfType<string>();
    Constants.POWER_UP_ATTRIBUTE_NAME_FULL
      .ShouldBeOfType<string>();
    Constants.POWER_UP_ATTRIBUTE_SOURCE
      .ShouldBeOfType<string>();
  }
}
