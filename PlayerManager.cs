using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PLGPlugin.Interfaces;

namespace PLGPlugin
{
    public class PlayerManager(Database database, ILoggingService Logger) : IPlayerManager
    {
        private readonly ConcurrentDictionary<ulong, PlgPlayer> _players = new();
        private readonly Database _database = database ?? throw new ArgumentNullException(nameof(database));
        private readonly ILoggingService _logger = Logger ?? throw new ArgumentNullException(nameof(Logger));
        private bool _disposed;

        private void ThrowIfDisposed()
        {
            if (!_disposed)
            {
                return;
            }

            throw new ObjectDisposedException(nameof(PlayerManager));
        }

        public IEnumerable<PlgPlayer> GetAllPlayers()
        {
            return _players.Values;
        }

        public PlgPlayer? GetPlayer(ulong steamId)
        {
            ThrowIfDisposed();
            return _players.TryGetValue(steamId, out PlgPlayer? player) ? player : null;
        }

        public void AddOrUpdatePlayer(PlgPlayer player)
        {
            ThrowIfDisposed();
            _logger.Debug($"Adding or updating player: {player.SteamID} - {player.PlayerName}");
            PlgPlayer plgPlayer = _players.AddOrUpdate(player.SteamID, player, (_, _) => player);

        }

        public void RemovePlayer(ulong steamId)
        {
            ThrowIfDisposed();
            _ = _players.TryRemove(steamId, out _);
        }

        public void ClearCache()
        {
            ThrowIfDisposed();
            _players.Clear();
        }

        public void UpdatePlayerWithData(
            CCSPlayerController playerController,
            PlayerFromDB playerDB
        )
        {
            ThrowIfDisposed();
            if (playerController == null)
            {
                return;
            }

            PlgPlayer playerPLG = new(playerController);
            if (playerDB != null)
            {
                playerPLG.Side = playerDB.Side;
                playerPLG.TeamName = playerDB.TeamName;
                playerPLG.SmokeColor = playerDB.SmokeColor;
                playerPLG.DiscordId = playerDB.DiscordId;
                playerPLG.TeamChannelId = playerDB.TeamChannelId;
                playerPLG.MemberId = playerDB.MemberId;
                if (playerDB.Weight != null)
                {
                    playerPLG.Weight = playerDB.Weight.ToString();
                }
            }
            AddOrUpdatePlayer(playerPLG);
        }

        public async Task AddPlgPlayer(CCSPlayerController playerController)
        {
            ThrowIfDisposed();
            if (playerController != null)
            {
                PlgPlayer playerPLG = new(playerController);
                ulong steamId = playerController.SteamID;
                PlayerFromDB? playerInfosDB = await _database.GetPlayerById(steamId);

                if (playerInfosDB != null)
                {
                    playerPLG.Side = playerInfosDB.Side;
                    playerPLG.TeamName = playerInfosDB.TeamName;
                    playerPLG.SmokeColor = playerInfosDB.SmokeColor;
                    playerPLG.DiscordId = playerInfosDB.DiscordId;
                    playerPLG.TeamChannelId = playerInfosDB.TeamChannelId;
                    playerPLG.MemberId = playerInfosDB.MemberId;
                    if (playerInfosDB.Weight != null)
                    {
                        playerPLG.Weight = playerInfosDB.Weight.ToString();
                    }
                }
                else
                {
                    await _database.CreatePlayerInDB(playerPLG);
                }
                AddOrUpdatePlayer(playerPLG);
            }
        }

        public async Task LoadCache()
        {
            ThrowIfDisposed();
            List<CCSPlayerController> allPlayers = Utilities.GetPlayers();
            foreach (CCSPlayerController player in allPlayers)
            {
                await AddPlgPlayer(player);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _players.Clear();

                    // Note: We dons unloaded
                }

                _disposed = true;
            }
        }
    }
}
