using NetMQ;
using NetMQ.Sockets;
using System.Threading.Channels;

namespace StockTvBlazor.Components.Networking;

public class NetMqPublisherService : BackgroundService
{
	private readonly PublisherSocket _pubSocket;
	// Ein Channel dient als sichere Warteschlange zwischen den Threads
	private readonly Channel<(string Topic, object Payload)> _messageChannel;

	public NetMqPublisherService()
	{
		_pubSocket = new PublisherSocket();
		_pubSocket.Bind("tcp://*:4748");
		_messageChannel = Channel.CreateUnbounded<(string, object)>();
	}

	// DIESE FUNKTION rufst du von anderen Klassen auf
	public void Publish(string topic, string payload)
	{
		_messageChannel.Writer.TryWrite((topic, payload));
	}

	// Version 2: Für Binär-Daten (Byte-Arrays)
	public void Publish(string topic, byte[] data)
	{
		_messageChannel.Writer.TryWrite((topic, data));
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Wir lesen kontinuierlich aus dem Channel und senden über NetMQ
		await foreach (var (topic, payload) in _messageChannel.Reader.ReadAllAsync(stoppingToken))
		{
			_pubSocket.SendMoreFrame(topic);
			// Prüfen, ob es Text oder Bytes sind und passend senden
			if (payload is string text)
			{
				_pubSocket.SendFrame(text);
			}
			else if (payload is byte[] bytes)
			{
				_pubSocket.SendFrame(bytes);
			}
		}
	}

	public override void Dispose()
	{
		_pubSocket.Close();
		_pubSocket.Dispose();
		NetMQConfig.Cleanup();
		base.Dispose();
	}
}
