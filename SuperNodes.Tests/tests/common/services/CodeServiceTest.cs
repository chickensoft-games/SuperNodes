namespace SuperNodes.Tests.Common.Services;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Shouldly;
using SuperNodes.Common.Services;
using Xunit;

public class BasicSyntaxOperationsServiceTest {
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

    var syntaxOps = new CodeService();

    syntaxOps.GetBaseClassHierarchy(sym.Object).ShouldBe(
      new[] { "BaseType", "BaseBaseType" }
    );
  }

  [Fact]
  public void GetUsings() {
    const string code = """
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

    var syntaxOps = new CodeService();

    syntaxOps.GetUsings(symbol).ShouldBe(
      new[] { "A", "B", "C" }.ToImmutableHashSet()
    );
  }
}
