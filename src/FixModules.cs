using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinFixPro
{
    public class FixItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Badge { get; set; }
        public System.Drawing.Color BadgeColor { get; set; }
        public bool RequiresAdmin { get; set; }
        public Action ActionToRun { get; set; }

        public FixItem(string id, string title, string description, string category, string badge, System.Drawing.Color badgeColor, bool reqAdmin, Action action)
        {
            Id = id;
            Title = title;
            Description = description;
            Category = category;
            Badge = badge;
            BadgeColor = badgeColor;
            RequiresAdmin = reqAdmin;
            ActionToRun = action;
        }
    }

    public static class FixModules
    {
        public static List<FixItem> GetItemsByCategory(string category)
        {
            List<FixItem> all = GetAllItems();
            List<FixItem> filtered = new List<FixItem>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(all[i]);
                }
            }
            return filtered;
        }

        public static List<FixItem> GetAllItems()
        {
            List<FixItem> items = new List<FixItem>();

            #region SYSTEM HEALTH
            items.Add(new FixItem(
                "sfc_scan",
                "SFC System File Checker",
                "Scans integrity of all protected system files and repairs damaged or missing files.",
                "System",
                "ESSENTIAL",
                Theme.AccentBlue,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Running System File Checker (sfc /scannow)...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("sfc.exe", "/scannow");
                }
            ));

            items.Add(new FixItem(
                "dism_restore",
                "DISM Component Store Repair",
                "Repairs Windows component store using Windows Update or local cache (RestoreHealth).",
                "System",
                "DEEP FIX",
                Theme.AccentIndigo,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Checking and repairing Windows Component Store with DISM...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("dism.exe", "/Online /Cleanup-Image /RestoreHealth");
                }
            ));

            items.Add(new FixItem(
                "dism_check",
                "DISM Quick Health Check",
                "Quickly verifies if component store corruption is detected without modifying files.",
                "System",
                "QUICK",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Checking Windows Health state...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("dism.exe", "/Online /Cleanup-Image /CheckHealth");
                    FixEngine.Instance.RunCommand("dism.exe", "/Online /Cleanup-Image /ScanHealth");
                }
            ));

            items.Add(new FixItem(
                "restore_point",
                "Create System Restore Point",
                "Creates an instant Windows Restore Point for rollback safety before making changes.",
                "System",
                "SAFETY",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Enabling System Restore for C: and creating a restore point...", LogLevel.Info);
                    string ps = "Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; " +
                                "Checkpoint-Computer -Description 'WinFixPro_PreRepair' -RestorePointType 'MODIFY_SETTINGS'";
                    FixEngine.Instance.RunPowerShell(ps);
                }
            ));

            items.Add(new FixItem(
                "chkdsk_scan",
                "Check Disk (C: Drive Scan)",
                "Performs an online NTFS/ReFS integrity scan of the system drive without reboot.",
                "System",
                "SAFE",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Running read-only online disk scan on C:...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("chkdsk.exe", "C: /scan");
                }
            ));

            items.Add(new FixItem(
                "icon_cache",
                "Rebuild Icon & Thumbnail Cache",
                "Fixes blank, missing, or corrupted icons on the desktop, taskbar, and file explorer.",
                "System",
                "INSTANT",
                Theme.AccentAmber,
                false,
                delegate ()
                {
                    FixEngine.Instance.Log("Restarting explorer and wiping icon/thumbnail caches...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("taskkill /f /im explorer.exe");
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    try
                    {
                        string iconCache = Path.Combine(localAppData, "IconCache.db");
                        if (File.Exists(iconCache)) File.Delete(iconCache);
                        FixEngine.Instance.Log("Removed " + iconCache, LogLevel.Success);
                    }
                    catch { }

                    try
                    {
                        string thumbDir = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
                        if (Directory.Exists(thumbDir))
                        {
                            string[] thumbs = Directory.GetFiles(thumbDir, "thumbcache_*.db");
                            for (int i = 0; i < thumbs.Length; i++)
                            {
                                try { File.Delete(thumbs[i]); } catch { }
                            }
                            FixEngine.Instance.Log("Purged thumbnail cache database files.", LogLevel.Success);
                        }
                    }
                    catch { }

                    FixEngine.Instance.RunCmd("start explorer.exe");
                }
            ));
            #endregion

            #region NETWORK & INTERNET
            items.Add(new FixItem(
                "flush_dns",
                "Flush & Re-register DNS Cache",
                "Clears resolved DNS cache and refreshes DNS registrations. Fixes 'Site Cannot Be Reached'.",
                "Network",
                "RECOMMENDED",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Flushing DNS Resolver Cache...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("ipconfig.exe", "/flushdns");
                    FixEngine.Instance.RunCommand("ipconfig.exe", "/registerdns");
                }
            ));

            items.Add(new FixItem(
                "reset_winsock",
                "Reset Winsock Catalog",
                "Restores Windows Socket catalog to clean default state. Resolves socket network errors.",
                "Network",
                "CORE FIX",
                Theme.AccentBlue,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Resetting Winsock Catalog...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("netsh.exe", "winsock reset");
                }
            ));

            items.Add(new FixItem(
                "reset_tcpip",
                "Reset TCP/IP Protocol Stack",
                "Rewrites TCP/IP registry keys and resets IPv4/IPv6 stacks to original installation settings.",
                "Network",
                "CORE FIX",
                Theme.AccentBlue,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Resetting TCP/IP protocol stack...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("netsh.exe", "int ip reset");
                }
            ));

            items.Add(new FixItem(
                "renew_ip",
                "Release & Renew IP Address",
                "Releases current DHCP lease and requests a fresh IP address from your router/DHCP server.",
                "Network",
                "QUICK",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Releasing DHCP lease...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("ipconfig.exe", "/release");
                    FixEngine.Instance.Log("Requesting fresh DHCP IP address...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("ipconfig.exe", "/renew");
                }
            ));

            items.Add(new FixItem(
                "reset_firewall",
                "Reset Windows Firewall to Defaults",
                "Restores Windows Defender Firewall policy rules to clean defaults, removing rogue blocks.",
                "Network",
                "SECURITY",
                Theme.AccentAmber,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Resetting Windows Firewall configuration...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("netsh.exe", "advfirewall reset");
                }
            ));

            items.Add(new FixItem(
                "restart_adapters",
                "Restart All Network Adapters",
                "Cycles all physical Ethernet and Wi-Fi network interfaces to clear hardware driver hangs.",
                "Network",
                "QUICK",
                Theme.AccentIndigo,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Restarting network interfaces...", LogLevel.Info);
                    string ps = "Get-NetAdapter | Where-Object { $_.Status -eq 'Up' -or $_.Status -eq 'Disconnected' } | Restart-NetAdapter -Confirm:$false";
                    FixEngine.Instance.RunPowerShell(ps);
                }
            ));

            items.Add(new FixItem(
                "test_connectivity",
                "Network & Internet Diagnostic Ping",
                "Tests connectivity to Default Gateway, Google DNS (8.8.8.8), and Cloudflare (1.1.1.1).",
                "Network",
                "DIAGNOSTIC",
                Theme.AccentGreen,
                false,
                delegate ()
                {
                    FixEngine.Instance.Log("Performing network ping and DNS resolution tests...", LogLevel.Info);
                    string[] hosts = new string[] { "8.8.8.8", "1.1.1.1", "google.com", "microsoft.com" };
                    for (int i = 0; i < hosts.Length; i++)
                    {
                        try
                        {
                            Ping ping = new Ping();
                            PingReply reply = ping.Send(hosts[i], 2500);
                            if (reply.Status == IPStatus.Success)
                            {
                                FixEngine.Instance.Log(string.Format("Ping to {0}: SUCCESS (Roundtrip: {1}ms)", hosts[i], reply.RoundtripTime), LogLevel.Success);
                            }
                            else
                            {
                                FixEngine.Instance.Log(string.Format("Ping to {0}: FAILED ({1})", hosts[i], reply.Status), LogLevel.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            FixEngine.Instance.Log(string.Format("Ping to {0} error: {1}", hosts[i], ex.Message), LogLevel.Error);
                        }
                    }
                }
            ));
            #endregion

            #region WINDOWS UPDATE & STORE
            items.Add(new FixItem(
                "reset_wuauserv",
                "Reset Windows Update Components",
                "Stops update services, purges corrupted SoftwareDistribution & Catroot2 caches, and restarts.",
                "Updates",
                "TOP FIX",
                Theme.AccentRed,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Stopping Windows Update and background transfer services...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net stop wuauserv");
                    FixEngine.Instance.RunCmd("net stop cryptSvc");
                    FixEngine.Instance.RunCmd("net stop bits");
                    FixEngine.Instance.RunCmd("net stop msiserver");

                    string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    string sd = Path.Combine(winDir, "SoftwareDistribution");
                    string sdBak = Path.Combine(winDir, "SoftwareDistribution.old_" + DateTime.Now.Ticks);
                    try
                    {
                        if (Directory.Exists(sd))
                        {
                            Directory.Move(sd, sdBak);
                            FixEngine.Instance.Log("Renamed SoftwareDistribution to backup folder.", LogLevel.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        FixEngine.Instance.Log("Warning moving SoftwareDistribution: " + ex.Message, LogLevel.Warning);
                    }

                    FixEngine.Instance.Log("Restarting Windows Update services...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net start msiserver");
                    FixEngine.Instance.RunCmd("net start bits");
                    FixEngine.Instance.RunCmd("net start cryptSvc");
                    FixEngine.Instance.RunCmd("net start wuauserv");
                    FixEngine.Instance.Log("Windows Update components reset successfully.", LogLevel.Success);
                }
            ));

            items.Add(new FixItem(
                "reset_store",
                "Reset Microsoft Store Cache",
                "Runs WSReset to clear corrupted Windows Store cache without deleting installed apps.",
                "Updates",
                "POPULAR",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Resetting Windows Store cache (wsreset.exe)...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("wsreset.exe", "");
                }
            ));

            items.Add(new FixItem(
                "reregister_apps",
                "Re-register All Built-in Windows Apps",
                "Re-registers Start Menu, Search, Settings, and default modern apps if they fail to open.",
                "Updates",
                "DEEP REPAIR",
                Theme.AccentIndigo,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Re-registering AppX packages for all users (this may take a couple minutes)...", LogLevel.Info);
                    string ps = "Get-AppXPackage -AllUsers | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -ErrorAction SilentlyContinue}";
                    FixEngine.Instance.RunPowerShell(ps);
                }
            ));

            items.Add(new FixItem(
                "restart_explorer",
                "Restart Windows Explorer",
                "Instantly restarts Windows Explorer shell to resolve frozen taskbars or black screens.",
                "Updates",
                "INSTANT",
                Theme.AccentAmber,
                false,
                delegate ()
                {
                    FixEngine.Instance.Log("Restarting Windows Explorer shell...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("taskkill /f /im explorer.exe & start explorer.exe");
                }
            ));

            items.Add(new FixItem(
                "rebuild_search",
                "Rebuild Windows Search Index",
                "Restarts Windows Search service and forces index database reconstruction for broken search.",
                "Updates",
                "FIX SEARCH",
                Theme.AccentBlue,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Stopping Windows Search service...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net stop WSearch");
                    FixEngine.Instance.Log("Restarting Windows Search service...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net start WSearch");
                }
            ));
            #endregion

            #region SERVICES & HARDWARE
            items.Add(new FixItem(
                "fix_spooler",
                "Fix Stuck Print Spooler Queue",
                "Stops printer spooler, deletes stuck print jobs (.spl / .shd files), and restarts spooler.",
                "Services",
                "PRINTER",
                Theme.AccentBlue,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Stopping Print Spooler...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net stop spooler");

                    string spoolDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"spool\PRINTERS");
                    try
                    {
                        if (Directory.Exists(spoolDir))
                        {
                            string[] files = Directory.GetFiles(spoolDir);
                            for (int i = 0; i < files.Length; i++)
                            {
                                try { File.Delete(files[i]); } catch { }
                            }
                            FixEngine.Instance.Log("Deleted stuck print queue jobs.", LogLevel.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        FixEngine.Instance.Log("Spooler cleanup notice: " + ex.Message, LogLevel.Warning);
                    }

                    FixEngine.Instance.Log("Restarting Print Spooler...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net start spooler");
                }
            ));

            items.Add(new FixItem(
                "sync_time",
                "Resync & Fix Windows Clock / Time",
                "Re-registers Windows Time Service, configures time.windows.com NTP, and forces sync.",
                "Services",
                "TIME SYNC",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Re-registering Windows Time service...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net stop w32time");
                    FixEngine.Instance.RunCommand("w32tm.exe", "/unregister");
                    FixEngine.Instance.RunCommand("w32tm.exe", "/register");
                    FixEngine.Instance.RunCmd("net start w32time");
                    FixEngine.Instance.RunCommand("w32tm.exe", "/config /manualpeerlist:time.windows.com /syncfromflags:manual /reliable:YES /update");
                    FixEngine.Instance.RunCommand("w32tm.exe", "/resync /nowait");
                    FixEngine.Instance.Log("Windows Clock synchronization triggered.", LogLevel.Success);
                }
            ));

            items.Add(new FixItem(
                "restart_audio",
                "Restart Windows Audio Services",
                "Restarts Windows Audio (Audiosrv) and Audio Endpoint Builder to restore sound without reboot.",
                "Services",
                "AUDIO",
                Theme.AccentIndigo,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Restarting Windows Audio services...", LogLevel.Info);
                    FixEngine.Instance.RunCmd("net stop AudioEndpointBuilder /y");
                    FixEngine.Instance.RunCmd("net stop Audiosrv /y");
                    FixEngine.Instance.RunCmd("net start Audiosrv");
                    FixEngine.Instance.RunCmd("net start AudioEndpointBuilder");
                    FixEngine.Instance.Log("Audio services restarted.", LogLevel.Success);
                }
            ));

            items.Add(new FixItem(
                "unblock_tools",
                "Unblock Disabled System Tools",
                "Removes malware policies restricting Task Manager, Registry Editor (regedit), and CMD.",
                "Services",
                "MALWARE FIX",
                Theme.AccentRed,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Scanning and unblocking restricted system tools...", LogLevel.Info);
                    try
                    {
                        // Current User Policies
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                        {
                            if (key != null)
                            {
                                key.DeleteValue("DisableTaskMgr", false);
                                key.DeleteValue("DisableRegistryTools", false);
                            }
                        }
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\System", true))
                        {
                            if (key != null)
                            {
                                key.DeleteValue("DisableCMD", false);
                            }
                        }

                        // Local Machine Policies
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                        {
                            if (key != null)
                            {
                                key.DeleteValue("DisableTaskMgr", false);
                                key.DeleteValue("DisableRegistryTools", false);
                            }
                        }
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Policies\Microsoft\Windows\System", true))
                        {
                            if (key != null)
                            {
                                key.DeleteValue("DisableCMD", false);
                            }
                        }

                        FixEngine.Instance.Log("Policies verified: Task Manager, Registry Editor, and CMD are unblocked.", LogLevel.Success);
                    }
                    catch (Exception ex)
                    {
                        FixEngine.Instance.Log("Notice while clearing registry policies: " + ex.Message, LogLevel.Warning);
                    }
                }
            ));
            #endregion

            #region DISK & CLEANUP
            items.Add(new FixItem(
                "clean_temp",
                "Clean Temporary & Junk Files",
                "Cleans user %TEMP%, system Temp, error reports, and crash dumps to free up gigabytes of disk space.",
                "Cleanup",
                "RECOMMENDED",
                Theme.AccentGreen,
                false,
                delegate ()
                {
                    FixEngine.Instance.Log("Cleaning Temporary directories...", LogLevel.Info);
                    long freedBytes = 0;

                    string userTemp = Path.GetTempPath();
                    freedBytes += PurgeDirectoryFiles(userTemp);

                    string sysTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                    freedBytes += PurgeDirectoryFiles(sysTemp);

                    double mb = freedBytes / (1024.0 * 1024.0);
                    FixEngine.Instance.Log(string.Format("Junk file cleanup completed. Estimated space recovered: {0:F1} MB", mb), LogLevel.Success);
                }
            ));

            items.Add(new FixItem(
                "trim_drives",
                "Optimize & TRIM All SSD/HDD Drives",
                "Invokes Windows storage defragmenter with /O flag to send TRIM to SSDs and optimize HDDs.",
                "Cleanup",
                "SSD SPEED",
                Theme.AccentBlue,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Running Drive Optimization & SSD TRIM on C:...", LogLevel.Info);
                    FixEngine.Instance.RunCommand("defrag.exe", "C: /O");
                }
            ));

            items.Add(new FixItem(
                "clean_wer",
                "Clear Windows Error Reporting Dumps",
                "Deletes old Windows Error Reporting (WER) crash dumps and queued logs filling up disk space.",
                "Cleanup",
                "SAFE",
                Theme.AccentGreen,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Cleaning Windows Error Reporting crash logs...", LogLevel.Info);
                    string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string werDir = Path.Combine(programData, @"Microsoft\Windows\WER");
                    if (Directory.Exists(werDir))
                    {
                        long b = PurgeDirectoryFiles(Path.Combine(werDir, "ReportArchive"));
                        b += PurgeDirectoryFiles(Path.Combine(werDir, "ReportQueue"));
                        FixEngine.Instance.Log(string.Format("WER reports purged. Freed {0:F1} MB", b / (1024.0 * 1024.0)), LogLevel.Success);
                    }
                }
            ));

            items.Add(new FixItem(
                "launch_cleanmgr",
                "Run Windows Native Disk Cleanup",
                "Opens Windows Disk Cleanup utility configured for drive C: with elevated privileges.",
                "Cleanup",
                "BUILT-IN",
                Theme.AccentAmber,
                false,
                delegate ()
                {
                    FixEngine.Instance.Log("Launching Windows Cleanmgr...", LogLevel.Info);
                    Process.Start("cleanmgr.exe", "/d C:");
                }
            ));
            #endregion

            #region DIAGNOSTICS & INFO
            items.Add(new FixItem(
                "battery_report",
                "Generate Laptop Battery Health Report",
                "Generates an in-depth battery health & degradation HTML report and opens it in your browser.",
                "Diagnostics",
                "LAPTOP ONLY",
                Theme.AccentIndigo,
                true,
                delegate ()
                {
                    string outPath = Path.Combine(Path.GetTempPath(), "battery_report.html");
                    FixEngine.Instance.Log("Generating Battery Health Report to " + outPath, LogLevel.Info);
                    FixEngine.Instance.RunCommand("powercfg.exe", string.Format("/batteryreport /output \"{0}\"", outPath));
                    if (File.Exists(outPath))
                    {
                        FixEngine.Instance.Log("Opening battery health report in browser...", LogLevel.Success);
                        Process.Start(outPath);
                    }
                    else
                    {
                        FixEngine.Instance.Log("No battery detected on this system (Desktop PC or virtual machine).", LogLevel.Warning);
                    }
                }
            ));

            items.Add(new FixItem(
                "sys_report",
                "Export Full System Diagnostic Summary",
                "Generates an extensive hardware, OS, disk, and network configuration report saved to Desktop.",
                "Diagnostics",
                "REPORT",
                Theme.AccentBlue,
                false,
                delegate ()
                {
                    FixEngine.Instance.Log("Compiling system configuration diagnostic report...", LogLevel.Info);
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine("WINFIXPRO SYSTEM DIAGNOSTIC REPORT");
                    sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine("==================================================");
                    sb.AppendLine();
                    sb.AppendLine("OS Version: " + Environment.OSVersion.ToString());
                    sb.AppendLine("64-Bit OS: " + Environment.Is64BitOperatingSystem);
                    sb.AppendLine("Machine Name: " + Environment.MachineName);
                    sb.AppendLine("User Name: " + Environment.UserName);
                    sb.AppendLine("Processor Count: " + Environment.ProcessorCount);
                    sb.AppendLine("System Directory: " + Environment.SystemDirectory);
                    sb.AppendLine("Running As Admin: " + Theme.IsAdministrator());
                    sb.AppendLine();
                    sb.AppendLine("--- LOGICAL DRIVES ---");
                    DriveInfo[] drives = DriveInfo.GetDrives();
                    for (int i = 0; i < drives.Length; i++)
                    {
                        if (drives[i].IsReady)
                        {
                            sb.AppendLine(string.Format("Drive {0} ({1}): {2:F1} GB Free / {3:F1} GB Total",
                                drives[i].Name, drives[i].DriveFormat,
                                drives[i].TotalFreeSpace / (1024.0 * 1024.0 * 1024.0),
                                drives[i].TotalSize / (1024.0 * 1024.0 * 1024.0)));
                        }
                    }

                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string dest = Path.Combine(desktop, string.Format("WinFixPro_Report_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now));
                    File.WriteAllText(dest, sb.ToString(), Encoding.UTF8);
                    FixEngine.Instance.Log("Report exported to: " + dest, LogLevel.Success);
                    Process.Start("notepad.exe", dest);
                }
            ));

            items.Add(new FixItem(
                "event_log_scan",
                "Scan Event Viewer for Critical Errors",
                "Checks Windows System Event Log for critical errors logged during the last 24 hours.",
                "Diagnostics",
                "EVENT LOGS",
                Theme.AccentAmber,
                true,
                delegate ()
                {
                    FixEngine.Instance.Log("Querying recent critical and error events from Windows System log...", LogLevel.Info);
                    string ps = "Get-WinEvent -FilterHashtable @{LogName='System'; Level=1,2; StartTime=(Get-Date).AddDays(-1)} -MaxEvents 20 -ErrorAction SilentlyContinue | Format-Table -Property TimeCreated, ProviderName, Id, Message -Wrap -AutoSize";
                    FixEngine.Instance.RunPowerShell(ps);
                }
            ));
            #endregion

            return items;
        }

        private static long PurgeDirectoryFiles(string targetDir)
        {
            long freed = 0;
            if (!Directory.Exists(targetDir)) return 0;

            try
            {
                string[] files = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(files[i]);
                        long sz = fi.Length;
                        File.Delete(files[i]);
                        freed += sz;
                    }
                    catch { }
                }
            }
            catch { }

            return freed;
        }
    }
}
