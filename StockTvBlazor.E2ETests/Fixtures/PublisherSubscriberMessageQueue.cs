using System.Collections.Concurrent;

namespace StockTvBlazor.E2ETests.Fixtures;

/// <summary>
/// Thread-safe queue for recording messages from the NetMQ Publisher-Subscriber socket.
/// Maintains a maximum capacity of 10,000 messages; oldest messages are discarded when full.
/// </summary>
public class PublisherSubscriberMessageQueue
{
	private readonly ConcurrentQueue<PublisherSubscriberMessage> _messages;
	private const int MaxCapacity = 10000;

	public PublisherSubscriberMessageQueue()
	{
		_messages = new ConcurrentQueue<PublisherSubscriberMessage>();
	}

	/// <summary>
	/// Add a message to the queue. If capacity is exceeded, discard the oldest message.
	/// </summary>
	public void Enqueue(PublisherSubscriberMessage message)
	{
		_messages.Enqueue(message);

        // Trim to max capacity if needed
        if (_messages.Count > MaxCapacity)
		{
			_messages.TryDequeue(out _);
		}
	}

	/// <summary>
	/// Get all messages with the specified topic.
	/// </summary>
	public IReadOnlyList<PublisherSubscriberMessage> GetMessagesByTopic(string topic)
	{
		return _messages
			.Where(m => m.Topic == topic)
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Clear all messages from the queue.
	/// </summary>
	public void Clear()
	{
		while (_messages.TryDequeue(out _)) { }
	}

	/// <summary>
	/// Get all messages currently in the queue.
	/// </summary>
	public IReadOnlyList<PublisherSubscriberMessage> GetAllMessages()
	{
		return _messages.ToList().AsReadOnly();
	}

	/// <summary>
	/// Get the count of messages currently in the queue.
	/// </summary>
	public int Count => _messages.Count;
}
