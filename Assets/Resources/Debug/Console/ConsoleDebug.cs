using UnityEngine;

namespace Services.DebugUtilities.Console
{
    public static class LoggerService
    {
        /// <summary>
        /// Logs a message with a given level and category.
        /// </summary>
        public static void PrintLogMessage(LogLevel logLevel, LogCategory logCategory, string message)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(logCategory.Color);
            string formattedMessage = $"<color=#{colorHex}>[{logCategory.Name.ToUpper()}]</color> {message}";
            Dispatch(logLevel, formattedMessage);
        }

        /// <summary>
        /// Logs a message with a given level, category, and success status.
        /// </summary>
        public static void PrintLogMessage(LogLevel logLevel, LogCategory logCategory, bool success, string message)
        {
            string categoryHex = ColorUtility.ToHtmlStringRGB(logCategory.Color);
            Color statusColor = success ? Color.green : Color.red;
            string statusHex = ColorUtility.ToHtmlStringRGB(statusColor);
            string statusLabel = success ? "[SUCCESSFUL]" : "[FAILURE]";

            string formattedMessage =
                $"<color=#{categoryHex}>[{logCategory.Name.ToUpper()}]</color> " +
                $"<color=#{statusHex}>{statusLabel}</color> " +
                $"{message}";

            Dispatch(logLevel, formattedMessage);
        }

        private static void Dispatch(LogLevel logLevel, string message)
        {
            switch (logLevel)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
                default: // LogLevel.Debug
                    Debug.Log(message);
                    break;
            }
        }
    }

    /// <summary>
    /// Defines the level of log
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Warning,
        Error
    }

    /// <summary>
    /// Associates a debug category with a display color.
    /// Green and Red are reserved for success/failure status indicators.
    /// </summary>
    public sealed class LogCategory
    {
        public readonly string Name;
        public readonly Color Color;

        private LogCategory(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public override string ToString() => Name;

        // =========================
        // Sistema base
        // =========================
        public static readonly LogCategory Core           = new("Core",           new Color(0.6f, 0.7f, 1f));   // RGB(153, 179, 255) - Azul claro
        public static readonly LogCategory Initialization = new("Initialization", new Color(0.6f, 0.6f, 1f));   // RGB(153, 153, 255) - Azul suave
        public static readonly LogCategory Lifecycle      = new("Lifecycle",      new Color(0.9f, 0.9f, 0.4f)); // RGB(230, 230, 102) - Amarelo suave

        // =========================
        // Gameplay
        // =========================
        public static readonly LogCategory Gameplay = new("Gameplay", new Color(0.95f, 0.95f, 0.95f)); // RGB(242, 242, 242) - Branco suave
        public static readonly LogCategory Combat   = new("Combat",   new Color(1f,    0.6f,  0.2f));  // RGB(255, 153, 51)  - Laranja
        public static readonly LogCategory Ability  = new("Ability",  new Color(0.6f,  0.3f,  1f));   // RGB(153, 77,  255) - Roxo
        public static readonly LogCategory AI       = new("AI",       new Color(1f,    0.7f,  0.3f)); // RGB(255, 179, 77)  - Laranja claro
        public static readonly LogCategory Player   = new("Player",   new Color(0.3f,  0.7f,  1f));   // RGB(77,  179, 255) - Azul
        public static readonly LogCategory Input    = new("Input",    new Color(0.75f, 0.75f, 0.75f));// RGB(191, 191, 191) - Cinza claro

        // =========================
        // Mundo
        // =========================
        public static readonly LogCategory World       = new("World",       new Color(0.4f, 0.8f, 1f));   // RGB(102, 204, 255) - Azul céu
        public static readonly LogCategory Environment = new("Environment", new Color(0.3f, 0.5f, 0.8f)); // RGB(77,  128, 204) - Azul escuro
        public static readonly LogCategory Physics     = new("Physics",     new Color(0.5f, 0.6f, 1f));   // RGB(128, 153, 255) - Azul médio
        public static readonly LogCategory Navigation  = new("Navigation",  new Color(1f,   0.8f, 0.3f)); // RGB(255, 204, 77)  - Dourado

        // =========================
        // UI
        // =========================
        public static readonly LogCategory UI = new("UI", new Color(1f,    0.5f, 1f)); // RGB(255, 128, 255) - Rosa
        public static readonly LogCategory UX = new("UX", new Color(0.85f, 0.4f, 1f));// RGB(217, 102, 255) - Roxo claro

        // =========================
        // Dados
        // =========================
        public static readonly LogCategory Data       = new("Data",       new Color(0.5f, 0.9f, 1f)); // RGB(128, 230, 255) - Ciano claro
        public static readonly LogCategory SaveSystem = new("SaveSystem", new Color(0.3f, 0.8f, 1f)); // RGB(77,  204, 255) - Azul ciano
        public static readonly LogCategory Loading    = new("Loading",    new Color(1f,   1f,   0.5f));// RGB(255, 255, 128) - Amarelo

        // =========================
        // Arquitetura
        // =========================
        public static readonly LogCategory Command   = new("CommandBus",   new Color(1f,   0.75f, 0.3f)); // RGB(255, 191, 77)  - Laranja claro
        public static readonly LogCategory EventBus     = new("EventBus",     new Color(0.4f, 0.9f,  1f));  // RGB(102, 230, 255) - Ciano
        public static readonly LogCategory StateMachine = new("StateMachine", new Color(0.7f, 0.3f,  1f));  // RGB(179, 77,  255) - Roxo forte

        // =========================
        // Multiplayer
        // =========================
        public static readonly LogCategory Network     = new("Network",     new Color(0.3f, 0.6f, 1f));   // RGB(77,  153, 255) - Azul rede
        public static readonly LogCategory Prediction  = new("Prediction",  new Color(0.7f, 0.7f, 1f));   // RGB(179, 179, 255) - Azul claro
        public static readonly LogCategory Replication = new("Replication", new Color(1f,   0.7f, 0.4f)); // RGB(255, 179, 102) - Laranja médio

        // =========================
        // Performance
        // =========================
        public static readonly LogCategory Performance = new("Performance", new Color(1f,   1f,   0.3f)); // RGB(255, 255, 77)  - Amarelo forte
        public static readonly LogCategory Memory      = new("Memory",      new Color(0.6f, 0.5f, 0.4f));// RGB(153, 128, 102) - Marrom claro

        // =========================
        // Debug
        // =========================
        public static readonly LogCategory Debug = new("Debug", new Color(0.7f, 0.7f, 0.7f)); // RGB(179, 179, 179) - Cinza
    }
}