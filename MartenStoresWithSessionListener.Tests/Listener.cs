using Marten;
using Marten.Events;
using Marten.Services;

namespace MartenStoresWithSessionListener.Tests;

public class Listener : IDocumentSessionListener
{
  public Listener()
  {
    Documents = new List<object>();
    Events = new List<IEvent>();
  }

  public List<object> Documents { get; set; }
  public List<IEvent> Events { get; set; }

  public Task AfterCommitAsync(
    IDocumentSession session,
    IChangeSet commit,
    CancellationToken token
  )
  {
    Events.AddRange(commit.GetEvents());
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
