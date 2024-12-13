using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace PLGPlugin;

public sealed partial class PLGPlugin
{
    [ConsoleCommand("css_load", "Reload the player current cache")]
    public void LoadPlayerCache(CCSPlayerController? player, CommandInfo? command)
    {
        if (_playerManager != null)
        {
            Server.NextFrame(async () =>
            {
                await _playerManager.LoadCache();
            });
        }
    }

    [ConsoleCommand("css_colors", "Print to chat all colors for smokes")]
    public void PrintColors(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null)
        {
            return;
        }
        var allColors = SmokeColorPalette.GetAllColorKeys();
        var stringColors = string.Join("--", allColors);
        player.PrintToChat(stringColors);
        player.PrintToChat("coucou!");
    }

    [ConsoleCommand("css_list", "Show list of the players")]
    public void ListPlayers(CCSPlayerController? player, CommandInfo? command)
    {
        var allPlayers = Utilities.GetPlayers();
        if (allPlayers == null || _playerManager == null || player == null)
        {
            return;
        }
        foreach (var playerCurrent in allPlayers)
        {
            var steamiId = playerCurrent.SteamID;
            var playerPlg = _playerManager.GetPlayer(steamiId);
            player.PrintToChat(
                $"{playerPlg?.SteamID} ---- {playerPlg?.TeamName} ---- {playerPlg?.PlayerName} ---- {playerPlg?.Side}"
            );
        }
    }

    [ConsoleCommand("css_help", "Triggers provided command on the server")]
    public void OnHelpCommand(CCSPlayerController? player, CommandInfo? command)
    {
        SendAvailableCommandsMessage(player);
    }

    [ConsoleCommand("css_unpause", "Triggers tes commandasd on the server")]
    public void OnUnpauseCommand(CCSPlayerController? player, CommandInfo? command)
    {
        UnPauseMatch(player, command);
    }

    [ConsoleCommand("css_warmup", "Warmup")]
    public void Warmup(CCSPlayerController? player, CommandInfo? command)
    {
        ExecWarmup();
    }

    [ConsoleCommand("css_map", "Changes the map using changelevel")]
    public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
    {
        var mapName = command.ArgByIndex(1);
        HandleMapChangeCommand(player, mapName);
    }

    [ConsoleCommand("css_start", "Warmup")]
    public void StartLive(CCSPlayerController? player, CommandInfo? command)
    {
        StartLive();
    }

    [ConsoleCommand("css_knife", "knife")]
    public void StartKnife(CCSPlayerController? player, CommandInfo? command)
    {
        StartKnife();
    }

    [ConsoleCommand("css_switch", "switch")]
    public void Switch(CCSPlayerController? player, CommandInfo? command)
    {
        Server.ExecuteCommand("mp_swapteams;");
    }

    [ConsoleCommand("css_pause", "Ttes")]
    public void OnPauseCommand(CCSPlayerController? player, CommandInfo? command)
    {
        PauseMatch(player, command);
    }
}
