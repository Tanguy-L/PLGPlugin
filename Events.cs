using CounterStrikeSharp.API.Core;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin : BasePlugin
    {
        public void InitializeEvents()
        {
            // Players
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

            // For Smokes
            RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawnedHandler);

            // For Matche Handling
            RegisterEventHandler<EventRoundPoststart>(OnRoundPostStart);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
            RegisterEventHandler<EventPlayerTeam>(OnJoinTeam);

            // SoundsEvents
            RegisterEventHandler<EventPlayerDeath>(EventPlayerDeathHandler);
            RegisterEventHandler<EventBombPlanted>(EventBombPlantedHandler);
            RegisterEventHandler<EventBombExploded>(EventBombExplodedHandler);
        }
    }
}
