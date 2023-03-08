namespace SuperNodes.Tests.PowerUpsFeature;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
using SuperNodes.Common.Utils;
using SuperNodes.PowerUpsFeature;
using Xunit;

public class PowerUpsRepoTest {
  [Fact]
  public void Initializes() {
    var codeService = new Mock<ICodeService>();
    var powerUpsRepo = new PowerUpsRepo(codeService.Object);

    powerUpsRepo.CodeService.ShouldBe(codeService.Object);
  }

  [Fact]
  public void IsPowerUpSyntaxCandidateValidates() {
    var codeService = new Mock<ICodeService>();
    var powerUpsRepo = new PowerUpsRepo(codeService.Object);

    var code = """
      namespace SuperNodes.Tests.PowerUpsFeature {
        [PowerUp]
        public class TestPowerUp {
        }
      }
    """;

    var classDeclaration = Tester.Parse<ClassDeclarationSyntax>(code);

    powerUpsRepo.IsPowerUpSyntaxCandidate(classDeclaration)
      .ShouldBeTrue();
  }

  [Fact]
  public void IsPowerUpSyntaxCandidateInvalidates() {
    var codeService = new Mock<ICodeService>();
    var powerUpsRepo = new PowerUpsRepo(codeService.Object);

    var code = """
      namespace SuperNodes.Tests.PowerUpsFeature {
        public struct SomeTestClass {
        }
      }
    """;

    var node = Tester.Parse<StructDeclarationSyntax>(code);

    powerUpsRepo.IsPowerUpSyntaxCandidate(node)
      .ShouldBeFalse();
  }

  [Fact]
  public void GetPowerUpGetsPowerUpWithSymbol() {
    var codeService = new Mock<ICodeService>();
    var powerUpsRepo = new PowerUpsRepo(codeService.Object);
    var @class = "TestPowerUp";
    var @namespace = "SuperNodes.Tests.PowerUpsFeature";

    var code = $$"""
      namespace {{@namespace}} {
        [PowerUp]
        public class {{@class}} : OtherClass {
          public string Property { get; set; } = "";
          public void On{{@class}}(int what) {
          }
        }

        public class OtherClass { }
      }
    """;

    var node = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      code, out var symbol
    );

    var name = node.Identifier.ValueText;
    var fullName = $"global::SuperNodes.Tests.PowerUpsFeature.{@class}";
    var baseClass = "global::SuperNodes.Tests.PowerUpsFeature.OtherClass";
    var members = symbol.GetMembers();

    codeService.Setup(cs => cs.GetNameFullyQualified(symbol, name))
      .Returns(fullName);
    codeService
      .Setup(cs => cs.GetBaseTypeFullyQualified(symbol, Constants.BaseClass))
      .Returns(baseClass);
    // redo these
    codeService.Setup(cs => cs.GetTypeParameters(node))
      .Returns(ImmutableArray<string>.Empty);
    codeService.Setup(cs => cs.GetVisibleInterfacesFullyQualified(
      node, symbol
    )).Returns(ImmutableArray<string>.Empty);
    codeService.Setup(cs => cs.GetContainingNamespace(symbol))
      .Returns(@namespace);
    codeService.Setup(cs => cs.GetUsings(symbol))
      .Returns(ImmutableHashSet<string>.Empty);
    codeService.Setup(cs => cs.GetMembers(symbol))
      .Returns(members);
    codeService.Setup(cs => cs.GetPropsAndFields(members))
      .Returns(ImmutableArray<PropOrField>.Empty);

    var powerUp = powerUpsRepo.GetPowerUp(node, symbol);

    powerUp.ShouldNotBeNull();
    powerUp.Namespace.ShouldBe(@namespace);
    powerUp.Name.ShouldBe(@class);
    powerUp.FullName.ShouldBe($"global::{@namespace}.{@class}");
    powerUp.Location.ShouldBe(node.GetLocation());
    powerUp.BaseClass.ShouldBe(
      "global::SuperNodes.Tests.PowerUpsFeature.OtherClass"
    );
    powerUp.TypeParameters.ShouldBeEmpty();
    powerUp.Interfaces.ShouldBeEmpty();
    powerUp.Source.ShouldBe(node.ToString());
    powerUp.PropsAndFields.ShouldBeEmpty();
    powerUp.Usings.ShouldBeEmpty();
    powerUp.HasOnPowerUpMethod.ShouldBeTrue();
  }
}
