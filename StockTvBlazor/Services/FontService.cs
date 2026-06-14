using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace StockTvBlazor.Services;

public class FontService
{
	private readonly List<string> _availableFonts;
	private readonly ILogger<FontService> _logger;

	public FontService(ILogger<FontService> logger)
	{
		_logger = logger;
		_availableFonts = LoadSystemFonts();
	}

	private List<string> LoadSystemFonts()
	{
		var fonts = new List<string>();

		// Versuche Fonts auf Windows aus Registry/Verzeichnissen zu laden
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			try
			{
				var windowsFonts = LoadWindowsFonts();
				if (windowsFonts.Count > 0)
				{
					_logger.LogInformation("Loaded {Count} system fonts on Windows", windowsFonts.Count);
					return windowsFonts;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to load fonts on Windows");
			}
		}

		// Versuche fc-list auf Linux/Unix zu nutzen
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			try
			{
				var linuxFonts = LoadFontsVisFcList();
				if (linuxFonts.Count > 0)
				{
					_logger.LogInformation("Loaded {Count} system fonts via fc-list on Linux", linuxFonts.Count);
					return linuxFonts;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to load fonts via fc-list on Linux");
			}
		}

		// Fallback: Web-sichere und Standard-Systemschriftarten
		_logger.LogInformation("Using fallback font list");
		fonts.AddRange(new[]
		{
			"Arial",
			"Arial Black",
			"Courier New",
			"Georgia",
			"Times New Roman",
			"Trebuchet MS",
			"Verdana",
			"Comic Sans MS",
			"Segoe UI",
			"Segoe UI Variable",
			"Segoe Print",
			"Consolas",
			"Courier",
			"Calibri",
			"Cambria",
			"Garamond",
			"Impact",
			"Lucida Console",
			"Tahoma"
		});

		fonts.Sort();
		return fonts;
	}

	private List<string> LoadWindowsFonts()
	{
		var fonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return fonts.ToList();

		// Versuche zuerst Registry zu lesen (am zuverlässigsten)
		var registrySuccess = LoadWindowsFontsFromRegistry(fonts);

		// Fallback: Lese Font-Dateinamen aus Fonts-Verzeichnis
		if (fonts.Count == 0 || !registrySuccess)
		{
			LoadWindowsFontsFromDirectory(fonts);
		}

		return fonts.OrderBy(f => f).ToList();
	}

	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	private bool LoadWindowsFontsFromRegistry(HashSet<string> fonts)
	{
		try
		{
			using var registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");

			if (registryKey == null) return false;

			foreach (var valueName in registryKey.GetValueNames())
			{
				// Format: "Arial (TrueType)" oder "Arial Bold (OpenType)"
				// Entferne Typ-Suffix
				var fontName = System.Text.RegularExpressions.Regex.Replace(
					valueName,
					@"\s*\((TrueType|OpenType|Type 1)\)\s*$",
					"",
					System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

				if (!string.IsNullOrWhiteSpace(fontName))
				{
					fonts.Add(fontName);
				}
			}

			_logger.LogInformation("Loaded {Count} fonts from Windows Registry", fonts.Count);
			return fonts.Count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error reading Windows Registry for fonts");
			return false;
		}
	}

	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	private void LoadWindowsFontsFromDirectory(HashSet<string> fonts)
	{
		try
		{
			var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));

			if (!Directory.Exists(fontPath))
			{
				_logger.LogWarning("Windows Fonts directory not found: {Path}", fontPath);
				return;
			}

			var fontExtensions = new[] { ".ttf", ".otf", ".ttc" };

			foreach (var file in Directory.EnumerateFiles(fontPath))
			{
				var ext = Path.GetExtension(file).ToLowerInvariant();
				if (!fontExtensions.Contains(ext)) continue;

				try
				{
					// Nutze Dateinamen (ohne Extension) als Font-Name
					var fontName = Path.GetFileNameWithoutExtension(file);

					if (!string.IsNullOrWhiteSpace(fontName))
					{
						fonts.Add(fontName);
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error processing font file: {File}", file);
				}
			}

			_logger.LogInformation("Loaded {Count} fonts from Windows Fonts directory", fonts.Count);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error reading Windows Fonts directory");
		}
	}

	private List<string> LoadFontsVisFcList()
	{
		var fonts = new List<string>();

		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "fc-list",
				Arguments = ":",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};

			using var process = Process.Start(psi);
			if (process == null) return fonts;

			using var reader = process.StandardOutput;
			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				// Format: /path/to/font.ttf: Family Name,Style:
				var parts = line.Split(':');
				if (parts.Length >= 2)
				{
					var fontInfo = parts[1].Trim();
					var fontNames = fontInfo.Split(',');
					if (fontNames.Length > 0)
					{
						var name = fontNames[0].Trim();
						if (!string.IsNullOrWhiteSpace(name) && !fonts.Contains(name))
						{
							fonts.Add(name);
						}
					}
				}
			}

			process.WaitForExit();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Error executing fc-list");
		}

		fonts.Sort();
		return fonts;
	}

	public List<string> GetAvailableFonts() => _availableFonts;
}
