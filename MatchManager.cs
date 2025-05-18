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
        private List<TeamManager>? _teams;
        // 0 = Terrorist and 1 = CT
        private List<bool>? _teamsReady;
        public MatchState state { get; set; }

        public MatchManager(Database database, PlayerManager playerManager, PlgConfig config, BackupManager backup)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<MatchManager>();
            _pathConfig = config.CfgFolder;
            _playerManager = playerManager;
            _database = database;
            _webhook = config.DiscordWebhook;
            _backup = backup;
            state = MatchState.None;
        }

        public void logAll()
        {

            if (_teams != null)
            {

                BroadcastMessage("Teams:");

                foreach (var team in _teams)
                {
                    var id = team.GetId();
                    var name = team.GetName();
                    BroadcastMessage($"id --- {id} name --- {name}");
                }
            }

            BroadcastMessage($"State: {state}");
            BroadcastMessage($"Knife winner: {_knifeWinner}");
            BroadcastMessage($"Map: {_mapName}");
            if (_teamsReady != null)
            {
                var ready = "";
                foreach (var isReady in _teamsReady)
                {
                    ready += isReady + " ";
                }
                BroadcastMessage($"ready: {ready}");
            }

        }

        public TeamManager GetTeamByName(string name)
        {
            return _teams?.FirstOrDefault(t => t.GetName() == name) ?? throw new Exception($"Team {name} not found");
        }

        public void InitSetupMatch()
        {
            state = MatchState.Setup;
            _teamsReady = [false, false];
        }

        public void SetWinnerTeam()
        {
            if (_teams == null)
            {
                return;
            }
            var score1 = _teams[0].Score;
            var score2 = _teams[1].Score;

            if (score1 > score2)
            {
                _teamWinner = 0;
            }
            else if (score2 > score1)
            {
                _teamWinner = 1;
            }
        }

        public TeamManager? GetWinnerTeam()
        {
            return _teams?.FirstOrDefault(t => t.GetId() == _teamWinner);
        }


        public void ReverseTeamSides()
        {
            if (_teams == null)
            {
                return;
            }
            foreach (var team in _teams)
            {
                team.ReverseSide();
            }
        }

        public List<string>? GetTeamNames()
        {
            if (_teams == null)
            {
                return null;

            }

            return _teams.Select(t => t.GetName()).ToList();
        }

        public void End()
        {
            Server.ExecuteCommand($"tv_stoprecord");
            state = MatchManager.MatchState.Ended;

            BroadcastMessage("Match terminé");

            // ExecWarmup();
        }

        public void SetTeamReady(CsTeam side, bool value)
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

        public TeamManager? TryGetTeamBySide(CsTeam side)
        {
            var teamFound = _teams?.FirstOrDefault(t => t.GetSide() == side);
            return teamFound;
        }

        private void StartTvRecord()
        {
            DateTime date = DateTime.Now;
            var map = Server.MapName;
            string dateFormatted = date.ToString("dd/MM/yyyy");
            var title = _matchId + "_" + date + "_" + map + ".dem";

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

        public void DetermineTheKnifeWinner(CsTeam sideOfTeam)
        {
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

            var winner = _teams?.FirstOrDefault(t => t.GetSide() == sideWinner);
            var id = winner?.GetId();
            _knifeWinner = id;

            state = MatchState.WaitingForSideChoice;
            ExecWarmup();
        }

        public bool IsAllTeamReady()
        {
            return _teamsReady != null && _teamsReady[0] && _teamsReady[1];
        }

        private async Task<string> CreateTheMatchInDB(string mapName)
        {
            var team1 = _teams?.FirstOrDefault(t => t.GetSide() == CsTeam.Terrorist);
            var team2 = _teams?.FirstOrDefault(t => t.GetSide() == CsTeam.CounterTerrorist);
            if (team1 == null || team2 == null)
            {
                return "Fail";
            }

            var matchId = await _database.NewMatch(mapName, team1.GetId(), team2.GetId());
            return matchId;
        }

        // get teams with corresponding hostname
        // PLG default, bleu or rouge for LAN
        // Parse Team into TeamManager
        private async Task<List<TeamManager>> GetDBTeamsOfMatch(string hostname)
        {
            var allTeamPLG = await _database.GetTeamsByHostname(hostname);
            return allTeamPLG.Select(t => new TeamManager(t)).ToList();
        }

        public async Task SetTheNewMatchConfig(string hostname, string mapName)
        {
            _mapName = mapName;
            _teams = await GetDBTeamsOfMatch(hostname);
            _matchId = await CreateTheMatchInDB(mapName);
        }

        public async Task RunMatch()
        {
            var hostnameValue = ConVar.Find("hostname");
            if (hostnameValue == null || hostnameValue.StringValue == null)
            {
                _logger.LogError("EROR: hostname not found");
                return;
            }
            _logger.LogInformation($"Hostname: {hostnameValue.StringValue}");

            var hostname = hostnameValue.StringValue;
            var mapName = Server.MapName;

            await Task.Run(async () =>
            {
                await SetTheNewMatchConfig(hostname, mapName);
                Server.NextFrame(() =>
                {
                    SetPlayersInTeams();
                    if (_teams != null)
                    {
                        foreach (var team in _teams)
                        {
                            var side = team.GetSide();
                            if (side == CsTeam.CounterTerrorist)
                            {
                                Server.ExecuteCommand($"mp_teamname_1 {team.GetName()}");
                            }
                            if (side == CsTeam.Terrorist)
                            {
                                Server.ExecuteCommand($"mp_teamname_2 {team.GetName()}");
                            }
                        }
                        StartTvRecord();
                        StartKnife();

                        var teamName1 = _teams[0].GetName();
                        var teamName2 = _teams[1].GetName();

                        BroadcastMessage($"Le match démarre sur {mapName} et avec les équipes {teamName1} et {teamName2}");
                        BroadcastMessage($"Tapez .switch ou .stay !");
                    }
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

        public async Task UpdateStatsMatch()
        {
            if (_teams != null && _matchId != null)
            {
                await _database.UpdateMatchStats(_matchId, _teams[0], _teams[1]);
                await _database.UpdatePlayersStats(_playerManager, _matchId);
            }
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
    }

}
