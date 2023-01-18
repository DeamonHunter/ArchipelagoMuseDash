using System;
using Archipelago.MultiClient.Net.Helpers;
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

        public void LogDebug(string source, string message) {
#if DEBUG
            _logger.Msg($"[{source}] {message}");
#endif
        }

        public void Log(string source, string message) {
            _logger.Msg($"[{source}] {message}");
        }

        public void Warning(string source, string message) {
            _logger.Warning($"[{source}] {message}");
        }

        public void Error(string source, Exception e) {
            _logger.Error($"Exception occured in: {source}.", e);
        }

        public void LogMessage(LogMessage message) {
            _logger.Msg(message.ToString());
        }
    }
}
