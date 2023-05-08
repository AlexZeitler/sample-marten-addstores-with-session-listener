namespace MartenStoresWithSessionListener.Tests;

public record SomethingHappened(
  Guid TrackingId
);

public class Something
{
  public Guid Id { get; set; }
  public Guid TrackingId { get; set; }
  
  public static Something Create(
    SomethingHappened happened
  )
  {
    return new Something()
    {
      TrackingId = happened.TrackingId
    };
  }
}
