using System.Text.Json;
namespace StockTvBlazor.Settings;

public interface IThemeRepository
{
	Task<List<CustomTheme>> LoadAllAsync();
	Task SaveAllAsync(List<CustomTheme> themes);
	Task<CustomTheme?> GetByIdAsync(Guid id);
	Task AddOrUpdateAsync(CustomTheme theme);
	Task DeleteAsync(Guid id);
}



public class JsonThemeRepository : IThemeRepository
{
	private readonly string _filePath;
	private readonly SemaphoreSlim _lock = new(1, 1);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true
	};

	public JsonThemeRepository(IWebHostEnvironment env)
	{
		var dataDir = Path.Combine(env.ContentRootPath, "data");
		Directory.CreateDirectory(dataDir);
		_filePath = Path.Combine(dataDir, "custom-themes.json");
	}

	public async Task<List<CustomTheme>> LoadAllAsync()
	{
		await _lock.WaitAsync();
		try
		{
			if (!File.Exists(_filePath))
				return new List<CustomTheme>();

			var json = await File.ReadAllTextAsync(_filePath);
			return JsonSerializer.Deserialize<List<CustomTheme>>(json, JsonOptions)
				   ?? new List<CustomTheme>();
		}
		finally
		{
			_lock.Release();
		}
	}

	public async Task SaveAllAsync(List<CustomTheme> themes)
	{
		await _lock.WaitAsync();
		try
		{
			var json = JsonSerializer.Serialize(themes, JsonOptions);
			await File.WriteAllTextAsync(_filePath, json);
		}
		finally
		{
			_lock.Release();
		}
	}

	public async Task<CustomTheme?> GetByIdAsync(Guid id)
	{
		var all = await LoadAllAsync();
		return all.FirstOrDefault(t => t.Id == id);
	}

	public async Task AddOrUpdateAsync(CustomTheme theme)
	{
		var all = await LoadAllAsync();
		var index = all.FindIndex(t => t.Id == theme.Id);

		if (index >= 0)
			all[index] = theme;
		else
			all.Add(theme);

		await SaveAllAsync(all);
	}

	public async Task DeleteAsync(Guid id)
	{
		var all = await LoadAllAsync();
		all.RemoveAll(t => t.Id == id);
		await SaveAllAsync(all);
	}
}
