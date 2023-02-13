namespace SuperNodes.Tests.PowerUpsFeature;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using SuperNodes.Common.Services;
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

    var classDeclaration = TestUtils.ParseAndFind<ClassDeclarationSyntax>(code);

    powerUpsRepo.IsPowerUpSyntaxCandidate(classDeclaration)
      .ShouldBeTrue();
  }

  [Fact]
  public void IsPowerUpSyntaxCandidateInvalidates() {
    var codeService = new Mock<ICodeService>();
    var powerUpsRepo = new PowerUpsRepo(codeService.Object);

    var code = """
      namespace SuperNodes.Tests.PowerUpsFeature {
        public class SomeTestClass {
        }
      }
    """;

    var classDeclaration = TestUtils.ParseAndFind<ClassDeclarationSyntax>(code);

    powerUpsRepo.IsPowerUpSyntaxCandidate(classDeclaration)
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
        public class {{@class}} {
          public void On{{@class}}(long what) {
          }
        }
      }
    """;

    var classDeclaration
      = TestUtils.ParseAndFind<ClassDeclarationSyntax, INamedTypeSymbol>(
        code, out var symbol
      );

    codeService.Setup(cs => cs.GetTypeParameters(classDeclaration))
      .Returns(ImmutableArray<string>.Empty);
    codeService.Setup(cs => cs.GetVisibleInterfacesFullyQualified(
      classDeclaration, symbol
    )).Returns(ImmutableHashSet<string>.Empty);
    codeService.Setup(cs => cs.GetContainingNamespace(symbol))
      .Returns(@namespace);
    codeService.Setup(cs => cs.GetUsings(symbol))
      .Returns(ImmutableHashSet<string>.Empty);
    codeService.Setup(cs => cs.GetPropsAndFields(symbol.GetMembers()!))
      .Returns(ImmutableArray<PropOrField>.Empty);

    var powerUp = powerUpsRepo.GetPowerUp(classDeclaration, symbol);

    powerUp.ShouldNotBeNull();
    powerUp.Namespace.ShouldBe(@namespace);
    powerUp.Name.ShouldBe(@class);
    powerUp.FullName.ShouldBe($"global::{@namespace}.{@class}");
    powerUp.Location.ShouldBe(classDeclaration.GetLocation());
    powerUp.BaseClass.ShouldBe("object");
    powerUp.TypeParameters.ShouldBeEmpty();
    powerUp.Interfaces.ShouldBeEmpty();
    powerUp.Source.ShouldBe(classDeclaration.ToString());
    powerUp.PropsAndFields.ShouldBeEmpty();
    powerUp.Usings.ShouldBeEmpty();
    powerUp.HasOnPowerUpMethod.ShouldBeTrue();
  }
}
