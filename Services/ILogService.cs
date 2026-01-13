using PromptRunner.Models;

namespace PromptRunner.Services
{
    public interface ILogService
    {
        void SaveLog(ExecutionLog log);
        List<ExecutionLog> GetAllLogs();
        ExecutionLog? GetLogById(string id);
        void DeleteLog(string id);
        void ClearAllLogs();
    }
}
