using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
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

        // ----------- FLAGS ----------------
        // @css/reservation # Reserved slot access.
        // @css/generic # Generic admin.
        // @css/kick # Kick other players.
        // @css/ban # Ban other players.
        // @css/unban # Remove bans.
        // @css/vip # General vip status.
        // @css/slay # Slay/harm other players.
        // @css/changemap # Change the map or major gameplay features.
        // @css/cvar # Change most cvars.
        // @css/config # Execute config files.
        // @css/chat # Special chat privileges.
        // @css/vote # Start or create votes.
        // @css/password # Set a password on the server.
        // @css/rcon # Use RCON commands.
        // @css/cheats # Change sv_cheats or use cheating commands.
        // @css/root # Magically enables all flags and ignores immunity values.
        // ----------- FLAGS ----------------
        public bool CanYouDoThat(CCSPlayerController player, string flag)
        {
            return AdminManager.PlayerHasPermissions(player, flag);
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
            var relativePath = Path.Join(Config.CfgFolder + nameFile);
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

        public void RecordTheDemo()
        {
            string formattedTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string map = Server.MapName;
            string titleDemo = formattedTime + "_" + map + ".dem";
            string path = Server.GameDirectory + "/csgo/demos";
            string? directoryPath = Path.GetDirectoryName(path);
            if (directoryPath != null)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            Server.ExecuteCommand($"tv_record ./demos/{titleDemo}");
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

            if (CanYouDoThat(player, "@css/generic"))
            {
                ReplyToUserCommand(
                    player,
                    $"{ChatColors.Red}----[INFOS ADMINS]----{ChatColors.Default}"
                );
                ReplyToUserCommand(player, "Match : .start .warmup .knife .switch");
                ReplyToUserCommand(player, "Backups : .lbackups, .restore <filename>");
                ReplyToUserCommand(player, "Discord: .dgroup .dsplit");
                ReplyToUserCommand(player, "Pause : .pause .unpause");
                ReplyToUserCommand(player, "Players : .list");
                ReplyToUserCommand(player, "DB : .set_teams");
            }
            ReplyToUserCommand(player, $"{ChatColors.White}----[INFOS]----{ChatColors.Default}");
            ReplyToUserCommand(player, "smoke : .smoke <red>, .colors");
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

        private void HandleRestore(CCSPlayerController? player, string filename)
        {
            Server.ExecuteCommand($"mp_backup_restore_load_file {filename}");
        }

        async Task ExecuteCommandDiscord(List<string> stateCommand, CommandInfo? command)
        {
            var payload = $"{{\"content\": \"{stateCommand[0]}\" }}";
            using var client = new HttpClient();
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(Config.DiscordWebhook, content);
                Console.WriteLine(
                    response.IsSuccessStatusCode
                        ? "Message sent successfully !"
                        : $"Failed to send message. Status code: {response.StatusCode}"
                );
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
    }
}
