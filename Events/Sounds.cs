
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin
    {

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
            // ------ Players
            CCSPlayerController? attacker = @event?.Attacker;
            CCSPlayerController? victim = @event?.Userid;

            // ------ Informations
            string? weapon = @event?.Weapon.ToLowerInvariant();
            bool isTeamKill = victim?.TeamNum == attacker?.TeamNum;
            bool? isBlind = @event?.Attackerblind;
            bool? ThruSmoke = @event?.Thrusmoke;
            List<CCSPlayerController> players = Utilities.GetPlayers();

            int ctCount = 0;
            int tCount = 1;

            if (_sounds == null)
            {
                return HookResult.Continue;
            }

            foreach (CCSPlayerController player in players)
            {
                CsTeam team = player.Team;
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

            if ((ctCount == 1 && tCount > 1) || (tCount == 1 && ctCount > 1))
            {
                Random random = new();
                int value = random.Next(0, 3);
                if (value == 2)
                {
                    _sounds.PlayForAllPlayers("sounds/plg_sounds/1vX.vsnd", 4000);
                }
            }

            if (attacker == null || !attacker.IsValid || attacker.IsBot || attacker.IsHLTV)
            {
                return HookResult.Continue;
            }

            if (weapon == "knife" && _matchManager?.State != MatchManager.MatchState.Knife)
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

            if (isTeamKill)
            {
                _sounds.PlayForAllPlayers("sounds/plg_sounds/tk.vsnd", 5000);
            }


            return HookResult.Continue;
        }
    }

}
