// 역할 : 실행 파일 옆의 JSON 설정 파일을 읽어 설정 객체로 만든다. 파일이 없으면 기본값으로 만들고 경고를 남긴다.
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Shared.Logging;

namespace Game.ServerCommon.Config;

public static class JsonConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static T Load<T>(string fileName)
        where T : new()
    {
        string path = Path.Combine(AppContext.BaseDirectory, fileName);
        if (!File.Exists(path))
        {
            Log.Warning($"설정 파일이 없어 기본값을 씁니다: {path}");
            return new T();
        }

        T? config = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        return config ?? new T();
    }

    public static string ToJson<T>(T config) => JsonSerializer.Serialize(config, Options);
}
