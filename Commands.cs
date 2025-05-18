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

    public void OnUnready(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null)
        {
            return;
        }
        if (_matchManager == null)
        {
            return;
        }
        var side = player.Team;
        _matchManager.SetTeamReady(side, false);

        var team = _matchManager.TryGetTeamBySide(side);
        var teamName = team != null ? team.GetName() : "Unknown";
        BroadcastMessage($"Team {teamName} unready by {player.PlayerName}");
    }

    public void OnReady(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null)
        {
            return;
        }
        if (_matchManager == null)
        {
            return;
        }
        var side = player.Team;
        _matchManager.SetTeamReady(side, true);

        if (_logger == null)
        {
            return;
        }

        var team = _matchManager.TryGetTeamBySide(side);
        var teamName = team != null ? team.GetName() : "Unknown";
        BroadcastMessage($"Team {teamName} ready by {player.PlayerName}");

        if (_matchManager.IsAllTeamReady())
        {
            Server.NextFrame(async () =>
            {
                await _matchManager.RunMatch();
                BroadcastMessage($"Everyone is ready, let's start the match !!");
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
        if (player == null || _playerManager == null)
        {
            return;
        }

        var allPlayers = Utilities.GetPlayers();

        foreach (var _player in allPlayers)
        {
            var id = _player.SteamID;
            var playerPlg = _playerManager.GetPlayer(id);
            Console.WriteLine("PlayerId: " + id);
            if (playerPlg != null)
            {
                if (_player != null && _player.ActionTrackingServices != null)
                {
                    var playerStats = _player.ActionTrackingServices.MatchStats;

                    Dictionary<string, object> stats = new Dictionary<string, object>
                    {
                        { "PlayerName", player.PlayerName },
                        { "Kills", playerStats.Kills },
                        { "Deaths", playerStats.Deaths },
                        { "Assists", playerStats.Assists },
                        { "Damage", playerStats.Damage },
                        { "Enemy2Ks", playerStats.Enemy2Ks },
                        { "Enemy3Ks", playerStats.Enemy3Ks },
                        { "Enemy4Ks", playerStats.Enemy4Ks },
                        { "Enemy5Ks", playerStats.Enemy5Ks },
                        { "EntryCount", playerStats.EntryCount },
                        { "EntryWins", playerStats.EntryWins },
                        { "1v1Count", playerStats.I1v1Count },
                        { "1v1Wins", playerStats.I1v1Wins },
                        { "1v2Count", playerStats.I1v2Count },
                        { "1v2Wins", playerStats.I1v2Wins },
                        { "UtilityCount", playerStats.Utility_Count },
                        { "UtilitySuccess", playerStats.Utility_Successes },
                        { "UtilityDamage", playerStats.UtilityDamage },
                        { "UtilityEnemies", playerStats.Utility_Enemies },
                        { "FlashCount", playerStats.Flash_Count },
                        { "FlashSuccess", playerStats.Flash_Successes },
                        { "HealthPointsRemovedTotal", playerStats.HealthPointsRemovedTotal },
                        { "HealthPointsDealtTotal", playerStats.HealthPointsDealtTotal },
                        { "ShotsFiredTotal", playerStats.ShotsFiredTotal },
                        { "ShotsOnTargetTotal", playerStats.ShotsOnTargetTotal },
                        { "EquipmentValue", playerStats.EquipmentValue },
                        { "MoneySaved", playerStats.MoneySaved },
                        { "KillReward", playerStats.KillReward },
                        { "LiveTime", playerStats.LiveTime },
                        { "HeadShotKills", playerStats.HeadShotKills },
                        { "CashEarned", playerStats.CashEarned },
                        { "EnemiesFlashed", playerStats.EnemiesFlashed }
                    };
                    playerPlg.Stats = stats;
                }
            }

        }
    }

    [ConsoleCommand("css_unpause", "Unpause the match !")]
    public void OnUnpauseCommand(CCSPlayerController? player, CommandInfo? command)
    {
        UnPauseMatch(player, command);

        if (_matchManager != null)
        {
            _matchManager.state = MatchManager.MatchState.Live;
        }
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

    [ConsoleCommand("css_stay", "team stay !")]
    public void OnStay(CCSPlayerController? player, CommandInfo? command)
    {
        if (_matchManager != null)
        {
            _matchManager.GoGoGo();
        }
    }

    [ConsoleCommand("css_no_match", "Remove the match manager ! (set it to null)")]
    public void OnNoMatch(CCSPlayerController? player, CommandInfo? command)
    {
        if (_matchManager != null)
        {
            _matchManager = null;

        }
    }

    [ConsoleCommand("css_stop_tv", "Stop the current record tv")]
    public void OnStopRecordTv(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null)
        {
            return;
        }

        if (player.IsValid)
        {
            Server.ExecuteCommand("tv_stoprecord");
            player.PrintToChat($"{ChatPrefix} TV stopped");
        }
    }

    [ConsoleCommand("css_switch", "switch")]
    public void Switch(CCSPlayerController? player, CommandInfo? command)
    {
        Server.ExecuteCommand("mp_swapteams;");

        if (_matchManager != null)
        {
            _matchManager.GoGoGo();
            _matchManager.ReverseTeamSides();
        }
    }

    [ConsoleCommand("css_pause", "pause the match")]
    public void OnPauseCommand(CCSPlayerController? player, CommandInfo? command)
    {
        PauseMatch(player, command);

        if (_matchManager != null)
        {
            _matchManager.state = MatchManager.MatchState.Paused;
        }
    }

    [ConsoleCommand("css_match", "start a PLG match")]
    public void OnStartPLGMatch(CCSPlayerController? player, CommandInfo? command)
    {

        if (_matchManager == null)
        {
            return;
        }
        _matchManager.SetTeamReady(CsTeam.Terrorist, true);
        _matchManager.SetTeamReady(CsTeam.CounterTerrorist, true);

        BroadcastMessage("Match started by admin");

        Server.NextFrame(async () =>
        {
            await _matchManager.RunMatch();
        });
    }

    [ConsoleCommand("css_set_teams", "Make every player in their teams based on DB")]
    public void OnSetTeams(CCSPlayerController? player, CommandInfo? command)
    {
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
