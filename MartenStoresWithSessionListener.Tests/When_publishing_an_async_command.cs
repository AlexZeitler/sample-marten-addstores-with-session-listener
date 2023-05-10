using Alba;
using Marten;
using Marten.Events.Projections;
using Marten.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using Serilog.Extensions.Logging;
using Shouldly;
using Wolverine;
using Xunit.Abstractions;

namespace MartenStoresWithSessionListener.Tests;

public class When_publishing_an_async_command : IAsyncLifetime
{
  private readonly ITestOutputHelper _testOutputHelper;
  private Listener _listener1;
  private Listener _listener2;
  private IAlbaHost _host;
  private IMessageBus? _bus;

  public When_publishing_an_async_command(
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

    Log.Logger = new LoggerConfiguration()
      .WriteTo.TestOutput(_testOutputHelper)
      .WriteTo.Console()
      .CreateLogger();

    var serilogLogger = Log.Logger = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .WriteTo.TestOutput(_testOutputHelper)
      .CreateLogger();

    var dotnetILogger = new SerilogLoggerFactory(serilogLogger)
      .CreateLogger("tests");

    _listener1 = new Listener("store1", dotnetILogger);
    _listener2 = new Listener("store2", dotnetILogger);
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
    builder.UseWolverine();
    _host = await builder.StartAlbaAsync();
    _bus = _host.Services.GetService<IMessageBus>();
  }

  [Fact]
  public async Task Should_write_projection_to_listener1()
  {
    var trackingId = Guid.NewGuid();
    await _bus.PublishAsync(new DoSomething(trackingId));
    await _listener1.WaitForProjection<Something>(s => s.TrackingId == trackingId);
    var store = _host.Services.GetService<IReproStore>();
    await using var session = store.LightweightSession();
    var something = session.Load<Something>(trackingId);
    something.ShouldNotBeNull();
  }

  [Fact]
  public async Task Should_write_projection_to_listener2()
  {
    var trackingId = Guid.NewGuid();
    await _bus.PublishAsync(new DoSomething(trackingId));
    await _listener2.WaitForProjection<Something>(s => s.TrackingId == trackingId);
    var store = _host.Services.GetService<IReproStore2>();
    await using var session = store.LightweightSession();
    var something = session.Load<Something>(trackingId);
    something.ShouldNotBeNull();
  }

  public async Task DisposeAsync() => await _host.DisposeAsync();
}

public record DoSomething(Guid TrackingId);

public class DoSomethingHandler
{
  public async Task Handle(DoSomething doSomething, IReproStore reproStore, IReproStore2 reproStore2)
  {
    await using var session = reproStore.LightweightSession();
    await using var session2 = reproStore2.LightweightSession();
    session.Events.Append(doSomething.TrackingId, new SomethingHappened(doSomething.TrackingId));
    session2.Events.Append(doSomething.TrackingId, new SomethingHappened(doSomething.TrackingId));
    await session.SaveChangesAsync();
    await session2.SaveChangesAsync();
  }
}
