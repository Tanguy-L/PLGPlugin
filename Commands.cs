using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin
    {
        [ConsoleCommand("css_load", "Reload the player current cache")]
        public void LoadPlayerCache(CCSPlayerController? player, CommandInfo? command)
        {
            if (_playerManager != null)
            {
                _playerManager.ClearCache();
                foreach (CCSPlayerController playerController in Utilities.GetPlayers())
                {
                    Server.NextFrame(async () =>
                    {
                        await _playerManager.AddPlgPlayer(playerController);
                    });
                }
            }
        }

        [ConsoleCommand("css_match_status", "Get the status of match manager")]
        public void GetMatchManagerStatus(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                Logger?.Error("Player is null");
                return;
            }

            if (_matchManager == null)
            {
                ReplyToUserCommand(player, $"[{ChatColors.Red} Match Manager is null]{ChatColors.Default}");
                ReplyToUserCommand(player, $"[{ChatColors.Green}Type .match_on to start it ! {ChatColors.Default}");
            }

            if (_matchManager != null)
            {
                MatchManager.MatchState state = _matchManager.State;
                ReplyToUserCommand(player, $"[{ChatColors.Red} Match Manager is {state.ToString()}]{ChatColors.Default}");
            }
        }

        public void OnUnready(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }
            if (_matchManager == null || _teams == null)
            {
                Logger?.Info("MatchManager or Teams is null");
                return;
            }
            CsTeam side = player.Team;
            _matchManager.SetTeamReadyBySide(side, false);

            TeamPLG? team = _teams.GetTeamBySide(side);
            string teamName = team != null ? team.Name : "Unknown";
            BroadcastMessage($"Team {teamName} unready by {player.PlayerName}");
        }

        public void OnReady(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }
            if (_matchManager == null || _teams == null)
            {
                Logger?.Error("MatchManager or Teams is null");
                return;
            }
            CsTeam side = player.Team;
            _matchManager.SetTeamReadyBySide(side, true);

            if (Logger == null)
            {
                return;
            }

            TeamPLG? team = _teams.GetTeamBySide(side);
            string teamName = team != null ? team.Name : "Unknown";
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
            IEnumerable<string> allColors = SmokeColorPalette.GetAllColorKeys();
            player.PrintToChat("Couleurs (.smoke maCouleur) :");
            foreach (string color in allColors)
            {
                player.PrintToChat(color);
            }
        }

        [ConsoleCommand("css_list", "Show list of the players")]
        public void ListPlayers(CCSPlayerController? player, CommandInfo? command)
        {
            List<CCSPlayerController> allPlayers = Utilities.GetPlayers();
            if (allPlayers == null || _playerManager == null || player == null)
            {
                return;
            }
            foreach (CCSPlayerController playerCurrent in allPlayers)
            {
                ulong steamiId = playerCurrent.SteamID;
                PlgPlayer? playerPlg = _playerManager.GetPlayer(steamiId);
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

            List<CCSPlayerController> allPlayers = Utilities.GetPlayers();


            IEnumerable<CCSTeam> teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (CCSTeam team in teams)
            {
                BroadcastMessage($"{team.Index}");
                BroadcastMessage($"{team.TeamNum}");
                BroadcastMessage($"{team.TeamMatchStat}");
                BroadcastMessage($"{team.Score}");

            }
        }

        [ConsoleCommand("css_unpause", "Unpause the match !")]
        public void OnUnpauseCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }
            UnPauseMatch();

            HandlePause(player, false);
        }

        [ConsoleCommand("css_warmup", "Warmup")]
        public void Warmup(CCSPlayerController? player, CommandInfo? command)
        {
            if (_matchManager != null)
            {
                if (_matchManager.State == MatchManager.MatchState.Ended)
                {
                    ReplyToUserCommand(player, $"[{ChatColors.Red}Cant do warmup when the match is ended !{ChatColors.Default}]");
                    ReplyToUserCommand(player, $"[{ChatColors.Green}Use .match_new to make a new match{ChatColors.Default}]");
                }
                ReplyToUserCommand(player, $"[{ChatColors.Red}]Cant do warmup when the match is on{ChatColors.Default}");
                return;
            }
            ExecWarmup();

            ReplyToUserCommand(player, $"[{ChatColors.Green}Warmup done !{ChatColors.Default}]");
        }

        [ConsoleCommand("css_map", "Changes the map using changelevel")]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (_matchManager?.State == MatchManager.MatchState.Live)
            {
                ReplyToUserCommand(player, "Cant change map while the match is live, use match_off to stop it.");
                return;
            }
            string mapName = command.ArgByIndex(1);
            HandleMapChangeCommand(player, mapName);
        }

        [ConsoleCommand("css_start", "Execute a match cfg")]
        public void StartLive(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }

            StartLive();

            RecordTheDemo();

            ReplyToUserCommand(player, $"[{ChatColors.Green}Live config started !{ChatColors.Default}]");
        }

        [ConsoleCommand("css_lbackups", "Get 3 last backups files")]
        public void OnGetBackups(CCSPlayerController? player, CommandInfo? command)
        {
            string map = Server.MapName;
            DateTime date = DateTime.Now;
            string parsedDate = date.ToString("yyyyMMdd");
            string path = Server.GameDirectory + "/csgo";
            string[] fileEntries = Directory.GetFiles(path);

            IEnumerable<string> files = Directory.EnumerateFiles(
                path,
                $"{parsedDate}_{map}*.txt",
                SearchOption.AllDirectories
            );

            IEnumerable<string> lastOnes = files
                .TakeLast(3)
                .ToList()
                .Select(static e =>
                {
                    string[] split = e.Split("/");
                    return split[^1];
                });

            foreach (string filename in lastOnes)
            {
                player?.PrintToChat(filename);
            }
        }

        [ConsoleCommand("css_restore", "restore a backup file by filename")]
        public void OnRestoreCommand(CCSPlayerController? player, CommandInfo command)
        {
            string filename = command.ArgByIndex(1);
            HandleRestore(player, filename);
        }

        [ConsoleCommand("css_knife", "knife")]
        public void StartKnife(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }
            if (_matchManager != null)
            {
                if (_matchManager.State == MatchManager.MatchState.Ended)
                {
                    ReplyToUserCommand(player, $"[{ChatColors.Red}Cant do knife when the match is ended !{ChatColors.Default}]");
                    ReplyToUserCommand(player, $"[{ChatColors.Green}Use .match_new to make a new match{ChatColors.Default}]");
                }
                else
                {
                    ReplyToUserCommand(player, $"[{ChatColors.Red}Cant do knife while the match is live !{ChatColors.Default}]");
                }

            }
            else
            {
                StartKnife();
                ReplyToUserCommand(player, $"[{ChatColors.Green}Knife done !{ChatColors.Default}]");
            }

        }

        [ConsoleCommand("css_stay", "team stay !")]
        public void OnStay(CCSPlayerController? player, CommandInfo? command)
        {
            if (Logger == null)
            {
                return;
            }
            if (player == null || _playerManager == null || _teams == null)
            {
                return;
            }
            if (_matchManager != null)
            {
                PlgPlayer? playerPlg = _playerManager.GetPlayer(player.SteamID);
                if (playerPlg == null || !playerPlg.IsValid)
                {
                    return;
                }
                int? idKnife = _matchManager.GetKnifeTeamId();
                if (idKnife == null)
                {
                    Logger.Error("idKnife is null");
                    return;
                }
                TeamPLG? knifeWinner = _teams.GetTeamById(idKnife.Value);
                string nameWinner = knifeWinner != null ? knifeWinner.Name : "Unknown";

                if (nameWinner == playerPlg.TeamName)
                {
                    BroadcastMessage($"L'équipe ${nameWinner} a décidé de Stay ");
                    _matchManager.GoGoGo();
                }
            }
        }

        [ConsoleCommand("css_no_match", "Remove the match manager ! (set it to null)")]
        public void MatchManagerOff(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                Logger?.Error("No player");
                return;
            }
            if (_matchManager != null)
            {
                _matchManager = null;
                ReplyToUserCommand(player, "MatchManager is set to off");
            }
            else
            {
                ReplyToUserCommand(player, "MatchManager is already off");
            }
        }

        [ConsoleCommand("css_match_on", "Set MatchManager to true !")]
        public void MatchManagerOn(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                Logger?.Error("No player");
                return;
            }
            if (_matchManager != null)
            {
                ReplyToUserCommand(player, "MatchManager is already on");
                return;
            }

            InitMatchManager();
            ReplyToUserCommand(player, "MatchManager is set to on");
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


        public void OnJoinTeam(CCSPlayerController? player, CommandInfo? command)
        {
            if (Logger == null)
            {
                return;
            }
            if (player == null || _matchManager == null || _database == null || _playerManager == null || _teams == null)
            {
                Logger.Error("No match manager or database or player manager");
                return;
            }

            if (player.IsValid)
            {
                CsTeam side = player.Team;
                TeamPLG? teamBySide = _teams.GetTeamBySide(side);
                if (teamBySide == null)
                {
                    Logger.Error("No team by side");
                    return;
                }
                int idTeamToJoin = teamBySide.Id;

                try
                {
                    ulong steamId = player.SteamID;
                    PlgPlayer? plgPlayer = _playerManager.GetPlayer(steamId);
                    if (plgPlayer == null || !plgPlayer.IsValid)
                    {
                        Logger.Error("No plg player");
                        return;
                    }
                    string? memberId = plgPlayer.MemberId;
                    if (memberId == null)
                    {
                        Logger.Error("No member id");
                        return;
                    }
                    _ = Task.Run(async () =>
                    {
                        await _database.JoinTeam(memberId, idTeamToJoin);
                        PlayerFromDB? playerData = await _database.GetPlayerById(steamId);
                        Server.NextFrame(() =>
                        {
                            if (playerData != null)
                            {
                                _playerManager.UpdatePlayerWithData(player, playerData);
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in HandlePlayerSetup: {ex.Message}");
                }

            }
        }

        [ConsoleCommand("css_switch", "switch")]
        public void Switch(CCSPlayerController? player, CommandInfo? command)
        {

            if (Logger == null)
            {
                return;
            }
            if (player == null || _playerManager == null || _matchManager == null || _teams == null)
            {
                Logger.Error("No match manager or no player manager");
                return;
            }
            if (_matchManager != null && _matchManager.State == MatchManager.MatchState.WaitingForSideChoice)
            {
                PlgPlayer? playerPlg = _playerManager.GetPlayer(player.SteamID);
                if (playerPlg == null || !playerPlg.IsValid)
                {
                    Console.WriteLine("EROR: No player");
                    return;
                }
                int? knifeWinner = _matchManager.GetKnifeTeamId();
                if (knifeWinner == null)
                {
                    Logger.Error("No knife winner");
                    return;
                }
                TeamPLG? teamWinnerKnife = _teams.GetTeamById(knifeWinner.Value);
                string nameWinner = teamWinnerKnife != null ? teamWinnerKnife.Name : "Unknown";

                if (nameWinner == playerPlg.TeamName)
                {

                    BroadcastMessage($"L'équipe ${nameWinner} a décidé de Stay ");
                    _teams.ReverseSide();
                    _matchManager.GoGoGo();
                    Server.ExecuteCommand("mp_swapteams;mp_restartgame 1");
                }
                else
                {
                    Logger.Error("No knife winner");
                }
            }
        }

        [ConsoleCommand("css_pause", "pause the match")]
        public void OnPauseCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }
            PauseMatch();
            HandlePause(player);
        }

        [ConsoleCommand("css_match", "start a PLG match")]
        public void OnStartPLGMatch(CCSPlayerController? player, CommandInfo? command)
        {

            if (_matchManager == null)
            {
                Logger?.Error("No match manager");
                return;
            }

            _matchManager.SetTeamReadyBySide(CsTeam.Terrorist, true);
            _matchManager.SetTeamReadyBySide(CsTeam.CounterTerrorist, true);

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

            List<CCSPlayerController> players = Utilities.GetPlayers();

            foreach (CCSPlayerController playerController in players)
            {
                PlgPlayer? plgPlayer = _playerManager.GetPlayer(playerController.SteamID);
                if (plgPlayer == null)
                {
                    return;
                }
                string? sideInDb = plgPlayer.Side;
                CsTeam sideInGame = playerController.Team;

                if (sideInDb == null)
                {
                    return;
                }

                if (!Enum.TryParse(sideInDb, out CsTeam sideInDbParsed))
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
}
