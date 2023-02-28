namespace SuperNodes.Tests.PowerUpsFeature;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using SuperNodes.Common.Utils;
using SuperNodes.PowerUpsFeature;
using Xunit;

public class PowerUpGeneratorTest {
  [Fact]
  public void Initializes() {
    var genService = new Mock<IPowerUpGeneratorService>();
    var generator = new PowerUpGenerator(genService.Object);
    generator.ShouldBeAssignableTo<IPowerUpGenerator>();
    generator.PowerUpGeneratorService.ShouldBe(genService.Object);
  }

  [Fact]
  public void GeneratesPowerUp() {
    var code = """
    [PowerUp]
    public partial class TestPowerUp<TA, TB>
      : Godot.Node, ITestPowerUp<TA, TB> {
      internal static ScriptPropertyOrField[] PropertiesAndFields { get; }
        = default!;
      internal static TResult GetScriptPropertyOrFieldType<TResult>(
        string scriptProperty, ITypeReceiver<TResult> receiver
      ) => default!;

      [PowerUpIgnore]
      public int IgnoreMe { get; set; } = 1;

      public string AddedProperty { get; set; } = "Property";

      private readonly int _addedField = 10;

      public static int MyNumber { get; set; } = 42;

      public void OnTestPowerUp(int what) {
        TestPowerUp<TA, TB>.MyNumber = 666;
        if (what == NotificationReady) {
          GD.Print("Hello, TestPowerUp!");
        }
      }
    }
    """;

    var usings = new string[] {
      "Godot",
      "System",
      "System.Collections.Generic",
      "System.Collections.Generic" // duplicate — should be ignored.
    }.ToImmutableHashSet();

    var powerUp = new PowerUp(
      Namespace: "Tests",
      Name: "TestPowerUp",
      FullName: "global::Tests.TestPowerUp",
      Location: new Mock<Location>().Object,
      BaseClass: "global::Godot.Node",
      TypeParameters: new string[] { "TA", "TB" }.ToImmutableArray(),
      Interfaces: new string[] {
        "global::Tests.ITestPowerUp<TA, TB>"
      }.ToImmutableArray(),
      Source: code,
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: new string[] {
        "Godot", "System.Collections"
      }.ToImmutableHashSet(),
      HasOnPowerUpMethod: true
    );

    var powerUpHooksByFullName = new Dictionary<string, PowerUpHook>() {
      ["global::Tests.TestPowerUp"] = new PowerUpHook(
        FullName: "global::Tests.TestPowerUp",
        TypeArguments: new string[] { "bool", "int" }.ToImmutableArray()
      )
    }.ToImmutableDictionary();

    var superNode = new SuperNode(
      Namespace: "Tests",
      Name: "TestSuperNode",
      Location: new Mock<Location>().Object,
      BaseClasses: new string[] { "global::Godot.Node" }.ToImmutableArray(),
      LifecycleHooks: ImmutableArray<IGodotNodeLifecycleHook>.Empty,
      PowerUpHooksByFullName: powerUpHooksByFullName,
      NotificationHandlers: ImmutableArray<string>.Empty,
      HasPartialNotificationMethod: true,
      HasOnNotificationMethodHandler: true,
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: ImmutableHashSet<string>.Empty
    );

    var genService = new Mock<IPowerUpGeneratorService>();
    var generator = new PowerUpGenerator(genService.Object);

    var rewriter = new Mock<PowerUpRewriter>();

    genService.Setup(gs => gs.CreatePowerUpRewriter(
      It.IsAny<ImmutableDictionary<string, string>>(),
      powerUp.Name,
      superNode.Name
    )).Returns(rewriter.Object);

    var rewrittenTreeRoot = CSharpSyntaxTree.ParseText("""
    partial class TestSuperNode:global::Tests.ITestPowerUp<bool, int> {
      public string AddedProperty { get; set; } = "Property";

      private readonly int _addedField = 10;

      public static int MyNumber { get; set; } = 42;

      public void OnTestPowerUp(int what) {
        TestSuperNode.MyNumber = 666;
        if (what == NotificationReady) {
          GD.Print("Hello, TestPowerUp!");
        }
      }
    }
    """).GetRoot();

    rewriter.Setup(rw => rw.Visit(It.IsAny<SyntaxNode>()))
      .Returns(rewrittenTreeRoot);

    var source = generator.GeneratePowerUp(powerUp, superNode);

    source.ShouldBe("""
    #nullable enable
    using System.Collections;
    using Godot;

    namespace Tests {
      partial class TestSuperNode : global::Tests.ITestPowerUp<bool, int>
      {
        public string AddedProperty { get; set; } = "Property";
        private readonly int _addedField = 10;
        public static int MyNumber { get; set; } = 42;
        public void OnTestPowerUp(int what)
        {
          TestSuperNode.MyNumber = 666;
          if (what == NotificationReady)
          {
            GD.Print("Hello, TestPowerUp!");
          }
        }
      }
    }
    #nullable disable
    """.NormalizeLineEndings());
  }
}
