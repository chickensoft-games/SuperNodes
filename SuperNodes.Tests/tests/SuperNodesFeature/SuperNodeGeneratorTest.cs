namespace SuperNodes.Tests.SuperNodesFeature;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using SuperNodes.Common.Utils;
using SuperNodes.SuperNodesFeature;
using Xunit;

public class SuperNodeGeneratorTest {
  [Fact]
  public void Initializes() {
    var genService = new SuperNodeGeneratorService();
    var generator = new SuperNodeGenerator(genService);

    generator.SuperNodeGeneratorService.ShouldBe(genService);
  }

  [Fact]
  public void GeneratesSuperNode() {
    var genService = new Mock<ISuperNodeGeneratorService>();
    var generator = new SuperNodeGenerator(genService.Object);

    var lifecycleHooks = ImmutableArray.Create<IGodotNodeLifecycleHook>();
    var notificationHandlers = ImmutableArray.Create<string>();
    var powerUps = ImmutableDictionary.Create<string, PowerUp>();

    var item = new SuperNodeGenerationItem(
      SuperNode: new SuperNode(
        Namespace: "global::Tests",
        Name: "TestSuperNode",
        NameWithoutGenerics: "TestSuperNode",
        Location: new Mock<Location>().Object,
        BaseClasses: new string[] { "global::Godot.Object" }.ToImmutableArray(),
        LifecycleHooks: lifecycleHooks,
        PowerUpHooksByFullName: ImmutableDictionary<string, PowerUpHook>.Empty,
        NotificationHandlers: notificationHandlers,
        HasPartialNotificationMethod: true,
        HasOnNotificationMethodHandler: true,
        PropsAndFields: ImmutableArray<PropOrField>.Empty,
        Usings: ImmutableHashSet<string>.Empty,
        ContainingTypes: ImmutableArray<ContainingType>.Empty
      ),
      PowerUps: powerUps
    );

    genService.Setup(gs => gs.GenerateLifecycleInvocations(
      lifecycleHooks,
      powerUps
    )).Returns(ImmutableArray.Create("One(what);", "Two(what);"));

    genService.Setup(gs => gs.GenerateNotificationHandlers(
      notificationHandlers
    )).Returns(
        """
        switch ((long)what) {
          case NotificationReady:
            OnReady();
            break;
          case NotificationProcess:
            OnProcess(GetProcessDeltaTime());
            break;
          default:
            break;
        }
        """.NormalizeLineEndings("\n").Split("\n").ToImmutableArray()
    );

    var source = generator.GenerateSuperNode(item);

    source.ShouldBe("""
    #pragma warning disable
    #nullable enable
    using SuperNodes.Types;

    namespace global::Tests {
      partial class TestSuperNode {
        public override partial void _Notification(int what) {
          // Invoke declared lifecycle method handlers.
          One(what);
          Two(what);
          // Invoke the notification handler in the script.
          OnNotification(what);
          // Invoke any notification handlers declared in the script.
          switch ((long)what) {
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
    #pragma warning restore
    """.NormalizeLineEndings());
  }

  [Fact]
  public void GeneratesSuperNodeStaticReflectionTables() {
    var genService = new Mock<ISuperNodeGeneratorService>();
    var generator = new SuperNodeGenerator(genService.Object);

    var propsAndFields = new PropOrField[] {
      new PropOrField(
        Name: "SomeProperty",
        Reference: "SomeProperty",
        Type: "int",
        Attributes: new AttributeDescription[] {
          new AttributeDescription(
            Name: "Export",
            Type: "global::Godot.ExportAttribute",
            ArgumentExpressions: new string[] {
              "PropertyHint.Range", "\"0, 100, 1\""
            }.ToImmutableArray()
          ),
          new AttributeDescription(
            Name: "Serializable",
            Type: "global::System.SerializableAttribute",
            ArgumentExpressions: ImmutableArray<string>.Empty
          )
        }.ToImmutableArray(),
        IsField: false,
        IsMutable: true,
        IsReadable: true,
        NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
        TypeParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty
      ),
      new PropOrField(
        Name: "_someField",
        Reference: "_someField",
        Type: "string",
        Attributes: ImmutableArray<AttributeDescription>.Empty,
        IsField: true,
        IsMutable: true,
        IsReadable: true,
        NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
        TypeParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty
      )
    }.ToImmutableArray();

    var usings = new string[] {
      "Godot",
      "System",
      "System.Collections.Generic",
      "System.Collections.Generic" // duplicate — should be ignored.
    }.ToImmutableHashSet();

    var powerUp = new PowerUp(
      Namespace: null,
      Name: "TestPowerUp",
      FullName: "global::TestPowerUp",
      Location: new Mock<Location>().Object,
      BaseClass: "global::Godot.Object",
      TypeParameters: ImmutableArray<string>.Empty,
      Interfaces: ImmutableArray<string>.Empty,
      Source: "[PowerUp] public class TestPowerUp {}",
      PropsAndFields: ImmutableArray<PropOrField>.Empty,
      Usings: new string[] {
        "Godot", "System.Collections"
      }.ToImmutableHashSet(),
      HasOnPowerUpMethod: true // Has lifecycle handler.
    );

    var appliedPowerUps = new PowerUp[] { powerUp }.ToImmutableArray();

    var powerUps = new Dictionary<string, PowerUp> {
      [powerUp.FullName] = powerUp
    }.ToImmutableDictionary();

    var powerUpHooksByFullName = new Dictionary<string, PowerUpHook> {
      [powerUp.FullName] = new PowerUpHook(
        FullName: powerUp.FullName,
        TypeArguments: System.Array.Empty<string>().ToImmutableArray()
      )
    }.ToImmutableDictionary();

    var superNode = new SuperNode(
      Namespace: "global::Tests",
      Name: "TestSuperNode",
      NameWithoutGenerics: "TestSuperNode",
      Location: new Mock<Location>().Object,
      BaseClasses: new string[] { "global::Godot.Object" }.ToImmutableArray(),
      LifecycleHooks: ImmutableArray<IGodotNodeLifecycleHook>.Empty,
      PowerUpHooksByFullName: powerUpHooksByFullName,
      NotificationHandlers: ImmutableArray<string>.Empty,
      HasPartialNotificationMethod: true,
      HasOnNotificationMethodHandler: true,
      PropsAndFields: propsAndFields,
      Usings: usings,
      ContainingTypes: ImmutableArray<ContainingType>.Empty
    );

    var item = new SuperNodeGenerationItem(
      SuperNode: superNode,
      PowerUps: powerUps
    );

    var typeParameterSubstitutions =
      new Dictionary<string, ImmutableDictionary<string, string>> {
        [powerUp.FullName] = new Dictionary<string, string>()
          .ToImmutableDictionary()
      }.ToImmutableDictionary();

    genService.Setup(
      gs => gs.GetTypeParameterSubstitutions(
        appliedPowerUps,
        powerUpHooksByFullName
      )
    ).Returns(typeParameterSubstitutions);

    genService.Setup(gs => gs.SubstituteTypeParametersInPowerUps(
      appliedPowerUps,
      typeParameterSubstitutions
    )).Returns(propsAndFields);

    var staticPropsAndFields = ImmutableArray.Create("staticPropsAndFields");

    genService.Setup(gs => gs.GenerateStaticPropsAndFields(
      It.IsAny<ImmutableArray<PropOrField>>()
    )).Returns(staticPropsAndFields);

    var getType = ImmutableArray.Create("getType");

    genService.Setup(gs => gs.GenerateGetType(
      superNode.Name, It.IsAny<ImmutableArray<PropOrField>>()
    )).Returns(getType);

    var getPropertyOrFieldFn = ImmutableArray.Create("getPropertyOrFieldFn");

    genService.Setup
      (gs => gs.GenerateGetPropertyOrField(
        superNode.Name,
        It.IsAny<ImmutableArray<PropOrField>>()
      )
    ).Returns(getPropertyOrFieldFn);

    var setPropertyOrFieldFn = ImmutableArray.Create("setPropertyOrFieldFn");

    genService.Setup
      (gs => gs.GenerateSetPropertyOrField(
        superNode.Name,
        It.IsAny<ImmutableArray<PropOrField>>()
      )
    ).Returns(setPropertyOrFieldFn);

    var source = generator.GenerateStaticReflection(item, appliedPowerUps);

    source.ShouldBe("""
    #pragma warning disable
    #nullable enable
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Godot;
    using SuperNodes.Types;

    namespace global::Tests {
      partial class TestSuperNode : ISuperNode {
        staticPropsAndFields

        getType

        getPropertyOrFieldFn

        setPropertyOrFieldFn
      }
    }
    #nullable disable
    #pragma warning restore
    """.NormalizeLineEndings());
  }
}
