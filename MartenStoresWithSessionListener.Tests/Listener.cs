using Marten;
using Marten.Events;
using Marten.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MartenStoresWithSessionListener.Tests;

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
}
