namespace SuperNodes.Types;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

/// <summary>
/// SuperObject attribute. Add this to a plain C# record or class to mixin
/// functionality from PowerUps (mixins).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SuperObjectAttribute : Attribute {
  /// <summary>
  /// PowerUps to "mixin" to the object.
  /// </summary>
  public Type[] Args { get; }

  /// <summary>
  /// SuperObject attribute. Add this to a Godot object class to use
  /// functionality from other compatible source generators.
  /// </summary>
  public SuperObjectAttribute() {
    Args = Array.Empty<Type>();
  }

  /// <summary>
  /// SuperObject attribute. Add this to a Godot object class to mixin PowerUps.
  /// </summary>
  /// <param name="args">Types of PowerUps to "mixin" to the object.</param>
  public SuperObjectAttribute(params Type[] args) {
    Args = args;
  }
}

/// <summary>
/// SuperNode attribute. Add this to a Godot object class or script to mixin
/// functionality from PowerUps (mixins).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SuperNodeAttribute : Attribute {
  /// <summary>
  /// PowerUps to "mixin" to the object.
  /// </summary>
  public object[] Args { get; }

  /// <summary>
  /// SuperNode attribute. Add this to a Godot object class to use
  /// functionality from other compatible source generators.
  /// </summary>
  public SuperNodeAttribute() {
    Args = Array.Empty<object>();
  }

  /// <summary>
  /// SuperNode attribute. Add this to a Godot object class to mixin PowerUps
  /// and/or call the names of methods denoted by strings.
  /// </summary>
  /// <param name="args">Compatible source generator lifecycle method
  /// names or the types of PowerUps.</param>
  public SuperNodeAttribute(params object[] args) {
    Args = args;
  }
}

/// <summary>
/// Power-up attribute. Add this to a class to create a mixin.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PowerUpAttribute : Attribute {
  /// <summary>PowerUp attribute. Add this to a class to create a mixin.
  /// </summary>
  public PowerUpAttribute() { }
}

/// <summary>
/// Place on PowerUp members to prevent them from being copied to the
/// SuperNode the PowerUp is applied to.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class PowerUpIgnoreAttribute : Attribute {
  /// <summary>
  /// Place on PowerUp members to prevent them from being copied to the
  /// SuperNode the PowerUp is applied to.
  /// </summary>
  public PowerUpIgnoreAttribute() { }
}

/// <summary>
/// Object containing a single method that can receive a generic type argument.
/// Sometimes, it is desirable for a PowerUp to be able to access the type of a
/// field or property on the node it is applied to. This interface allows
/// PowerUps to create a receiver object for the type in a generic context,
/// which would otherwise require a source generator or reflection to obtain.
/// <br />
/// This interface is supplied by SuperNodes.
/// </summary>
/// <typeparam name="TResult">Return value of the receive method.</typeparam>
public interface ITypeReceiver<TResult> {
  /// <summary>
  /// Generic method which receives the type of the field or property.
  /// </summary>
  /// <typeparam name="T">Type of the field or property, as a generic type
  /// variable.</typeparam>
  /// <returns>A value of type <typeparamref name="TResult" />.</returns>
  TResult Receive<T>();
}

/// <summary>
/// A description of an attribute used within a node script. This model is
/// supplied by SuperNodes.
/// </summary>
/// <param name="Name">Name of the attribute, without the "Attribute" suffix.
/// </param>
/// <param name="Type"></param>
/// <param name="ArgumentExpressions">Expressions (as plain text) given for each
/// argument.</param>
public record ScriptAttributeDescription(
  string Name,
  Type Type,
  ImmutableArray<dynamic?> ArgumentExpressions
);

/// <summary>
/// A description of a property or field used within a node script. This model
/// is supplied by SuperNodes.
/// </summary>
/// <param name="Name">Name of the member.</param>
/// <param name="Type">Type of the member.</param>
/// <param name="IsField">True if the member is a field, false if it is a
/// property.</param>
/// <param name="IsMutable">True if the member can be written to.</param>
/// <param name="IsReadable">True if the member's value can be read.</param>
/// <param name="Attributes">Descriptions of the attributes applied to the field
/// or property.</param>
public record struct ScriptPropertyOrField(
  string Name,
  Type Type,
  bool IsField,
  bool IsMutable,
  bool IsReadable,
  IDictionary<string, ImmutableArray<ScriptAttributeDescription>> Attributes
);

/// <summary>
/// Base interface of <see cref="ISuperNode" /> and <see cref="ISuperItem" />.
/// </summary>
public interface ISuperItem {
  /// <summary>
  /// A map of all properties and fields in this node script from
  /// generated identifier name to their type, attribute, and
  /// mutability information.
  /// <br />
  /// Note that instance members added to node scripts by PowerUps are
  /// not registered with Godot. The Godot source generators have no way
  /// to detect generated code from other source generators.
  /// </summary>
  ImmutableDictionary<string, ScriptPropertyOrField>
    PropertiesAndFields { get; }

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
  /// <exception cref="ArgumentException">Thrown if the given script
  /// property or field does not exist.</exception>
  TResult GetScriptPropertyOrFieldType<TResult>(
    string scriptProperty, ITypeReceiver<TResult> receiver
  );

  /// <summary>
  /// Gets the value of the given script property or field. Generated by
  /// SuperNodes.
  /// </summary>
  /// <param name="scriptProperty">The name of the script property or
  /// field to get the value of.</param>
  /// <returns>The value of the script property or field.</returns>
  /// <exception cref="ArgumentException">Thrown if the given
  /// script property or field does not exist.</exception>
  dynamic? GetScriptPropertyOrField(string scriptProperty);

  /// <summary>
  /// Sets the value of the given script property or field. Generated by
  /// SuperNodes.
  /// </summary>
  /// <param name="scriptProperty">The name of the script property or
  /// field to set the value of.</param>
  /// <param name="value">The value to set the script property or
  /// field to.</param>
  /// <exception cref="ArgumentException">Thrown if the given
  /// script property or field does not exist.</exception>
  void SetScriptPropertyOrField(string scriptProperty, dynamic? value);
}

/// <summary>
/// Interface added to all super objects.
/// </summary>
public interface ISuperObject : ISuperItem { }

/// <summary>
/// Interface added to all super nodes.
/// </summary>
public interface ISuperNode : ISuperItem { }

/// <summary>Extensions added to all objects.</summary>
public static class ObjectExtensions {
  /// <summary>
  /// Returns the simple type name of a type parameter's type if the represented
  /// type is not a built-in type. Otherwise, returns the built-in type name.
  /// <br />
  /// This may break if you are referencing the built-in types as their formal
  /// name (e.g. System.Int32 instead of int). For best results, use the
  /// simpler type names wherever possible.
  /// </summary>
  /// <param name="node">Godot object.</param>
  /// <param name = "type">Type whose name should be computed.</param>
  /// <exception cref = "InvalidOperationException">Thrown if an unrecognized
  /// primitive type is encountered.</exception>
  /// <returns>Simple name of the type (or it's built-in type).</returns>
  public static string TypeParam(this object node, Type type) {
    var fullName = type.FullName;
    switch (fullName) {
      case "System.Boolean":
        return "bool";
      case "System.Byte":
        return "byte";
      case "System.SByte":
        return "sbyte";
      case "System.Char":
        return "char";
      case "System.Decimal":
        return "decimal";
      case "System.Double":
        return "double";
      case "System.Single":
        return "float";
      case "System.Int32":
        return "int";
      case "System.UInt32":
        return "uint";
      case "System.UIntPtr":
        return "nuint";
      case "System.Int64":
        return "long";
      case "System.UInt64":
        return "ulong";
      case "System.Int16":
        return "short";
      case "System.UInt16":
        return "ushort";
      case "System.Object":
        return "object";
      case "System.String":
        return "string";
      default:
        if (fullName is null) {
          return type.Name;
        }
        return "global::" + type.FullName;
    }
  }
}
