namespace SuperNodes.Types.Tests;

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Shouldly;
using Xunit;

public class TypesTest {
  [Fact]
  public void SuperNodeAttributeDefaultInitializer() {
    var attribute = new SuperNodeAttribute();
    attribute.Args.ShouldBeEmpty();
  }

  [Fact]
  public void SuperNodeAttributeInitializer() {
    var attribute = new SuperNodeAttribute(typeof(int), "Test");
    attribute.Args.ShouldBe(new object[] { typeof(int), "Test" });
  }

  [Fact]
  public void PowerUpAttributeInitializer() {
    var attribute = new PowerUpAttribute();
    attribute.ShouldBeOfType<PowerUpAttribute>();
  }

  [Fact]
  public void PowerUpIgnoreAttribute() {
    var attribute = new PowerUpIgnoreAttribute();
    attribute.ShouldBeOfType<PowerUpIgnoreAttribute>();
  }

  [Fact]
  public void ScriptAttributeDescription() {
    var attribute = new ScriptAttributeDescription(
      Name: "Test",
      Type: typeof(TestAttribute),
      new dynamic[] { "argument" }.ToImmutableArray()
    );

    attribute.Name.ShouldBe("Test");
    attribute.Type.ShouldBe(typeof(TestAttribute));
    attribute.ArgumentExpressions.ShouldBe(new object[] { "argument" });
  }

  [Fact]
  public void ScriptPropertyOrField() {
    var property = new ScriptPropertyOrField(
      Name: "Property",
      Type: typeof(int),
      IsField: false,
      IsMutable: true,
      IsReadable: true,
      Attributes: ImmutableDictionary<
        string, ImmutableArray<ScriptAttributeDescription>
      >.Empty
    );

    property.Name.ShouldBe("Property");
    property.Type.ShouldBe(typeof(int));
    property.IsField.ShouldBeFalse();
    property.IsMutable.ShouldBeTrue();
    property.IsReadable.ShouldBeTrue();
    property.Attributes.ShouldBeEmpty();
  }
}

public class NodeExtensionsTest {
  [Fact]
  public void TypeParam() {
    NodeExtensions.TypeParam(default!, typeof(int)).ShouldBe("int");
    NodeExtensions.TypeParam(default!, typeof(string)).ShouldBe("string");
    NodeExtensions.TypeParam(default!, typeof(bool)).ShouldBe("bool");
    NodeExtensions.TypeParam(default!, typeof(byte)).ShouldBe("byte");
    NodeExtensions.TypeParam(default!, typeof(sbyte)).ShouldBe("sbyte");
    NodeExtensions.TypeParam(default!, typeof(char)).ShouldBe("char");
    NodeExtensions.TypeParam(default!, typeof(decimal)).ShouldBe("decimal");
    NodeExtensions.TypeParam(default!, typeof(double)).ShouldBe("double");
    NodeExtensions.TypeParam(default!, typeof(float)).ShouldBe("float");
    NodeExtensions.TypeParam(default!, typeof(uint)).ShouldBe("uint");
    NodeExtensions.TypeParam(default!, typeof(nuint)).ShouldBe("nuint");
    NodeExtensions.TypeParam(default!, typeof(long)).ShouldBe("long");
    NodeExtensions.TypeParam(default!, typeof(ulong)).ShouldBe("ulong");
    NodeExtensions.TypeParam(default!, typeof(short)).ShouldBe("short");
    NodeExtensions.TypeParam(default!, typeof(ushort)).ShouldBe("ushort");
    NodeExtensions.TypeParam(default!, typeof(object)).ShouldBe("object");
    NodeExtensions.TypeParam(default!, typeof(TestAttribute))
      .ShouldBe("global::SuperNodes.Types.Tests.TestAttribute");
    NodeExtensions.TypeParam(default!, new TestType())
      .ShouldBe(nameof(TestType));
  }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TestAttribute : Attribute {
  public TestAttribute(string a) { }
}

public class TestType : Type {
  public override Assembly Assembly => throw new NotImplementedException();
  public override string? AssemblyQualifiedName
    => throw new NotImplementedException();
  public override Type? BaseType => throw new NotImplementedException();
  public override string? FullName => null;
#pragma warning disable CA1720
  public override Guid GUID => throw new NotImplementedException();
#pragma warning restore CA1720
  public override Module Module => throw new NotImplementedException();
  public override string? Namespace => throw new NotImplementedException();
  public override Type UnderlyingSystemType
    => throw new NotImplementedException();
  public override string Name => nameof(TestType);
  public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override object[] GetCustomAttributes(bool inherit)
    => throw new NotImplementedException();
  public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    => throw new NotImplementedException();
  public override Type? GetElementType() => throw new NotImplementedException();
  public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override EventInfo[] GetEvents(BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override FieldInfo[] GetFields(BindingFlags bindingAttr)
    => throw new NotImplementedException();
  [return: DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.Interfaces
  )]
  public override Type? GetInterface(string name, bool ignoreCase)
    => throw new NotImplementedException();
  public override Type[] GetInterfaces() => throw new NotImplementedException();
  public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override Type? GetNestedType(string name, BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override Type[] GetNestedTypes(BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
    => throw new NotImplementedException();
  public override object? InvokeMember(
    string name,
    BindingFlags invokeAttr,
    Binder? binder,
    object? target,
    object?[]? args,
    ParameterModifier[]? modifiers,
    CultureInfo? culture,
    string[]? namedParameters
  ) => throw new NotImplementedException();
  public override bool IsDefined(Type attributeType, bool inherit)
    => throw new NotImplementedException();
  protected override TypeAttributes GetAttributeFlagsImpl()
     => throw new NotImplementedException();
  protected override ConstructorInfo? GetConstructorImpl(
    BindingFlags bindingAttr,
    Binder? binder,
    CallingConventions callConvention,
    Type[] types,
    ParameterModifier[]? modifiers
  ) => throw new NotImplementedException();
  protected override MethodInfo? GetMethodImpl(
    string name,
    BindingFlags bindingAttr,
    Binder? binder,
    CallingConventions callConvention,
    Type[]? types,
    ParameterModifier[]? modifiers
  ) => throw new NotImplementedException();
  protected override PropertyInfo? GetPropertyImpl(
    string name,
    BindingFlags bindingAttr,
    Binder? binder,
    Type? returnType,
    Type[]? types,
    ParameterModifier[]? modifiers
  ) => throw new NotImplementedException();
  protected override bool HasElementTypeImpl()
    => throw new NotImplementedException();
  protected override bool IsArrayImpl()
    => throw new NotImplementedException();
  protected override bool IsByRefImpl()
    => throw new NotImplementedException();
  protected override bool IsCOMObjectImpl()
    => throw new NotImplementedException();
  protected override bool IsPointerImpl()
    => throw new NotImplementedException();
  protected override bool IsPrimitiveImpl()
    => throw new NotImplementedException();
}
