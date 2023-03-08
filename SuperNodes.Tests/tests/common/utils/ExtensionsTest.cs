namespace SuperNodes.Tests.Common.Utils;

using System;
using Shouldly;
using SuperNodes.Common.Utils;
using Xunit;

public class ExtensionsTest {
  [Fact]
  public void NormalizeLineEndingsTests() {
    NormalizeLineEndings("").ShouldBe("");
    NormalizeLineEndings("One Two Three").ShouldBe("One Two Three");
    if (Environment.NewLine == "\r\n") {
      NormalizeLineEndings("\n").ShouldBe("\r\n");
      NormalizeLineEndings("\r\n").ShouldBe("\r\n");
    }
    else {
      NormalizeLineEndings("\n").ShouldBe("\n");
      NormalizeLineEndings("\r\n").ShouldBe("\n");
    }
  }

  private static string NormalizeLineEndings(string str)
    => str.NormalizeLineEndings(Environment.NewLine);
}
