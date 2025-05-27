using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin : BasePlugin
    {
        private static readonly string ChatPrefix =
            $"[{ChatColors.Blue}P{ChatColors.Yellow}L{ChatColors.Red}G{ChatColors.Default}]";
        private static readonly string AdminChatPrefix =
            $"[{ChatColors.Red}ADMIN{ChatColors.Default}]";

        public async Task HandleUpdateSmoke(CCSPlayerController playerController, string commandArg)
        {
            if (_database == null || _playerManager == null)
            {
                return;
            }

            try
            {
                ulong steamId = playerController.SteamID;
                await Task.Run(async () =>
                {
                    await _database.SetSmoke(steamId, commandArg);
                    PlayerFromDB? playerData = await _database.GetPlayerById(steamId);
                    await Server.NextFrameAsync(() =>
                    {
                        if (playerData != null)
                        {
                            _playerManager.UpdatePlayerWithData(playerController, playerData);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandlePlayerSetup: {ex.Message}");
            }
        }


        public void BroadcastMessage(string message)
        {
            Server.PrintToChatAll($"{ChatPrefix} {message}");
        }

        // ----------- FLAGS ----------------
        // @css/reservation # Reserved slot access.
        // @css/generic # Generic admin.
        // @css/kick # Kick other players.
        // @css/ban # Ban other players.
        // @css/unban # Remove bans.
        // @css/vip # General vip status.
        // @css/slay # Slay/harm other players.
        // @css/changemap # Change the map or major gameplay features.
        // @css/cvar # Change most cvars.
        // @css/config # Execute config files.
        // @css/chat # Special chat privileges.
        // @css/vote # Start or create votes.
        // @css/password # Set a password on the server.
        // @css/rcon # Use RCON commands.
        // @css/cheats # Change sv_cheats or use cheating commands.
        // @css/root # Magically enables all flags and ignores immunity values.
        // ----------- FLAGS ----------------
        public bool CanYouDoThat(CCSPlayerController player, string flag = "@css/generic")
        {
            return AdminManager.PlayerHasPermissions(player, flag);
        }

        public static void ReplyToUserCommand(CCSPlayerController? player, string message)
        {
            {
                player?.PrintToChat($"{ChatPrefix} {message}");
            }
        }

        public static bool IsPlayerValid(CCSPlayerController? player)
        {
            return player != null
                && player.IsValid
                && !player.IsBot
                && player.Pawn != null
                && player.Pawn.IsValid
                && player.Connected == PlayerConnectedState.PlayerConnected
                && !player.IsHLTV;
        }

        public void ExecCfg(string nameFile)
        {
            string relativePath = Path.Join(Config.CfgFolder + nameFile);
            Server.ExecuteCommand($"exec {relativePath}");
        }

        public void ExecWarmup()
        {
            ExecCfg("warmup.cfg");
        }

        public void StartKnife()
        {
            ExecCfg("knife.cfg");
        }

        public void StartLive()
        {
            ExecCfg("match.cfg");
        }

        public void HandlePause(CCSPlayerController player, bool isPause = true)
        {
            string pauseMessage = isPause ? "Paused by" : "Unpaused by";
            if (_matchManager != null && _playerManager != null)
            {
                PlgPlayer? plgPlayer = _playerManager.GetPlayer(player.SteamID);
                if (plgPlayer != null && plgPlayer.IsValid)
                {
                    string? teamPlayer = plgPlayer.TeamName;
                    BroadcastMessage($"{pauseMessage} {plgPlayer.PlayerName} in team {teamPlayer}");

                }
                _matchManager.State = isPause ? MatchManager.MatchState.Paused : MatchManager.MatchState.Live;
            }

            if (isPause)
            {
                PauseMatch();
            }
            else
            {
                UnPauseMatch();
            }
        }

        public void RecordTheDemo()
        {
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string map = Server.MapName;
            string titleDemo = formattedTime + "_" + map + ".dem";
            string path = Server.GameDirectory + "/csgo/demos";
            string? directoryPath = Path.GetDirectoryName(path);
            if (directoryPath != null)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            Server.ExecuteCommand($"tv_record ./demos/{titleDemo}");
        }

        private void PauseMatch()
        {
            Server.ExecuteCommand("mp_pause_match");
        }

        private void UnPauseMatch()
        {
            Server.ExecuteCommand("mp_unpause_match");
        }

        private void ClearMatchManager()
        {
            if (_teams == null)
            {
                PLGPlugin.Instance.Logger?.Error("Teams is null");
                return;
            }
            _teams.ClearTeams();

            if (_matchManager == null)
            {
                PLGPlugin.Instance.Logger?.Error("MatchManager is null");
                return;
            }
            _matchManager = null;
        }

        private void InitMatchManager()
        {
            if (Logger == null)
            {
                throw new InvalidOperationException("Logger must be initialized before initializing the match manager.");
            }

            if (_database != null && _playerManager != null && _backup != null)
            {
                try
                {
                    _teams = new TeamManager();
                    Logger?.Info("Match manager initialized successfully");

                    if (Config.StartOnMatch && _teams != null)
                    {
                        string? hostnameValue = ConVar.Find("hostname")?.StringValue;
                        if (string.IsNullOrEmpty(hostnameValue))
                        {
                            Logger?.Warning("Cannot initialize match: hostname is null or empty");
                            return;
                        }

                        // Handle async database operations properly
                        Server.NextFrame(async () =>
                        {
                            try
                            {
                                List<TeamPLG> teams = await _database.GetTeamsByHostname(hostnameValue);

                                // Validate teams data
                                if (teams == null || teams.Count < 2)
                                {
                                    Logger?.Error($"Failed to load teams for hostname: {hostnameValue}. Not enough teams returned.");
                                    return;
                                }

                                // Schedule UI updates on the main thread
                                Server.NextFrame(() =>
                                {
                                    try
                                    {
                                        // Reinitialize teams manager with loaded data
                                        _teams.AddTeam(teams[0]);
                                        _teams.AddTeam(teams[1]);

                                        _matchManager = new MatchManager(_database, _playerManager, Config, _backup, _teams, Logger);
                                        // Initialize the match
                                        _matchManager.InitSetupMatch(hostnameValue);
                                        Logger?.Info($"Match initialized successfully for {hostnameValue}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger?.Error($"Error during match setup: {ex.Message}");
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                Logger?.Error($"Database error when fetching teams: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Error($"Failed to initialize match manager: {ex.Message}");
                }
            }
            else
            {
                // More descriptive error that indicates exactly what's missing
                Logger?.Error($"Cannot initialize match manager. Missing dependencies: " +
                             $"{(_database == null ? "database " : "")}" +
                             $"{(_playerManager == null ? "playerManager " : "")}" +
                             $"{(_backup == null ? "backup " : "")}" +
                             $"{(_teams == null ? "teams" : "")}");
            }
        }

        private void SendAvailableCommandsMessage(CCSPlayerController? player)
        {
            if (player == null)
            {
                return;
            }

            if (CanYouDoThat(player, "@css/generic"))
            {
                ReplyToUserCommand(
                    player,
                    $"{ChatColors.Red}----[INFOS ADMINS]----{ChatColors.Default}"
                );
                ReplyToUserCommand(player, "Player Manager : .load (reload players cache)");
                ReplyToUserCommand(player, "Match mode: .match_on -- .match_off");
                ReplyToUserCommand(player, "Match : .start -- .warmup -- .knife -- .switch");
                ReplyToUserCommand(player, "Backups : .lbackups -- .restore <filename>");
                ReplyToUserCommand(player, "Pause : .pause -- .unpause");
                ReplyToUserCommand(player, "Players : .list");
                ReplyToUserCommand(player, "DB : .set_teams");
            }
            ReplyToUserCommand(player, $"{ChatColors.White}----[INFOS]----{ChatColors.Default}");
            ReplyToUserCommand(player, "smoke : .smoke <red> -- .colors");
        }

        private void HandleMapChangeCommand(CCSPlayerController? player, string mapName)
        {
            if (!long.TryParse(mapName, out _) && !mapName.Contains('_'))
            {
                mapName = "de_" + mapName;
            }

            if (long.TryParse(mapName, out _))
            { // Check if mapName is a long for workshop map ids
                Server.ExecuteCommand($"bot_kick");
                Server.ExecuteCommand($"host_workshop_map \"{mapName}\"");
            }
            else if (Server.IsMapValid(mapName))
            {
                Server.ExecuteCommand($"bot_kick");
                Server.ExecuteCommand($"changelevel \"{mapName}\"");
            }
        }

        private void HandleRestore(CCSPlayerController? player, string filename)
        {
            Server.ExecuteCommand($"mp_backup_restore_load_file {filename}");
        }
    }
}
