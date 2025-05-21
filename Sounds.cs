using System.Timers;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

namespace PLGPlugin
{

    public class Sounds
    {
        public bool isPlaying = false;
        public System.Timers.Timer soundBlockTimer = new();
        public int duration = 0;
        private readonly ILogger<Sounds>? _logger;

        public Sounds()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<Sounds>();
            soundBlockTimer.Elapsed += OnSoundTimerElapsed;
        }

        public void OnSoundTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            isPlaying = false;
            soundBlockTimer.Stop();
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
                soundBlockTimer.Interval = duration;
                soundBlockTimer.Start();

                var players = Utilities.GetPlayers().Where(player => player != null && player.IsValid && player.IsBot == false && player.IsHLTV == false);


                foreach (var player in players)
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
