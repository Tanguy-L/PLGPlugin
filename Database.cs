using Dapper;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using PLGPlugin.Interfaces;
using MySqlConnector;
using CounterStrikeSharp.API;

namespace PLGPlugin
{
    public class Database : IDatabase
    {
        private readonly ILogger<Database> _logger;
        private readonly string _connectionString;
        private readonly MySQLConfig _config;
        private bool _disposed;

        public Database(MySQLConfig config)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = loggerFactory.CreateLogger<Database>();
            _connectionString = BuildDatabaseConnectionString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger?.LogInformation("Database service disposed");
                _disposed = true;
            }
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

        public async Task UpdatePlayersStats(IPlayerManager playerManager, string matchId, ITeamManager teamManager)
        {
            await using MySqlConnection connection = new(_connectionString);
            _logger.LogInformation("UPDATING PLAYER STATS");

            if (playerManager == null)
            {
                _logger.LogWarning("No players stats found");
                return;
            }

            foreach (PlgPlayer plgPlayer in playerManager.GetAllPlayers())
            {
                _logger.LogInformation("update player stats");
                if (plgPlayer == null)
                {
                    continue;
                }

                string sqlQuery = $@"
                    INSERT INTO match_stats_players (
                        matchid, mapnumber, member_id, team_id, kills, deaths, damage, assists,
                        enemy5ks, enemy4ks, enemy3ks, enemy2ks, utility_count, utility_damage,
                        utility_successes, utility_enemies, flash_count, flash_successes,
                        health_points_removed_total, health_points_dealt_total, shots_fired_total,
                        shots_on_target_total, v1_count, v1_wins, v2_count, v2_wins, entry_count, entry_wins,
                        equipment_value, money_saved, kill_reward, live_time, head_shot_kills,
                        cash_earned, enemies_flashed)
                    VALUES (
                        @match_id, @map_number, @member_id, @team_id, @kills, @deaths, @damage, @assists,
                        @enemy5ks, @enemy4ks, @enemy3ks, @enemy2ks, @utility_count, @utility_damage,
                        @utility_successes, @utility_enemies, @flash_count, @flash_successes,
                        @health_points_removed_total, @health_points_dealt_total, @shots_fired_total,
                        @shots_on_target_total, @v1_count, @v1_wins, @v2_count, @v2_wins, @entry_count,
                        @entry_wins, @equipment_value, @money_saved, @kill_reward, @live_time,
                        @head_shot_kills, @cash_earned, @enemies_flashed)
                    ON DUPLICATE KEY UPDATE
                        team_id = @team_id, kills = @kills, deaths = @deaths, damage = @damage,
                        assists = @assists, enemy5ks = @enemy5ks, enemy4ks = @enemy4ks, enemy3ks = @enemy3ks,
                        enemy2ks = @enemy2ks, utility_count = @utility_count, utility_damage = @utility_damage,
                        utility_successes = @utility_successes, utility_enemies = @utility_enemies,
                        flash_count = @flash_count, flash_successes = @flash_successes,
                        health_points_removed_total = @health_points_removed_total,
                        health_points_dealt_total = @health_points_dealt_total,
                        shots_fired_total = @shots_fired_total, shots_on_target_total = @shots_on_target_total,
                        v1_count = @v1_count, v1_wins = @v1_wins, v2_count = @v2_count, v2_wins = @v2_wins,
                        entry_count = @entry_count, entry_wins = @entry_wins,
                        equipment_value = @equipment_value, money_saved = @money_saved,
                        kill_reward = @kill_reward, live_time = @live_time, head_shot_kills = @head_shot_kills,
                        cash_earned = @cash_earned, enemies_flashed = @enemies_flashed";

                Dictionary<string, object>? playerStats = plgPlayer.Stats;

                TeamPLG? team = teamManager.GetTeamByName(plgPlayer.TeamName);
                _logger.LogInformation(team.Id.ToString());
                if (playerStats == null || team == null)
                {
                    _logger.LogWarning($"No stats found for player {plgPlayer.MemberId}");
                    continue;
                }

                string? memberId = plgPlayer.MemberId;

                _logger.LogInformation($"player ---- {plgPlayer.PlayerName} ----- {team.Id.ToString()} ---- {matchId}");

                try
                {
                    _ = await connection.ExecuteAsync(sqlQuery,
                    new
                    {
                        match_id = matchId,
                        map_number = 1,
                        member_id = memberId,
                        team_id = team.Id,
                        name = plgPlayer.PlayerName,
                        kills = playerStats["Kills"],
                        deaths = playerStats["Deaths"],
                        damage = playerStats["Damage"],
                        assists = playerStats["Assists"],
                        enemy5ks = playerStats["Enemy5Ks"],
                        enemy4ks = playerStats["Enemy4Ks"],
                        enemy3ks = playerStats["Enemy3Ks"],
                        enemy2ks = playerStats["Enemy2Ks"],
                        utility_count = playerStats["UtilityCount"],
                        utility_damage = playerStats["UtilityDamage"],
                        utility_successes = playerStats["UtilitySuccess"],
                        utility_enemies = playerStats["UtilityEnemies"],
                        flash_count = playerStats["FlashCount"],
                        flash_successes = playerStats["FlashSuccess"],
                        health_points_removed_total = playerStats["HealthPointsRemovedTotal"],
                        health_points_dealt_total = playerStats["HealthPointsDealtTotal"],
                        shots_fired_total = playerStats["ShotsFiredTotal"],
                        shots_on_target_total = playerStats["ShotsOnTargetTotal"],
                        v1_count = playerStats["1v1Count"],
                        v1_wins = playerStats["1v1Wins"],
                        v2_count = playerStats["1v2Count"],
                        v2_wins = playerStats["1v2Wins"],
                        entry_count = playerStats["EntryCount"],
                        entry_wins = playerStats["EntryWins"],
                        equipment_value = playerStats["EquipmentValue"],
                        money_saved = playerStats["MoneySaved"],
                        kill_reward = playerStats["KillReward"],
                        live_time = playerStats["LiveTime"],
                        head_shot_kills = playerStats["HeadShotKills"],
                        cash_earned = playerStats["CashEarned"],
                        enemies_flashed = playerStats["EnemiesFlashed"]
                    });
                }
                catch (Exception ex)
                {
                    string message = $"Error : {ex}";
                    _logger.LogError(message);
                    throw;
                }

            }
        }

        public async Task<List<TeamPLG>> GetTeamsByHostname(string hostname)
        {
            try
            {
                await using MySqlConnection connection = new MySqlConnection(_connectionString);
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
                IEnumerable<(int Id, string Name, CsTeam Side, string Players)> rows = await connection.QueryAsync<(int Id, string Name, CsTeam Side, string Players)>(query, parameters);
                List<TeamPLG> groupedTeams = rows
                    .GroupBy(row => new { row.Name, row.Side, row.Id })
                    .Select(group => new TeamPLG
                    {
                        Name = group.Key.Name,
                        Id = group.Key.Id,
                        Side = group.Key.Side,
                        Players = group.Select(x => x.Players).ToList(),
                        Score = 0,
                        Ready = false,
                        HasPaused = false
                    }).ToList();
                return groupedTeams;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting teams by Hostname");
                throw;
            }
        }

        public async Task UpdateMatchStats(string id, TeamPLG team1, TeamPLG team2)
        {
            try
            {
                if (team1 == null || team2 == null)
                {
                    Exception exception = new("Teams not found");
                    throw exception;
                }

                int scoreTeam1 = team1.Score;
                int scoreTeam2 = team2.Score;
                int winner = scoreTeam1 > scoreTeam2 ? team1.Id : team2.Id;

                await using MySqlConnection connection = new MySqlConnection(_connectionString);

                string query = @"UPDATE plg.match_stats_matches 
                SET end_time = @EndTime, 
                    winner = @Winner, 
                    team1_score = @Team1Score, 
                    team2_score = @Team2Score
                WHERE matchid = @Id";

                var parameters = new
                {
                    Id = id,
                    EndTime = DateTime.Now,
                    Winner = winner,
                    Team1Score = scoreTeam1,
                    Team2Score = scoreTeam2,
                };
                int rowsAffected = await connection.ExecuteAsync(query, parameters);

                if (rowsAffected == 0)
                {
                    throw new Exception($"Update failed: No match found with ID {id}");
                }

                _logger.LogInformation($"Match {id} updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while openning the database connection");
                throw;
            }
        }

        public async Task<string> NewMatch(string map, int teamId1, int teamId2)
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);

                // Start a transaction to ensure both inserts succeed
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Insert into match_stats_matches
                    string matchQuery = @"INSERT INTO plg.match_stats_matches 
                (start_time, end_time, winner, team1_score, team2_score, series_type, server_ip, team1_id, team2_id)
                VALUES 
                (@StarTime, NULL, NULL, 0, 0, 'BO1', '-', @team1, @team2);
                SELECT LAST_INSERT_ID();";

                    var matchParameters = new
                    {
                        StarTime = DateTime.Now,
                        team1 = teamId1,
                        team2 = teamId2
                    };

                    object? matchId = await connection.ExecuteScalarAsync(matchQuery, matchParameters, transaction);
                    string? matchIdString = matchId?.ToString();

                    if (string.IsNullOrWhiteSpace(matchIdString))
                    {
                        throw new Exception("Insertion of match failed");
                    }

                    // Insert into match_stats_maps
                    string mapQuery = @"INSERT INTO plg.match_stats_maps 
                (matchid, mapnumber, mapname, start_time) 
                VALUES 
                (@matchId, @mapNumber, @mapName, @startTime)";

                    var mapParameters = new
                    {
                        matchId = matchIdString,
                        mapNumber = 1,
                        mapName = map,
                        startTime = DateTime.Now
                    };

                    await connection.ExecuteAsync(mapQuery, mapParameters, transaction);

                    // Commit the transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Match created with ID: {matchIdString}");
                    return matchIdString;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating new match");
                throw;
            }
        }

        private async Task<MySqlConnection> GetOpenConnectionAsync()
        {
            try
            {
                MySqlConnection connection = new(_connectionString);
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
                await using MySqlConnection connection = await GetOpenConnectionAsync();

                string query =
                    $@"UPDATE members SET smoke_color = '{color}' WHERE steam_id = {steamId}";
                _ = await connection.ExecuteAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection error on SetSmoke");
            }
        }

        public async Task JoinTeam(string memberId, int? idTeam)
        {
            try
            {
                await using MySqlConnection connection = await GetOpenConnectionAsync();

                string query = @"
            INSERT INTO team_members (member_id, team_id) 
            VALUES (@MemberId, @IdTeam)
            ON DUPLICATE KEY UPDATE team_id = @IdTeam";

                await connection.ExecuteAsync(query, new { MemberId = memberId, IdTeam = idTeam });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection error on JoinTeam");
            }
        }

        public async Task CreatePlayerInDB(string name, ulong steamID)
        {
            try
            {
                await using MySqlConnection connection = await GetOpenConnectionAsync();
                string query =
                    @"INSERT INTO plg.members (steam_id, weight, is_logged_in, smoke_color, name)
                             VALUES (@SteamID, @Weight, @IsLoggedIn, @SmokeColor, @Name);
                             SELECT LAST_INSERT_ID();";
                var parameters = new
                {
                    steamID,
                    Weight = 6, // Default weight, adjust as needed
                    IsLoggedIn = false,
                    SmokeColor = "red",
                    Name = name ?? "test",
                };
                _ = await connection.ExecuteAsync(query, parameters);
            }
            catch (Exception)
            {
                Console.WriteLine($"Error creating player in DB: {steamID} -- {name}");
                throw;
            }
        }

        public async Task<PlayerFromDB?> GetPlayerById(ulong steamId)
        {
            try
            {
                await using MySqlConnection connection = await GetOpenConnectionAsync();
                string query =
                    $@"SELECT
                     m.discord_id DiscordId,
                     m.name Name,
                     m.steam_id SteamId,
                     m.id MemberId,
                     m.weight,
                     m.is_logged_in IsLoggedIn,
                     m.smoke_color SmokeColor,
                     t.name TeamName,
                     t.side Side,
                     t.channel_id TeamChannelId,
                    t.hostname TeamHostname
                 FROM
                     plg.members m
                 LEFT JOIN
                     plg.team_members tm ON m.id = tm.member_id
                 LEFT JOIN
                     plg.teams t ON tm.team_id = t.team_id
                 WHERE
                     m.steam_id = @SteamID
                 ;";
                var parameters = new { steamId };
                PlayerFromDB? playerDB = await connection.QueryFirstOrDefaultAsync<PlayerFromDB>(
                    query,
                    parameters
                );
                return playerDB;
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                {
                    Console.WriteLine($"Error retrieving player by ID {steamId}: {ex.Message}");
                });
                throw;
            }
        }
    }
}
