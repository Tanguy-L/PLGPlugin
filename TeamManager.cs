using CounterStrikeSharp.API.Modules.Utils;

namespace PLGPlugin
{
    public class TeamManager
    {

        private List<TeamPLG>? _teams;

        public TeamManager()
        {
            _teams = new List<TeamPLG>();
        }

        public void AddTeam(TeamPLG team)
        {
            if (_teams == null)
            {
                PLGPlugin.Instance.Logger?.Error("Teams is null");
                return;
            }
            _teams.Add(team);
        }

        public TeamPLG? GetTeamByIndex(int index)
        {
            if (_teams == null)
            {
                PLGPlugin.Instance.Logger?.Error("Teams is null");
                return null;
            }
            return _teams[index];
        }

        public TeamPLG? GetTeamById(int id)
        {
            if (_teams == null)
            {
                PLGPlugin.Instance.Logger?.Error("Teams is null");
                return null;
            }
            var result = _teams.FirstOrDefault(t => t.Id == id);
            if (result == null)
            {
                PLGPlugin.Instance.Logger?.Error($"Team with id {id} not found");
                return null;
            }
            return result;
        }

        public TeamPLG? GetTeamBySide(CsTeam side)
        {
            if (_teams == null)
            {
                PLGPlugin.Instance.Logger?.Error("Teams is null");
                return null;
            }
            var result = _teams.FirstOrDefault(t => t.Side == side);
            if (result == null)
            {
                PLGPlugin.Instance.Logger?.Error($"Team with side {side} not found");
                return null;
            }
            return result;
        }

        public void ReverseSide()
        {
            if (_teams == null)
            {
                PLGPlugin.Instance.Logger?.Error("Teams is null");
                return;
            }
            _teams.ForEach(t => t.Side = t.Side == CsTeam.CounterTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
        }

        public bool isSomeTeamWithName(string nameTeam)
        {
            if (_teams == null)
            {
                return false;
            }
            return _teams.Any(t => t.Name == nameTeam);
        }

        public int? IdOfBestTeam()
        {
            if (_teams == null)
            {
                PLGPlugin.Instance.Logger?.Error("Teams is null");
                return null;
            }

            return this._teams.MaxBy(t => t.Score)?.Id;

        }
    }
}
