namespace SuperNodes.Tests.Common.Models;

using Microsoft.CodeAnalysis;
using Shouldly;
using SuperNodes.Common.Models;
using Xunit;

public class SimpleSymbolDisplayPartTest {
  [Fact]
  public void Test() {
    var part =
      new SimpleSymbolDisplayPart(SymbolDisplayPartKind.ClassName, "Value");
    part.ToString().ShouldBe("Value");
  }
}
