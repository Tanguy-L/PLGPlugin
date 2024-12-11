using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;

namespace PLGPlugin
{

    public sealed partial class PLGPlugin : BasePlugin
    {
        public void InitializeEvents()
        {
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        }

        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {

            var playerId = @event.Userid;
            var steamId = playerId?.SteamID;

            if (playerId != null && _playerManager != null)
            {
                Task.Run(async () => await _playerManager.AddPlgPlayer(playerId));
            }
            return HookResult.Continue;
        }
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var playerId = @event.Playerid;
            var playerController = Utilities.GetPlayerFromUserid(playerId);

            if (playerController != null && _playerManager != null)
            {
                _playerManager.RemovePlayer(playerController.SteamID);
            }

            return HookResult.Continue;
        }
    }
}

