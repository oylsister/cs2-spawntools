using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;


namespace SpawnTools;

public partial class SpawnTools
{
    List<CDynamicProp>? _spawnModel = null;

    [ConsoleCommand("css_addspawn", "Adds a new spawn point")]
    [RequiresPermissions("@css/generic")]
    [CommandHelper(whoCanExecute:CommandUsage.CLIENT_ONLY, minArgs:1, usage:"[ct/t/both]")]
    public void CommandAddSpawnPoint(CCSPlayerController? player, CommandInfo command)
    {
        if(_config == null)
            _config = new Config();

        if (player == null || !player.PlayerPawn.IsValid) return;
        var arg = command.GetArg(1);
        if (!arg.Equals("ct") && !arg.Equals("t") && !arg.Equals("both"))
        {
            command.ReplyToCommand($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} {ChatColors.LightRed}{arg}{ChatColors.Default} is not a valid team.");
            return;
        }
        var origin = player.PlayerPawn.Value?.AbsOrigin;
        if (origin == null)
        {
            command.ReplyToCommand($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} You do not have an origin");
            return;
        }

        var angle = player.PlayerPawn.Value?.AbsRotation;
        if (angle == null)
        {
            command.ReplyToCommand($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} You do not have a rotation");
            return;
        }
        if(arg.Equals("ct") || arg.Equals("both"))
        {
            var point = new CustomSpawnPoint
            {
                Team = CsTeam.CounterTerrorist,
                Origin = VectorToString(new Vector3(origin.X, origin.Y, origin.Z)),
                Angle = VectorToString(new Vector3(angle.X, angle.Y, angle.Z))
            };
            _config?.SpawnPoints.Add(point);

            CreateSpawnPoint(origin, angle);
        }

        if (arg.Equals("t") || arg.Equals("both"))
        {
            var point = new CustomSpawnPoint
            {
                Team = CsTeam.Terrorist,
                Origin = VectorToString(new Vector3(origin.X, origin.Y, origin.Z)),
                Angle = VectorToString(new Vector3(angle.X, angle.Y, angle.Z))
            };
            _config?.SpawnPoints.Add(point);

            CreateSpawnPoint(origin, angle, false);
        }
        var jsonString = JsonConvert.SerializeObject(_config, Formatting.Indented);
        File.WriteAllText(_configPath, jsonString);
        player.PrintToChat($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} Added {(arg.Equals("ct") ? ChatColors.Blue : ChatColors.LightRed)}{arg}{ChatColors.Default} spawn point");
    }

    [ConsoleCommand("css_delspawn", "Deletes the closest spawn point")]
    [RequiresPermissions("@css/generic")]
    [CommandHelper(whoCanExecute:CommandUsage.CLIENT_ONLY)]
    public void CommandDelSpawnPoint(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PlayerPawn.IsValid) return;
        if(player.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
        var spawnPoints = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").ToList();
        spawnPoints.AddRange(Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist"));

        SpawnPoint? closest = null;
        float distance = -1f;
        foreach(var spawn in spawnPoints) 
        {
            var howFar = (spawn.AbsOrigin! - player!.PlayerPawn.Value?.AbsOrigin!).Length2DSqr();
            if(howFar > 20f || (distance != -1f && distance < howFar)) return;
            closest = spawn;
            distance = howFar;
        }
        if(closest is null) return;

        if(closest.UniqueHammerID == "42069")
        {
            var elem = _config!.SpawnPoints.First((p) => p.Angle == VectorToString(closest.AbsRotation!));
            if(elem is null) return;

            player.PrintToChat($" {ChatColors.LightRed}[SpawnTools]{ChatColors.Default} Remove {elem.Team} | {elem.Origin} | {elem.Angle} spawn point");

            _config!.SpawnPoints.Remove(elem);
            closest.Remove();
        }

        var jsonString = JsonConvert.SerializeObject(_config, Formatting.Indented);
        File.WriteAllText(_configPath, jsonString);
    }

    [ConsoleCommand("css_spawncount")]
    public void CommandSpawnCount(CCSPlayerController? player, CommandInfo info)
    {
        var ct = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist");
        var t = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist");

        info.ReplyToCommand($"CT = {ct.Count()} | T = {t.Count()}");
    }

    [ConsoleCommand("css_showspawn")]
    public void ShowSpawnModel(CCSPlayerController? player, CommandInfo info)
    {
        if (_spawnModel == null)
            _spawnModel = new List<CDynamicProp>();

        if(_spawnModel.Count > 0)
        {
            foreach(var prop in _spawnModel)
            {
                prop.AddEntityIOEvent("Kill", prop, null, "", 0.1f);
            }
        }

        _spawnModel.Clear();

        var ct = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist");
        var t = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist");

        foreach(var spawn in ct)
        {
            var spawnpoint = CreatePlayerEntity(spawn.AbsOrigin!, spawn.AbsRotation!);

            if(spawnpoint == null) continue;

            _spawnModel.Add(spawnpoint);
        }

        foreach (var spawn in t)
        {
            var spawnpoint = CreatePlayerEntity(spawn.AbsOrigin!, spawn.AbsRotation!, false);

            if (spawnpoint == null) continue;

            _spawnModel.Add(spawnpoint);
        }
    }

    [ConsoleCommand("css_spawndata")]
    public void SpawnData(CCSPlayerController? player, CommandInfo info)
    {
        var config = _config?.SpawnPoints;

        if (config == null)
        {
            info.ReplyToCommand("Config is null");
            return;
        }

        foreach(var spawn in config)
        {

        }
    }

    private CDynamicProp? CreatePlayerEntity(Vector Position, QAngle Rotation, bool ct = true)
    {
        var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (model == null)
            return null;

        model.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(model.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
        if (ct) model.SetModel("characters/models/ctm_fbi/ctm_fbi_variantf.vmdl");
        else model.SetModel("characters/models/tm_professional/tm_professional_varf4.vmdl");
        model.UseAnimGraph = false;
        model.AcceptInput("SetAnimation", value: "tools_preview");
        model.DispatchSpawn();
        model.Teleport(Position, Rotation, new Vector(0, 0, 0));

        return model;
    }
    
    private SpawnPoint? CreateSpawnPoint(Vector pos, QAngle angle, bool ct = true)
    {
        SpawnPoint? entity;

        if(ct)
            entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_counterterrorist");

        else
            entity = Utilities.CreateEntityByName<CInfoPlayerTerrorist>("info_player_terrorist");

        if (entity == null)
            return null;

        entity.Teleport(pos, angle);
        entity.UniqueHammerID = "42069";
        entity.DispatchSpawn();

        return entity;
    }
}