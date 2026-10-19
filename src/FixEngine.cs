using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WinFixPro
{
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error,
        Header
    }

    public class FixEngine
    {
        private static FixEngine instance;
        public static FixEngine Instance
        {
            get
            {
                if (instance == null) instance = new FixEngine();
                return instance;
            }
        }

        public delegate void LogHandler(string message, LogLevel level);
        public delegate void StatusHandler(string status, int progress);
        public delegate void TaskStateChangedHandler(bool isRunning, string taskName);

        public event LogHandler OnLog;
        public event StatusHandler OnStatus;
        public event TaskStateChangedHandler OnTaskStateChanged;

        private Process activeProcess;
        private Thread workerThread;
        private bool isCancelling = false;
        private readonly object lockObj = new object();
        private readonly List<string> fullSessionLogs = new List<string>();

        public bool IsRunning { get; private set; }
        public string CurrentTaskName { get; private set; }

        public FixEngine()
        {
            IsRunning = false;
            CurrentTaskName = "Idle";
        }

        public void Log(string message, LogLevel level)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formatted = string.Format("[{0}] {1}", timestamp, message);

            lock (fullSessionLogs)
            {
                fullSessionLogs.Add(formatted);
            }

            if (OnLog != null)
            {
                OnLog(message, level);
            }
        }

        public void UpdateStatus(string status, int progress)
        {
            if (OnStatus != null)
            {
                OnStatus(status, progress);
            }
        }

        public void StartTask(string taskName, Action action)
        {
            lock (lockObj)
            {
                if (IsRunning)
                {
                    Log("A task is already running. Please wait or cancel the current task.", LogLevel.Warning);
                    return;
                }

                IsRunning = true;
                CurrentTaskName = taskName;
                isCancelling = false;
            }

            if (OnTaskStateChanged != null)
            {
                OnTaskStateChanged(true, taskName);
            }

            Log("==================================================", LogLevel.Header);
            Log("Starting: " + taskName, LogLevel.Header);
            Log("==================================================", LogLevel.Header);
            UpdateStatus("Running: " + taskName, 0);

            workerThread = new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    action();
                    if (!isCancelling)
                    {
                        Log(taskName + " completed successfully.", LogLevel.Success);
                        UpdateStatus("Completed: " + taskName, 100);
                    }
                    else
                    {
                        Log(taskName + " was cancelled by user.", LogLevel.Warning);
                        UpdateStatus("Cancelled", 0);
                    }
                }
                catch (ThreadAbortException)
                {
                    Log("Task aborted.", LogLevel.Warning);
                    UpdateStatus("Aborted", 0);
                }
                catch (Exception ex)
                {
                    Log("Error executing " + taskName + ": " + ex.Message, LogLevel.Error);
                    UpdateStatus("Error", 0);
                }
                finally
                {
                    lock (lockObj)
                    {
                        IsRunning = false;
                        activeProcess = null;
                        workerThread = null;
                    }

                    if (OnTaskStateChanged != null)
                    {
                        OnTaskStateChanged(false, "Idle");
                    }
                }
            }));

            workerThread.IsBackground = true;
            workerThread.Start();
        }

        public void CancelCurrentTask()
        {
            lock (lockObj)
            {
                if (!IsRunning) return;
                isCancelling = true;
                Log("Cancelling active task...", LogLevel.Warning);

                try
                {
                    if (activeProcess != null && !activeProcess.HasExited)
                    {
                        // Kill process tree
                        KillProcessTree(activeProcess.Id);
                    }
                }
                catch (Exception ex)
                {
                    Log("Error terminating process: " + ex.Message, LogLevel.Warning);
                }

                try
                {
                    if (workerThread != null && workerThread.IsAlive)
                    {
                        workerThread.Abort();
                    }
                }
                catch { }

                IsRunning = false;
                activeProcess = null;
                workerThread = null;
            }

            if (OnTaskStateChanged != null)
            {
                OnTaskStateChanged(false, "Idle");
            }
        }

        public int RunCommand(string fileName, string arguments)
        {
            if (isCancelling) return -1;

            Log(string.Format("Executing: {0} {1}", fileName, arguments), LogLevel.Info);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = fileName;
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            Process p = new Process();
            p.StartInfo = psi;

            p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    string trimmed = e.Data.Trim();
                    if (trimmed.Length > 0)
                    {
                        Log("  " + trimmed, LogLevel.Info);
                    }
                }
            };

            p.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    string trimmed = e.Data.Trim();
                    if (trimmed.Length > 0)
                    {
                        Log("  [ERR] " + trimmed, LogLevel.Warning);
                    }
                }
            };

            lock (lockObj)
            {
                activeProcess = p;
            }

            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                return p.ExitCode;
            }
            catch (Exception ex)
            {
                Log("Command execution failed: " + ex.Message, LogLevel.Error);
                return -1;
            }
            finally
            {
                lock (lockObj)
                {
                    if (activeProcess == p) activeProcess = null;
                }
                p.Close();
            }
        }

        public int RunPowerShell(string script)
        {
            string encoded = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "\\\"") + "\"";
            return RunCommand("powershell.exe", encoded);
        }

        public int RunCmd(string cmdCommand)
        {
            return RunCommand("cmd.exe", "/c " + cmdCommand);
        }

        private void KillProcessTree(int processId)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("taskkill.exe", string.Format("/PID {0} /T /F", processId));
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch { }
        }

        public bool ExportLog(string filePath)
        {
            try
            {
                lock (fullSessionLogs)
                {
                    File.WriteAllLines(filePath, fullSessionLogs.ToArray(), Encoding.UTF8);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ClearLogs()
        {
            lock (fullSessionLogs)
            {
                fullSessionLogs.Clear();
            }
        }

        public static void RelaunchAsAdmin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Application.ExecutablePath;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not elevate application: " + ex.Message,
                    "Elevation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
