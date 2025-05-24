using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin
    {

        private void OnEntitySpawnedHandler(CEntityInstance entity)
        {
            if (entity.DesignerName != "smokegrenade_projectile" || _playerManager == null)
            {
                return;
            }

            CSmokeGrenadeProjectile projectile = new(entity.Handle);
            CHandle<CCSPlayerPawn> thrower = projectile.Thrower;

            Server.NextFrame(() =>
            {
                ulong? playerSteamId = thrower.Value?.Controller?.Value?.SteamID;
                if (playerSteamId != null)
                {
                    Color defaultColor = new(255, 255, 255);

                    PlgPlayer? playerCurrent = _playerManager.GetPlayer((ulong)playerSteamId);
                    string? smokePlayer = playerCurrent?.SmokeColor;

                    if (smokePlayer != null)
                    {
                        Color smokeColorFind = SmokeColorPalette.GetColorByKey(smokePlayer);
                        if (smokeColorFind != null)
                        {
                            Color smokeColor = smokeColorFind ?? defaultColor; // Use found color or default if null
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
