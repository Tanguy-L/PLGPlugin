using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public class TeamManager
    {
        private TeamPLG _team;

        public int Score { get; set; } = 0;
        public bool Ready { get; set; } = false;
        public bool HasPaused { get; set; } = false;

        public TeamManager(TeamPLG teamInit)
        {
            _team = teamInit;
        }

        public string? GetPlayerById(string id)
        {
            return _team.Players.FirstOrDefault(playerId => playerId == id);
        }

        public bool IsReady()
        {
            return Ready;
        }

        public CsTeam GetSide()
        {
            return _team.Side;
        }

        public void ReverseSide()
        {
            _team.Side = _team.Side == CsTeam.CounterTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
        }

        public string GetName()
        {
            return _team.Name;
        }

        public int GetId()
        {
            return _team.Id;
        }
    }
}
