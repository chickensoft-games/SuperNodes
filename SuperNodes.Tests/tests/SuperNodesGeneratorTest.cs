namespace SuperNodes.Tests;

using System.Linq;
using Shouldly;
using SuperNodes.Common.Utils;
using Xunit;

public class SuperNodesGeneratorTest {
  [Fact]
  public void InjectsPostInitSources() {
    var result = Tester.Generate("");

    foreach (var postInitSource in Constants.PostInitializationSources) {
      var filename = $"{postInitSource.Key}.g.cs";
      var contents = result.Outputs[filename];
      contents.Clean().ShouldBe(postInitSource.Value.Clean());
    }
  }

  [Fact]
  public void GeneralFeatureTest() {
    var source = """
    namespace Test;

    using Godot;

    [SuperNode(typeof(GeneralFeaturePowerUp<int>), "OtherGenerator")]
    public partial class GeneralFeatureSuperNode : Node {
      public override partial void _Notification(int what);

      public void OnReady() { }
    }

    [PowerUp]
    public partial class GeneralFeaturePowerUp<T>
      : Node, IGeneralFeaturePowerUp<T> {
      T IGeneralFeaturePowerUp<T>.TestValue { get; } = default!;
    }

    public interface IGeneralFeaturePowerUp<T> {
      T TestValue { get; }
    }

    public partial class GeneralFeatureSuperNode {
      public void OtherGenerator(int what) { }
    }
    """;

    var result = Tester.Generate(source);

    result.Diagnostics.ShouldBeEmpty();
    result.Outputs.Count.ShouldBe(7);
  }

  [Fact]
  public void ReportsMissingPartialNotificationMethod() {
    var source = """
    namespace Test;

    using Godot;

    [SuperNode(typeof(GeneralFeaturePowerUp<int>), "OtherGenerator")]
    public partial class GeneralFeatureSuperNode : Node {
      // public override partial void _Notification(int what);

      public void OnReady() { }
    }

    [PowerUp]
    public partial class GeneralFeaturePowerUp<T>
      : Node, IGeneralFeaturePowerUp<T> {
      T IGeneralFeaturePowerUp<T>.TestValue { get; } = default!;
    }

    public interface IGeneralFeaturePowerUp<T> {
      T TestValue { get; }
    }

    public partial class GeneralFeatureSuperNode {
      public void OtherGenerator(int what) { }
    }
    """;

    var result = Tester.Generate(source);

    result.Diagnostics.Any(
      diagnostic => diagnostic.Id ==
        Constants.SUPER_NODE_MISSING_NOTIFICATION_METHOD
    ).ShouldBeTrue();
  }

  [Fact]
  public void ReportsInvalidPowerUpUsage() {
    var source = """
    namespace Test;

    using Godot;

    [SuperNode(typeof(GeneralFeaturePowerUp<int>), "OtherGenerator")]
    public partial class GeneralFeatureSuperNode : Node3D {
      public override partial void _Notification(int what);

      public void OnReady() { }
    }

    [PowerUp]
    public partial class GeneralFeaturePowerUp<T>
      : Node2D, IGeneralFeaturePowerUp<T> {
      T IGeneralFeaturePowerUp<T>.TestValue { get; } = default!;
    }

    public interface IGeneralFeaturePowerUp<T> {
      T TestValue { get; }
    }

    public partial class GeneralFeatureSuperNode {
      public void OtherGenerator(int what) { }
    }
    """;

    var result = Tester.Generate(source);

    result.Diagnostics.Any(
      diagnostic => diagnostic.Id ==
        Constants.SUPER_NODE_INVALID_POWER_UP
    ).ShouldBeTrue();
  }
}
