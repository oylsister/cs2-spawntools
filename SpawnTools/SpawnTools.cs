using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;


namespace SpawnTools;

[MinimumApiVersion(78)]
public partial class SpawnTools : BasePlugin
{
    public override string ModuleName { get; } = "Spawn Tools";
    public override string ModuleVersion { get; } = "1.3";
    public override string ModuleAuthor { get; } = "Retro - https://insanitygaming.net";
    public override string ModuleDescription { get; } = "Allows you to dynamically create spawn points per map";

    private string _configPath = "";
    private bool _wasHotReload = false;

    private Config? _config;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        _wasHotReload = hotReload;
        if(hotReload)
            OnMapStart(Server.MapName);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Logger.LogInformation($"Round started with {_config?.SpawnPoints.Count ?? 0} spawn point!");

        _spawnModel?.Clear();

        var noVel = new Vector(0f, 0f, 0f);
        var spawn = 0;

        if (_config?.SpawnPoints == null)
        {
            Logger.LogInformation($"It's null!");
            return HookResult.Continue;
        }

        foreach (var spawnPoint in _config?.SpawnPoints!)
        {
            var angleString = StringToVector(spawnPoint.Angle!);
            var angle = new QAngle(angleString.X, angleString.Y, angleString.Z);
            var pos = StringToVector(spawnPoint.Origin!);

            SpawnPoint? entity;

            if (spawnPoint.Team == CsTeam.Terrorist)
                entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_terrorist");

            else
                entity = Utilities.CreateEntityByName<CInfoPlayerCounterterrorist>("info_player_counterterrorist");

            if (entity == null)
            {
                Server.PrintToChatAll("It's null");
                continue;
            }

            entity.Teleport(pos, new QAngle(angle.X, angle.Y, angle.Z), noVel);
            entity.UniqueHammerID = "42069";
            entity.DispatchSpawn();
            Server.PrintToChatAll("Did it");
            spawn++;
        }

        Logger.LogInformation("Created a total of {0} out of {1}", spawn, _config.SpawnPoints.Count);
        
        return HookResult.Continue;
    }
}