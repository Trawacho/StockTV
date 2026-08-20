using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace StockTvBlazor.Services;

public record UpdateCheckResult(string CurrentVersion, string? LatestVersion, bool UpdateAvailable, string? ErrorMessage);

/// <summary>
/// Prüft auf GitHub, ob ein neueres StockTV-Release existiert, und stößt bei Bedarf ein Update an.
/// Der eigentliche Update-Vorgang läuft über die feste, in build/rpi/install.sh per sudoers
/// freigegebene Unit "stocktv-update" (systemd-run), da das darin gestartete install.sh den
/// eigenen stocktv-Dienst neu startet - siehe StartUpdateAsync.
/// </summary>
public class UpdateService
{
	private const string GitHubReleaseUrl = "https://api.github.com/repos/Trawacho/StockTV/releases/latest";
	private const string HttpClientName = "GitHub";
	private const string UpdateScriptPath = "/usr/local/sbin/stocktv-run-update.sh";

	private readonly ILogger<UpdateService> _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	public bool UpdateInProgress { get; private set; }

	public UpdateService(ILogger<UpdateService> logger, IHttpClientFactory httpClientFactory)
	{
		_logger = logger;
		_httpClientFactory = httpClientFactory;
	}

	public string GetCurrentVersion()
	{
		var version = Assembly.GetExecutingAssembly().GetName().Version;
		return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
	}

	public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default)
	{
		var currentVersion = GetCurrentVersion();
		var client = _httpClientFactory.CreateClient(HttpClientName);

		try
		{
			using var response = await client.GetAsync(GitHubReleaseUrl, ct);

			if (response.StatusCode == HttpStatusCode.Forbidden)
			{
				_logger.LogWarning("GitHub API antwortete mit 403 (vermutlich Rate-Limit ueberschritten)");
				return new UpdateCheckResult(currentVersion, null, false,
					"GitHub-Anfragelimit erreicht, bitte später erneut versuchen.");
			}

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("GitHub-Release-Abfrage fehlgeschlagen: {Status}", response.StatusCode);
				return new UpdateCheckResult(currentVersion, null, false, $"GitHub antwortete mit {(int)response.StatusCode}.");
			}

			await using var stream = await response.Content.ReadAsStreamAsync(ct);
			using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

			var tagName = doc.RootElement.GetProperty("tag_name").GetString();
			if (string.IsNullOrWhiteSpace(tagName))
				return new UpdateCheckResult(currentVersion, null, false, "GitHub-Antwort enthielt keinen Tag-Namen.");

			var latestVersionText = tagName.TrimStart('v', 'V');
			var updateAvailable = IsNewerVersion(latestVersionText, currentVersion);

			return new UpdateCheckResult(currentVersion, latestVersionText, updateAvailable, null);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Update-Check fehlgeschlagen");
			return new UpdateCheckResult(currentVersion, null, false, "Update-Check fehlgeschlagen: " + ex.Message);
		}
	}

	private static bool IsNewerVersion(string latestText, string currentText)
	{
		if (!Version.TryParse(NormalizeToThreeParts(latestText), out var latest))
			return false;
		if (!Version.TryParse(NormalizeToThreeParts(currentText), out var current))
			return false;

		return latest > current;
	}

	// Nur Major.Minor.Build vergleichen - die csproj-Version haengt immer eine ".0"-Revision an,
	// die GitHub-Tags (z.B. "v1.7.1") nicht kennen.
	private static string NormalizeToThreeParts(string text)
	{
		var parts = text.Split('.');
		return parts.Length >= 3 ? string.Join('.', parts[0], parts[1], parts[2]) : text;
	}

	public async Task<NetworkOperationResult> StartUpdateAsync(CancellationToken ct = default)
	{
		if (UpdateInProgress)
			return new NetworkOperationResult(false, "Es läuft bereits ein Update.");

		UpdateInProgress = true;

		var psi = new ProcessStartInfo
		{
			FileName = "sudo",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		psi.ArgumentList.Add("-n");
		psi.ArgumentList.Add("/usr/bin/systemd-run");
		psi.ArgumentList.Add("--unit=stocktv-update");
		psi.ArgumentList.Add("--collect");
		psi.ArgumentList.Add(UpdateScriptPath);

		try
		{
			using var process = Process.Start(psi);
			if (process == null)
			{
				UpdateInProgress = false;
				return new NetworkOperationResult(false, "Prozess konnte nicht gestartet werden.");
			}

			await process.WaitForExitAsync(ct);
			var stdErr = await process.StandardError.ReadToEndAsync(ct);

			if (process.ExitCode != 0)
			{
				_logger.LogWarning("systemd-run fuer Update fehlgeschlagen: {StdErr}", stdErr);
				UpdateInProgress = false;
				return new NetworkOperationResult(false, stdErr);
			}

			// UpdateInProgress bleibt bewusst true: die transiente "stocktv-update"-Unit laeuft jetzt
			// unabhaengig weiter und wird in Kuerze den stocktv-Dienst (diesen Prozess) neu starten.
			_logger.LogInformation("Update-Unit stocktv-update gestartet");
			return new NetworkOperationResult(true, null);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Update konnte nicht gestartet werden");
			UpdateInProgress = false;
			return new NetworkOperationResult(false, ex.Message);
		}
	}
}
