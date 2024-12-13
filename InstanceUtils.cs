using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public sealed partial class PLGPlugin : BasePlugin
    {
        public async Task HandleUpdateSmoke(CCSPlayerController playerController, string commandArg)
        {
            if (_database == null || _playerManager == null)
            {
                return;
            }

            try
            {
                var steamId = playerController.SteamID;
                await Task.Run(async () =>
                {
                    await _database.SetSmoke(steamId, commandArg);
                    var playerData = await _database.GetPlayerById(steamId);
                    await Server.NextFrameAsync(() =>
                    {
                        if (playerData != null)
                        {
                            _playerManager.UpdatePlayerWithData(playerController, playerData);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandlePlayerSetup: {ex.Message}");
            }
        }

        private static readonly string ChatPrefix =
            $"[{ChatColors.Blue}P{ChatColors.Yellow}L{ChatColors.Red}G{ChatColors.Default}]";
        private static readonly string AdminChatPrefix =
            $"[{ChatColors.Red}ADMIN{ChatColors.Default}]";

        public static void BroadcastMessage(string message)
        {
            Server.PrintToChatAll($"{ChatPrefix} {message}");
        }

        public static void Log(string message)
        {
            Console.WriteLine($"[MATCH CRAFT PLG] {message}");
        }

        public static void ReplyToUserCommand(CCSPlayerController? player, string message)
        {
            {
                player?.PrintToChat($"{ChatPrefix} {message}");
            }
        }

        public static bool IsPlayerValid(CCSPlayerController? player)
        {
            return player != null
                && player.IsValid
                && !player.IsBot
                && player.Pawn != null
                && player.Pawn.IsValid
                && player.Connected == PlayerConnectedState.PlayerConnected
                && !player.IsHLTV;
        }

        public void ExecCfg(string nameFile)
        {
            var relativePath = Path.Join("PLG/" + nameFile);
            Server.ExecuteCommand($"exec {relativePath}");
        }

        public void ExecWarmup()
        {
            ExecCfg("warmup.cfg");
        }

        public void StartKnife()
        {
            ExecCfg("knife.cfg");
        }

        public void StartLive()
        {
            ExecCfg("match.cfg");
        }

        private void PauseMatch(CCSPlayerController? player, CommandInfo? command)
        {
            Server.ExecuteCommand("mp_pause_match");
        }

        private void UnPauseMatch(CCSPlayerController? player, CommandInfo? command)
        {
            Server.ExecuteCommand("mp_unpause_match");
        }

        private void SendAvailableCommandsMessage(CCSPlayerController? player)
        {
            if (player == null)
                return;
            ReplyToUserCommand(player, $"{ChatColors.Red}----[INFOS]----{ChatColors.Default}");
            ReplyToUserCommand(player, "Match : .start .warmup .knife .switch");
            ReplyToUserCommand(player, "Discord: .dgroup .dsplit");
            ReplyToUserCommand(player, "Pause : .pause .unpause");
            ReplyToUserCommand(player, "DB : .set_teams");
        }

        private void HandleMapChangeCommand(CCSPlayerController? player, string mapName)
        {
            if (!long.TryParse(mapName, out _) && !mapName.Contains('_'))
            {
                mapName = "de_" + mapName;
            }

            if (long.TryParse(mapName, out _))
            { // Check if mapName is a long for workshop map ids
                Server.ExecuteCommand($"bot_kick");
                Server.ExecuteCommand($"host_workshop_map \"{mapName}\"");
            }
            else if (Server.IsMapValid(mapName))
            {
                Server.ExecuteCommand($"bot_kick");
                Server.ExecuteCommand($"changelevel \"{mapName}\"");
            }
        }
    }
}
