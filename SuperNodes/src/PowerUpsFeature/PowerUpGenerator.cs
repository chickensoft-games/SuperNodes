namespace SuperNodes.PowerUpsFeature;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperNodes.Common.Models;
using SuperNodes.Common.Utils;

public interface IPowerUpGenerator {
  /// <summary>PowerUp generator service.</summary>
  IPowerUpGeneratorService PowerUpGeneratorService { get; }

  /// <summary>
  /// Generates an applied PowerUp implementation on a specific SuperNode.
  /// </summary>
  /// <param name="powerUp">PowerUp to generate.</param>
  /// <param name="node">SuperNode to apply the PowerUp to.</param>
  /// <returns>Generated source string.</returns>
  string GeneratePowerUp(PowerUp powerUp, SuperNode node);
}

public class PowerUpGenerator : ChickensoftGenerator, IPowerUpGenerator {
  public IPowerUpGeneratorService PowerUpGeneratorService { get; }

  public PowerUpGenerator(IPowerUpGeneratorService powerUpGeneratorService) {
    PowerUpGeneratorService = powerUpGeneratorService;
  }

  public string GeneratePowerUp(PowerUp powerUp, SuperNode node) {
    // Edit the pieces of the user's power-up needed to make it suitable to be
    // a partial class of the specific node script it's applied to.

    var tree = CSharpSyntaxTree.ParseText(powerUp.Source);

    var typeParameterSubstitutions = new Dictionary<string, string>();
    for (var i = 0; i < powerUp.TypeParameters.Length; i++) {
      var typeParameter = powerUp.TypeParameters[i];
      var correspondingPowerUpHook =
        node.PowerUpHooksByFullName[powerUp.FullName];
      typeParameterSubstitutions[typeParameter] =
        correspondingPowerUpHook.TypeArguments[i];
    }

    var root = (CompilationUnitSyntax)tree.GetRoot();
    var classDeclaration = (ClassDeclarationSyntax)root.Members.First();
    var interfaces = powerUp.Interfaces;

    // Strip [PowerUp] attribute off the class declaration
    var newClassDeclaration = classDeclaration.WithAttributeLists(
      SyntaxFactory.List(
        classDeclaration.AttributeLists.Where(
          attributeList => attributeList.Attributes.All(
            attribute => attribute.Name.ToString()
              != Constants.POWER_UP_ATTRIBUTE_NAME
          )
        )
      )
    )
    // Change power up name to the node's name.
    .WithIdentifier(SyntaxFactory.Identifier(node.Name))
    .WithTypeParameterList(null)
    // Strip modifiers, add partial modifier back.
    .WithModifiers(new SyntaxTokenList())
    .AddModifiers(
      new[] {
        SyntaxFactory
          .Token(SyntaxKind.PartialKeyword)
          .WithTrailingTrivia(SyntaxFactory.Whitespace(" "))
      }
    )
    // Remove power-up constructors. Since PowerUps can constrain which classes
    // they are applicable on, PowerUp authors will sometimes have to provide
    // a parameterless constructor to satisfy Godot's node requirements.
    .WithMembers(
      SyntaxFactory.List(
        classDeclaration.Members.Where(
          member => member is not ConstructorDeclarationSyntax
        )
      )
    )
    // Remove base list
    .WithBaseList(null);
    // Remove static stubs with the same names as the static reflection tables
    // generated by SuperNodes. PowerUps that use static reflection will need
    // to supply stubs of the static reflection tables themselves since the
    // PowerUp must also be able to compile without being applied to a
    // SuperNode.
    //
    // Once static inheritance is implemented in .NET 7, it will not be
    // necessary for power-ups to declare stubs for the static reflection
    // tables.
    newClassDeclaration = newClassDeclaration.WithMembers(
      SyntaxFactory.List(
        newClassDeclaration.Members.Where(
          member => member.AttributeLists.All(
            attributeList => attributeList.Attributes.Any(
              attribute => attribute.Name.ToString()
                != Constants.POWER_UP_IGNORE_ATTRIBUTE_NAME
            )
          )
        )
      )
    );

    if (interfaces.Length > 0) {
      // Add only interfaces back to the base list.
      newClassDeclaration = newClassDeclaration.WithBaseList(
        SyntaxFactory.BaseList(
          SyntaxFactory.SeparatedList<BaseTypeSyntax>(
            interfaces.Select(
              @interface => SyntaxFactory.SimpleBaseType(
                SyntaxFactory.ParseTypeName(@interface)
              )
            ),
            Enumerable.Repeat(
              SyntaxFactory.Token(SyntaxKind.CommaToken)
                .WithTrailingTrivia(SyntaxFactory.Whitespace(" ")),
              interfaces.Length - 1
            )
          )
        ).WithTrailingTrivia(SyntaxFactory.Whitespace(" "))
      );
    }

    // Edit the user's power up class tree based on the above changes.
    root = root.ReplaceNode(classDeclaration, newClassDeclaration);
    tree = tree.WithRootAndOptions(root, tree.Options);

    var powerUpRewriter = PowerUpGeneratorService.CreatePowerUpRewriter(
      typeParameters: typeParameterSubstitutions.ToImmutableDictionary(),
      powerUpClassName: powerUp.Name,
      superNodeClassName: node.Name
    );

    // Rewrite the user's power up class tree to substitute type parameters
    // and static references to the power-up class with the specific node class
    tree = tree.WithRootAndOptions(
      powerUpRewriter.Visit(tree.GetRoot()), tree.Options
    );

    var allUsings = powerUp.Usings.Union(
      new string[] { "Godot" }
    ).Distinct();

    var usings = allUsings
      .Where(@using => @using.StartsWith("System"))
      .OrderBy(@using => @using)
      .Concat(
        allUsings
          .Where(@using => !@using.StartsWith("System"))
          .OrderBy(@using => @using)
      )
      .Select(@using => $"using {@using};").ToImmutableArray();

    // Get the modified source code of the PowerUp itself and format it.
    var source = tree
      .GetRoot()
      .NormalizeWhitespace("  ", "\n", true)
      .ToFullString()
      .NormalizeLineEndings("\n").Split('\n').ToImmutableArray();

    return Format($$"""
    #nullable enable
    {{usings}}

    {{If(
      node.Namespace is not null,
      $$"""namespace {{node.Namespace}} {"""
    )}}
      {{source}}
    {{If(node.Namespace is not null, "}")}}
    #nullable disable
    """);
  }
}
