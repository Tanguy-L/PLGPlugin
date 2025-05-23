using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

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
            RegisterEventHandler<EventCsWinPanelMatch>(WinPanelEventHandler);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
            RegisterEventHandler<EventPlayerTeam>(OnJoinTeam);

            // SoundsEvents
            RegisterEventHandler<EventPlayerDeath>(EventPlayerDeathHandler);
            RegisterEventHandler<EventBombPlanted>(EventBombPlantedHandler);
            RegisterEventHandler<EventBombExploded>(EventBombExplodedHandler);
        }

        public HookResult EventBombExplodedHandler(EventBombExploded @event, GameEventInfo info)
        {
            if (_sounds == null)
            {
                return HookResult.Continue;
            }
            Server.NextFrame(() =>
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/explode.vsnd");
            });
            return HookResult.Continue;
        }

        public HookResult EventBombPlantedHandler(EventBombPlanted @event, GameEventInfo info)
        {

            if (_sounds == null)
            {
                return HookResult.Continue;
            }
            Server.NextFrame(() =>
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/bombe.vsnd", 5000);

            });
            return HookResult.Continue;
        }

        public HookResult EventPlayerDeathHandler(EventPlayerDeath @event, GameEventInfo info)
        {
            var weapon = @event.Weapon;
            var attacker = @event?.Attacker;
            var victim = @event?.Userid;
            var isTeamKill = victim?.TeamNum == attacker?.TeamNum;
            var isBlind = @event?.Attackerblind;
            var ThruSmoke = @event?.Thrusmoke;
            var players = Utilities.GetPlayers();

            var ctCount = 0;
            var tCount = 0;

            if (_sounds == null)
            {
                return HookResult.Continue;
            }

            foreach (var player in players)
            {
                var team = player.Team;
                if (team == CsTeam.Terrorist)
                {
                    tCount++;
                }

                if (team == CsTeam.CounterTerrorist)
                {
                    ctCount++;
                }
            }

            if (ctCount == 1 && tCount == 1)
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/start.vsnd", 4000);
            }

            if (ctCount == 1 && tCount > 1 || tCount == 1 && ctCount > 1)
            {
                Random random = new Random();
                int value = random.Next(0, 3);
                if (value == 2)
                {
                    _sounds.PlayForAllPlayers("sounds/plg_sounds/1vX.vsnd", 4000);
                }
            }

            if (attacker == null || !attacker.IsValid || attacker.IsBot || attacker.IsHLTV) return HookResult.Continue;

            if (weapon == "knife" && _matchManager?.state != MatchManager.MatchState.Knife)
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/knife.vsnd", 4000);
            }

            if (weapon == "taser")
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/taser.vsnd");
            }

            if (weapon == "hegrenade")
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/he.vsnd");
            }

            if (weapon == "inferno")
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/moly.vsnd");
            }

            if (ThruSmoke == true)
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/smokekill.vsnd");
            }

            if (isTeamKill == true)
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/tk.vsnd", 5000);
            }


            return HookResult.Continue;
        }
        public HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            if (_matchManager != null && _playerManager != null && _teams != null)
            {
                var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

                foreach (var team in teams)
                {
                    var teamNumber = team.TeamNum;
                    // 2 = T, 3 = CT, 1 = Spectator, 0 = Unassigned
                    var side = teamNumber == 2 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                    var teamPLG = _teams.GetTeamBySide(side);
                    if (teamPLG == null)
                    {
                        Logger?.Error($"Team with side {side} not found");
                        return HookResult.Continue;
                    }
                    teamPLG.Score = team.Score;
                }

                var bestTeam = _teams.IdOfBestTeam();


                if (bestTeam != null)
                {
                    _matchManager.SetWinnerTeam(bestTeam.Value);
                }

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
            if (Logger == null)
            {
                return HookResult.Continue;
            }
            var winnerSide = (CsTeam)@event.Winner;
            if (_matchManager == null || _teams == null)
            {
                Logger.Info("OnRoundEnd: No match manager or no team manager");
                return HookResult.Continue;
            }
            if (_matchManager.state == MatchManager.MatchState.Knife)
            {
                _matchManager.DetermineTheKnifeWinner();

                var winnerKnife = _matchManager.GetKnifeTeamId();
                if (winnerKnife == null)
                {
                    Logger.Error("No knife winner");
                    return HookResult.Continue;
                }
                var teamWinnferKnife = _teams.GetTeamById(winnerKnife.Value);
                if (teamWinnferKnife != null)
                {
                    var nameTeam = teamWinnferKnife.Name;
                    BroadcastMessage($"L'équipe {nameTeam} a remporté le couteau et décide son side");
                    BroadcastMessage($".switch ou .stay");
                    _matchManager.state = MatchManager.MatchState.WaitingForSideChoice;
                    ExecWarmup();
                }
                else
                {
                    Logger.Error("No knife winner");
                }
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

        public HookResult OnJoinTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            if (Logger == null) return HookResult.Continue;

            var player = @event.Userid;
            // 2 = T, 3 = CT, 1 = Spectator, 0 = Unassigned
            var newTeamId = @event.Team;

            if (player == null || _playerManager == null || _teams == null || _matchManager == null)
            {
                Logger.Log("JoinTeam: No match manager or no player manager");
                return HookResult.Continue;
            }

            var plgPlayer = _playerManager.GetPlayer(player.SteamID);
            var teamNamePlayer = plgPlayer?.TeamName;

            if (teamNamePlayer == null)
            {
                Logger.Error("JoinTeam: No team name player");
            }
            else
            {
                if (_matchManager != null)
                {
                    var checkIfPlayerIsInTeam = _teams.isSomeTeamWithName(teamNamePlayer);
                    Logger.Info($"player {player} is in team {teamNamePlayer}");
                    if (!checkIfPlayerIsInTeam)
                    {
                        var side = newTeamId == 2 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                        Logger.Info($"side: {side}");
                        var team = _teams.GetTeamBySide(side);

                        if (team != null)
                        {
                            Logger.Info($"team: {team.Name}");
                            ReplyToUserCommand(player, $"[OPTIONNEL] Tu n'es pas dans une equipe,{ChatColors.Green} .join pour rejoindre l'équipe {team.Name} {ChatColors.Default}");
                        }
                    }
                }
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
                    ReplyToUserCommand(playerId, "Bienvenue dans le serveur PLG !");
                    ReplyToUserCommand(playerId, "Tapez .help pour voir la liste des commandes");
                }
            }
            if (_sounds != null)
            {
                _sounds.PlaySound(playerId, "sounds/plg_sounds/bangbang.vsnd");
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
