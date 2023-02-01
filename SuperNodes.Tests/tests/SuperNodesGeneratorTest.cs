namespace SuperNodes.Tests;

using System.Linq;
using Shouldly;
using Xunit;

public class SuperNodesGeneratorTest {
  [Fact]
  public void InjectsRequiredCode() {
    var result = TestUtils.Generate("");

    result.Outputs.Any(
      output => output.Contains("class SuperNodeAttribute : Attribute")
    ).ShouldBeTrue();

    result.Outputs.Any(
      output => output.Contains("class PowerUpAttribute : Attribute")
    ).ShouldBeTrue();

    result.Outputs.Any(output => output.Contains("ITypeReceiver"))
      .ShouldBeTrue();
    result.Outputs.Any(
      output => output.Contains("record ScriptAttributeDescription")
    ).ShouldBeTrue();
    result.Outputs.Any(
      output => output.Contains("record struct ScriptPropertyOrField")
    ).ShouldBeTrue();

    result.Diagnostics.ShouldBeEmpty();
  }

  [Fact]
  public void GeneratesSuperNodeStaticImplementation() {
    var result = TestUtils.Generate("""
    namespace GeneratorTest;

    using System;
    using Godot;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExampleAttribute : Attribute {
      public string A { get; }
      public int B { get; }
      public bool C { get; }
      public ExampleAttribute(string a = "", int b = 0, bool c = false) {
        A = a;
        B = b;
        C = c;
      }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PlainExampleAttribute : Attribute { }

    [SuperNode]
    public partial class OtherNode : Node {
      public override partial void _Notification(long what);

      [Example(c: true)]
      public string PropertyA { get; set; } = "hello, world!";

      [Example("hello", 1, true)]
      public int PropertyB { get; set; } = 1;

      [PlainExample]
      public int PropertyC { get; set; } = 1;

      private float _fieldA = 1.0f;

      [Example(b: 1, c: true)]
      private float _fieldB = 1.0f;

      [PlainExample]
      private float _fieldC = 1.0f;

      public void OnReady() { }

      public void OnProcess(double delta) { }
    }
    """);

    var text =
"""
#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace GeneratorTest {
  partial class OtherNode {
    /// <summary>
    /// A list of all properties and fields on this node script, along with
    /// basic information about the member and its attributes.
    /// This is provided to allow PowerUps to access script member data
    /// without having to resort to reflection.
    /// </summary>
    internal static ScriptPropertyOrField[] PropertiesAndFields
    = new ScriptPropertyOrField[] {
      new ScriptPropertyOrField(
        "_fieldA",
        typeof(float),
        new Dictionary<string, ScriptAttributeDescription>()
      ),
      new ScriptPropertyOrField(
        "_fieldB",
        typeof(float),
        new Dictionary<string, ScriptAttributeDescription>() {
          ["global::GeneratorTest.ExampleAttribute"] =
            new ScriptAttributeDescription(
              "ExampleAttribute",
              typeof(global::GeneratorTest.ExampleAttribute),
              ImmutableArray.Create<dynamic>(
                "",
                1,
                true
              )
            )
        }.ToImmutableDictionary()
      ),
      new ScriptPropertyOrField(
        "_fieldC",
        typeof(float),
        new Dictionary<string, ScriptAttributeDescription>() {
          ["global::GeneratorTest.PlainExampleAttribute"] =
            new ScriptAttributeDescription(
              "PlainExampleAttribute",
              typeof(global::GeneratorTest.PlainExampleAttribute),
              Array.Empty<dynamic>().ToImmutableArray()
            )
        }.ToImmutableDictionary()
      ),
      new ScriptPropertyOrField(
        "PropertyA",
        typeof(string),
        new Dictionary<string, ScriptAttributeDescription>() {
          ["global::GeneratorTest.ExampleAttribute"] =
            new ScriptAttributeDescription(
              "ExampleAttribute",
              typeof(global::GeneratorTest.ExampleAttribute),
              ImmutableArray.Create<dynamic>(
                "",
                0,
                true
              )
            )
        }.ToImmutableDictionary()
      ),
      new ScriptPropertyOrField(
        "PropertyB",
        typeof(int),
        new Dictionary<string, ScriptAttributeDescription>() {
          ["global::GeneratorTest.ExampleAttribute"] =
            new ScriptAttributeDescription(
              "ExampleAttribute",
              typeof(global::GeneratorTest.ExampleAttribute),
              ImmutableArray.Create<dynamic>(
                "hello",
                1,
                true
              )
            )
        }.ToImmutableDictionary()
      ),
      new ScriptPropertyOrField(
        "PropertyC",
        typeof(int),
        new Dictionary<string, ScriptAttributeDescription>() {
          ["global::GeneratorTest.PlainExampleAttribute"] =
            new ScriptAttributeDescription(
              "PlainExampleAttribute",
              typeof(global::GeneratorTest.PlainExampleAttribute),
              Array.Empty<dynamic>().ToImmutableArray()
            )
        }.ToImmutableDictionary()
      )
    };

    /// <summary>
    /// Calls the given type receiver with the generic type of the given
    /// script property or field. Generated by SuperNodes.
    /// </summary>
    /// <typeparam name="TResult">The return type of the type receiver's
    /// receive method.</typeparam>
    /// <param name="scriptProperty">The name of the script property or field
    /// to get the type of.</param>
    /// <param name="receiver">The type receiver to call with the type
    /// of the script property or field.</param>
    /// <returns>The result of the type receiver's receive method.</returns>
    /// <exception cref="System.ArgumentException">Thrown if the given script
    /// property or field does not exist.</exception>
    internal static TResult GetScriptPropertyOrFieldType<TResult>(
      string scriptProperty, ITypeReceiver<TResult> receiver
    ) {
      switch (scriptProperty) {
        case "_fieldA":
          return receiver.Receive<float>();
        case "_fieldB":
          return receiver.Receive<float>();
        case "_fieldC":
          return receiver.Receive<float>();
        case "PropertyA":
          return receiver.Receive<string>();
        case "PropertyB":
          return receiver.Receive<int>();
        case "PropertyC":
          return receiver.Receive<int>();
        default:
          throw new System.ArgumentException(
            $"No field or property named '{scriptProperty}' was found on OtherNode."
          );
      }
    }
  }
}
#nullable disable
""".ReplaceLineEndings();

    // Even with line ending normalization, sometimes whitespace is still off
    var fuzz = FuzzySharp.Process.ExtractOne(text, result.Outputs);
    fuzz.Score.ShouldBeGreaterThanOrEqualTo(100);
  }

  [Fact]
  public void GeneratesSuperNodeImplementation() {
    var result = TestUtils.Generate("""
    namespace GeneratorTest;

    using Godot;

    [SuperNode]
    public partial class MyNode : Node {
      public override partial void _Notification(long what);

      public void OnReady() { }

      public void OnProcess(double delta) { }
    }
    """);

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace GeneratorTest
    {
      partial class MyNode
      {
        public override partial void _Notification(long what)
        {
          // Invoke any notification handlers declared in the script.
          switch (what)
          {
            case NotificationReady:
              OnReady();
              break;
            case NotificationProcess:
              OnProcess(GetProcessDeltaTime());
              break;
            default:
              break;
          }
        }
      }
    }
    #nullable disable
    """.ReplaceLineEndings())).ShouldBeTrue();

    result.Diagnostics.ShouldBeEmpty();
  }

  [Fact]
  public void ReportsMissingPartialNotificationMethod() {
    var result = TestUtils.Generate("""
    namespace GeneratorTest;

    using Godot;

    [SuperNode]
    public partial class MyNode : Node {
      public void OnReady() { }

      public void OnProcess(double delta) { }
    }
    """);

    result.Diagnostics.Any(
      diagnostic => diagnostic.Id ==
        SuperNodesGenerator.SUPER_NODE_MISSING_NOTIFICATION_METHOD
    ).ShouldBeTrue();
  }

  [Fact]
  public void GeneratesMethodInvocationsForThirdPartySourceGenerators() {
    var result = TestUtils.Generate("""
    namespace GeneratorTest;

    using Godot;

    [SuperNode("GeneratorA", "GeneratorB")]
    public partial class MyNode : Node {
      public override partial void _Notification(long what);

      public void OnReady() { }

      public void OnProcess(double delta) { }
    }
    """);

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace GeneratorTest
    {
      partial class MyNode
      {
        public override partial void _Notification(long what)
        {
          // Invoke declared lifecycle method handlers.
          GeneratorA(what);
          GeneratorB(what);
          // Invoke any notification handlers declared in the script.
          switch (what)
          {
            case NotificationReady:
              OnReady();
              break;
            case NotificationProcess:
              OnProcess(GetProcessDeltaTime());
              break;
            default:
              break;
          }
        }
      }
    }
    #nullable disable
    """.ReplaceLineEndings())).ShouldBeTrue();

    result.Diagnostics.ShouldBeEmpty();
  }

  [Fact]
  public void GeneratesAndAppliesPowerUp() {
    var result = TestUtils.Generate("""
    namespace GeneratorTest;

    using Godot;

    [SuperNode(nameof(MyPowerUp))]
    public partial class MyNode : Node {
      public override partial void _Notification(long what);

      public void OnReady() { }

      public void OnProcess(double delta) { }
    }

    [PowerUp]
    public partial class MyPowerUp : Node {
      public string AddedProperty { get; set; } = "";
      public void OnMyPowerUp(long what) { }
    }
    """);

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace GeneratorTest
    {
      partial class MyNode
      {
        public override partial void _Notification(long what)
        {
          // Invoke declared lifecycle method handlers.
          OnMyPowerUp(what);
          // Invoke any notification handlers declared in the script.
          switch (what)
          {
            case NotificationReady:
              OnReady();
              break;
            case NotificationProcess:
              OnProcess(GetProcessDeltaTime());
              break;
            default:
              break;
          }
        }
      }
    }
    #nullable disable
    """.ReplaceLineEndings())).ShouldBeTrue();

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace GeneratorTest
    {
      partial class MyNode
      {
        public string AddedProperty { get; set; } = "";
        public void OnMyPowerUp(long what)
        {
        }
      }
    }
    #nullable disable
    """.ReplaceLineEndings())).ShouldBeTrue();

    result.Diagnostics.ShouldBeEmpty();
  }

  [Fact]
  public void GeneratesAndAppliesPowerUpWithInterfaces() {
    var result = TestUtils.Generate("""
    namespace GeneratorTest;

    using Godot;

    [SuperNode(nameof(MyPowerUp))]
    public partial class MyNode : Node {
      public override partial void _Notification(long what);

      public void OnReady() { }

      public void OnProcess(double delta) { }
    }

    [PowerUp]
    public partial class MyPowerUp : Node, IMyPowerUp, IOther {
      public string AddedProperty { get; set; } = "";
      public void OnMyPowerUp(long what) { }
    }
    """);

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace GeneratorTest
    {
      partial class MyNode
      {
        public override partial void _Notification(long what)
        {
          // Invoke declared lifecycle method handlers.
          OnMyPowerUp(what);
          // Invoke any notification handlers declared in the script.
          switch (what)
          {
            case NotificationReady:
              OnReady();
              break;
            case NotificationProcess:
              OnProcess(GetProcessDeltaTime());
              break;
            default:
              break;
          }
        }
      }
    }
    #nullable disable
    """.ReplaceLineEndings())).ShouldBeTrue();

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace GeneratorTest
    {
      partial class MyNode : IMyPowerUp, IOther
      {
        public string AddedProperty { get; set; } = "";
        public void OnMyPowerUp(long what)
        {
        }
      }
    }
    #nullable disable
    """.ReplaceLineEndings())).ShouldBeTrue();

    result.Diagnostics.ShouldBeEmpty();
  }

  [Fact]
  public void ReportsInvalidPowerUpUse() {
    var result = TestUtils.Generate("""
    namespace GeneratorTest;

    using Godot;

    [SuperNode(nameof(MyPowerUp))]
    public partial class MyNode : Node2D {
      public override partial void _Notification(long what);

      public void OnReady() { }

      public void OnProcess(double delta) { }
    }

    [PowerUp]
    public partial class MyPowerUp : Node3D {
      public string AddedProperty { get; set; } = "";
      public void OnMyPowerUp(long what) { }
    }
    """);

    result.Diagnostics.Any(
      diagnostic => diagnostic.Id ==
        SuperNodesGenerator.SUPER_NODE_INVALID_POWER_UP
    ).ShouldBeTrue();
  }

  [Fact]
  public void GeneratesHandlersAndOnNotificationInCorrectOrder() {
    var result = TestUtils.Generate("""
    namespace SuperNodes.Example;

    using Godot;

    [SuperNode(nameof(MyPowerUp), "OtherGeneratorMethod")]
    public partial class MyNode : Node {
      public override partial void _Notification(long what);

      public void OnReady() { }
      public void OnProcess(double _) { }
      public void OnNotification(long what) { }
    }

    [PowerUp]
    public partial class MyPowerUp : Node {
      public string AddedProperty { get; set; } = "";
      public void OnMyPowerUp(long _) { }
    }
    """);

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace SuperNodes.Example
    {
      partial class MyNode
      {
        public override partial void _Notification(long what)
        {
          // Invoke declared lifecycle method handlers.
          OnMyPowerUp(what);
          OtherGeneratorMethod(what);
          // Invoke the notification handler in the script.
          OnNotification(what);
          // Invoke any notification handlers declared in the script.
          switch (what)
          {
            case NotificationReady:
              OnReady();
              break;
            case NotificationProcess:
              OnProcess(GetProcessDeltaTime());
              break;
            default:
              break;
          }
        }
      }
    }
    #nullable disable
    """.ReplaceLineEndings())).ShouldBeTrue();
  }
}
