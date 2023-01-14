namespace SuperNodes.Tests;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
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
        """
      );

  [Fact]

  public void If() {

    ChickensoftGenerator.If(true, "a", "b").ShouldBe("a");
    ChickensoftGenerator.If(false, "a", "b").ShouldBe("b");
    ChickensoftGenerator.If(true, "a").ShouldBe("a");
    ChickensoftGenerator.If(false, "a").ShouldBe("");
  }

  [Fact]
  public void GetBaseClassHierarchy() {
    var sym = new Mock<INamedTypeSymbol>();
    var baseType = new Mock<INamedTypeSymbol>();
    var baseBaseType = new Mock<INamedTypeSymbol>();

    sym.Setup(s => s.BaseType).Returns(baseType.Object);
    baseType.Setup(s => s.BaseType).Returns(baseBaseType.Object);
    baseBaseType.Setup(s => s.BaseType).Returns((INamedTypeSymbol?)null);

    baseType.Setup
      (s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
    ).Returns("BaseType");

    baseBaseType.Setup(
      s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
    ).Returns("BaseBaseType");

    ChickensoftGenerator.GetBaseClassHierarchy(sym.Object).ShouldBe(
      new[] { "BaseType", "BaseBaseType" }
    );
  }

  [Fact]
  public void GetUsings() {
    var code = """
    using A;
    using B;
    namespace Foo {
      using C;
      class Bar {
      }
    }
    """;

    var model = TestUtils.GetSemanticModel(code);

    var classDecl = model.SyntaxTree.GetRoot()
      .DescendantNodes()
      .OfType<ClassDeclarationSyntax>()
      .First();

    var symbol = model.GetDeclaredSymbol(classDecl)!;

    ChickensoftGenerator.GetUsings(symbol).ShouldBe(
      new[] { "A", "B", "C" }.ToImmutableHashSet()
    );
  }
}
