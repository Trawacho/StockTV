using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public partial class SysUpdatePage : IDisposable
{
	[Inject] private PlatformInfoService PlatformInfo { get; set; } = default!;
	[Inject] private NetworkConfigService NetworkConfig { get; set; } = default!;
	[Inject] private UpdateService UpdateService { get; set; } = default!;
	[Inject] private MatchService MatchService { get; set; } = default!;
	[Inject] private ZielService ZielService { get; set; } = default!;
	[Inject] private NavigationManager NavigationManager { get; set; } = default!;
	[Inject] private ILogger<SysUpdatePage> Logger { get; set; } = default!;

	// Sicherheitssperre: sobald Match-/Zieldaten hinterlegt sind, verhaelt sich die Seite wie auf
	// einem Nicht-Pi (siehe Markup) - verhindert versehentliche Netzwerk-/Update-/Reboot-Aktionen
	// waehrend eines laufenden bzw. bereits vorbereiteten Spiels. Zentrale Logik in GameStateGuard,
	// damit der NetMQ-Pfad (NetMqResponseService) dieselbe Sperre verwendet und sie nicht umgangen
	// werden kann.
	private bool HasRecordedValues => GameStateGuard.HasRecordedValues(MatchService, ZielService);

	private readonly CancellationTokenSource _cts = new();

	private string _hostnameText = "";
	private string _hostnameErrorMessage = "";
	private string _hostnameSuccessMessage = "";
	private bool _isApplyingHostname;

	private bool _loadingInterfaces;
	private IReadOnlyList<NetworkInterfaceInfo> _interfaces = [];
	private string? _selectedDevice;
	private NetworkInterfaceInfo? _selectedInterface;
	private NetworkConnectionDetails? _details;
	private bool _isStaticMode;
	private string _ipText = "";
	private string _prefixText = "24";
	private string _gatewayText = "";
	private string _dnsText = "";
	private string _networkErrorMessage = "";
	private string _networkSuccessMessage = "";
	private bool _isApplyingNetwork;

	private UpdateCheckResult? _updateCheckResult;
	private string? _updateStartedMessage;
	private bool _isCheckingUpdate;
	private bool _isUpdating;

	private bool _rebootArmed;
	private bool _isRebooting;
	private string _rebootErrorMessage = "";

	private bool _disposed;

	protected override async Task OnInitializedAsync()
	{
		MatchService.OnGlobalRefresh += HandleRecordedValuesChanged;
		ZielService.OnGlobalRefresh += HandleRecordedValuesChanged;

		if (!PlatformInfo.IsRaspberryPi || HasRecordedValues)
			return;

		_hostnameText = NetworkConfig.GetHostname();

		_loadingInterfaces = true;
		try
		{
			_interfaces = await NetworkConfig.GetInterfacesAsync(_cts.Token);
			var firstConnected = _interfaces.FirstOrDefault(i => i.ConnectionName is not null);
			if (firstConnected is not null)
			{
				_selectedDevice = firstConnected.Device;
				await LoadSelectedInterfaceAsync();
			}
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Netzwerk-Interfaces konnten nicht geladen werden");
			_networkErrorMessage = "Netzwerk-Interfaces konnten nicht geladen werden.";
		}
		finally
		{
			_loadingInterfaces = false;
		}
	}

	public void Dispose()
	{
		_disposed = true;
		MatchService.OnGlobalRefresh -= HandleRecordedValuesChanged;
		ZielService.OnGlobalRefresh -= HandleRecordedValuesChanged;
		_cts.Cancel();
		_cts.Dispose();
	}

	private void HandleRecordedValuesChanged()
	{
		if (_disposed) return;
		InvokeAsync(StateHasChanged);
	}

	private async Task ApplyHostnameAsync()
	{
		_hostnameErrorMessage = "";
		_hostnameSuccessMessage = "";

		var candidate = _hostnameText.Trim();
		if (!NetworkConfigService.HostnameRegex.IsMatch(candidate))
		{
			_hostnameErrorMessage = "Hostname darf nur Buchstaben, Ziffern und Bindestriche enthalten, " +
				"darf nicht mit einem Bindestrich beginnen/enden und muss 1-63 Zeichen lang sein.";
			return;
		}

		_isApplyingHostname = true;
		try
		{
			var result = await NetworkConfig.SetHostnameAsync(candidate, _cts.Token);
			if (result.Success)
			{
				_hostnameText = candidate;
				_hostnameSuccessMessage = $"Hostname geändert zu '{candidate}'. Bitte den Pi neu starten, " +
					"damit die Änderung überall (z.B. mDNS) wirksam wird.";
			}
			else
			{
				_hostnameErrorMessage = result.ErrorMessage ?? "Unbekannter Fehler beim Ändern des Hostnamens.";
			}
		}
		finally
		{
			_isApplyingHostname = false;
		}
	}

	private async Task OnDeviceChangedAsync(ChangeEventArgs e)
	{
		_selectedDevice = e.Value?.ToString();
		_networkErrorMessage = "";
		_networkSuccessMessage = "";
		await LoadSelectedInterfaceAsync();
	}

	private async Task LoadSelectedInterfaceAsync()
	{
		_selectedInterface = _interfaces.FirstOrDefault(i => i.Device == _selectedDevice);
		_details = null;

		if (_selectedInterface is null || _selectedInterface.ConnectionName is null)
			return;

		try
		{
			_details = await NetworkConfig.GetConnectionDetailsAsync(_selectedInterface, _cts.Token);
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Verbindungsdetails fuer {Device} konnten nicht geladen werden", _selectedInterface.Device);
			_networkErrorMessage = "Verbindungsdetails konnten nicht geladen werden.";
			return;
		}

		if (_details is null)
			return;

		_isStaticMode = !_details.IsDhcp;
		_ipText = _details.IpAddress ?? "";
		_prefixText = (_details.Prefix ?? 24).ToString();
		_gatewayText = _details.Gateway ?? "";
		_dnsText = _details.DnsServers.Count > 0 ? string.Join(", ", _details.DnsServers) : "";
	}

	private async Task ApplyNetworkChangesAsync()
	{
		if (_details is null)
			return;

		_networkSuccessMessage = "";
		_networkErrorMessage = "";

		NetworkOperationResult result;

		if (_isStaticMode)
		{
			if (!TryValidateStaticInput(out var ip, out var prefix, out var gateway, out var dnsServers, out var validationError))
			{
				_networkErrorMessage = validationError!;
				return;
			}

			_isApplyingNetwork = true;
			try
			{
				result = await NetworkConfig.SetStaticAsync(_details.ConnectionName, ip!, prefix, gateway!, dnsServers!, _cts.Token);
			}
			finally
			{
				_isApplyingNetwork = false;
			}
		}
		else
		{
			_isApplyingNetwork = true;
			try
			{
				result = await NetworkConfig.SetDhcpAsync(_details.ConnectionName, _cts.Token);
			}
			finally
			{
				_isApplyingNetwork = false;
			}
		}

		if (result.Success)
		{
			_networkSuccessMessage = "Netzwerkeinstellungen übernommen.";
			await LoadSelectedInterfaceAsync();
		}
		else
		{
			_networkErrorMessage = result.ErrorMessage ?? "Unbekannter Fehler beim Anwenden der Netzwerkeinstellungen.";
		}
	}

	// Zeigt die Ziel-URL schon VOR dem Speichern an (nicht erst danach): wenn der Browser ueber das
	// gerade geaenderte Interface verbunden ist, kann die Verbindung durch "nmcli con up" abreissen,
	// bevor eine Erfolgsmeldung mit der neuen IP ueberhaupt noch gerendert wird.
	private string? GetTargetUrlPreview()
	{
		if (!IPAddress.TryParse(_ipText.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
			return null;

		var current = new Uri(NavigationManager.BaseUri);
		return $"{current.Scheme}://{ip}:{current.Port}/sysupdate";
	}

	private bool TryValidateStaticInput(
		out IPAddress? ip, out int prefix, out IPAddress? gateway, out List<IPAddress>? dnsServers, out string? error)
	{
		ip = null;
		prefix = 0;
		gateway = null;
		dnsServers = null;

		if (!IPAddress.TryParse(_ipText.Trim(), out var parsedIp) || parsedIp.AddressFamily != AddressFamily.InterNetwork)
		{
			error = "IP-Adresse ist keine gültige IPv4-Adresse.";
			return false;
		}

		if (!int.TryParse(_prefixText.Trim(), out var parsedPrefix) || parsedPrefix < 0 || parsedPrefix > 32)
		{
			error = "Netzwerkpräfix muss zwischen 0 und 32 liegen.";
			return false;
		}

		if (!IPAddress.TryParse(_gatewayText.Trim(), out var parsedGateway) || parsedGateway.AddressFamily != AddressFamily.InterNetwork)
		{
			error = "Gateway ist keine gültige IPv4-Adresse.";
			return false;
		}

		var dnsEntries = _dnsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (dnsEntries.Length == 0)
		{
			error = "Mindestens ein DNS-Server ist erforderlich.";
			return false;
		}

		var parsedDns = new List<IPAddress>();
		foreach (var entry in dnsEntries)
		{
			if (!IPAddress.TryParse(entry, out var parsedDnsEntry) || parsedDnsEntry.AddressFamily != AddressFamily.InterNetwork)
			{
				error = $"DNS-Server '{entry}' ist keine gültige IPv4-Adresse.";
				return false;
			}
			parsedDns.Add(parsedDnsEntry);
		}

		ip = parsedIp;
		prefix = parsedPrefix;
		gateway = parsedGateway;
		dnsServers = parsedDns;
		error = null;
		return true;
	}

	private async Task CheckForUpdateAsync()
	{
		_isCheckingUpdate = true;
		try
		{
			_updateCheckResult = await UpdateService.CheckForUpdateAsync(_cts.Token);
		}
		finally
		{
			_isCheckingUpdate = false;
		}
	}

	private async Task StartUpdateAsync()
	{
		_isUpdating = true;
		try
		{
			var result = await UpdateService.StartUpdateAsync(_cts.Token);
			_updateStartedMessage = result.Success
				? "Update gestartet — die Seite lädt sich nicht mehr automatisch neu, bitte in ca. 1 Minute manuell neu laden."
				: $"Update konnte nicht gestartet werden: {result.ErrorMessage}";
		}
		finally
		{
			_isUpdating = false;
		}
	}

	private async Task RebootAsync()
	{
		_rebootErrorMessage = "";
		_isRebooting = true;
		try
		{
			var result = await NetworkConfig.RebootAsync(_cts.Token);
			if (!result.Success)
			{
				_rebootErrorMessage = result.ErrorMessage ?? "Neustart konnte nicht ausgelöst werden.";
				_rebootArmed = false;
			}
			// Bei Erfolg bewusst kein weiterer State-Wechsel: der Pi faehrt gleich herunter,
			// die Seite ist ohnehin in Kuerze nicht mehr erreichbar.
		}
		finally
		{
			_isRebooting = false;
		}
	}
}
