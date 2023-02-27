namespace SuperNodes.Tests.Common.Models;

using System.Collections.Immutable;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class AttributeDescriptionTest {
  [Fact]
  public void Initializes() {
    var attributeDescription = new AttributeDescription(
      Name: "Name",
      Type: "string",
      ArgumentExpressions: ImmutableArray<string>.Empty
    );

    attributeDescription.ShouldBeOfType<AttributeDescription>();
  }
}
