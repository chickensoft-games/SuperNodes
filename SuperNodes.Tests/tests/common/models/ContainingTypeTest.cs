namespace SuperNodes.Tests.Common.Models;

using System;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class ContainingTypeTest {
  [Fact]
  public void Initializes() {
    var containingType = new ContainingType(
      FullName: "TestClass",
      Kind: ContainingTypeKind.Record,
      Accessibility: Microsoft.CodeAnalysis.Accessibility.Public,
      IsPartial: false
    );

    containingType.TypeDeclarationKeyword.ShouldBe("record");
    containingType.AccessibilityKeywords.ShouldBe("public");
  }

  [Fact]
  public void GetTypeDeclarationKeyword() {
    ContainingType.GetTypeDeclarationKeyword(
      ContainingTypeKind.Record
    ).ShouldBe("record");

    ContainingType.GetTypeDeclarationKeyword(
      ContainingTypeKind.Class
    ).ShouldBe("class");

    Should.Throw<ArgumentException>(
      () => ContainingType.GetTypeDeclarationKeyword(
        (ContainingTypeKind)3
      )
    );
  }

  [Fact]
  public void GetAccessibilityKeywords() {
    ContainingType.GetAccessibilityKeywords(
      Microsoft.CodeAnalysis.Accessibility.Public
    ).ShouldBe("public");

    ContainingType.GetAccessibilityKeywords(
      Microsoft.CodeAnalysis.Accessibility.Protected
    ).ShouldBe("protected");

    ContainingType.GetAccessibilityKeywords(
      Microsoft.CodeAnalysis.Accessibility.Internal
    ).ShouldBe("internal");

    ContainingType.GetAccessibilityKeywords(
      Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal
    ).ShouldBe("protected internal");

    ContainingType.GetAccessibilityKeywords(
      Microsoft.CodeAnalysis.Accessibility.Private
    ).ShouldBe("private");

    ContainingType.GetAccessibilityKeywords(
      Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal
    ).ShouldBe("private protected");

    Should.Throw<ArgumentException>(
      () => ContainingType.GetAccessibilityKeywords(
        (Microsoft.CodeAnalysis.Accessibility)7
      )
    );
  }
}
