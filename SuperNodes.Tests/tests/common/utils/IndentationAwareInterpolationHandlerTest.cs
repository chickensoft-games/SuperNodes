namespace SuperNodes.Tests;

using System.Collections.Immutable;
using Shouldly;
using SuperNodes.Common.Utils;
using Xunit;

public class IndentationAwareInterpolationHandler {
  [Fact]
  public void IndentationAwareInterpolationHandlerIndents() {
    var value = 10;

    var lines = ImmutableArray.CreateBuilder<string>();
    lines.Add("switch (value) {");
    lines.Add("  case 0:");
    lines.Add("    return \"zero\";");
    lines.Add("  case 1:");
    lines.Add("    return \"one\";");
    lines.Add("  default:");
    lines.Add("    return \"other number\";");
    lines.Add("}");

    var results = lines.ToImmutable();

    var blank = new string[] { "" }.ToImmutableArray();
    var justNull = (string?)null;

    var result = ChickensoftGenerator.Format($$"""
    I once was a string!{{blank}}{{justNull}}
      { I have {{value}} values! }
        // This stuff should be indented even more
        {{results}}
      { I have {{value}} values! }
    I am done being a string :'(
    """);

    result.ShouldBe("""
    I once was a string!
      { I have 10 values! }
        // This stuff should be indented even more
        switch (value) {
          case 0:
            return "zero";
          case 1:
            return "one";
          default:
            return "other number";
        }
      { I have 10 values! }
    I am done being a string :'(
    """.NormalizeLineEndings());
  }
}
