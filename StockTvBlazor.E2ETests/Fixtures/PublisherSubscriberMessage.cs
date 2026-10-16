namespace StockTvBlazor.E2ETests.Fixtures;

/// <summary>
/// Represents a message received from the NetMQ Publisher-Subscriber socket.
/// </summary>
public record PublisherSubscriberMessage
{
	public required string Topic { get; init; }
	public required string Payload { get; init; }
	public required DateTime Timestamp { get; init; }
	public int NetMqFrameCount { get; init; } 
}
