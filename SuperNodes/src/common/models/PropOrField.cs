namespace SuperNodes.Common.Models;

using System.Collections.Immutable;

/// <summary>
/// Field or property information. Script properties are captured into a static
/// table for reflection-style lookups. This allows power-ups to be more, uh,
/// powerful.
/// </summary>
/// <param name="Name">Property or field name.</param>
/// <param name="Type">Property or field type.</param>
/// <param name="Attributes">Basic attribute information.</param>
/// <param name="IsField">True if a field, false if a property.</param>
public record PropOrField(
  string Name,
  string Type,
  IImmutableSet<AttributeDescription> Attributes,
  bool IsField
);
