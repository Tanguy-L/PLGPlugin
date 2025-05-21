using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace PLGPlugin
{
    public class MatchManager
    {
        public enum MatchState
        {
            None,
            Setup,
            Knife,
            WaitingForSideChoice,
            Live,
            Paused,
            Ended
        }

        // duplicate keys from PLGPlugin instance
        // Because BroadcastMessage was not accessible
        private static readonly string ChatPrefix =
            $"[{ChatColors.Blue}P{ChatColors.Yellow}L{ChatColors.Red}G{ChatColors.Default}]";
        private static readonly string AdminChatPrefix =
            $"[{ChatColors.Red}ADMIN{ChatColors.Default}]";
        private readonly PlayerManager _playerManager;
        private readonly Database _database;
        private readonly string _webhook;
        private readonly ILogger<MatchManager> _logger;
        private readonly BackupManager _backup;
        private readonly string _pathConfig;
        private string? _mapName;
        private string? _matchId;
        private int? _teamWinner;
        private int? _knifeWinner;
        private int? _idTeam1;
        private int? _idTeam2;
        private TeamManager? _teamManager;
        // 0 = Terrorist and 1 = CT
        private List<bool>? _teamsReady;
        public MatchState state { get; set; }

        public MatchManager(Database database, PlayerManager playerManager, PlgConfig config, BackupManager backup, TeamManager teamManager)
        {

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<MatchManager>();

            _logger.LogInformation("MatchManager created");
            if (_teamManager != null)
            {
                _idTeam1 = teamManager?.GetTeamByIndex(0)?.Id;
                _idTeam2 = teamManager?.GetTeamByIndex(1)?.Id;
                _logger.LogInformation("team Handled !");
            }
            _teamManager = teamManager;
            _pathConfig = config.CfgFolder;
            _playerManager = playerManager;
            _database = database;
            _webhook = config.DiscordWebhook;
            _backup = backup;
            state = MatchState.None;
        }

        public void logAll()
        {
            Console.WriteLine($"Log State: {state}");
            Console.WriteLine($"Log Knife winner: {_knifeWinner}");
            Console.WriteLine($"Log Map: {_mapName}");
        }

        public void InitSetupMatch(string hostname)
        {
            state = MatchState.Setup;
            _teamsReady = [false, false];
            ExecWarmup();
        }

        public void SetWinnerTeam(int id)
        {
            _teamWinner = id;
        }

        public void End()
        {
            Server.ExecuteCommand($"tv_stoprecord");
            state = MatchManager.MatchState.Ended;
            // ExecWarmup();
        }

        public void SetTeamReadyBySide(CsTeam side, bool value)
        {
            int index = -1;

            if (side == CsTeam.CounterTerrorist)
            {
                index = 1;
            }
            if (side == CsTeam.Terrorist)
            {
                index = 0;
            }
            if (index == -1)
            {

            }

            // _teamsReady is an [false, false]
            // But it can be null
            if (_teamsReady == null)
            {
                return;
            }
            _teamsReady[index] = value;
        }

        private void StartTvRecord()
        {
            if (_teamManager == null)
            {
                PLGPlugin.Instance.Logger?.Error("TeamManager is null");
                return;
            }
            DateTime date = DateTime.Now;
            var map = Server.MapName;
            string dateFormatted = date.ToString("dd/MM/yyyy");
            var T = _teamManager.GetTeamBySide(CsTeam.Terrorist);
            var CT = _teamManager.GetTeamBySide(CsTeam.CounterTerrorist);
            if (T == null || CT == null)
            {
                PLGPlugin.Instance.Logger?.Error("T or CT is null");
                return;
            }
            var title = _matchId + "_" + T.Name + "_" + CT.Name + "_" + map + ".dem";

            try
            {
                string? directoryPath = Path.GetDirectoryName(Path.Join(Server.GameDirectory + "/csgo/", "/demos"));
                if (directoryPath != null)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
                string demoPath = "/demos/" + title;
                _logger.LogInformation($"[StartDemoRecoding] Starting demo recording, path: {demoPath}");
                Server.ExecuteCommand($"tv_record {demoPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[StartDemoRecording - FATAL] Error: {ex.Message}.");
            }
        }

        private (int alivePlayers, int totalHealth) GetAlivePlayers(int team)
        {
            int count = 0;
            int totalHealth = 0;
            var allPlayers = Utilities.GetPlayers();
            foreach (var player in allPlayers)
            {
                if (player.IsValid)
                {
                    if (player.TeamNum == team)
                    {
                        if (player.PlayerPawn.Value!.Health > 0) count++;
                        totalHealth += player.PlayerPawn.Value!.Health;
                    }

                }
            }
            return (count, totalHealth);
        }

        private void SetPlayersInTeams()
        {
            Console.WriteLine("INFO Set teams !");

            if (_playerManager == null)
            {
                return;
            }

            var players = Utilities.GetPlayers();

            foreach (var playerController in players)
            {
                var plgPlayer = _playerManager.GetPlayer(playerController.SteamID);
                if (plgPlayer == null)
                {
                    return;
                }
                var sideInDb = plgPlayer.Side;
                var sideInGame = playerController.Team;

                if (sideInDb == null)
                {
                    return;
                }

                if (!Enum.TryParse<CsTeam>(sideInDb, out CsTeam sideInDbParsed))
                {
                    Console.WriteLine($"Could not parse team value: {sideInDb}");
                    return;
                }

                if (sideInGame != sideInDbParsed)
                {
                    playerController.SwitchTeam(sideInDbParsed);
                    playerController.CommitSuicide(false, true);
                }
            }
        }

        public void DetermineTheKnifeWinner()
        {
            if (_teamManager == null)
            {
                PLGPlugin.Instance.Logger?.Error("TeamManager is null");
                return;
            }
            CsTeam sideWinner = 0;

            (int tAlive, int tHealth) = GetAlivePlayers(2);
            (int ctAlive, int ctHealth) = GetAlivePlayers(3);
            if (ctAlive > tAlive)
            {
                sideWinner = CsTeam.CounterTerrorist;
            }
            else if (tAlive > ctAlive)
            {
                sideWinner = CsTeam.Terrorist;
            }
            else if (ctHealth > tHealth)
            {
                sideWinner = CsTeam.CounterTerrorist;
            }
            else if (tHealth > ctHealth)
            {
                sideWinner = CsTeam.Terrorist;
            }
            else
            {
                sideWinner = CsTeam.CounterTerrorist;
            }

            var winner = _teamManager.GetTeamBySide(sideWinner);
            if (winner == null)
            {
                PLGPlugin.Instance.Logger?.Error("No knife winner");
                return;
            }
            _knifeWinner = winner.Id;
            state = MatchState.WaitingForSideChoice;
            ExecWarmup();
        }

        public int? GetKnifeTeamId()
        {
            return _knifeWinner;
        }

        public bool IsAllTeamReady()
        {
            return _teamsReady != null && _teamsReady[0] && _teamsReady[1];
        }

        public async Task RunMatch()
        {
            var hostnameValue = ConVar.Find("hostname");
            if (hostnameValue == null || hostnameValue.StringValue == null)
            {
                _logger.LogError("EROR: hostname not found");
                return;
            }
            if (_teamManager == null)
            {
                _logger.LogError("EROR: teamManager not found");
                return;
            }
            if (_idTeam1 == null || _idTeam2 == null)
            {
                _logger.LogError("EROR: idTeam1 or idTeam2 not found");
                return;
            }
            _logger.LogInformation($"Hostname: {hostnameValue.StringValue}");

            var hostname = hostnameValue.StringValue;
            var mapName = Server.MapName;

            await Task.Run(async () =>
            {

                _mapName = mapName;
                var matchId = await _database.NewMatch(mapName, _idTeam1.Value, _idTeam2.Value);
                _matchId = matchId;
                Server.NextFrame(() =>
                {
                    SetPlayersInTeams();
                    var team1Name = _teamManager.GetTeamById(_idTeam1.Value)?.Name;
                    var team2Name = _teamManager.GetTeamById(_idTeam2.Value)?.Name;
                    Server.ExecuteCommand($"mp_teamname_1 {team1Name}");
                    Server.ExecuteCommand($"mp_teamname_2 {team2Name}");

                    StartTvRecord();
                    StartKnife();

                    Console.WriteLine($"Le match démarre sur {mapName} et avec les équipes {team1Name} et {team2Name}");
                    Console.WriteLine($"Place à la boucherie");
                });

            });
        }


        public void BroadcastMessage(string message)
        {
            Server.PrintToChatAll($"{ChatPrefix} {message}");
        }

        public void GoGoGo()
        {

            if (state == MatchManager.MatchState.WaitingForSideChoice)
            {
                state = MatchManager.MatchState.Live;
                StartLive();
            }
            else
            {
                _logger.LogError("EROR: state not waiting for side choice");
            }
        }

        // to refactor
        public async Task UpdateStatsMatch()
        {
            if (_teamManager != null && _matchId != null && _teamManager != null && _idTeam1 != null && _idTeam2 != null)
            {
                var team1 = _teamManager.GetTeamById(_idTeam1.Value);
                var team2 = _teamManager.GetTeamById(_idTeam2.Value);
                if (team1 == null || team2 == null)
                {
                    _logger.LogError("EROR: team not found");
                    return;
                }
                await _database.UpdateMatchStats(_matchId, team1, team2);
                await _database.UpdatePlayersStats(_playerManager, _matchId);
            }
        }

        private void ExecCfg(string nameFile)
        {
            var relativePath = Path.Join(_pathConfig + nameFile);
            var command = $"exec {relativePath}";
            _logger.LogInformation($"Exec: {command}");
            Server.ExecuteCommand(command);
        }

        private void ExecWarmup()
        {
            ExecCfg("warmup.cfg");
        }

        private void StartKnife()
        {
            state = MatchState.Knife;

            ExecCfg("knife.cfg");
        }

        private void StartLive()
        {
            ExecCfg("match.cfg");
        }
    }

}
