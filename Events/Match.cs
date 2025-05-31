
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin
    {

        private HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
        {
            string map = Server.MapName;
            DateTime date = DateTime.Now;

            if (_matchManager != null && _playerManager != null && _matchManager.State == MatchManager.MatchState.Ended)
            {
                _matchManager = null;
            }

            return HookResult.Continue;
        }

        public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            if (Logger == null)
            {
                return HookResult.Continue;
            }
            if (_matchManager == null || _teams == null)
            {
                Logger.Info("OnRoundEnd: No match manager or no team manager");
                return HookResult.Continue;
            }

            if (_sounds != null && _sounds.is1vXAlreadyPlayed)
            {
                _sounds.is1vXAlreadyPlayed = false;
            }

            if (_matchManager.State == MatchManager.MatchState.Knife)
            {
                _matchManager.DetermineTheKnifeWinner();

                int? winnerKnife = _matchManager.GetKnifeTeamId();
                if (winnerKnife == null)
                {
                    Logger.Error("No knife winner");
                    return HookResult.Continue;
                }
                TeamPLG? teamWinnferKnife = _teams.GetTeamById(winnerKnife.Value);
                if (teamWinnferKnife != null)
                {
                    string nameTeam = teamWinnferKnife.Name;
                    BroadcastMessage($"L'équipe {nameTeam} a remporté le couteau et décide son side");
                    BroadcastMessage($".switch ou .stay");
                    _matchManager.State = MatchManager.MatchState.WaitingForSideChoice;
                    ExecWarmup();
                }
                else
                {
                    Logger.Error("No knife winner");
                }
            }

            bool isSwapRequired = IsTeamSwapRequired();
            if (isSwapRequired)
            {
                _teams.ReverseSide();
            }

            return HookResult.Continue;
        }


        public HookResult OnJoinTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            if (Logger == null)
            {
                return HookResult.Continue;
            }

            CCSPlayerController? player = @event.Userid;
            // 2 = T, 3 = CT, 1 = Spectator, 0 = Unassigned
            int newTeamId = @event.Team;

            if (player == null || !player.IsValid || player.IsBot)
            {
                return HookResult.Continue;
            }

            if (_database == null)
            {
                Logger.Warning("No DB");
                return HookResult.Continue;
            }

            if (_playerManager == null)
            {
                Logger.Error("No player caches");
                return HookResult.Continue;
            }

            if (_teams == null || _matchManager == null)
            {
                return HookResult.Continue;
            }

            PlgPlayer? plgPlayer = _playerManager.GetPlayer(player.SteamID);
            string? teamNamePlayer = plgPlayer?.TeamName;
            CsTeam side = newTeamId == 2 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
            TeamPLG? team = _teams.GetTeamBySide(side);

            if (team == null || plgPlayer == null)
            {
                return HookResult.Continue;
            }

            if (teamNamePlayer == null)
            {
                Logger.Error("JoinTeam: No team name player");
                ReplyToUserCommand(player, $"Tu n'as pas encore d'équipe, tu peux faire .join pour rejoindre l'équipe {team.Name}");
            }
            else
            {
                if (_matchManager != null)
                {
                    bool checkIfPlayerIsInTeam = _teams.IsSomeTeamWithName(teamNamePlayer);
                    if (!checkIfPlayerIsInTeam)
                    {
                        if (team != null)
                        {
                            ReplyToUserCommand(player, $"[OPTIONNEL] Pas d'équipe sur ce serveur,{ChatColors.Green} .join pour rejoindre l'équipe {team.Name} {ChatColors.Default}");
                            ReplyToUserCommand(player, $"Ton équipe {plgPlayer?.TeamName} est au serveur {plgPlayer?.TeamHostname}");
                        }
                    }
                }
            }
            return HookResult.Continue;
        }


        public HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
        {
            if (_matchManager == null || _playerManager == null || _teams == null)
            {
                return HookResult.Continue;
            }

            // ------------- ADD TEAM STATS MATCH ---------------
            IEnumerable<CCSTeam> teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
            if (_matchManager != null && _playerManager != null && _teams != null)
            {
                foreach (CCSTeam team in teams)
                {
                    if (team.TeamNum != 2 && team.TeamNum != 3)
                    {
                        continue;
                    }
                    byte teamNumber = team.TeamNum;
                    // 2 = T, 3 = CT, 1 = Spectator, 0 = Unassigned
                    CsTeam side = teamNumber == 2 ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                    TeamPLG? teamPLG = _teams.GetTeamBySide(side);

                    if (teamPLG == null)
                    {
                        Logger?.Error($"Team with side {side} not found");
                        return HookResult.Continue;
                    }
                    teamPLG.Score = team.Score;
                }
            }

            int? bestTeam = _teams?.IdOfBestTeam();


            if (bestTeam != null)
            {
                _matchManager?.SetWinnerTeam(bestTeam.Value);
            }

            // ------------ ADD PLAYERS STATS --------------
            List<CCSPlayerController> allPlayers = Utilities.GetPlayers();
            Dictionary<string, Dictionary<string, object>>? playersStatsOnly = [];

            foreach (CCSPlayerController _player in allPlayers)
            {
                if (!_player.IsValid || _player.IsBot)
                {
                    continue;
                }
                ulong id = _player.SteamID;
                PlgPlayer? playerPlg = _playerManager?.GetPlayer(id);
                if (playerPlg != null)
                {
                    if (_player != null && _player.ActionTrackingServices != null)
                    {
                        CSMatchStats_t playerStats = _player.ActionTrackingServices.MatchStats;

                        Dictionary<string, object> stats = new()
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
                        }
                    }
                }
            }
            if (_matchManager != null)
            {
                _ = Task.Run(_matchManager.UpdateStatsMatch);
            }


            Server.NextFrame(() =>
            {
                _matchManager.EndMatch();
            });


            return HookResult.Continue;
        }
    }
}
