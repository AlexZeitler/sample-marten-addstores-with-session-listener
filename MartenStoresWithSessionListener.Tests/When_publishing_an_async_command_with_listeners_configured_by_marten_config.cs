using Alba;
using Marten;
using Marten.Events.Projections;
using Marten.Internal;
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

public class When_publishing_an_async_command_with_listeners_configured_by_marten_config : IAsyncLifetime
{
  private readonly ITestOutputHelper _testOutputHelper;
  private GenericListener<IReproStore>? _listener1;
  private GenericListener<IReproStore2>? _listener2;
  private IAlbaHost _host;
  private IMessageBus? _bus;

  public When_publishing_an_async_command_with_listeners_configured_by_marten_config(
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


    var builder = Host.CreateDefaultBuilder();
    builder
      .ConfigureServices(
        services =>
        {
          services.AddSingleton(new GenericListener<IReproStore>("repro-1", dotnetILogger));
          services.AddSingleton(new GenericListener<IReproStore2>("repro-2", dotnetILogger));
          services.AddSingleton<IConfigureMarten<IReproStore>, MartenEventListenerConfig<IReproStore>>();
          services.AddSingleton<IConfigureMarten<IReproStore2>, MartenEventListenerConfig<IReproStore2>>();
          services.AddMartenStore<IReproStore>(
            _ =>
            {
              _.Connection(connectionString1);
              _.Policies.ForAllDocuments(d => d.TenancyStyle = TenancyStyle.Conjoined);
              _.Events.TenancyStyle = TenancyStyle.Conjoined;
              _.Projections.Snapshot<Something>(SnapshotLifecycle.Inline);
            }
          );

          services.AddMartenStore<IReproStore2>(
            _ =>
            {
              _.Connection(connectionString2);
              _.Policies.ForAllDocuments(d => d.TenancyStyle = TenancyStyle.Conjoined);
              _.Events.TenancyStyle = TenancyStyle.Conjoined;
              _.Projections.Snapshot<Something>(SnapshotLifecycle.Inline);
            }
          );
        }
      );
    builder.UseWolverine();
    _host = await builder.StartAlbaAsync();
    _bus = _host.Services.GetService<IMessageBus>();

    _listener1 = _host.Services.GetService<GenericListener<IReproStore>>();
    _listener2 = _host.Services.GetService<GenericListener<IReproStore2>>();
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
