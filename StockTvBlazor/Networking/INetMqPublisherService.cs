namespace StockTvBlazor.Networking;

public interface INetMqPublisherService
{
	void Publish(string topic, object payload);
	event Action<bool>? OnBlockLocalChangesChanged;
	Task ExecuteAsync(CancellationToken stoppingToken);
}
