using System.Threading.Channels;

namespace StockTvBlazor.Services;

/// <summary>
/// Serialisiert alle zustandsaendernden Kommandos von aussen - egal ob sie über NetMQ oder über
/// die REST-Schnittstelle hereinkommen.
/// </summary>
/// <remarks>
/// Vorher hatte der NetMQ-Dienst dafuer einen eigenen Channel, weil seine Rueckrufe auf dem
/// Poller-Thread laufen. Mit der REST-Schnittstelle daneben reicht das nicht mehr: deren Aufrufe
/// laufen auf Thread-Pool-Threads und koennten parallel zueinander und zum NetMQ-Pfad in
/// <see cref="MatchService"/>/<see cref="ZielService"/> schreiben - beide sind nicht thread-sicher,
/// und die Blazor-Circuits lesen gleichzeitig mit.
///
/// Solange beide Protokolle nebeneinander laufen, ist das hier der einzige Punkt, an dem
/// Aenderungen von aussen ankommen. Reihenfolge bleibt erhalten (FIFO), und es laeuft immer nur
/// ein Kommando gleichzeitig.
/// </remarks>
public sealed class GameCommandQueue(ILogger<GameCommandQueue> logger) : BackgroundService
{
	private readonly Channel<Func<Task>> _commands = Channel.CreateUnbounded<Func<Task>>();

	/// <summary>Reiht ein Kommando ein. Kehrt sofort zurueck.</summary>
	public void Enqueue(Func<Task> command)
	{
		if (!_commands.Writer.TryWrite(command))
			logger.LogWarning("Kommando konnte nicht eingereiht werden.");
	}

	/// <summary>Reiht ein Kommando ohne Rueckgabe ein.</summary>
	public void Enqueue(Action command) => Enqueue(() =>
	{
		command();
		return Task.CompletedTask;
	});

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Kommando-Warteschlange gestartet");

		try
		{
			await foreach (var command in _commands.Reader.ReadAllAsync(stoppingToken))
			{
				try
				{
					await command();
				}
				catch (Exception ex)
				{
					// Ein fehlgeschlagenes Kommando darf die Warteschlange nicht anhalten -
					// sonst bliebe die Anzeige fuer alle weiteren Befehle taub.
					logger.LogError(ex, "Fehler beim Ausfuehren eines Kommandos");
				}
			}
		}
		catch (OperationCanceledException)
		{
			logger.LogInformation("Kommando-Warteschlange beendet");
		}
	}

	public override void Dispose()
	{
		_commands.Writer.TryComplete();
		base.Dispose();
	}
}
