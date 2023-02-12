namespace SuperNodes.Tests;
using Shouldly;
using SuperNodes.Common.Services;
using Xunit;

public class LogTest {
  [Fact]
  public void LogTests() {
    var log = new Log();
    log.Print("Hello, world!");
    log.Contents.ShouldBe("//\tHello, world!");
    log.Clear();
    log.Contents.ShouldBeEmpty();
  }
}
