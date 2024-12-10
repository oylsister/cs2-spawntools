using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;


namespace SpawnTools;
public partial class SpawnTools {
    private void OnMapStart(string mapName)
    {
        _config = new();
        _configPath =
            Path.Combine(ModuleDirectory, $"../../configs/plugins/spawntools/{Server.MapName.ToLower()}.json");

        if (!File.Exists(_configPath))
        {
            Logger.LogInformation("Couldn't find {0}", _configPath);
            return;
        }

        Logger.LogInformation("Found {0}", _configPath);

        var jsonString = File.ReadAllText(_configPath);
        var config = JsonSerializer.Deserialize<Config>(jsonString);
        _config = config;

        /*
        Logger.LogInformation("Config Loaded success!");
        Logger.LogInformation("{0}", jsonString);
        Logger.LogInformation("{0}", config?.SpawnPoints[0].Origin);
        */

        if (_wasHotReload)
        {
            OnRoundStart(null!, null!);
            _wasHotReload = false;
        }
    }

    public class CustomSpawnPoint
    {
        public CsTeam Team { get; set; }
        public string? Origin { get; set; }
        public string? Angle { get; set; }   
    }

    public class AngleConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the property name
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                
                // Advance to the value
                reader.Read();
                
                if (propertyName == "angle" || propertyName == "angel")
                {
                    return reader.GetString()!;
                }
            }

            throw new JsonException("Invalid JSON format for CustomSpawnPoint.");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteString("angle", value);
        }
    }

    private class Config
    {
        public List<CustomSpawnPoint> SpawnPoints { get; set; } = [];
    }
}