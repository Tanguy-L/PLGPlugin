using Dapper;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace PLGPlugin
{
    public class Database
    {
        private readonly ILogger<Database> _logger;
        private readonly string _connectionString;
        private readonly MySQLConfig _config;

        public Database(MySQLConfig config)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<Database>();
            _config = config;
            _connectionString = BuildDatabaseConnectionString();
        }

        private string BuildDatabaseConnectionString()
        {
            if (
                string.IsNullOrWhiteSpace(_config.HostDB)
                || string.IsNullOrWhiteSpace(_config.Username)
                || string.IsNullOrWhiteSpace(_config.Password)
                || string.IsNullOrWhiteSpace(_config.Database)
                || _config.Port == 0
            )
            {
                throw new InvalidOperationException("Database is not set in the config file");
            }

            MySqlConnectionStringBuilder builder = new()
            {
                Server = _config.HostDB,
                Port = (uint)_config.Port,
                UserID = _config.Username,
                Password = _config.Password,
                Database = _config.Database,
                Pooling = true,
            };

            return builder.ConnectionString;
        }

        public async Task<List<TeamPLG>> GetTeamsByHostname(string hostname)
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                string query = @"
                    SELECT 
                        t.team_id as Id,
                        t.name AS Name,
                        t.side AS Side,
                        m.id AS Players
                    FROM 
                        teams t
                    JOIN 
                        team_members tm ON t.team_id = tm.team_id
                    JOIN 
                        members m ON tm.member_id = m.id
                    WHERE 
                        t.hostname = @hostname;
                ";
                var parameters = new { hostname };
                var rows = await connection.QueryAsync<(int Id, string Name, CsTeam Side, string Players)>(query, parameters);
                var groupedTeams = rows
                    .GroupBy(row => new { row.Name, row.Side, row.Id })
                    .Select(group => new TeamPLG
                    {
                        Name = group.Key.Name,
                        Id = group.Key.Id,
                        Side = group.Key.Side,
                        Players = group.Select(x => x.Players).ToList()
                    }).ToList();
                return groupedTeams;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while getting teams by Hostname");
                throw;
            }

        }

        public async Task<string> NewMatch(string map, int teamId1, int teamId2)
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                //TODO remove ip_server from db
                string query = @"INSERT INTO plg.match_stats_matches 
                    (start_time, end_time, winner, team1_score, team2_score, series_type, server_ip, team1_id, team2_id)
                    VALUES 
                    (@StarTime, NULL, NULL, 0, 0, 'BO1', '-', @team1, @team2);
                    SELECT LAST_INSERT_ID();
                ";
                var parameters = new
                {
                    StarTime = DateTime.Now,
                    MapName = map,
                    team1 = teamId1,
                    team2 = teamId2
                };
                var matchId = await connection.ExecuteScalarAsync(query, parameters);
                _logger.LogInformation("Match id: " + matchId);
                var matchIdString = matchId?.ToString();
                if (string.IsNullOrWhiteSpace(matchIdString))
                {
                    throw new Exception("Insertion of match failed");
                }
                return matchIdString;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while openning the database connection");
                throw;
            }
        }

        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            try
            {
                var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while openning the database connection");
                throw;
            }
        }

        public async Task SetSmoke(ulong steamId, string color)
        {
            try
            {
                await using var connection = await GetOpenConnectionAsync();

                string query =
                    $@"UPDATE members SET smoke_color = '{color}' WHERE steam_id = {steamId}";
                await connection.ExecuteAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection error on SetSmoke");
            }
        }

        public async Task CreatePlayerInDB(PlgPlayer player)
        {
            try
            {
                await using var connection = await GetOpenConnectionAsync();
                string query =
                    @"INSERT INTO plg.members (steam_id, weight, is_logged_in, smoke_color, discord_id, discord_name)
                             VALUES (@SteamID, @Weight, @IsLoggedIn, @SmokeColor, 0, @DiscordName);
                             SELECT LAST_INSERT_ID();";
                var parameters = new
                {
                    SteamID = player.SteamID,
                    Weight = 6, // Default weight, adjust as needed
                    IsLoggedIn = false,
                    SmokeColor = "red",
                    DiscordName = player.PlayerName ?? "test",
                };
                await connection.ExecuteAsync(query, parameters);
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<PlayerFromDB?> GetPlayerById(ulong steamId)
        {
            try
            {
                await using var connection = await GetOpenConnectionAsync();
                string query =
                    $@"SELECT
                     m.discord_id DiscordId,
                     m.discord_name DiscordName,
                     m.steam_id SteamId,
                     m.weight,
                     m.is_logged_in IsLoggedIn,
                     m.smoke_color SmokeColor,
                     t.name TeamName,
                     t.side Side,
                     t.channel_id TeamChannelId
                 FROM
                     plg.members m
                 LEFT JOIN
                     plg.team_members tm ON m.id = tm.member_id
                 LEFT JOIN
                     plg.teams t ON tm.team_id = t.team_id
                 WHERE
                     m.steam_id = @SteamID
                 ;";
                var parameters = new { SteamId = steamId.ToString() };
                PlayerFromDB? playerDB = await connection.QueryFirstOrDefaultAsync<PlayerFromDB>(
                    query,
                    parameters
                );
                return playerDB;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Database connection error on GetPlayerById");
                throw;
            }
        }
    }
}
