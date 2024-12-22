using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

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
        RecordTheDemo();
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

    [ConsoleCommand("css_pause", "pause the match")]
    public void OnPauseCommand(CCSPlayerController? player, CommandInfo? command)
    {
        PauseMatch(player, command);
    }

    [ConsoleCommand("css_set_teams", "Make every player in their teams based on DB")]
    public void OnSetTeams(CCSPlayerController? player, CommandInfo? command)
    {
        Console.WriteLine("INFO Set teams !");

        if (_playerManager == null)
        {
            return;
        }

        var players = Utilities.GetPlayers();

        foreach (var playerController in players)
        {
            var plgPlayer = _playerManager.GetPlayer(playerController.SteamID);
            if (plgPlayer == null)
            {
                return;
            }
            var sideInDb = plgPlayer.Side;
            var sideInGame = playerController.Team;

            if (sideInDb == null)
            {
                return;
            }

            if (!Enum.TryParse<CsTeam>(sideInDb, out CsTeam sideInDbParsed))
            {
                Console.WriteLine($"Could not parse team value: {sideInDb}");
                return;
            }

            if (sideInGame != sideInDbParsed)
            {
                playerController.SwitchTeam(sideInDbParsed);
                playerController.CommitSuicide(false, true);
            }
        }
    }

    // Pretty sure its not working, rewrite this bad boy with Task.RUN
    [ConsoleCommand("css_group", "Group players from the channels")]
    private void OnGroupPlayers(CCSPlayerController? player, CommandInfo? commandInfo)
    {
        var stateCommands = new List<string>()
        {
            "!group-parties",
            "Regroupement des channels",
            "Echec du regroupement",
        };

        Server.NextFrame(() =>
        {
            Task.Run(async () =>
            {
                await ExecuteCommandDiscord(stateCommands, commandInfo);
            });
        });
    }

    [ConsoleCommand("css_split", "Split players in the 2 channels")]
    private void OnSplitPlayers(CCSPlayerController? player, CommandInfo? commandInfo)
    {
        var stateCommands = new List<string>()
        {
            "!split-parties",
            "Séparation des channels",
            "Echec de la séparation",
        };
        Server.NextFrame(() =>
        {
            Task.Run(async () =>
            {
                await ExecuteCommandDiscord(stateCommands, commandInfo);
            });
        });
    }
}
