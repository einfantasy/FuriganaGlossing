using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FuriganaGlossing.Services
{
    public interface IProcessManagerService
    {
        void StartProcess(string command);
        void StopAll();
    }

    public class ProcessManagerService : IProcessManagerService
    {
        private readonly ILogService _logService;
        private readonly Queue<Process> _managedProcesses = new Queue<Process>();

        public ProcessManagerService(ILogService logService)
        {
            _logService = logService;
        }

        public void StartProcess(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    //FileName = "cmd.exe",
                    //Arguments = $"/c {command}",
                    FileName = command.Split(" ")[0],
                    Arguments = command.Replace(command.Split(" ")[0], ""),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            _logService.Log(e.Data, LogLevel.Info);
                        }
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            _logService.Log(e.Data, LogLevel.Info);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    _managedProcesses.Enqueue(process);
                }
            }
            catch (Exception ex)
            {
                _logService.Log($"Error starting process: {ex.Message}", LogLevel.Error);
                throw new ProcessStartException($"Error starting process: {ex.Message}", ex);
            }
        }

        public void StopAll()
        {
            while (_managedProcesses.Count > 0)
            {
                var process = _managedProcesses.Dequeue();
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch
                {
                }
            }
        }
    }
}
