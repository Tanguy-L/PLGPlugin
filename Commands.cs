using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
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
}
