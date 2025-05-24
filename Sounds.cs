using System.Timers;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

namespace PLGPlugin
{

    public class Sounds : IDisposable
    {
        public bool isPlaying;
        public System.Timers.Timer _soundBlockTimer = new();
        public int duration;
        private readonly ILogger<Sounds>? _logger;

        public Sounds()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<Sounds>();
            _soundBlockTimer.Elapsed += OnSoundTimerElapsed;
        }

        public void Dispose()
        {
            _soundBlockTimer?.Stop();
            _soundBlockTimer?.Dispose(); // Properly clean up!
        }

        public void OnSoundTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            isPlaying = false;
            _soundBlockTimer.Stop();
        }

        public void PlayForAllPlayers(string sound, int duration = 3000)
        {
            if (_logger == null)
            {
                Console.WriteLine($"Skipping sound {sound} - logger is null");
                return;
            }
            if (isPlaying)
            {
                _logger.LogInformation($"Skipping sound {sound} - another sound is playing");
                return;
            }

            Server.NextFrame(() =>
            {
                isPlaying = true;
                _soundBlockTimer.Interval = duration;
                _soundBlockTimer.Start();

                IEnumerable<CCSPlayerController> players = Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.IsBot == false && player.IsHLTV == false);


                foreach (CCSPlayerController player in players)
                {
                    PlaySound(player, sound);
                }
            });
        }

        public void PlaySound(CCSPlayerController? player, string sound)
        {
            Server.NextFrame(() =>
            {
                player?.ExecuteClientCommand($"play {sound}");
            });
        }
    }
}
