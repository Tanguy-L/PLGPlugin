using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin.Interfaces
{
    public interface IDatabase : IDisposable
    {
        Task<PlayerFromDB?> GetPlayerById(ulong steamId);
        Task<List<TeamPLG>> GetTeamsByHostname(string hostname);
        Task<string> NewMatch(string map, int teamId1, int teamId2);
        Task UpdateMatchStats(string id, TeamPLG team1, TeamPLG team2);
        Task UpdatePlayersStats(IPlayerManager playerManager, string matchId, ITeamManager teamManager);
        Task SetSmoke(ulong steamId, string color);
        Task JoinTeam(string memberId, int? idTeam);
        Task CreatePlayerInDB(string name, ulong steamID);
    }

    // public interface IBackupManager: IDisposable
    // {
    //
    // }

    public interface IPlayerManager : IDisposable
    {
        IEnumerable<PlgPlayer> GetAllPlayers();
        PlgPlayer? GetPlayer(ulong steamId);
        void AddOrUpdatePlayer(PlgPlayer player);
        void RemovePlayer(ulong steamId);
        void ClearCache();
        // Task AddPlgPlayer(CCSPlayerController playerController);
        void UpdatePlayerWithData(CCSPlayerController playerController, PlayerFromDB playerDB);
    }

    public interface ITeamManager : IDisposable
    {
        void AddTeam(TeamPLG team);
        TeamPLG? GetTeamByIndex(int index);
        TeamPLG? GetTeamById(int id);
        TeamPLG? GetTeamBySide(CsTeam side);
        TeamPLG? GetTeamByName(string name);
        void ReverseSide();
        bool IsSomeTeamWithName(string nameTeam);
        int? IdOfBestTeam();
        void ClearTeams();
    }

    public interface IMatchManager : IDisposable
    {
        MatchManager.MatchState State { get; set; }
        void InitSetupMatch(string hostname);
        void SetWinnerTeam(int id);
        void EndMatch();
        void SetTeamReadyBySide(CsTeam side, bool value);
        bool IsAllTeamReady();
        Task RunMatch();
        void GoGoGo();
        void DetermineTheKnifeWinner();
        int? GetKnifeTeamId();
        Task UpdateStatsMatch();
    }

    public interface ISoundService : IDisposable
    {
        void PlayForAllPlayers(string sound, int duration = 3000);
        void PlaySound(CCSPlayerController? player, string sound);
    }

    public interface ILoggingService
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? ex = null);
        void Critical(string message);
    }
}
