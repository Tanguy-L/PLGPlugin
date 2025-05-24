using CounterStrikeSharp.API;
using PLGPlugin.Interfaces;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    //TODO add ThrowIfDisposed in methods
    public class MatchManager : IMatchManager
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

        // ------------
        // Interfaces
        private readonly IDatabase _database;
        private readonly IPlayerManager _playerManager;
        private readonly ITeamManager _teamManager;
        private readonly ILoggingService _logger;
        private readonly PlgConfig _config;
        private readonly BackupManager _backup;

        // duplicate keys from PLGPlugin instance
        // Because BroadcastMessage was not accessible
        // private static readonly string ChatPrefix =
        //     $"[{ChatColors.Blue}P{ChatColors.Yellow}L{ChatColors.Red}G{ChatColors.Default}]";
        // private static readonly string AdminChatPrefix =
        //     $"[{ChatColors.Red}ADMIN{ChatColors.Default}]";

        public MatchState State { get; set; }
        private List<bool>? _teamsReady;
        private readonly string _pathConfig;

        private string? _mapName;
        private string? _matchId;

        private int? _teamWinner;
        private int? _knifeWinner;
        private int? _idTeam1;
        private int? _idTeam2;
        private bool _disposed;
        // private TeamManager? _teamManager;
        // 0 = Terrorist and 1 = CT

        public MatchManager(
            IDatabase database,
            IPlayerManager playerManager,
            PlgConfig config,
            BackupManager backup,
            ITeamManager teamManager,
            ILoggingService logger
        )
        {
            // ---------------
            // INIT DEPENDENCY INJECTIONS
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _teamManager = teamManager ?? throw new ArgumentNullException(nameof(teamManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _backup = backup ?? throw new ArgumentNullException(nameof(backup));
            _pathConfig = config.CfgFolder;


            _logger.Info("MatchManager created");

            InitializeTeamIds();
            State = MatchState.Setup;
        }

        private void InitializeTeamIds()
        {
            _idTeam1 = _teamManager.GetTeamByIndex(0)?.Id;
            _idTeam2 = _teamManager.GetTeamByIndex(1)?.Id;

            if (_idTeam1.HasValue && _idTeam2.HasValue)
            {
                _logger.Info($"Teams initialized: Team1 ID={_idTeam1}, Team2 ID={_idTeam2}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed)
            {
                return;
            }

            throw new ObjectDisposedException(nameof(MatchManager));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _teamsReady?.Clear();
                _logger?.Info("MatchManager disposed");
                _disposed = true;
            }
        }

        public void InitSetupMatch(string hostname)
        {
            State = MatchState.Setup;
            _teamsReady = [false, false];
            ExecWarmup();
        }

        public void SetWinnerTeam(int id)
        {
            _teamWinner = id;
        }

        public void EndMatch()
        {
            Server.ExecuteCommand($"tv_stoprecord");
            State = MatchState.Ended;
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
            string map = Server.MapName;
            TeamPLG? T = _teamManager.GetTeamBySide(CsTeam.Terrorist);
            TeamPLG? CT = _teamManager.GetTeamBySide(CsTeam.CounterTerrorist);
            if (T == null || CT == null)
            {
                PLGPlugin.Instance.Logger?.Error("T or CT is null");
                return;
            }
            string title = _matchId + "_" + T.Name + "_" + CT.Name + "_" + map + ".dem";

            try
            {
                string? directoryPath = Path.GetDirectoryName(Path.Join(Server.GameDirectory + "/csgo/", "/demos"));
                if (directoryPath != null)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        _ = Directory.CreateDirectory(directoryPath);
                    }
                }
                string demoPath = "/demos/" + title;
                _logger.Info($"[StartDemoRecoding] Starting demo recording, path: {demoPath}");
                Server.ExecuteCommand($"tv_record {demoPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[StartDemoRecording - FATAL] Error: {ex.Message}.");
            }
        }

        private (int alivePlayers, int totalHealth) GetAlivePlayers(int team)
        {
            int count = 0;
            int totalHealth = 0;
            List<CounterStrikeSharp.API.Core.CCSPlayerController> allPlayers = Utilities.GetPlayers();
            foreach (CounterStrikeSharp.API.Core.CCSPlayerController player in allPlayers)
            {
                if (player.IsValid)
                {
                    if (player.TeamNum == team)
                    {
                        if (player.PlayerPawn.Value!.Health > 0)
                        {
                            count++;
                        }

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

            List<CounterStrikeSharp.API.Core.CCSPlayerController> players = Utilities.GetPlayers();

            foreach (CounterStrikeSharp.API.Core.CCSPlayerController playerController in players)
            {
                PlgPlayer? plgPlayer = _playerManager.GetPlayer(playerController.SteamID);
                if (plgPlayer == null)
                {
                    return;
                }
                string? sideInDb = plgPlayer.Side;
                CsTeam sideInGame = playerController.Team;

                if (sideInDb == null)
                {
                    return;
                }

                if (!Enum.TryParse(sideInDb, out CsTeam sideInDbParsed))
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
            CsTeam sideWinner;

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
            else
            {
                sideWinner = ctHealth > tHealth ? CsTeam.CounterTerrorist : tHealth > ctHealth ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
            }

            TeamPLG? winner = _teamManager.GetTeamBySide(sideWinner);
            if (winner == null)
            {
                PLGPlugin.Instance.Logger?.Error("No knife winner");
                return;
            }
            _knifeWinner = winner.Id;
            State = MatchState.WaitingForSideChoice;
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
            ConVar? hostnameValue = ConVar.Find("hostname");
            if (hostnameValue == null || hostnameValue.StringValue == null)
            {
                _logger.Error("EROR: hostname not found");
                return;
            }
            if (_teamManager == null)
            {
                _logger.Error("EROR: teamManager not found");
                return;
            }
            if (_idTeam1 == null || _idTeam2 == null)
            {
                _logger.Error("EROR: idTeam1 or idTeam2 not found");
                return;
            }
            _logger.Info($"Hostname: {hostnameValue.StringValue}");

            string hostname = hostnameValue.StringValue;
            string mapName = Server.MapName;

            await Task.Run(async () =>
            {

                _mapName = mapName;
                string matchId = await _database.NewMatch(mapName, _idTeam1.Value, _idTeam2.Value);
                _matchId = matchId;
                Server.NextFrame(() =>
                {
                    SetPlayersInTeams();
                    string? team1Name = _teamManager.GetTeamById(_idTeam1.Value)?.Name;
                    string? team2Name = _teamManager.GetTeamById(_idTeam2.Value)?.Name;
                    Server.ExecuteCommand($"mp_teamname_1 {team1Name}");
                    Server.ExecuteCommand($"mp_teamname_2 {team2Name}");

                    StartTvRecord();
                    StartKnife();

                    Console.WriteLine($"Le match démarre sur {mapName} et avec les équipes {team1Name} et {team2Name}");
                    Console.WriteLine($"Place à la boucherie");
                });

            });
        }

        public void GoGoGo()
        {

            if (State == MatchState.WaitingForSideChoice)
            {
                State = MatchState.Live;
                StartLive();
            }
            else
            {
                _logger.Error("EROR: state not waiting for side choice");
            }
        }

        // to refactor
        public async Task UpdateStatsMatch()
        {
            if (_teamManager != null && _matchId != null && _teamManager != null && _idTeam1 != null && _idTeam2 != null)
            {
                TeamPLG? team1 = _teamManager.GetTeamById(_idTeam1.Value);
                TeamPLG? team2 = _teamManager.GetTeamById(_idTeam2.Value);
                if (team1 == null || team2 == null)
                {
                    _logger.Error("EROR: team not found");
                    return;
                }
                await _database.UpdateMatchStats(_matchId, team1, team2);
                // await _database.UpdatePlayersStats(_playerManager, _matchId);
            }
        }

        private void ExecCfg(string nameFile)
        {
            string relativePath = Path.Join(_pathConfig + nameFile);
            string command = $"exec {relativePath}";
            _logger.Info($"Exec: {command}");
            Server.ExecuteCommand(command);
        }

        private void ExecWarmup()
        {
            ExecCfg("warmup.cfg");
        }

        private void StartKnife()
        {
            State = MatchState.Knife;

            ExecCfg("knife.cfg");
        }

        private void StartLive()
        {
            ExecCfg("match.cfg");
        }
    }

}
