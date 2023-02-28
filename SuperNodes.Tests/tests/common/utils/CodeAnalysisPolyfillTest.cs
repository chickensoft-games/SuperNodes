namespace SuperNodes.Common.Utils;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Shouldly;
using SuperNodes.Tests;
using Xunit;

public class CodeAnalysisPolyfillTest {
  [Fact]
  public void ReturnsEmpty() {
    var @class = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      "public class Test { }", out var symbol
    );

    symbol.ExplicitOrImplicitInterfaceImplementations().ShouldBeEmpty();
  }

  [Fact]
  public void FindsMember() {
    var @class = Tester.Parse<ClassDeclarationSyntax, INamedTypeSymbol>(
      """
        public class Test : ITest {
          public bool TestProperty { get; } = true;
        }

        public interface ITest {
          bool TestProperty { get; }
        }
      """,
      out var symbol
    );

    var member = symbol.GetMembers("TestProperty").Single();
    var implementations = member.ExplicitOrImplicitInterfaceImplementations();

    implementations.Length.ShouldBe(1);
  }
}
