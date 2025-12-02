namespace NovaLight.Core
{
    public delegate void Log(string message);

    public class Logger
    {
        private readonly List<string> _logs = [];
        public string[] Logs => [.. _logs];
        public event Log? OnLog;

        public void SaveToPath(string filePath)
        {
            File.Create(filePath);
            File.WriteAllLines(filePath, _logs);
        }

        public void Log(string message)
        {
            OnLog?.Invoke(message);
            _logs.Add(message);
        }
    }
}