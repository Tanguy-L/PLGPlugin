
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin
    {
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {


            if (Logger == null)
            {
                Console.WriteLine("[PLG] Logger is null in OnPlayerConnectFull");
                return HookResult.Continue;
            }

            CCSPlayerController? playerId = @event.Userid;

            if (playerId == null)
            {
                return HookResult.Continue;
            }

            bool isAdmin = CanYouDoThat(playerId);

            if (!playerId.IsValid || playerId.IsBot)
            {
                return HookResult.Continue;
            }

            if (_playerManager == null)
            {
                Logger.Warning("PlayerManager is null in OnPlayerConnectFull");
                return HookResult.Continue;
            }

            if (_database == null)
            {
                Logger.Warning("DB is null in OnPlayerConnectFull");
                ReplyToUserCommand(playerId, $"Bienvenue dans le serveur PLG !");
                ReplyToUserCommand(playerId, "Tapez .help pour voir la liste des commandes");

                if (isAdmin)
                {
                    ReplyToUserCommand(playerId, "DB null");
                    ReplyToUserCommand(playerId, $"{ChatColors.Red} ADMIN Détecté ! {ChatColors.Default}");
                }

                return HookResult.Continue;
            }

            if (isAdmin)
            {
                ReplyToUserCommand(playerId, $"{ChatColors.Red} ADMIN Détecté ! {ChatColors.Default}");
            }

            ulong steamId = playerId.SteamID;
            string playerName = playerId.PlayerName;

            _ = Task.Run(async () =>
            {
                PlayerFromDB? playerDB;
                try
                {
                    playerDB = await _database.GetPlayerById(steamId);

                    if (playerDB == null)
                    {
                        await _database.CreatePlayerInDB(playerName, steamId);
                        playerDB = await _database.GetPlayerById(steamId);
                        Logger.Info($"User {playerName} created !");
                    }

                    if (playerDB == null)
                    {
                        Server.NextFrame(() =>
                        {
                            Logger.Error("Cant create in DB");
                        });
                        return;
                    }

                    Server.NextFrame(() =>
                    {
                        // -----------
                        // UPDATE CACHE WITH NEW PLAYER
                        _playerManager.UpdatePlayerWithData(playerId, playerDB);
                        PlgPlayer? playerPLG = _playerManager.GetPlayer(steamId);

                        //TODO Check if the sound is hello or bangbang
                        _sounds?.PlaySound(playerId, "sounds/plg_sounds/hello.vsnd");

                        // --------------------
                        // HANDLE PLAYER WITH MATCH MANAGER
                        // SET PLAYER IN HIS TEAM SIDE
                        if (_teams != null && playerPLG != null && _matchManager != null)
                        {
                            string? teamName = playerPLG.TeamName;

                            if (teamName == null)
                            {
                                return;
                            }

                            TeamPLG? team = _teams.GetTeamByName(teamName);
                            if (team == null)
                            {
                                return;
                            }

                            playerId.ChangeTeam(team.Side);
                            if (_matchManager != null && _matchManager.State == MatchManager.MatchState.Setup)
                            {
                                Server.NextFrame(() =>
                                {
                                    playerId.Respawn();
                                });
                            }
                        }

                        // ---------------
                        // AFTER HANDLING PLAYER DB
                        ReplyToUserCommand(playerId, $"Bienvenue dans le serveur PLG {playerPLG?.PlayerName} !");
                        ReplyToUserCommand(playerId, "Tapez .help pour voir la liste des commandes");
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"message : {ex}");
                }
            });


            return HookResult.Continue;
        }

        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? playerController = @event.Userid;

            if (playerController != null && _playerManager != null)
            {
                _playerManager.RemovePlayer(playerController.SteamID);
            }

            return HookResult.Continue;
        }


    }

}
