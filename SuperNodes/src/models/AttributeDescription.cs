namespace SuperNodes.Models;

/// <summary>
/// Description of an attribute applied to a class field or property member.
/// </summary>
/// <param name="Name">Name of the attribute.</param>
/// <param name="Type">Type of the attribute.</param>
/// <param name="ArgumentExpressions">Argument expressions given to the
/// attribute.
/// </param>
public record AttributeDescription(
  string Name,
  string Type,
  string[] ArgumentExpressions
);
