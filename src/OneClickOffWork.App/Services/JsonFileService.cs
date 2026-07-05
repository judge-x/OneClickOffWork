using System.Text.Json;
using System.IO;

namespace OneClickOffWork.Services;

public static class JsonFileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T> LoadOrCreateAsync<T>(string path, Func<T> defaultFactory, LogService? log = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            var value = defaultFactory();
            await SaveAsync(path, value);
            return value;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var value = await JsonSerializer.DeserializeAsync<T>(stream, Options);
            return value ?? defaultFactory();
        }
        catch (Exception ex)
        {
            var backup = $"{path}.broken.{DateTime.Now:yyyyMMddHHmmss}.bak";
            try { File.Copy(path, backup, true); } catch { }
            log?.Error("JSON 文件损坏，已生成默认配置", ex);
            var value = defaultFactory();
            await SaveAsync(path, value);
            return value;
        }
    }

    public static async Task SaveAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = $"{path}.tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, value, Options);
        }
        if (File.Exists(path)) File.Delete(path);
        File.Move(temp, path);
    }
}
