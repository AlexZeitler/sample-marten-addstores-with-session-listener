using Alba;
using Marten;
using Marten.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Shouldly;
using Xunit.Abstractions;

namespace MartenStoresWithSessionListener.Tests;

public class When_registering_a_single_marten_store : IAsyncLifetime
{
  private readonly ITestOutputHelper _testOutputHelper;
  private IAlbaHost _host;
  private Listener _listener;

  public When_registering_a_single_marten_store(
    ITestOutputHelper testOutputHelper
  ) => _testOutputHelper = testOutputHelper;


  public async Task InitializeAsync()
  {
    var connectionString = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5435,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "postgres",
      Password = "123456",
      Username = "postgres"
    }.ToString();

    _listener = new Listener();
    var builder = Host.CreateDefaultBuilder();
    builder
      .ConfigureServices(
        services => services.AddMartenStore<IReproStore>(
          _ =>
          {
            _.Connection(connectionString);
            _.Events.TenancyStyle = TenancyStyle.Conjoined;
            _.Listeners.Add(_listener);
            _.Projections.AsyncListeners.Add(_listener);
          }
        )
      );
    _host = await builder.StartAlbaAsync();
  }

  [Fact]
  public async Task Should_write_events_to_listener()
  {
    var id = Guid.NewGuid();
    var something = new SomethingHappened(id);
    var store = _host.Services.GetService<IReproStore>();
    await using var session = store.LightweightSession();
    session.Events.Append(id, something);
    await session.SaveChangesAsync();
    _listener.Events.Count.ShouldBe(1);
    _listener.Events[0]
      .Data
      .ShouldBeOfType<SomethingHappened>();
  }

  public async Task DisposeAsync() => await _host.DisposeAsync();
}
