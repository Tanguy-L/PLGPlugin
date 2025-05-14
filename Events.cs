using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin : BasePlugin
    {
        public void InitializeEvents()
        {
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawnedHandler);
            RegisterEventHandler<EventRoundPoststart>(OnRoundPostStart);
            RegisterEventHandler<EventCsWinPanelMatch>(WinPanelEventHandler);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
        }

        public HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            if (_matchManager != null)
            {
                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
                var teamNames = _matchManager.GetTeamNames();
                if (teamNames == null)
                {
                    return HookResult.Continue;
                }

                foreach (var team in teams)
                {
                    var score = team.Score;
                    var teamName = team.Teamname;
                    teamName = teamNames.FirstOrDefault(x => x == teamName);






                }

                // TODO HANDLE THE MATCH END
                // _matchManager.state = MatchManager.MatchState.End;
                // var winnerTeam = (CsTeam)@event)
            }
            return HookResult.Continue;
        }

        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            var winnerSide = (CsTeam)@event.Winner;
            if (_matchManager == null)
            {
                Console.WriteLine("EROR: MatchManager is null");
                return HookResult.Continue;
            }
            if (_matchManager.state == MatchManager.MatchState.Knife)
            {
                _matchManager.DetermineTheKnifeWinner(winnerSide);
            }

            return HookResult.Continue;
        }

        private HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
        {
            var map = Server.MapName;
            var date = DateTime.Now;

            // TODO Restore the backup with match id !

            return HookResult.Continue;
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

        public HookResult WinPanelEventHandler(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            Server.ExecuteCommand("tv_stoprecord;");
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
