using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin : BasePlugin
    {
        public void InitializeEvents()
        {
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawnedHandler);
        }

        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var playerId = @event.Userid;
            if (playerId != null && _playerManager != null)
            {
                if (_playerManager != null)
                {
                    Server.NextFrame(async () =>
                    {
                        await _playerManager.AddPlgPlayer(playerId);
                    });
                }
            }
            return HookResult.Continue;
        }

        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var playerController = @event.Userid;

            if (playerController != null && _playerManager != null)
            {
                _playerManager.RemovePlayer(playerController.SteamID);
            }

            return HookResult.Continue;
        }

        private void OnEntitySpawnedHandler(CEntityInstance entity)
        {
            if (entity.DesignerName != "smokegrenade_projectile" || _playerManager == null)
                return;

            var projectile = new CSmokeGrenadeProjectile(entity.Handle);
            var thrower = projectile.Thrower;

            Server.NextFrame(() =>
            {
                var playerSteamId = thrower.Value?.Controller?.Value?.SteamID;
                if (playerSteamId != null)
                {
                    var defaultColor = new Color(255, 255, 255);

                    var playerCurrent = _playerManager.GetPlayer((ulong)playerSteamId);
                    var smokePlayer = playerCurrent?.SmokeColor;

                    if (smokePlayer != null)
                    {
                        var smokeColorFind = SmokeColorPalette.GetColorByKey(smokePlayer);
                        if (smokeColorFind != null)
                        {
                            var smokeColor = smokeColorFind ?? defaultColor; // Use found color or default if null
                            projectile.SmokeColor.X = smokeColor.Red;
                            projectile.SmokeColor.Y = smokeColor.Green;
                            projectile.SmokeColor.Z = smokeColor.Blue;
                        }
                    }
                }
            });
        }
    }
}
