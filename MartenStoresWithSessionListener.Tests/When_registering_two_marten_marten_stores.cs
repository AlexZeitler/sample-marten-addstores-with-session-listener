using Alba;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Marten.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Shouldly;
using Xunit.Abstractions;

namespace MartenStoresWithSessionListener.Tests;

public class When_registering_two_marten_stores : IAsyncLifetime
{
  private readonly ITestOutputHelper _testOutputHelper;
  private IAlbaHost _host;
  private Listener _listener1;
  private Listener _listener2;

  public When_registering_two_marten_stores(
    ITestOutputHelper testOutputHelper
  ) => _testOutputHelper = testOutputHelper;


  public async Task InitializeAsync()
  {
    var connectionString1 = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5435,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "postgres",
      Password = "123456",
      Username = "postgres"
    }.ToString();
    var connectionString2 = new NpgsqlConnectionStringBuilder()
    {
      Pooling = false,
      Port = 5436,
      Host = "localhost",
      CommandTimeout = 20,
      Database = "postgres",
      Password = "123456",
      Username = "postgres"
    }.ToString();

    _listener1 = new Listener();
    _listener2 = new Listener();
    var builder = Host.CreateDefaultBuilder();
    builder
      .ConfigureServices(
        services =>
        {
          services.AddMartenStore<IReproStore>(
            _ =>
            {
              _.Connection(connectionString1);
              _.Policies.ForAllDocuments(d => d.TenancyStyle = TenancyStyle.Conjoined);
              _.Events.TenancyStyle = TenancyStyle.Conjoined;
              _.Listeners.Add(_listener1);
              _.Projections.Snapshot<Something>(SnapshotLifecycle.Inline);
              _.Projections.AsyncListeners.Add(_listener1);
            }
          );

          services.AddMartenStore<IReproStore2>(
            _ =>
            {
              _.Connection(connectionString2);
              _.Policies.ForAllDocuments(d => d.TenancyStyle = TenancyStyle.Conjoined);
              _.Events.TenancyStyle = TenancyStyle.Conjoined;
              _.Listeners.Add(_listener2);
              _.Projections.Snapshot<Something>(SnapshotLifecycle.Inline);
              _.Projections.AsyncListeners.Add(_listener2);
            }
          );
        }
      );
    _host = await builder.StartAlbaAsync();
  }

  [Fact]
  public async Task Should_write_events_to_listener1()
  {
    var id = Guid.NewGuid();
    var something = new SomethingHappened(id);
    var store = _host.Services.GetService<IReproStore>();
    await using var session = store.LightweightSession();
    session.Events.Append(id, something);
    await session.SaveChangesAsync();
    _listener1.Events.Count.ShouldBe(1);
    _listener1.Events[0]
      .Data
      .ShouldBeOfType<SomethingHappened>();
    (_listener1.Events[0] as IEvent<SomethingHappened>).Data.TrackingId.ShouldBe(id);
  }

  [Fact]
  public async Task Should_write_events_to_listener2()
  {
    var id = Guid.NewGuid();
    var something = new SomethingHappened(id);
    var store = _host.Services.GetService<IReproStore2>();
    await using var session = store.LightweightSession();
    session.Events.Append(id, something);
    await session.SaveChangesAsync();
    _listener2.Events.Count.ShouldBe(1);
    _listener2.Events[0]
      .Data
      .ShouldBeOfType<SomethingHappened>();
    (_listener2.Events[0] as IEvent<SomethingHappened>).Data.TrackingId.ShouldBe(id);
  }

  [Fact]
  public async Task Should_write_projections_to_listener1()
  {
    var id = Guid.NewGuid();
    var something = new SomethingHappened(id);
    var store = _host.Services.GetService<IReproStore>();
    await using var session = store.LightweightSession();
    session.Events.Append(id, something);
    await session.SaveChangesAsync();
    _listener1.Documents.Count.ShouldBe(1);
  }

  [Fact]
  public async Task Should_write_projections_to_listener2()
  {
    var id = Guid.NewGuid();
    var something = new SomethingHappened(id);
    var store = _host.Services.GetService<IReproStore2>();
    await using var session = store.LightweightSession();
    session.Events.Append(id, something);
    await session.SaveChangesAsync();
    _listener2.Documents.Count.ShouldBe(1);
  }

  public async Task DisposeAsync() => await _host.DisposeAsync();
}
