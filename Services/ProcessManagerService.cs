using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace FuriganaGlossing.Services
{
    public interface IProcessManagerService
    {
        void StartProcess(string command);
        void StopAll();
    }

    public class ProcessManagerService : IProcessManagerService
    {
        private Queue<Process> _managedProcesses = new Queue<Process>();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

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
                            App.LogService.Log(e.Data, LogLevel.Info);
                        }
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            App.LogService.Log(e.Data, LogLevel.Info);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    _managedProcesses.Enqueue(process);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting process: {ex.Message}");
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
                        //if (AttachConsole((uint)process.Id))
                        //{
                        //    GenerateConsoleCtrlEvent(0, 0);
                        //    FreeConsole();
                        //}

                        process.Kill();
                    }
                }

                catch { }
            }
        }
    }
}
