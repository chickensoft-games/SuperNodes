namespace SuperNodes.Tests;

using System;
using Shouldly;
using Xunit;

public class ExtensionsTest {
  [Fact]
  public void ReplaceLineEndingsTests() {
    ReplaceLineEndings("").ShouldBe("");
    ReplaceLineEndings("One Two Three").ShouldBe("One Two Three");
    if (Environment.NewLine == "\r\n") {
      ReplaceLineEndings("\n").ShouldBe("\r\n");
      ReplaceLineEndings("\r\n").ShouldBe("\r\n");
    }
    else {
      ReplaceLineEndings("\n").ShouldBe("\n");
      ReplaceLineEndings("\r\n").ShouldBe("\n");
    }
  }

  private static string ReplaceLineEndings(string str)
    => Extensions.ReplaceLineEndings(str, Environment.NewLine);
}
