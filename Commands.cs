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
            if (_playerManager == null)
            {
                Logger?.Error("player manager is not set");
                return;
            }
            if (_database == null)
            {
                Logger?.Error("database is not set");
                return;
            }
            _playerManager.ClearCache();


            foreach (CCSPlayerController playerController in Utilities.GetPlayers())
            {
                if (playerController == null || !playerController.IsValid)
                {
                    continue;
                }
                _ = Task.Run(async () =>
                {
                    PlayerFromDB? playerDB;
                    try
                    {
                        playerDB = await _database.GetPlayerById(playerController.SteamID);
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    if (playerDB != null)
                    {
                        Server.NextFrame(() =>
                        {

                            PlgPlayer playerPLG = new(playerController)
                            {
                                Side = playerDB.Side,
                                TeamName = playerDB.TeamName,
                                SmokeColor = playerDB.SmokeColor,
                                DiscordId = playerDB.DiscordId,
                                TeamChannelId = playerDB.TeamChannelId,
                                MemberId = playerDB.MemberId
                            };
                            if (playerDB.Weight != null)
                            {
                                playerPLG.Weight = playerDB.Weight.ToString();
                            }
                            _playerManager.UpdatePlayerWithData(playerController, playerDB);
                        });
                    }
                });

            }
        }

        [ConsoleCommand("css_lbackups", "List the 3 most recent backup files with details")]
        public void ListDetailedBackups(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || _backup == null)
            {
                return;
            }

            if (!CanYouDoThat(player, "@css/generic"))
            {
                ReplyToUserCommand(player, $"{ChatColors.Red}You don't have permission to use this command{ChatColors.Default}");
                return;
            }

            _backup.RefreshBackupCache();

            try
            {
                bool isMatchPLG = _matchManager != null && _matchManager.State != MatchManager.MatchState.Setup;

                List<BackupFile> recentBackups = isMatchPLG ? _backup.GetLastPLGBackups(_matchManager.GetMatchId()) : _backup.GetLastBackups();

                if (recentBackups.Count == 0)
                {
                    ReplyToUserCommand(player, $"{ChatColors.Yellow}Pas de backups trouvés{ChatColors.Default}");
                    return;
                }

                ReplyToUserCommand(player, $"{ChatColors.Blue}=== Backups ==={ChatColors.Default}");

                int maxBackups = Math.Min(3, recentBackups.Count);
                for (int i = 0; i < maxBackups; i++)
                {
                    BackupFile backup = recentBackups[i];
                    char indexColor = i == 0 ? ChatColors.Green : ChatColors.White;
                    string message = $"{indexColor}{i} {ChatColors.Default} --- {backup.DisplayName}";

                    ReplyToUserCommand(player, message);
                }

                ReplyToUserCommand(player, $"{ChatColors.Green}'.restore_last' pour récupérer le dernier round{ChatColors.Default}");
                ReplyToUserCommand(player, $"{ChatColors.Green}'.restore_at <index>' pour récupérer au round <index>{ChatColors.Default}");

            }
            catch (Exception ex)
            {
                Logger?.Error($"Error listing detailed backups: {ex.Message}", ex);
                ReplyToUserCommand(player, $"{ChatColors.Red}Error retrieving backup information{ChatColors.Default}");
            }
        }

        [ConsoleCommand("css_restore_last", "Restore the most recent backup")]
        public void RestoreLastBackup(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || _backup == null)
            {
                return;
            }

            if (!CanYouDoThat(player, "@css/generic"))
            {
                ReplyToUserCommand(player, $"{ChatColors.Red}Vous n'avez pas la permission d'utiliser cette commande{ChatColors.Default}");
                return;
            }

            _backup.RefreshBackupCache();
            _backup.RestoreMostRecent();
            ReplyToUserCommand(player, $"{ChatColors.Green}Backup le plus récent restauré{ChatColors.Default}");
        }

        [ConsoleCommand("css_restore", "Restore backup at specific index")]
        public void RestoreBackupAtIndex(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || _backup == null)
            {
                return;
            }

            if (!CanYouDoThat(player, "@css/generic"))
            {
                ReplyToUserCommand(player, $"{ChatColors.Red}Vous n'avez pas la permission d'utiliser cette commande{ChatColors.Default}");
                return;
            }

            string indexArg = command.ArgByIndex(1);
            if (string.IsNullOrEmpty(indexArg) || !int.TryParse(indexArg, out int index))
            {
                ReplyToUserCommand(player, $"{ChatColors.Red}Index invalide. Utilisez: .restore <index>{ChatColors.Default}");
                return;
            }

            _backup.RefreshBackupCache();
            _backup.RestoreAtIndex(index);
            ReplyToUserCommand(player, $"{ChatColors.Green}Backup à l'index {index} restauré{ChatColors.Default}");
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
                ReplyToUserCommand(player, $"[{ChatColors.Red} Match Manager is {state}]{ChatColors.Default}");
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
                Logger?.Info("MatchManager or Teams is null");
                ReplyToUserCommand(player, "Impossibe d'utiliser unready car le match manager est désactivé");
                return;
            }
            if (_teams == null)
            {
                Logger?.Error("The teams are not defined for the match");
                ReplyToUserCommand(player, "Les équipes ne sont pas définis");
                return;
            }
            CsTeam side = player.Team;
            _matchManager.SetTeamReadyBySide(side, false);

            TeamPLG? team = _teams.GetTeamBySide(side);
            string teamName = team != null ? team.Name : "Unknown";
            BroadcastMessage($"L'équipe {teamName} est unready par {player.PlayerName}");
        }

        public void OnReady(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }
            if (_matchManager == null)
            {
                ReplyToUserCommand(player, "Le match manager n'est pas activé !");
                Logger?.Warning("MatchManager not defined");
                return;
            }
            if (_teams == null)
            {
                ReplyToUserCommand(player, $"{ChatColors.DarkRed}Les équipes n'ont pas été trouvé pour le match${ChatColors.Default}");
                Logger?.Error("Teams not defined");
                return;
            }
            if (_playerManager == null)
            {
                Logger?.Error("player manager is null");
                return;
            }

            PlgPlayer? plgPlayer = _playerManager.GetPlayer(player.SteamID);
            string? teamOfPlayer = plgPlayer?.TeamName;

            if (teamOfPlayer == null)
            {

                ReplyToUserCommand(player, "Vous n'avez pas d'équipe, changez de serveur ou faites .join pour rejoindre l'équipe");
                Logger?.Error("the player has no team");
                return;
            }

            TeamPLG? team = _teams.GetTeamByName(teamOfPlayer);

            if (team == null)
            {
                Logger?.Error("the team of the player is not found");
                ReplyToUserCommand(player, "Votre équipe n'a pas été trouvé, impossible de mettre ready");
                ReplyToUserCommand(player, "Vous devez changez de serveur");
                return;
            }

            _matchManager.SetTeamReadyBySide(team.Side, true);

            if (Logger == null)
            {
                return;
            }

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
            player.PrintToChat("Pour la commande, faire : .smoke maCouleur");
            player.PrintToChat("Couleurs :");
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

            if (_matchManager == null || _playerManager == null || _teams == null)
            {
                return;
            }

            _ = Task.Run(_matchManager.UpdateStatsMatch);

            Server.NextFrame(() =>
            {
                ReplyToUserCommand(player, "Stats envoyé ! ");
            });

        }

        [ConsoleCommand("css_unpause", "Unpause the match !")]
        public void OnUnpauseCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }

            UnPauseMatch();

            if (_matchManager == null || _teams == null)
            {
                return;
            }

            HandlePause(player, false);
        }

        [ConsoleCommand("css_warmup", "Warmup")]
        public void Warmup(CCSPlayerController? player, CommandInfo? command)
        {
            if (_matchManager != null)
            {
                if (_matchManager.State == MatchManager.MatchState.Ended)
                {
                    ReplyToUserCommand(player, $"[{ChatColors.Red}Pas de match en cours, impossible de mettre pause{ChatColors.Default}]");
                    ReplyToUserCommand(player, $"[{ChatColors.Green}Utiliser la commande .match_on et .match pour relancer un match{ChatColors.Default}]");
                }
                ReplyToUserCommand(player, $"[{ChatColors.Red}]Impossibe de lancer le warmup en cours de match{ChatColors.Default}");
                return;
            }
            ExecWarmup();

            ReplyToUserCommand(player, $"[{ChatColors.Green}Warmup !{ChatColors.Default}]");
        }

        [ConsoleCommand("css_map", "Changes the map using changelevel")]
        public void OnChangeMapCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (_matchManager?.State == MatchManager.MatchState.Live)
            {
                ReplyToUserCommand(player, "Impossible de changer de map pendant le match");
                ReplyToUserCommand(player, "Faire .match_off pour désactivé le match !");
                return;
            }
            string mapName = command.ArgByIndex(1);
            HandleMapChangeCommand(player, mapName);
            Server.NextFrame(ResetMatchManager);
        }

        // Start the match.cfg, no knife or warmup here
        [ConsoleCommand("css_start", "Execute a match cfg")]
        public void StartLive(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null)
            {
                return;
            }

            if (_backup == null)
            {
                return;
            }

            if (_matchManager != null)
            {
                ReplyToUserCommand(player, "Impossible de lancer car le match manager est on");
                ReplyToUserCommand(player, ".match pour lancer avec le MatchManager");
                ReplyToUserCommand(player, "OU .match_off pour désactiver le MatchManager");
                return;
            }

            StartLive();

            RecordTheDemo();

            _backup.SetStandardBackup();

            ReplyToUserCommand(player, $"[{ChatColors.Green}Live config started !{ChatColors.Default}]");
        }

        [ConsoleCommand("css_restore", "restore a backup file by filename")]
        public void OnRestoreCommand(CCSPlayerController? player, CommandInfo command)
        {
            string filename = command.ArgByIndex(1);
            HandleRestore(player, filename);
        }

        // Start knife.cfg
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

        [ConsoleCommand("css_match_off", "Remove the match manager ! (set it to null)")]
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
            if (player == null || !player.IsValid || _matchManager == null || _database == null || _playerManager == null || _teams == null)
            {
                Logger.Error("No match manager or database or player manager");
                return;
            }

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
                            ReplyToUserCommand(player, $"Tu as rejoint l'équipe {teamBySide.Name}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandlePlayerSetup: {ex.Message}");
            }

        }

        [ConsoleCommand("css_switch", "switch")]
        public void Switch(CCSPlayerController? player, CommandInfo? command)
        {
            if (Logger == null)
            {
                return;
            }

            if (player == null)
            {
                Logger.Error("No players");
                return;
            }

            if (_database == null || _playerManager == null || _matchManager == null || _teams == null)
            {
                Server.ExecuteCommand("mp_swapteams;mp_restartgame 1");
                return;
            }

            if (_matchManager.State == MatchManager.MatchState.WaitingForSideChoice)
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

            if (_matchManager == null || _teams == null)
            {
                return;
            }
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

            if (_database == null)
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
