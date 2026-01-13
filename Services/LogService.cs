using Newtonsoft.Json;
using PromptRunner.Models;
using System.IO;

namespace PromptRunner.Services
{
    public class LogService : ILogService
    {
        private readonly string _logDirectory;
        private readonly string _logFilePath;

        public LogService()
        {
            // ログファイルのパスを設定（ユーザーのドキュメントフォルダに保存）
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PromptRunner",
                "Logs"
            );

            _logFilePath = Path.Combine(_logDirectory, "execution_logs.json");

            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void SaveLog(ExecutionLog log)
        {
            var logs = GetAllLogs();
            logs.Insert(0, log); // 最新のログを先頭に追加

            // 最大100件までログを保持
            if (logs.Count > 100)
            {
                logs = logs.Take(100).ToList();
            }

            SaveLogsToFile(logs);
        }

        public List<ExecutionLog> GetAllLogs()
        {
            if (!File.Exists(_logFilePath))
            {
                return new List<ExecutionLog>();
            }

            try
            {
                var json = File.ReadAllText(_logFilePath);
                var logs = JsonConvert.DeserializeObject<List<ExecutionLog>>(json);
                return logs ?? new List<ExecutionLog>();
            }
            catch
            {
                return new List<ExecutionLog>();
            }
        }

        public ExecutionLog? GetLogById(string id)
        {
            var logs = GetAllLogs();
            return logs.FirstOrDefault(l => l.Id == id);
        }

        public void DeleteLog(string id)
        {
            var logs = GetAllLogs();
            logs.RemoveAll(l => l.Id == id);
            SaveLogsToFile(logs);
        }

        public void ClearAllLogs()
        {
            SaveLogsToFile(new List<ExecutionLog>());
        }

        private void SaveLogsToFile(List<ExecutionLog> logs)
        {
            var json = JsonConvert.SerializeObject(logs, Formatting.Indented);
            File.WriteAllText(_logFilePath, json);
        }
    }
}
