namespace SuperNodes.Tests.Common.Utils;

using System.Collections.Generic;
using System.Collections.Immutable;
using Shouldly;
using SuperNodes.Common.Utils;
using Xunit;

public class ChickensoftGeneratorTest {
  [Fact]
  public void Tabs() {
    ChickensoftGenerator.Tab(0).ShouldBe("");
    ChickensoftGenerator.Tab(1).ShouldBe("  ");
    ChickensoftGenerator.Tab(2).ShouldBe("    ");

    ChickensoftGenerator.Tab(0, "foo").ShouldBe("foo");
    ChickensoftGenerator.Tab(1, "foo").ShouldBe("  foo");
    ChickensoftGenerator.Tab(2, "foo").ShouldBe("    foo");
  }

  [Fact]
  public void Format() {
    var lines = new string[] {
      "if (condition) {",
      "  print('hi');",
      "}"
    }.ToImmutableArray();
    var code = ChickensoftGenerator.Format($$"""
    {
      {{lines}}
    }
    """);

    code.ShouldBe("""
    {
      if (condition) {
        print('hi');
      }
    }
    """.NormalizeLineEndings());
  }

  [Fact]
  public void If() {
    var array = new List<string>() { "a" };
    ChickensoftGenerator.If(true, array).ShouldBe(array);
    ChickensoftGenerator.If(false, array).ShouldBeEmpty();

    ChickensoftGenerator.If(true, "a", "b").ShouldBe(new[] { "a", "b" });
    ChickensoftGenerator.If(false, "a", "b").ShouldBeEmpty();
  }
}
