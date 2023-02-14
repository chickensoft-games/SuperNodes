namespace SuperNodes.Tests.Common.Services;

using System.Collections.Immutable;
using DeepEqual.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
using Xunit;

public class BasicSyntaxOperationsServiceTest {
  [Fact]
  public void GetVisibleInterfacesFullyQualified() {
    var code = """
    namespace Tests {
      public interface ITestable {}
      public interface ITestable2<T> {}
      class Test : ITestable, ITestable2<int> {
      }
    }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    var codeService = new CodeService();

    codeService.GetVisibleInterfacesFullyQualified(
      node, null
    ).ShouldBe(new string[] {
      "ITestable", "ITestable2"
    }.ToImmutableHashSet());

    codeService.GetVisibleInterfacesFullyQualified(
      node, symbol
    ).ShouldBe(new string[] {
      "global::Tests.ITestable", "global::Tests.ITestable2<int>"
    }.ToImmutableHashSet());
  }

  [Fact]
  public void GetVisibleInterfacesFullyQualifiedWithoutInterfaces() {
    var code = """
    namespace Tests {
      class Test { }
    }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    var codeService = new CodeService();

    codeService.GetVisibleInterfacesFullyQualified(
      node, null
    ).ShouldBe(ImmutableHashSet<string>.Empty);

    codeService.GetVisibleInterfacesFullyQualified(
      node, symbol
    ).ShouldBe(ImmutableHashSet<string>.Empty);
  }

  [Fact]
  public void GetTypeParametersWithParameters() {
    const string code = """
    namespace Foo {
      class Bar<T> { }
    }
    """;

    var foo = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    var codeService = new CodeService();

    codeService.GetTypeParameters(foo).ShouldBe(
      new[] { "T" }.ToImmutableArray()
    );
  }

  [Fact]
  public void GetTypeParametersWithoutParameters() {
    const string code = """
    namespace Foo {
      class Bar { }
    }
    """;

    var foo = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    var codeService = new CodeService();

    codeService.GetTypeParameters(foo).ShouldBe(ImmutableArray<string>.Empty);
  }

  [Fact]
  public void GetContainingNamespaceInGlobalNs() {
    var sym = new Mock<INamedTypeSymbol>();
    var ns = new Mock<INamespaceSymbol>();
    sym.Setup(s => s.ContainingNamespace).Returns(ns.Object);
    ns.Setup(ns => ns.IsGlobalNamespace).Returns(true);

    var codeService = new CodeService();

    codeService.GetContainingNamespace(sym.Object).ShouldBe("");
  }

  [Fact]
  public void GetContainingNamespaceInNormalNs() {
    var sym = new Mock<INamedTypeSymbol>();
    var ns = new Mock<INamespaceSymbol>();
    sym.Setup(s => s.ContainingNamespace).Returns(ns.Object);
    ns.Setup(ns => ns.IsGlobalNamespace).Returns(false);
    ns.Setup(ns => ns.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
      .Returns("global::Tests");

    var codeService = new CodeService();

    codeService.GetContainingNamespace(sym.Object).ShouldBe("Tests");
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

    var codeService = new CodeService();

    codeService.GetBaseClassHierarchy(sym.Object).ShouldBe(
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

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    var codeService = new CodeService();

    codeService.GetUsings(symbol).ShouldBe(
      new[] { "A", "B", "C" }.ToImmutableHashSet()
    );
  }

  [Fact]
  public void GetPropsAndFields() {
    const string code = """
    namespace Foo {
      using System;

      class Bar {
        public event Action? MyEvent;

        [Obsolete("Old property.")]
        public string Property { get; set; } = "";

        [Obsolete]
        private int _field = 0;

        public void Method() {}
      }
    }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    var codeService = new CodeService();

    var result = codeService.GetPropsAndFields(symbol.GetMembers());

    var expected = new[] {
        new PropOrField(
          Name: "_field",
          Type: "int",
          Attributes: new AttributeDescription[] {
            new AttributeDescription(
              Name: "ObsoleteAttribute",
              Type: "global::System.ObsoleteAttribute",
              ArgumentExpressions: ImmutableArray<string>.Empty
            )
          }.ToImmutableHashSet(),
          IsField: true
        ),
        new PropOrField(
          Name: "Property",
          Type: "string",
          Attributes: new AttributeDescription[] {
            new AttributeDescription(
              Name: "ObsoleteAttribute",
              Type: "global::System.ObsoleteAttribute",
              ArgumentExpressions: new string[] {
                "\"Old property.\""
              }.ToImmutableArray()
            )
          }.ToImmutableHashSet(),
          IsField: false
        ),
       }.ToImmutableArray();

    result.ShouldDeepEqual(expected);
  }
}
