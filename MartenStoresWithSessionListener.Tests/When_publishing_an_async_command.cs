using Xunit;
using Xunit.Abstractions;

namespace MartenStoresWithSessionListener.Tests;

public class When_publishing_an_async_command : IAsyncLifetime
{
  private readonly ITestOutputHelper _testOutputHelper;

  public When_publishing_an_async_command(
    ITestOutputHelper testOutputHelper
  ) => _testOutputHelper = testOutputHelper;
  
  public Task InitializeAsync()
  {
    throw new NotImplementedException();
  }

  [Fact]
  public void Should_write_event_to_listener()
  {
  }

  public Task DisposeAsync()
  {
    throw new NotImplementedException();
  }
}
