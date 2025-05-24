
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

            Logger.Info($"Player {playerId.PlayerName} (ID: {playerId.SteamID}) connected in OnPlayerConnectFull");

            _ = Task.Run(async () =>
            {
                await _playerManager.AddPlgPlayer(playerId);
            });

            // Background player processing with detailed logging
            // _ = Task.Run(async () =>
            // {
            //     try
            //     {
            //         await _playerManager.AddPlgPlayer(playerId);
            //
            //         // Verify the player was actually added
            //         PlgPlayer? addedPlayer = _playerManager.GetPlayer(playerId.SteamID);
            //     }
            //     catch (Exception ex)
            //     {
            //         Console.WriteLine($"Error adding player {playerId.SteamID} to PLG: {ex.Message}");
            //     }
            // });

            return HookResult.Continue;
        }
        // public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        // {
        //     if (Logger == null)
        //     {
        //         return HookResult.Continue;
        //     }
        //     CCSPlayerController? playerId = @event.Userid;
        //     if (playerId != null && _playerManager != null && playerId.IsValid)
        //     {
        //         _ = Task.Run(async () =>
        //         {
        //             try
        //             {
        //                 await _playerManager.AddPlgPlayer(playerId);
        //             }
        //             catch (Exception ex)
        //             {
        //                 Logger.Error($"Error adding player {playerId.SteamID} to PLG: {ex.Message}");
        //             }
        //         });
        //         Server.NextFrame(() =>
        //         {
        //             ReplyToUserCommand(playerId, "Bienvenue dans le serveur PLG !");
        //             ReplyToUserCommand(playerId, "Tapez .help pour voir la liste des commandes");
        //             //TODO Check if the sound is hello or bangbang
        //             _sounds?.PlaySound(playerId, "sounds/plg_sounds/hello.vsnd");
        //         });
        //     }
        //     return HookResult.Continue;
        // }

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
