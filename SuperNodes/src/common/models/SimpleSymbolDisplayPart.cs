namespace SuperNodes.Common.Models;

using Microsoft.CodeAnalysis;

/// <summary>
/// A simplified version of Microsoft.CodeAnalysis.SymbolDisplayPart. Only
/// contains the text and the kind. No need to keep symbol references alive by
/// referencing the symbol from the original SymbolDisplayPart.
/// </summary>
/// <param name="Kind">Symbol display part kind. Can be used to determine if
/// the part is a type parameter or not.</param>
/// <param name="Value">String value.</param>
public record SimpleSymbolDisplayPart(
  SymbolDisplayPartKind Kind,
  string Value
) {
  public override string ToString() => Value;
}
