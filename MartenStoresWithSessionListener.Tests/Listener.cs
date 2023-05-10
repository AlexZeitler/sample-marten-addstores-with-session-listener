using Marten;
using Marten.Events;
using Marten.Internal;
using Marten.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MartenStoresWithSessionListener.Tests;

public class MartenEventListenerConfig<T> : IConfigureMarten<T> where T : IDocumentStore
{
  private readonly Serilog.ILogger _logger;


  public void Configure(
    IServiceProvider services,
    StoreOptions options
  )
  {
    var listener = services.GetService<GenericListener<T>>();
    options.Listeners.Add(listener);
    options.Projections.AsyncListeners.Add(listener);
  }
}

public class GenericListener<T> : IDocumentSessionListener where T : IDocumentStore
{
  private readonly string _category;
  private readonly ILogger _logger;

  public GenericListener(
    string category,
    ILogger logger
  )
  {
    _category = category;
    _logger = logger;
    _logger.LogInformation("Creating instance of {Type}", typeof(T));
    Documents = new List<object>();
    Events = new List<IEvent>();
  }

  public GenericListener(
    string category
  ) : this(category, NullLogger.Instance)
  {
  }

  public List<object> Documents { get; set; }
  public List<IEvent> Events { get; set; }

  public Task AfterCommitAsync(
    IDocumentSession session,
    IChangeSet commit,
    CancellationToken token
  )
  {
    _logger.LogInformation(
      "Listener \\\"{Category}\\\" fetched {Count} events",
      _category,
      commit.GetEvents()
        .Count()
    );
    Events.AddRange(commit.GetEvents());
    _logger.LogInformation(
      "Listener \\\"{Category}\\\" fetched {Count} projections",
      _category,
      commit.Updated.Count()
    );
    Documents.AddRange(commit.Updated);
    return Task.CompletedTask;
  }

  public void BeforeSaveChanges(
    IDocumentSession session
  )
  {
    _logger.LogInformation("Hit BeforeSaveChanges");
  }

  public Task BeforeSaveChangesAsync(
    IDocumentSession session,
    CancellationToken token
  )
  {
    _logger.LogInformation("Hit BeforeSaveChangesAsync");
    return Task.CompletedTask;
  }

  public void AfterCommit(
    IDocumentSession session,
    IChangeSet commit
  )
  {
    _logger.LogInformation("Hit AfterCommit");
  }

  public void DocumentLoaded(
    object id,
    object document
  )
  {
    _logger.LogInformation("Hit DocumentLoaded");
  }

  public void DocumentAddedForStorage(
    object id,
    object document
  )
  {
    _logger.LogInformation("Hit AfterCommit");
  }

  public Task WaitForProjection<T>(
    Func<T, bool> predicate,
    CancellationToken? token = default
  )
  {
    _logger.LogInformation($"Listener waiting for Projection {typeof(T)}");

    void Check(
      CancellationToken token
    )
    {
      var from = 0;
      var attempts = 1;
      while (!token.IsCancellationRequested)
      {
        _logger.LogInformation($"Looking for expected projection - attempt #{attempts}");
        var upTo = Documents.Count;

        for (var index = from; index < upTo; index++)
        {
          var ev = Documents[index];

          if (typeof(T) == ev.GetType() && predicate((T)ev))
          {
            _logger.LogInformation($"Listener Found Projection {typeof(T).Name} with Id: {((dynamic)ev).Id}");
            return;
          }
        }

        from = upTo;

        Thread.Sleep(200);
        attempts++;
      }
    }

    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(10));

    var t = token ?? cts.Token;

    return Task.Run(() => Check(t), t);
  }
}

public class Listener : IDocumentSessionListener
{
  private readonly string _category;
  private readonly ILogger _logger;

  public Listener(
    string category,
    ILogger logger
  )
  {
    _category = category;
    _logger = logger;
    Documents = new List<object>();
    Events = new List<IEvent>();
  }

  public Listener(
    string category
  ) : this(category, NullLogger.Instance)
  {
  }

  public List<object> Documents { get; set; }
  public List<IEvent> Events { get; set; }

  public Task AfterCommitAsync(
    IDocumentSession session,
    IChangeSet commit,
    CancellationToken token
  )
  {
    _logger.LogDebug(
      "Listener \\\"{Category}\\\" fetched {Count} events",
      _category,
      commit.GetEvents()
        .Count()
    );
    Events.AddRange(commit.GetEvents());
    _logger.LogDebug(
      "Listener \\\"{Category}\\\" fetched {Count} projections",
      _category,
      commit.Updated.Count()
    );
    Documents.AddRange(commit.Updated);
    return Task.CompletedTask;
  }

  public void BeforeSaveChanges(
    IDocumentSession session
  )
  {
  }

  public Task BeforeSaveChangesAsync(
    IDocumentSession session,
    CancellationToken token
  )
  {
    return Task.CompletedTask;
  }

  public void AfterCommit(
    IDocumentSession session,
    IChangeSet commit
  )
  {
  }

  public void DocumentLoaded(
    object id,
    object document
  )
  {
  }

  public void DocumentAddedForStorage(
    object id,
    object document
  )
  {
  }

  public Task WaitForProjection<T>(
    Func<T, bool> predicate,
    CancellationToken? token = default
  )
  {
    _logger.LogInformation($"Listener waiting for Projection {typeof(T)}");

    void Check(
      CancellationToken token
    )
    {
      var from = 0;
      var attempts = 1;
      while (!token.IsCancellationRequested)
      {
        _logger.LogInformation($"Looking for expected projection - attempt #{attempts}");
        var upTo = Documents.Count;

        for (var index = from; index < upTo; index++)
        {
          var ev = Documents[index];

          if (typeof(T) == ev.GetType() && predicate((T)ev))
          {
            _logger.LogInformation($"Listener Found Projection {typeof(T).Name} with Id: {((dynamic)ev).Id}");
            return;
          }
        }

        from = upTo;

        Thread.Sleep(200);
        attempts++;
      }
    }

    var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(10));

    var t = token ?? cts.Token;

    return Task.Run(() => Check(t), t);
  }
}
