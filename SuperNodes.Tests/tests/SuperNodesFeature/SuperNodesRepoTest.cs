namespace SuperNodes.Tests.SuperNodesFeature;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
using SuperNodes.Common.Utils;
using SuperNodes.SuperNodesFeature;
using Xunit;

public class SuperNodesRepoTest {
  [Fact]
  public void Initializes() {
    var codeService = new Mock<ICodeService>();
    var superNodesRepo
      = new SuperNodesRepo(codeService.Object);

    superNodesRepo.CodeService.ShouldBe(codeService.Object);
  }

  [Fact]
  public void IsSuperNodeSyntaxCandidateValidates() {
    var codeService = new Mock<ICodeService>();
    var superNodesRepo
      = new SuperNodesRepo(codeService.Object);

    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        [SuperNode]
        public class TestSuperNode {
        }
      }
    """;

    var classDeclaration = Tester.Parse<ClassDeclarationSyntax>(code);

    superNodesRepo.IsSuperNodeSyntaxCandidate(classDeclaration)
      .ShouldBeTrue();
  }

  [Fact]
  public void IsSuperNodeSyntaxCandidateInvalidates() {
    var codeService = new Mock<ICodeService>();
    var superNodesRepo
      = new SuperNodesRepo(codeService.Object);

    var code = """
      namespace SuperNodes.Tests.SuperNodesFeature {
        public struct SomeTestClass {
        }
      }
    """;

    var node = Tester.Parse<StructDeclarationSyntax>(code);

    superNodesRepo.IsSuperNodeSyntaxCandidate(node)
      .ShouldBeFalse();
  }

  [Fact]
  public void GetSuperNodeGetsSuperNodeWithSymbol() {
    var codeService = new Mock<ICodeService>();

    var superNodesRepo
      = new SuperNodesRepo(codeService.Object);

    var code = $$"""
      namespace Tests {
        using System;

        [SuperNode]
        public partial class TestSuperNode {
          public void OnReady() { }
        }

        {{Tester.SUPER_NODE_ATTRIBUTE}}
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    codeService.Setup(cs => cs.GetNameWithGenerics(symbol, node))
      .Returns(symbol.Name);
    codeService.Setup(cs => cs.GetContainingNamespace(symbol))
      .Returns("Tests");
    codeService.Setup(cs => cs.GetBaseClassHierarchy(symbol))
      .Returns(ImmutableArray<string>.Empty);

    codeService.Setup(cs => cs.HasPartialNotificationMethod(node.Members))
      .Returns(true);
    codeService.Setup(cs => cs.HasOnNotificationMethodHandler(node.Members))
      .Returns(true);

    codeService.Setup(
      cs => cs.GetAttribute(symbol, Constants.SUPER_NODE_ATTRIBUTE_NAME_FULL)
    ).Returns((AttributeData?)null);

    codeService.Setup(cs => cs.GetLifecycleHooks(null))
      .Returns(LifecycleHooksResponse.Empty);

    var members = ImmutableArray<ISymbol>.Empty;

    codeService.Setup(cs => cs.GetMembers(symbol))
      .Returns(members);
    codeService.Setup(cs => cs.GetUsings(symbol))
      .Returns(ImmutableHashSet<string>.Empty);
    codeService.Setup(cs => cs.GetPropsAndFields(members))
      .Returns(ImmutableArray<PropOrField>.Empty);

    var superNode = superNodesRepo.GetSuperNode(node, symbol);

    superNode.Namespace.ShouldBe("Tests");
    superNode.Name.ShouldBe("TestSuperNode");
    superNode.Location.ShouldBe(node.GetLocation());
    superNode.BaseClasses.ShouldBe(ImmutableArray<string>.Empty);
    superNode.LifecycleHooks.ShouldBeEmpty();
    superNode.PowerUpHooksByFullName.ShouldBeEmpty();
    superNode.NotificationHandlers.ShouldBe(new string[] {
      "OnReady"
    });
    superNode.HasPartialNotificationMethod.ShouldBeTrue();
    superNode.HasOnNotificationMethodHandler.ShouldBeTrue();
    superNode.PropsAndFields.ShouldBeEmpty();
    superNode.Usings.ShouldBeEmpty();
  }
}
