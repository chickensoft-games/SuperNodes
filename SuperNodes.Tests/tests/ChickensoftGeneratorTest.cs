namespace SuperNodes.Tests;

using Shouldly;
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
  public void Lines() {
    ChickensoftGenerator.Lines(new[] { "foo" }).ShouldBe("foo");
    ChickensoftGenerator.Lines(new[] { "foo", "bar" }).ShouldBe("foo\nbar");

    ChickensoftGenerator.Lines(1, new[] { "foo" }).ShouldBe("foo");
    ChickensoftGenerator.Lines(1, new[] { "foo", "bar" })
      .ShouldBe("foo\n  bar");
    ChickensoftGenerator.Lines(2, new[] { "foo", "bar", "boo" })
      .ShouldBe("foo\n    bar\n    boo");
  }

  [Fact]
  public void Format() => ChickensoftGenerator.Format("namespace   Foo { }")
      .ShouldBe(
        """
        namespace Foo
        {
        }
        """.ReplaceLineEndings()
      );

  [Fact]

  public void If() {
    ChickensoftGenerator.If(true, "a", "b").ShouldBe("a");
    ChickensoftGenerator.If(false, "a", "b").ShouldBe("b");
    ChickensoftGenerator.If(true, "a").ShouldBe("a");
    ChickensoftGenerator.If(false, "a").ShouldBe("");
  }
}
