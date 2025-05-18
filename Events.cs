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
            if (_matchManager != null && _playerManager != null)
            {
                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
                var teamNames = _matchManager.GetTeamNames();
                if (teamNames == null)
                {
                    return HookResult.Continue;
                }

                foreach (var team in teams)
                {
                    var name = team.Teamname;
                    var teamManager = _matchManager.GetTeamByName(name);
                    teamManager.Score = team.Score;
                }

                _matchManager.SetWinnerTeam();

                var allPlayers = Utilities.GetPlayers();
                Dictionary<string, Dictionary<string, object>>? playersStatsOnly = new();

                foreach (var _player in allPlayers)
                {
                    var id = _player.SteamID;
                    var playerPlg = _playerManager.GetPlayer(id);
                    if (playerPlg != null)
                    {
                        if (_player != null && _player.ActionTrackingServices != null)
                        {
                            var playerStats = _player.ActionTrackingServices.MatchStats;

                            Dictionary<string, object> stats = new Dictionary<string, object>
                            {
                                { "PlayerName", _player.PlayerName },
                                { "Kills", playerStats.Kills },
                                { "Deaths", playerStats.Deaths },
                                { "Assists", playerStats.Assists },
                                { "Damage", playerStats.Damage },
                                { "Enemy2Ks", playerStats.Enemy2Ks },
                                { "Enemy3Ks", playerStats.Enemy3Ks },
                                { "Enemy4Ks", playerStats.Enemy4Ks },
                                { "Enemy5Ks", playerStats.Enemy5Ks },
                                { "EntryCount", playerStats.EntryCount },
                                { "EntryWins", playerStats.EntryWins },
                                { "1v1Count", playerStats.I1v1Count },
                                { "1v1Wins", playerStats.I1v1Wins },
                                { "1v2Count", playerStats.I1v2Count },
                                { "1v2Wins", playerStats.I1v2Wins },
                                { "UtilityCount", playerStats.Utility_Count },
                                { "UtilitySuccess", playerStats.Utility_Successes },
                                { "UtilityDamage", playerStats.UtilityDamage },
                                { "UtilityEnemies", playerStats.Utility_Enemies },
                                { "FlashCount", playerStats.Flash_Count },
                                { "FlashSuccess", playerStats.Flash_Successes },
                                { "HealthPointsRemovedTotal", playerStats.HealthPointsRemovedTotal },
                                { "HealthPointsDealtTotal", playerStats.HealthPointsDealtTotal },
                                { "ShotsFiredTotal", playerStats.ShotsFiredTotal },
                                { "ShotsOnTargetTotal", playerStats.ShotsOnTargetTotal },
                                { "EquipmentValue", playerStats.EquipmentValue },
                                { "MoneySaved", playerStats.MoneySaved },
                                { "KillReward", playerStats.KillReward },
                                { "LiveTime", playerStats.LiveTime },
                                { "HeadShotKills", playerStats.HeadShotKills },
                                { "CashEarned", playerStats.CashEarned },
                                { "EnemiesFlashed", playerStats.EnemiesFlashed }
                            };

                            if (playerPlg.MemberId != null)
                            {
                                playerPlg.Stats = stats;
                                _playerManager.AddOrUpdatePlayer(playerPlg);
                            }
                        }
                    }
                }

                Task.Run(async () =>
                {
                    await _matchManager.UpdateStatsMatch();
                });

                _matchManager.End();

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

            if (_matchManager != null && _playerManager != null && _matchManager.state == MatchManager.MatchState.Ended)
            {
                _matchManager = null;
            }

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
