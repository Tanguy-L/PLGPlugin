
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin
    {
        // Enhanced OnPlayerConnectFull with detailed logging
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (Logger == null)
            {
                Console.WriteLine("[PLG] Logger is null in OnPlayerConnectFull");
                return HookResult.Continue;
            }

            CCSPlayerController? playerId = @event.Userid;

            // Enhanced validation with logging
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
                Logger.Error("PlayerManager is null in OnPlayerConnectFull");
                return HookResult.Continue;
            }

            if (_database == null)
            {
                Logger.Error("PlayerManager is null in OnPlayerConnectFull");
                return HookResult.Continue;
            }

            Logger.Info($"Player {playerId.PlayerName} (ID: {playerId.SteamID}) connected in OnPlayerConnectFull");

            ulong steamId = playerId.SteamID;

            _ = Task.Run(async () =>
            {
                PlayerFromDB? playerDB;
                try
                {
                    playerDB = await _database.GetPlayerById(steamId);
                    Console.WriteLine($"Player data retrieved for ${playerDB?.DiscordName}");
                }
                catch (Exception)
                {
                    throw;
                }

                if (playerDB != null)
                {
                    Server.NextFrame(() =>
                    {

                        PlgPlayer playerPLG = new(playerId)
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
                        _playerManager.UpdatePlayerWithData(playerId, playerDB);
                        Console.WriteLine($"Player data retrieved for {steamId}: {playerPLG.PlayerName}");
                        ReplyToUserCommand(playerId, $"Bienvenue dans le serveur PLG, {playerPLG.PlayerName} !");
                        ReplyToUserCommand(playerId, "Tapez .help pour voir la liste des commandes");
                        //TODO Check if the sound is hello or bangbang
                        _sounds?.PlaySound(playerId, "sounds/plg_sounds/hello.vsnd");
                    });
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
