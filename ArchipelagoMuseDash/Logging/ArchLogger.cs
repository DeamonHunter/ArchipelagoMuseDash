using System;
using MelonLoader;

namespace ArchipelagoMuseDash.Logging {
    /// <summary>
    /// Used to output to MelonLoader's logging system
    /// </summary>
    public class ArchLogger {
        readonly MelonLogger.Instance _logger;

        public ArchLogger() {
            _logger = new MelonLogger.Instance("Archipelago");
        }

        public void Log(string message) => Log("", message);

        public void Log(string source, string message) {
            _logger.Msg($"[{source}] {message}");
        }

        public void Warning(string source, string message) {
            _logger.Msg(ConsoleColor.DarkYellow, $"[{source}] {message}");
        }

        public void Error(string source, Exception e) {
            _logger.Msg(ConsoleColor.DarkRed, $"[{source}] {e}");
        }
    }
}
