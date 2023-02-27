namespace SuperNodes.Tests.Common.Services;

using System.Collections.Generic;
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
    }.ToImmutableArray());

    codeService.GetVisibleInterfacesFullyQualified(
      node, symbol
    ).ShouldBe(new string[] {
      "global::Tests.ITestable", "global::Tests.ITestable2<int>"
    }.ToImmutableArray());
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

    codeService.GetContainingNamespace(sym.Object).ShouldBe(null);
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
  public void GetContainingNamespaceWithNoSymbol() {
    var codeService = new CodeService();

    codeService.GetContainingNamespace(null).ShouldBe(null);
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

    codeService.GetBaseClassHierarchy(null)
      .ShouldBe(ImmutableArray<string>.Empty);
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
  public void GetUsingsWithNoSymbol() {
    var codeService = new CodeService();

    codeService.GetUsings(null).ShouldBe(ImmutableHashSet<string>.Empty);
  }

  [Fact]
  public void GetPropsAndFields() {
    const string code = """
    namespace Foo {
      using System;

      public class Bar : IFoo {
        bool IFoo.FooProp => true;

        public event Action? MyEvent;

        [Obsolete("Old property.")]
        public string Property { get; set; } = "";

        [Obsolete]
        private int _field = 0;

        protected readonly int _field2 = 1;

        public void Method() {}
      }

      public interface IFoo {
        bool FooProp { get; }
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
          Reference: "_field",
          Type: "int",
          Attributes: new AttributeDescription[] {
            new AttributeDescription(
              Name: "ObsoleteAttribute",
              Type: "global::System.ObsoleteAttribute",
              ArgumentExpressions: ImmutableArray<string>.Empty
            )
          }.ToImmutableArray(),
          IsField: true,
          IsMutable: true,
          IsReadable: true,
          NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
          TypeParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty
        ),
        new PropOrField(
          Name: "_field2",
          Reference: "_field2",
          Type: "int",
          Attributes: ImmutableArray<AttributeDescription>.Empty,
          IsField: true,
          IsMutable: false,
          IsReadable: true,
          NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
          TypeParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty
        ),
        new PropOrField(
          Name: "FooProp",
          Reference: "FooProp",
          Type: "bool",
          Attributes: ImmutableArray<AttributeDescription>.Empty,
          IsField: false,
          IsMutable: false,
          IsReadable: true,
          NameParts: new SimpleSymbolDisplayPart[] {
            new SimpleSymbolDisplayPart(SymbolDisplayPartKind.InterfaceName, "IFoo"),
            new SimpleSymbolDisplayPart(SymbolDisplayPartKind.Punctuation, "."),
            new SimpleSymbolDisplayPart(SymbolDisplayPartKind.PropertyName, "FooProp")
          }.ToImmutableArray(),
          TypeParts: ImmutableArray.Create(
            new SimpleSymbolDisplayPart(SymbolDisplayPartKind.Keyword, "bool")
          )
        ),
        new PropOrField(
          Name: "Property",
          Reference: "Property",
          Type: "string",
          Attributes: new AttributeDescription[] {
            new AttributeDescription(
              Name: "ObsoleteAttribute",
              Type: "global::System.ObsoleteAttribute",
              ArgumentExpressions: new string[] {
                "\"Old property.\""
              }.ToImmutableArray()
            )
          }.ToImmutableArray(),
          IsField: false,
          IsMutable: true,
          IsReadable: true,
          NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
          TypeParts: ImmutableArray.Create(
            new SimpleSymbolDisplayPart(SymbolDisplayPartKind.Keyword, "string")
          )
        ),
       }.ToImmutableArray();

    result.ShouldDeepEqual(expected);
  }

  [Fact]
  public void ExtractRelevantIdentifierParts() {
    var codeService = new CodeService();

    var @interface = "ITestInterface";

    var makeSymbol = (SymbolDisplayPartKind kind, string text)
      => new SymbolDisplayPart(kind, null, text);

    var makeSimple = (SymbolDisplayPartKind kind, string text)
      => new SimpleSymbolDisplayPart(kind, text);

    var input = new SymbolDisplayPart[] {
      makeSymbol(SymbolDisplayPartKind.NamespaceName, "TestNamespace"),
      makeSymbol(SymbolDisplayPartKind.NamespaceName, "NestedNamespace"),
      makeSymbol(SymbolDisplayPartKind.ClassName, "TestStaticClass"),
      makeSymbol(SymbolDisplayPartKind.Punctuation, "<"),
      makeSymbol(SymbolDisplayPartKind.TypeParameterName, "TA"),
      makeSymbol(SymbolDisplayPartKind.Punctuation, ">"),
      makeSymbol(SymbolDisplayPartKind.InterfaceName, @interface),
      makeSymbol(SymbolDisplayPartKind.Punctuation, "<"),
      makeSymbol(SymbolDisplayPartKind.TypeParameterName, "TB"),
      makeSymbol(SymbolDisplayPartKind.Punctuation, ">"),
    }.ToImmutableArray();

    codeService.ExtractRelevantIdentifierParts(@interface, input).ShouldBe(
      new SimpleSymbolDisplayPart[] {
        makeSimple(SymbolDisplayPartKind.InterfaceName, @interface),
        makeSimple(SymbolDisplayPartKind.Punctuation, "<"),
        makeSimple(SymbolDisplayPartKind.TypeParameterName, "TB"),
        makeSimple(SymbolDisplayPartKind.Punctuation, ">"),
      }.ToImmutableArray()
    );
  }

  [Fact]
  public void GetAttributesForPropOrFieldHandlesNullValues() {
    var attribute = new TestAttributeData();
    var attributes = new[] { attribute }.ToImmutableArray<AttributeData>();

    var codeService = new CodeService();

    codeService.GetAttributesForPropOrField(attributes).ShouldDeepEqual(
      new AttributeDescription[] {
        new AttributeDescription(
          Name: "",
          Type: "",
          ArgumentExpressions: ImmutableArray<string>.Empty
        )
      }.ToImmutableArray()
    );
  }

  [Fact]
  public void GetBaseTypeFullyQualifiedGetsType() {
    var sym = new Mock<INamedTypeSymbol>();
    var baseType = new Mock<INamedTypeSymbol>();

    sym.Setup(s => s.BaseType).Returns(baseType.Object);
    baseType.Setup(s => s.BaseType).Returns((INamedTypeSymbol?)null);

    baseType.Setup
      (s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
    ).Returns("BaseType");

    var codeService = new CodeService();

    codeService.GetBaseTypeFullyQualified(sym.Object).ShouldBe("BaseType");
  }

  [Fact]
  public void GetBaseTypeFullyQualifiedUsesFallbackWhenNoBaseType() {
    var sym = new Mock<INamedTypeSymbol>();

    sym.Setup(s => s.BaseType).Returns((INamedTypeSymbol?)null);

    var codeService = new CodeService();

    codeService
      .GetBaseTypeFullyQualified(sym.Object, "fallback")
      .ShouldBe("fallback");
  }

  [Fact]
  public void GetBaseTypeFullyQualifiedUsesFallbackWhenNoSymbol() {
    var codeService = new CodeService();

    codeService
      .GetBaseTypeFullyQualified(null, "fallback")
      .ShouldBe("fallback");
  }

  [Fact]
  public void GetNameFullyQualifiedGetsName() {
    var sym = new Mock<INamedTypeSymbol>();

    sym.Setup(s => s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
      .Returns("Tests.Foo");

    var codeService = new CodeService();

    codeService
    .GetNameFullyQualified(sym.Object, "fallback")
    .ShouldBe("Tests.Foo");
  }

  [Fact]
  public void GetNameFullyQualifiedUsesFallbackWhenNoSymbol() {
    var codeService = new CodeService();

    codeService
      .GetNameFullyQualified(null, "fallback")
      .ShouldBe("fallback");
  }

  [Fact]
  public void GetMembersGetsMembers() {
    var symbol = new Mock<INamedTypeSymbol>();
    var members = new[] {
      new Mock<ISymbol>().Object,
      new Mock<ISymbol>().Object,
      new Mock<ISymbol>().Object,
    }.ToImmutableArray();
    symbol.Setup(s => s.GetMembers()).Returns(members);

    var codeService = new CodeService();

    codeService.GetMembers(symbol.Object).ShouldBe(members);
  }

  [Fact]
  public void GetMembersReturnsEmptyArrayWhenNoSymbol() {
    var codeService = new CodeService();

    codeService.GetMembers(null).ShouldBe(ImmutableArray<ISymbol>.Empty);
  }

  [Fact]
  public void GetAttribute() {
    var code = """
      namespace Tests {
        using System;

        [TestAttribute, OtherAttribute]
        public class TestClass { }

        [AttributeUsage(AttributeTargets.Class)]
        public class TestAttribute : Attribute { }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );
    var attributeData = symbol.GetAttributes()[0];

    var codeService = new CodeService();

    codeService.GetAttribute(symbol, "TestAttribute").ShouldBe(attributeData);
    codeService.GetAttribute(null, "TestAttribute").ShouldBeNull();
  }

  [Fact]
  public void GetAttributeHandlesNullAttributeClass() {
    var symbol = new Mock<INamedTypeSymbol>();
    var attributeData = new TestAttributeData();
    symbol.Setup(s => s.GetAttributes())
      .Returns(new[] { attributeData }.ToImmutableArray<AttributeData>());

    var codeService = new CodeService();

    codeService.GetAttribute(symbol.Object, "TestAttribute").ShouldBeNull();
  }

  [Fact]
  public void GetNameWithoutFallback() {
    var symbol = new Mock<INamedTypeSymbol>();

    symbol.Setup(s => s.Name).Returns("TestClass");

    var codeService = new CodeService();

    codeService.GetName(symbol.Object).ShouldBe("TestClass");
    codeService.GetName(null).ShouldBeNull();
  }

  [Fact]
  public void GetNameWithFallback() {
    var code = """
      namespace Tests {
        public class TestClass { }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax>(code);

    var symbol = new Mock<INamedTypeSymbol>();

    symbol.Setup(s => s.Name).Returns("TestClass");

    var codeService = new CodeService();

    codeService.GetName(symbol.Object, node).ShouldBe("TestClass");
    codeService.GetName(null, node).ShouldBe(node.Identifier.ValueText);
  }

  [Fact]
  public void HasOnNotificationMethodHandlerFindsHandler() {
    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        public class TestSuperNode {
          public string Property { get; set; } = "Hi!";
          public void SomethingElse(int what) { }
          public void AnotherOne() { }
          public void OnNotification(int what) { }
        }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax>(code);

    var service = new CodeService();

    service.HasOnNotificationMethodHandler(node.Members).ShouldBeTrue();
  }

  [Fact]
  public void HasOnNotificationMethodHandlerNoParams() {
    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        public class TestSuperNode {
          public void OnNotification() { }
        }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax>(code);

    var service = new CodeService();

    service.HasOnNotificationMethodHandler(node.Members).ShouldBeFalse();
  }

  [Fact]
  public void HasOnNotificationMethodHandlerInvalidType() {
    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        public class TestSuperNode {
          public void OnNotification(int what) { }
        }
      }
    """;

    // Simulate nullable type on the parameter since I can't figure out when
    // roslyn ever lets it be null (despite being nullable).
    var node = Tester.Parse<ClassDeclarationSyntax>(code);
    var method = (MethodDeclarationSyntax)node.Members.First();
    var param = method!.ParameterList.Parameters.First();
    var newMethod = method.WithParameterList(
      method.ParameterList.WithParameters(
        method.ParameterList.Parameters.Replace(
          param,
          param.WithType(null)
        )
      )
    );
    node = node.WithMembers(node.Members.Replace(method, newMethod));

    var service = new CodeService();

    service.HasOnNotificationMethodHandler(node.Members).ShouldBeFalse();
  }

  [Fact]
  public void HasPartialNotificationMethodFindsSignature() {
    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        public partial class TestSuperNode {
          public override partial void _Notification(int what);
        }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax>(code);

    var service = new CodeService();

    service.HasPartialNotificationMethod(node.Members).ShouldBeTrue();
  }

  [Fact]
  public void HasPartialNotificationMethodNoParams() {
    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        public partial class TestSuperNode {
          public override partial void _Notification();
        }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax>(code);

    var service = new CodeService();

    service.HasPartialNotificationMethod(node.Members).ShouldBeFalse();
  }

  [Fact]
  public void HasPartialNotificationMethodInvalidType() {
    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        public partial class TestSuperNode {
          public override partial void _Notification(int what);
        }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax>(code);
    var method = (MethodDeclarationSyntax)node.Members.First();
    var param = method!.ParameterList.Parameters.First();
    var newMethod = method.WithParameterList(
      method.ParameterList.WithParameters(
        method.ParameterList.Parameters.Replace(
          param,
          param.WithType(null)
        )
      )
    );
    node = node.WithMembers(node.Members.Replace(method, newMethod));

    var service = new CodeService();

    service.HasPartialNotificationMethod(node.Members).ShouldBeFalse();
  }

  [Fact]
  public void GetLifecycleHooksHandlesNullAttributeData() {
    var service = new CodeService();

    service.GetLifecycleHooks(null)
      .ShouldBeSameAs(LifecycleHooksResponse.Empty);
  }

  [Fact]
  public void GetLifecycleHooksFindsHooks() {
    var code = $$"""
      namespace Tests {
        using System;

        [SuperNode("One", typeof(Two<string, bool>))]
        public partial class TestSuperNode { }

        {{Tester.SUPER_NODE_ATTRIBUTE}}

        public class Two<A, B> { }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );
    var attributeData = symbol.GetAttributes()[0];

    var service = new CodeService();

    var lifecycleMethodHook = new LifecycleMethodHook("One");
    var powerUpHookGenericFullName = "global::Tests.Two<A, B>";
    var powerUpHookGeneric = new PowerUpHook(
      FullName: powerUpHookGenericFullName,
      TypeArguments: new string[] { "string", "bool" }.ToImmutableArray()
    );

    // Ensure SuperNode attribute parameters are converted to the correct
    // lifecycle hook models.
    service.GetLifecycleHooks(attributeData)
      .ShouldDeepEqual(
        new LifecycleHooksResponse(
        LifecycleHooks: new IGodotNodeLifecycleHook[] {
          lifecycleMethodHook,
          powerUpHookGeneric
        }.ToImmutableArray(),
        PowerUpHooksByFullName: new Dictionary<string, PowerUpHook> {
          [powerUpHookGenericFullName] = powerUpHookGeneric
        }.ToImmutableDictionary()
      )
    );
  }
}

internal class TestAttributeData : AttributeData {
  public new INamedTypeSymbol? AttributeClass { get; }
  protected override INamedTypeSymbol? CommonAttributeClass { get; }
  protected override IMethodSymbol? CommonAttributeConstructor { get; }
  protected override SyntaxReference? CommonApplicationSyntaxReference { get; }
  protected override ImmutableArray<TypedConstant> CommonConstructorArguments {
    get;
  } = ImmutableArray<TypedConstant>.Empty;
  protected override ImmutableArray<KeyValuePair<string, TypedConstant>>
    CommonNamedArguments { get; } =
      ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty;
}
