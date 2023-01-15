namespace SuperNodes.Tests;

using System.Linq;
using Shouldly;
using Xunit;

public class SuperNodesGeneratorTest {
  [Fact]
  public void InjectsAttributes() {
    var result = TestUtils.Generate("");

    result.Outputs.Any(
      output => output.Contains("class SuperNodeAttribute : Attribute")
    ).ShouldBeTrue();

    result.Outputs.Any(
      output => output.Contains("class PowerUpAttribute : Attribute")
    ).ShouldBeTrue();

    result.Diagnostics.ShouldBeEmpty();
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
      public partial class MyNode
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
    """)).ShouldBeTrue();

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
      public partial class MyNode
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
    """)).ShouldBeTrue();

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
      public partial class MyNode
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
    """)).ShouldBeTrue();

    result.Outputs.Any(output => output.Contains("""
    #nullable enable
    using Godot;

    namespace GeneratorTest
    {
      public partial class MyNode
      {
        public string AddedProperty { get; set; } = "";
        public void OnMyPowerUp(long what)
        {
        }
      }
    }
    #nullable disable
    """)).ShouldBeTrue();

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
      public partial class MyNode
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
    """)).ShouldBeTrue();
  }
}
