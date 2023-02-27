namespace SuperNodes.Tests.Common.Models;

using SuperNodes.Common.Models;
using Xunit;

public class LifecycleHooksResponseTest {
  [Fact]
  public void Initializes() {
    var response = LifecycleHooksResponse.Empty;
    Assert.Empty(response.LifecycleHooks);
    Assert.Empty(response.PowerUpHooksByFullName);
  }
}
