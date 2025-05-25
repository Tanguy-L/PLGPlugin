
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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
                Logger.Warning("Player ID is null in OnPlayerConnectFull");
                return HookResult.Continue;
            }

            if (!playerId.IsValid)
            {
                Logger.Warning($"Player {playerId.PlayerName} is not valid in OnPlayerConnectFull");
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
                return HookResult.Continue;
            }

            ulong steamId = playerId.SteamID;

            _ = Task.Run(async () =>
            {
                PlayerFromDB? playerDB;

                try
                {
                    playerDB = await _database.GetPlayerById(steamId);

                    // ---------------------
                    // GET OR CREATE PLAYER IN DB
                    if (playerDB == null)
                    {
                        await _database.CreatePlayerInDB(playerId.PlayerName, playerId.SteamID);
                        playerDB = await _database.GetPlayerById(steamId);
                        Console.WriteLine($"PLG: Create player in DB");
                    }

                    if (playerDB == null)
                    {
                        Console.WriteLine("PLG : No player on connection");
                        return;
                    }

                    Server.NextFrame(() =>
                    {
                        _playerManager.UpdatePlayerWithData(playerId, playerDB);
                        var playerPLG = _playerManager.GetPlayer(playerId.SteamID);

                        //TODO Check if the sound is hello or bangbang
                        _sounds?.PlaySound(playerId, "sounds/plg_sounds/hello.vsnd");

                        // --------------------
                        // HANDLE PLAYER WITH MATCH MANAGER
                        if (_teams != null && playerPLG != null && _matchManager != null)
                        {
                            string? teamName = playerPLG.TeamName;
                            if (teamName == null)
                            {
                                return;
                            }
                            var team = _teams.GetTeamByName(teamName);
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

                        ReplyToUserCommand(playerId, $"Bienvenue dans le serveur PLG {playerPLG?.PlayerName} !");
                        ReplyToUserCommand(playerId, "Tapez .help pour voir la liste des commandes");
                    });
                    Console.WriteLine($"Player data retrieved for ${playerDB?.DiscordName}");
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
