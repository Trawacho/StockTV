using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockTvBlazor.Services;

/// <summary>
/// "Verschluesselt" einzelne Konfigurationswerte mit AES-256 (CBC, PKCS7, zufaellige IV pro
/// Aufruf), damit sie nicht im Klartext in <c>_config/stocktv.config.json</c> stehen.
/// </summary>
/// <remarks>
/// Uebernommen aus StockTvKiosk (AppConfig.Secrets) - dort mit derselben Einschraenkung:
/// Der Schluessel wird deterministisch aus dem Anwendungsnamen abgeleitet (SHA-256). Das ist
/// KEIN echtes Geheimnis und KEIN Schutz vor jemandem, der Konfigurationsdatei UND Programm
/// hat. Es verhindert nur, dass der Wert beim Screenshot, im Log oder in einer Support-Anfrage
/// versehentlich mitgelesen wird.
///
/// Fuer echten, an Benutzer/Rechner gebundenen Schutz siehe ProtectedData (DPAPI, Windows-only)
/// oder eine externe Schluesselablage. Da die Ciphertexte nicht authentifiziert (MAC) sind, ist
/// das hier nicht fuer Szenarien geeignet, in denen jemand wiederholt Entschluesselungsversuche
/// mit manipulierten Werten beobachten kann - fuer eine lokal gelesene Konfigurationsdatei ist
/// das kein praktisches Risiko.
///
/// Das Praefix "enc:" kennzeichnet bereits verschluesselte Werte. Fehlt es, gilt der Wert als
/// Klartext und wird beim naechsten Speichern verschluesselt - ein Schluessel kann also von
/// Hand im Klartext in die Datei eingetragen werden.
/// </remarks>
public static class SecretProtector
{
	private const string EncryptedPrefix = "enc:";

	private const int IvSizeBytes = 16; // AES-Blockgroesse

	/// <summary>
	/// Grundlage des abgeleiteten Schluessels. Bewusst eine eigene Zeichenfolge und nicht der
	/// Assembly-Name: ein Umbenennen der Anwendung darf bestehende Konfigurationsdateien nicht
	/// unlesbar machen.
	/// </summary>
	private const string ApplicationName = "StockTvBlazor";

	public static bool IsEncrypted(string? value)
		=> !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

	/// <summary>Verschluesselt, sofern noch nicht geschehen. Leere Werte bleiben leer.</summary>
	public static string ProtectIfRequired(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		return IsEncrypted(value) ? value : EncryptedPrefix + Protect(value);
	}

	/// <summary>
	/// Entschluesselt, sofern das Praefix vorhanden ist. Ein defekter oder von einer anderen
	/// Anwendung stammender Wert wird unveraendert zurueckgegeben statt eine Ausnahme zu werfen -
	/// sonst liesse sich die Anwendung wegen eines kaputten Konfigurationswerts nicht mehr starten.
	/// </summary>
	public static string UnprotectIfRequired(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		if (!IsEncrypted(value))
			return value;

		try
		{
			return Unprotect(value[EncryptedPrefix.Length..]);
		}
		catch (CryptographicException)
		{
			return value;
		}
		catch (FormatException)
		{
			// Kein gueltiges Base64 - Wert ist defekt.
			return value;
		}
	}

	private static string Protect(string plainText)
	{
		var plainBytes = Encoding.UTF8.GetBytes(plainText);

		using var aes = Aes.Create();
		aes.Key = DerivedKey;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		aes.GenerateIV();

		var iv = aes.IV;
		byte[] cipherBytes;

		using (var encryptor = aes.CreateEncryptor())
			cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

		// iv + cipherText
		var result = new byte[iv.Length + cipherBytes.Length];
		Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
		Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

		return Convert.ToBase64String(result);
	}

	private static string Unprotect(string encryptedText)
	{
		var data = Convert.FromBase64String(encryptedText);

		if (data.Length < IvSizeBytes)
			throw new CryptographicException("Verschluesselter Wert ist zu kurz.");

		var iv = new byte[IvSizeBytes];
		var cipherBytes = new byte[data.Length - IvSizeBytes];
		Buffer.BlockCopy(data, 0, iv, 0, IvSizeBytes);
		Buffer.BlockCopy(data, IvSizeBytes, cipherBytes, 0, cipherBytes.Length);

		using var aes = Aes.Create();
		aes.Key = DerivedKey;
		aes.IV = iv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		using var decryptor = aes.CreateDecryptor();
		var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

		return Encoding.UTF8.GetString(plainBytes);
	}

	// SHA-256 liefert 32 Byte, also genau die Schluessellaenge fuer AES-256.
	private static byte[] DerivedKey => SHA256.HashData(Encoding.UTF8.GetBytes(ApplicationName));
}

/// <summary>
/// Legt ein string-Property verschluesselt in der Konfigurationsdatei ab und liest es beim
/// Laden wieder entschluesselt ein.
/// </summary>
/// <remarks>
/// StockTvKiosk laeuft dafuer per Reflection ueber den gesamten Konfigurationsbaum und sucht
/// [Secret]-Attribute. Hier genuegt ein Converter direkt am Property, weil es genau ein
/// Geheimnis gibt - das Dateiformat ("enc:"-Praefix) ist identisch.
/// </remarks>
public sealed class SecretStringConverter : JsonConverter<string>
{
	public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> SecretProtector.UnprotectIfRequired(reader.GetString());

	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
		=> writer.WriteStringValue(SecretProtector.ProtectIfRequired(value));
}
