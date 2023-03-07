namespace SuperNodes.Tests.SuperNodesFeature;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DeepEqual.Syntax;
using Microsoft.CodeAnalysis;
using Moq;
using Shouldly;
using SuperNodes.Common.Models;
using SuperNodes.Common.Utils;
using SuperNodes.SuperNodesFeature;
using Xunit;

public class SuperNodeGeneratorServiceTest {
  private static readonly ImmutableArray<PropOrField> _propsAndFields =
    new PropOrField[] {
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

  private static readonly ImmutableArray<PowerUp> _appliedPowerUps = new PowerUp[] {
    new PowerUp(
      Namespace: null,
      Name: "TestPowerUp",
      FullName: "global::TestPowerUp",
      Location: new Mock<Location>().Object,
      BaseClass: "global::Godot.Node",
      TypeParameters: new string[] { "TA", "TB" }.ToImmutableArray(),
      Interfaces: ImmutableArray<string>.Empty,
      Source: "[PowerUp] public class TestPowerUp {}",
      PropsAndFields: new PropOrField[] {
        new PropOrField(
          Name: "_field",
          Reference: "_field",
          Type: "TA",
          Attributes: ImmutableArray<AttributeDescription>.Empty,
          IsField: true,
          IsMutable: true,
          IsReadable: true,
          NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
          TypeParts: new SimpleSymbolDisplayPart[] {
            new SimpleSymbolDisplayPart(
              Kind: SymbolDisplayPartKind.TypeParameterName,
              Value: "TA"
            )
          }.ToImmutableArray()
        )
      }.ToImmutableArray(),
      Usings: ImmutableHashSet<string>.Empty,
      HasOnPowerUpMethod: false
    )
  }.ToImmutableArray();

  [Fact]
  public void Initializes() {
    var service = new SuperNodeGeneratorService();
    service.ShouldBeAssignableTo<ISuperNodeGeneratorService>();
  }

  [Fact]
  public void GetTypeParameterSubstitutions() {
    var powerUpHooks = new Dictionary<string, PowerUpHook>() {
      ["global::TestPowerUp"] = new PowerUpHook(
        FullName: "global::TestPowerUp",
        TypeArguments: new string[] { "string", "bool" }.ToImmutableArray()
      )
    }.ToImmutableDictionary();

    var service = new SuperNodeGeneratorService();

    var substitutions = service
      .GetTypeParameterSubstitutions(_appliedPowerUps, powerUpHooks);

    substitutions.ShouldDeepEqual(
      new Dictionary<string, ImmutableDictionary<string, string>>() {
        ["global::TestPowerUp"] = new Dictionary<string, string>() {
          ["TA"] = "string",
          ["TB"] = "bool"
        }.ToImmutableDictionary()
      }.ToImmutableDictionary()
    );
  }

  [Fact]
  public void SubstituteTypeParametersInPowerUps() {
    var service = new SuperNodeGeneratorService();

    var typeParameterSubstitutions
      = new Dictionary<string, ImmutableDictionary<string, string>>() {
        ["global::TestPowerUp"] = new Dictionary<string, string>() {
          ["TA"] = "string",
          ["TB"] = "bool"
        }.ToImmutableDictionary()
      }.ToImmutableDictionary();

    var resolvedPropsAndFields = service.SubstituteTypeParametersInPowerUps(
      _appliedPowerUps,
      typeParameterSubstitutions
    );

    resolvedPropsAndFields.ShouldDeepEqual(
      new PropOrField[] {
        new PropOrField(
          Name: "_field",
          Reference: "_field",
          Type: "string",
          Attributes: ImmutableArray<AttributeDescription>.Empty,
          IsField: true,
          IsMutable: true,
          IsReadable: true,
          NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
          TypeParts: new SimpleSymbolDisplayPart[] {
            new SimpleSymbolDisplayPart(
              Kind: SymbolDisplayPartKind.TypeParameterName,
              Value: "TA"
            )
          }.ToImmutableArray()
        )
      }.ToImmutableArray()
    );
  }

  [Fact]
  public void GeneratesLifecycleInvocations() {
    var lifecycleInvocations = new IGodotNodeLifecycleHook[] {
      new LifecycleMethodHook(Name: "OtherGenerator"),
      new PowerUpHook(
        FullName: "global::TestPowerUp",
        TypeArguments: new string[] { "A", "B" }.ToImmutableArray()
      )
    }.ToImmutableArray();

    var powerUps = new Dictionary<string, PowerUp>() {
      ["global::TestPowerUp"] = new PowerUp(
        Namespace: null,
        Name: "TestPowerUp",
        FullName: "global::TestPowerUp",
        Location: new Mock<Location>().Object,
        BaseClass: "global::Godot.Node",
        TypeParameters: ImmutableArray<string>.Empty,
        Interfaces: ImmutableArray<string>.Empty,
        Source: "[PowerUp] public class TestPowerUp {}",
        PropsAndFields: ImmutableArray<PropOrField>.Empty,
        Usings: ImmutableHashSet<string>.Empty,
        HasOnPowerUpMethod: true // Has lifecycle handler.
      ),
      ["global::TestPowerUp2"] = new PowerUp(
        Namespace: null,
        Name: "TestPowerUp2",
        FullName: "global::TestPowerUp2",
        Location: new Mock<Location>().Object,
        BaseClass: "global::Godot.Node",
        TypeParameters: ImmutableArray<string>.Empty,
        Interfaces: ImmutableArray<string>.Empty,
        Source: "[PowerUp] public class TestPowerUp2 {}",
        PropsAndFields: ImmutableArray<PropOrField>.Empty,
        Usings: ImmutableHashSet<string>.Empty,
        HasOnPowerUpMethod: false // No lifecycle handler.
      )
    }.ToImmutableDictionary();

    var service = new SuperNodeGeneratorService();

    var invocations = service
      .GenerateLifecycleInvocations(lifecycleInvocations, powerUps);

    invocations.ShouldBe(new string[] {
      "OtherGenerator(what);",
      "OnTestPowerUp(what);"
    });
  }

  [Fact]
  public void GeneratesNotificationHandlers() {
    var notificationHandlers = new string[] {
      "OnReady",
      "OnProcess",
    }.ToImmutableArray();

    var service = new SuperNodeGeneratorService();

    var handlers = service.GenerateNotificationHandlers(notificationHandlers);

    string.Join(Environment.NewLine, handlers).ShouldBe("""
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
    """.NormalizeLineEndings());
  }

  [Fact]
  public void GeneratesStaticPropsAndFields() {
    var service = new SuperNodeGeneratorService();

    var staticPropsAndFields = service
      .GenerateStaticPropsAndFields(_propsAndFields);

    string.Join(Environment.NewLine, staticPropsAndFields).ShouldBe("""
    public ImmutableDictionary<string, ScriptPropertyOrField> PropertiesAndFields
      => ScriptPropertiesAndFields;

    public static ImmutableDictionary<string, ScriptPropertyOrField> ScriptPropertiesAndFields { get; }
      = new Dictionary<string, ScriptPropertyOrField>() {
      ["SomeProperty"] = new ScriptPropertyOrField(
        "SomeProperty",
        typeof(int),
        false,
        true,
        true,
        new Dictionary<string, ImmutableArray<ScriptAttributeDescription>>() {
          ["global::Godot.ExportAttribute"] = new ScriptAttributeDescription[] {
            new ScriptAttributeDescription(
              "Export",
              typeof(global::Godot.ExportAttribute),
              new dynamic[] {
                PropertyHint.Range, "0, 100, 1",
              }.ToImmutableArray()
            )
          }.ToImmutableArray(),
          ["global::System.SerializableAttribute"] = new ScriptAttributeDescription[] {
            new ScriptAttributeDescription(
              "Serializable",
              typeof(global::System.SerializableAttribute),
              ImmutableArray<dynamic>.Empty
            )
          }.ToImmutableArray()
        }.ToImmutableDictionary()
      ),
      ["_someField"] = new ScriptPropertyOrField(
        "_someField",
        typeof(string),
        true,
        true,
        true,
        ImmutableDictionary<string, ImmutableArray<ScriptAttributeDescription>>.Empty
      )
      }.ToImmutableDictionary();
    """.NormalizeLineEndings());
  }

  [Fact]
  public void GeneratesGetType() {
    var superNodeName = "TestSuperNode";

    var service = new SuperNodeGeneratorService();

    var getType = service.GenerateGetType(superNodeName, _propsAndFields);

    string.Join(Environment.NewLine, getType).ShouldBe("""
    public TResult GetScriptPropertyOrFieldType<TResult>(
      string scriptProperty, ITypeReceiver<TResult> receiver
    ) => ReceiveScriptPropertyOrFieldType(scriptProperty, receiver);

    public static TResult ReceiveScriptPropertyOrFieldType<TResult>(
      string scriptProperty, ITypeReceiver<TResult> receiver
    ) {
      switch (scriptProperty) {
        case "SomeProperty":
          return receiver.Receive<int>();
        case "_someField":
          return receiver.Receive<string>();
        default:
          throw new System.ArgumentException(
            $"No field or property named '{scriptProperty}' was found on TestSuperNode."
          );
      }
    }
    """.NormalizeLineEndings());
  }

  [Fact]
  public void GeneratesGetPropertyOrField() {
    var superNodeName = "TestSuperNode";

    var service = new SuperNodeGeneratorService();

    var getPropertyOrField = service
      .GenerateGetPropertyOrField(superNodeName, _propsAndFields);

    string.Join(Environment.NewLine, getPropertyOrField).ShouldBe("""
    public dynamic GetScriptPropertyOrField(string scriptProperty) {
      switch (scriptProperty) {
        case "SomeProperty":
          return SomeProperty;
        case "_someField":
          return _someField;
        default:
          throw new System.ArgumentException(
            $"No field or property named '{scriptProperty}' was found on TestSuperNode."
          );
      }
    }
    """.NormalizeLineEndings());
  }

  [Fact]
  public void GeneratesSetPropertyOrField() {
    var superNodeName = "TestSuperNode";

    var service = new SuperNodeGeneratorService();

    var setPropertyOrField = service
      .GenerateSetPropertyOrField(superNodeName, _propsAndFields);

    string.Join(Environment.NewLine, setPropertyOrField).ShouldBe("""
    public void SetScriptPropertyOrField(string scriptProperty, dynamic value) {
      switch (scriptProperty) {
        case "SomeProperty":
          SomeProperty = value;
          break;
        case "_someField":
          _someField = value;
          break;
        default:
          throw new System.ArgumentException(
            $"No field or property named '{scriptProperty}' was found on TestSuperNode."
          );
      }
    }
    """.NormalizeLineEndings());
  }
}
