namespace PluginFramework.Interfaces {
    public interface ILogger {
        void Log(string text);
        void LogWarning(string text);
        void LogError(string text);
    }
}