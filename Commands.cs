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
            var allPlayers = Utilities.GetPlayers();
            foreach (var playerPlg in allPlayers)
            {
                _playerManager.ClearCache();
                Server.NextFrame(async () =>
                {
                    await _playerManager.AddPlgPlayer(playerPlg);
                });
            }
        }
    }

    [ConsoleCommand("css_dgroup", "Regroup on the discord")]
    public void OnGroupPlayers(CCSPlayerController? player, CommandInfo? command)
    {
        Console.WriteLine("test1");
    }

    [ConsoleCommand("css_dgroup", "Regroup on the discord")]
    public void OnSplitPlayers(CCSPlayerController? player, CommandInfo? command)
    {
        Console.WriteLine("test2");
    }

    [ConsoleCommand("css_colors", "Print to chat all colors for smokes")]
    public void PrintColors(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null)
        {
            return;
        }
        var allColors = SmokeColorPalette.GetAllColorKeys();
        var stringColors = string.Join("\n", allColors);
        player.PrintToChat(stringColors);
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
            if (playerPlg != null && playerPlg.IsValid)
            {
                player.PrintToChat(
                    $"{playerPlg?.SteamID} ---- {playerPlg?.TeamName} ---- {playerPlg?.PlayerName} ---- {playerPlg?.Side}"
                );
            }
        }
    }

    [ConsoleCommand("css_help", "Triggers provided command on the server")]
    public void OnHelpCommand(CCSPlayerController? player, CommandInfo? command)
    {
        SendAvailableCommandsMessage(player);
    }

    [ConsoleCommand("css_test", "Dont test that command !")]
    public void OnTestCommand(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null)
        {
            return;
        }
        var canBan = CanYouDoThat(player, "@css/generic");
    }

    [ConsoleCommand("css_unpause", "Unpause the match !")]
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

    [ConsoleCommand("css_lbackups", "Get 3 last backups files")]
    public void OnGetBackups(CCSPlayerController? player, CommandInfo? command)
    {
        var map = Server.MapName;
        var date = DateTime.Now;
        var parsedDate = date.ToString("yyyyMMdd");
        var path = Server.GameDirectory + "/csgo";
        string[] fileEntries = Directory.GetFiles(path);

        var files = Directory.EnumerateFiles(
            path,
            $"{parsedDate}_{map}*.txt",
            SearchOption.AllDirectories
        );

        var lastOnes = files
            .TakeLast(3)
            .ToList()
            .Select(e =>
            {
                var split = e.Split("/");
                return split[split.Count() - 1];
            });

        foreach (var filename in lastOnes)
        {
            player?.PrintToChat(filename);
        }
    }

    [ConsoleCommand("css_restore", "restore a backup file by filename")]
    public void OnRestoreCommand(CCSPlayerController? player, CommandInfo command)
    {
        var filename = command.ArgByIndex(1);
        HandleRestore(player, filename);
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
}
