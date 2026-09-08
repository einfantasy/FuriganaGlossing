using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FuriganaGlossing.Services
{
    public interface IProcessManagerService
    {
        void StartProcess(string command);
        void StopAll();
    }

    public class ProcessManagerService : IProcessManagerService
    {
        private readonly List<Process> _managedProcesses = new List<Process>();

        public void StartProcess(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    _managedProcesses.Add(process);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting process: {ex.Message}");
            }
        }

        public void StopAll()
        {
            foreach (var process in _managedProcesses)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch { }
            }
            _managedProcesses.Clear();
        }
    }
}
