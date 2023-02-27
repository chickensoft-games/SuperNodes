namespace SuperNodes.Tests.Common.Models;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class PropOrFieldTest {
  [Fact]
  public void Initializes() {
    var propOrField = new PropOrField(
      Name: "Name",
      Reference: "Name",
      Type: "string",
      Attributes: ImmutableArray<AttributeDescription>.Empty,
      IsField: false,
      IsMutable: true,
      IsReadable: true,
      NameParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty,
      TypeParts: ImmutableArray<SimpleSymbolDisplayPart>.Empty
    );

    propOrField.ShouldBeOfType<PropOrField>();
  }

  [Fact]
  public void SubstituteTypeParameters() {
    var substitutions = new Dictionary<string, string>() {
      ["TA"] = "string",
      ["TB"] = "int"
    }.ToImmutableDictionary();

    var reference = "ITestInterface<TA, TB>.SomeProperty";

    var parts = new List<SimpleSymbolDisplayPart>() {
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.InterfaceName,
        "ITestInterface"
      ),
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.Punctuation,
        "<"
      ),
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.TypeParameterName,
        "TA"
      ),
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.Punctuation,
        ", "
      ),
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.TypeParameterName,
        "TB"
      ),
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.Punctuation,
        ">"
      ),
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.Punctuation,
        "."
      ),
      new SimpleSymbolDisplayPart(
        SymbolDisplayPartKind.PropertyName,
        "SomeProperty"
      )
    }.ToImmutableArray();

    PropOrField.SubstituteTypeParameters(reference, substitutions, parts)
      .ShouldBe("ITestInterface<string, int>.SomeProperty");
  }
}
