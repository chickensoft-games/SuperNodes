namespace SuperNodes.Tests.PowerUpsFeature;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using SuperNodes.Common.Utils;
using SuperNodes.PowerUpsFeature;
using Xunit;

public class PowerUpRewriterTest {
  [Fact]
  public void RewritesPowerUpAppropriately() {
    var powerUpName = "TestPowerUp";
    var superNodeName = "TestSuperNode";

    var code = $$"""
    namespace Tests {
      [PowerUp]
      public class {{powerUpName}}<TA, TB> {
        public static string Value = "Hi.";
        TA ExecuteA(TA item) {
          {{powerUpName}}.Value = "Hello.";
          OtherGenericClass<TA>.Value = "Goodbye.";
          {{powerUpName}}<TA, TB>.Value = "Whatever.";
          return item;
        }
        TB ExecuteB(TB item) => item;
      }
    }
    """;

    var classDecl = Tester.Parse<ClassDeclarationSyntax>(code);

    var rewriter = new DefaultPowerUpRewriter(
      typeParameters: new Dictionary<string, string>() {
        ["TA"] = "string",
        ["TB"] = "int"
      }.ToImmutableDictionary(),
      powerUpClassName: powerUpName,
      superNodeClassName: superNodeName
    );

    // PowerUpRewriter doesn't change class name, remove PowerUpAttribute, and
    // drop off type parameters. That's done outside it.
    var newClassDecl = classDecl
      .WithIdentifier(SyntaxFactory.Identifier(superNodeName))
      .WithTypeParameterList(null)
      .WithAttributeLists(SyntaxFactory.List(
        classDecl.AttributeLists.Where(
          attributeList => attributeList.Attributes.All(
            attribute => attribute.Name.ToString() !=
              Constants.POWER_UP_ATTRIBUTE_NAME
          )
        )
      ));

    var tree = classDecl.SyntaxTree;
    var root = tree.GetRoot();
    root = root.ReplaceNode(classDecl, newClassDecl);
    tree = tree.WithRootAndOptions(root, tree.Options);
    tree = tree.WithRootAndOptions(
      rewriter.Visit(tree.GetRoot()), tree.Options
    );

    root = tree.GetRoot();

    // Get generated source code.
    var source = root
      .NormalizeWhitespace(indentation: "  ")
      .ToFullString()
      .NormalizeLineEndings();

    source.ShouldBe($$"""
    namespace Tests
    {
      public class {{superNodeName}}
      {
        public static string Value = "Hi.";
        string ExecuteA(string item)
        {
          {{superNodeName}}.Value = "Hello.";
          OtherGenericClass<string>.Value = "Goodbye.";
          {{superNodeName}}.Value = "Whatever.";
          return item;
        }

        int ExecuteB(int item) => item;
      }
    }
    """.NormalizeLineEndings());
  }
}
