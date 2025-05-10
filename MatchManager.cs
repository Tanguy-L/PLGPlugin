using CounterStrikeSharp.API;
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
        private readonly PlayerManager _playerManager;
        private readonly Database _database;
        private readonly string _webhook;
        private readonly ILogger<MatchManager> _logger;
        private readonly BackupManager _backup;
        private readonly string _pathConfig;
        private string? _mapName;
        private string? _matchId;
        private int? _knifeWinner;
        private List<TeamManager>? _teams;
        // 0 = Terrorist and 1 = CT
        private List<bool>? _teamsReady;
        public MatchState state { get; set; }

        public MatchManager(Database database, PlayerManager playerManager, PlgConfig config, BackupManager backup)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _pathConfig = config.CfgFolder;
            _logger = loggerFactory.CreateLogger<MatchManager>();
            _playerManager = playerManager;
            _database = database;
            _webhook = config.DiscordWebhook;
            _backup = backup;
            state = MatchState.None;
        }

        public void InitSetupMatch()
        {
            state = MatchState.Setup;
            _teamsReady = [false, false];
        }

        public void SetTeamReady(int index, bool value)
        {
            if (_teamsReady == null)
            {
                return;
            }
            _teamsReady[index] = value;
        }

        public void DetermineTheKnifeWinner(CsTeam sideOfTeam)
        {
            var winner = _teams?.FirstOrDefault(t => t.GetSide() == sideOfTeam);
            var id = winner?.GetId();
            if (id == null)
            {
                _logger.LogError("Winner is null");
                return;
            }
            _knifeWinner = id;
        }

        public bool IsAllTeamReady()
        {
            return _teamsReady != null && _teamsReady[0] && _teamsReady[1];
        }

        public async Task NewMatch(string hostname, string mapName)
        {
            var matchId = await _database.NewMatch(mapName);
            var allTeamPLG = await _database.GetTeamsByHostname(hostname);
            _matchId = matchId;
            _mapName = mapName;
            _teams = allTeamPLG.Select(t => new TeamManager(t)).ToList();
        }


        private void ExecCfg(string nameFile)
        {
            var relativePath = Path.Join(_pathConfig + nameFile);
            Server.ExecuteCommand($"exec {relativePath}");
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


        public void StartTheMatch()
        {
            StartKnife();
        }
    }

}
