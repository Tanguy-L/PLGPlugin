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

            Console.WriteLine("PlayerManager is disposed.");
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
                playerPLG.TeamHostname = playerDB.TeamHostname;
                if (playerDB.Weight != null)
                {
                    playerPLG.Weight = playerDB.Weight.ToString();
                }
            }
            AddOrUpdatePlayer(playerPLG);
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
