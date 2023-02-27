namespace SuperNodes.Common.Models;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// Field or property information. Script properties are captured into a static
/// table for reflection-style lookups. This allows power-ups to be more, uh,
/// powerful.
/// </summary>
/// <param name="Name">Property or field name.</param>
/// <param name="Reference">Expression by which this property or field can be
/// referenced from a class instance. This value acts as a placeholder when
/// the model is first created, as there is no way to know this expression
/// until a SuperNode can determine which PowerUps to apply and what type
/// arguments to use.</param>
/// <param name="Type">Property or field type.</param>
/// <param name="Attributes">Basic attribute information.</param>
/// <param name="IsField">True if a field, false if a property.</param>
/// <param name="IsMutable">True if the member's value can be set.</param>
/// <param name="IsReadable">True if the member's value can be read.</param>
/// <param name="NameParts">Symbol display parts for the property's name.
/// This is only used for properties which are explicit interface
/// implementations and may need the type parameters referenced to be
/// substituted with the actual type arguments later.
/// </param>
/// <param name="TypeParts">Symbol display parts for the property's
/// type. This is used when the property or field's type contains type
/// parameter references which need to be substituted with the actual type
/// arguments later.</param>
public record PropOrField(
  string Name,
  string Reference,
  string Type,
  ImmutableArray<AttributeDescription> Attributes,
  bool IsField,
  bool IsMutable,
  bool IsReadable,
  ImmutableArray<SimpleSymbolDisplayPart> NameParts,
  ImmutableArray<SimpleSymbolDisplayPart> TypeParts
) {
  /// <summary>
  /// Expression to use when referencing this member.
  /// </summary>
  public string NameReference => NameParts.Length > 0 ? Reference : Name;

  /// <summary>
  /// Expression to use when referencing this member from a class instance.
  /// </summary>
  public string NameInstance {
    get {
      if (NameParts.Length == 0) { return Name; }

      var split = NameReference.Split('.');

      var prefix = string.Join(".", split.Take(
        NameReference.Split('.').Length - 1
      ));
      var suffix = split.Last();

      return
        $"(({prefix})this).{suffix}";
    }
  }

  /// <summary>
  /// Given a simple identifier string, the actual identifier parts,
  /// and a set of type parameter substitutions, this computes the actual
  /// identifier string by substituting the type parameters with the actual
  /// type arguments, or returns the simple identifier string if no parts are
  /// given.
  /// </summary>
  /// <param name="id">Identifier string.</param>
  /// <param name="typeParameterSubstitutions">Mapping of type parameters to
  /// type arguments.</param>
  /// <param name="parts">Display parts for the identifier, simplified.</param>
  /// <returns>Resolved identifier.</returns>
  public static string SubstituteTypeParameters(
    string id,
    ImmutableDictionary<string, string> typeParameterSubstitutions,
    ImmutableArray<SimpleSymbolDisplayPart> parts
  ) {
    if (parts.Length == 0) { return id; }
    var name = string.Empty;
    foreach (var part in parts) {
      if (
        part.Kind == SymbolDisplayPartKind.TypeParameterName &&
        typeParameterSubstitutions.TryGetValue(
          part.Value, out var typeArg
        )
      ) {
        name += typeArg;
        continue;
      }
      name += part.Value;
    }
    return name;
  }
}
