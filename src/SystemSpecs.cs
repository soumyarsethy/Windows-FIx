using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using Microsoft.Win32;

namespace WinFixPro
{
    public class InstalledApp
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string InstallDate { get; set; }

        public InstalledApp()
        {
            Name = "";
            Version = "—";
            Publisher = "Unknown";
            InstallDate = "—";
        }
    }

    public class OfficeInfo
    {
        public bool IsInstalled { get; set; }
        public string ProductName { get; set; }
        public string Version { get; set; }
        public string Architecture { get; set; }
        public bool IsActivated { get; set; }
        public string LicenseStatus { get; set; }
        public string LicenseName { get; set; }
        public string LicenseChannel { get; set; }
        public string PartialKey { get; set; }
        public string InstallPath { get; set; }

        public OfficeInfo()
        {
            IsInstalled = false;
            ProductName = "Not Detected";
            Version = "";
            Architecture = "";
            IsActivated = false;
            LicenseStatus = "Not Installed";
            LicenseName = "";
            LicenseChannel = "";
            PartialKey = "";
            InstallPath = "";
        }
    }

    public class VolumeInfo
    {
        public string DriveLetter { get; set; }
        public string VolumeLabel { get; set; }
        public string FileSystem { get; set; }
        public double TotalGB { get; set; }
        public double FreeGB { get; set; }
        public double UsedGB { get; set; }
        public int PctUsed { get; set; }
    }

    public class GpuInfo
    {
        public string Name { get; set; }
        public string DriverVersion { get; set; }
        public long VramMB { get; set; }
    }

    public class SystemSpecs
    {
        // OS & Activation
        public string OsName { get; set; }
        public string OsVersion { get; set; }
        public string OsBuild { get; set; }
        public string OsArch { get; set; }
        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public string ActivationStatus { get; set; }
        public string ActivationChannel { get; set; }
        public string PartialKey { get; set; }
        public bool IsActivated { get; set; }

        // Microsoft Office
        public OfficeInfo Office { get; set; }

        // Installed Applications
        public List<InstalledApp> InstalledSoftware { get; set; }

        // CPU
        public string CpuName { get; set; }
        public int CpuCores { get; set; }
        public int CpuThreads { get; set; }
        public string CpuArch { get; set; }

        // RAM
        public double TotalRamGB { get; set; }
        public double FreeRamGB { get; set; }
        public double UsedRamGB { get; set; }
        public int RamPctUsed { get; set; }

        // GPU
        public List<GpuInfo> Gpus { get; set; }

        // Disks
        public List<string> PhysicalDisks { get; set; }
        public List<VolumeInfo> Volumes { get; set; }

        // Motherboard & BIOS
        public string Motherboard { get; set; }
        public string BiosVersion { get; set; }

        public SystemSpecs()
        {
            Office = new OfficeInfo();
            InstalledSoftware = new List<InstalledApp>();
            Gpus = new List<GpuInfo>();
            PhysicalDisks = new List<string>();
            Volumes = new List<VolumeInfo>();
            ActivationStatus = "Checking...";
            ActivationChannel = "";
            PartialKey = "";
        }

        public static SystemSpecs FetchLiveSpecs()
        {
            SystemSpecs specs = new SystemSpecs();

            // 1. Basic OS Info
            specs.ComputerName = Environment.MachineName;
            specs.UserName = Environment.UserName;
            specs.OsArch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            specs.CpuArch = Environment.Is64BitProcess ? "x64" : "x86";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Caption, Version, BuildNumber, TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        specs.OsName = mo["Caption"] != null ? mo["Caption"].ToString().Trim() : Environment.OSVersion.ToString();
                        specs.OsVersion = mo["Version"] != null ? mo["Version"].ToString() : "";
                        specs.OsBuild = mo["BuildNumber"] != null ? mo["BuildNumber"].ToString() : "";

                        double totalKB = Convert.ToDouble(mo["TotalVisibleMemorySize"]);
                        double freeKB = Convert.ToDouble(mo["FreePhysicalMemory"]);
                        specs.TotalRamGB = Math.Round(totalKB / (1024.0 * 1024.0), 1);
                        specs.FreeRamGB = Math.Round(freeKB / (1024.0 * 1024.0), 1);
                        specs.UsedRamGB = Math.Round(specs.TotalRamGB - specs.FreeRamGB, 1);
                        if (specs.TotalRamGB > 0)
                        {
                            specs.RamPctUsed = (int)Math.Round((specs.UsedRamGB / specs.TotalRamGB) * 100);
                        }
                    }
                }
            }
            catch
            {
                specs.OsName = Environment.OSVersion.ToString();
            }

            // 2. Windows Activation Status via WMI
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name, LicenseStatus, Description, PartialProductKey FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string name = mo["Name"] != null ? mo["Name"].ToString() : "";
                        if (name.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int status = Convert.ToInt32(mo["LicenseStatus"]);
                            specs.IsActivated = (status == 1);
                            specs.ActivationStatus = specs.IsActivated ? "Activated (Licensed)" :
                                (status == 0 ? "Unlicensed" : "Notification / Grace (" + status + ")");
                            specs.ActivationChannel = mo["Description"] != null ? mo["Description"].ToString() : "";
                            specs.PartialKey = mo["PartialProductKey"] != null ? mo["PartialProductKey"].ToString() : "";
                            break;
                        }
                    }
                }
            }
            catch
            {
                specs.ActivationStatus = "Unknown (Check via SLMGR)";
            }

            // 3. CPU Info
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        specs.CpuName = mo["Name"] != null ? mo["Name"].ToString().Trim() : "Generic CPU";
                        specs.CpuCores = mo["NumberOfCores"] != null ? Convert.ToInt32(mo["NumberOfCores"]) : Environment.ProcessorCount;
                        specs.CpuThreads = mo["NumberOfLogicalProcessors"] != null ? Convert.ToInt32(mo["NumberOfLogicalProcessors"]) : Environment.ProcessorCount;
                        break;
                    }
                }
            }
            catch
            {
                specs.CpuName = "Unknown CPU";
                specs.CpuCores = Environment.ProcessorCount;
                specs.CpuThreads = Environment.ProcessorCount;
            }

            // 4. GPU Info
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name, DriverVersion, AdapterRAM FROM Win32_VideoController"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string gName = mo["Name"] != null ? mo["Name"].ToString().Trim() : "";
                        if (!string.IsNullOrEmpty(gName))
                        {
                            GpuInfo gpu = new GpuInfo();
                            gpu.Name = gName;
                            gpu.DriverVersion = mo["DriverVersion"] != null ? mo["DriverVersion"].ToString() : "N/A";
                            long ramBytes = 0;
                            try { ramBytes = Convert.ToInt64(mo["AdapterRAM"]); } catch { }
                            gpu.VramMB = ramBytes / (1024 * 1024);
                            specs.Gpus.Add(gpu);
                        }
                    }
                }
            }
            catch { }

            // 5. Physical Disks
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Model, InterfaceType, Size FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string model = mo["Model"] != null ? mo["Model"].ToString().Trim() : "Generic Disk";
                        long szBytes = 0;
                        try { szBytes = Convert.ToInt64(mo["Size"]); } catch { }
                        long szGB = szBytes / (1024 * 1024 * 1024);
                        specs.PhysicalDisks.Add(string.Format("{0} ({1} GB)", model, szGB));
                    }
                }
            }
            catch { }

            // 6. Logical Volumes
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    DriveInfo d = drives[i];
                    if (d.IsReady)
                    {
                        VolumeInfo v = new VolumeInfo();
                        v.DriveLetter = d.Name;
                        v.VolumeLabel = string.IsNullOrEmpty(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel;
                        v.FileSystem = d.DriveFormat;
                        v.TotalGB = Math.Round(d.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                        v.FreeGB = Math.Round(d.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
                        v.UsedGB = Math.Round(v.TotalGB - v.FreeGB, 1);
                        if (v.TotalGB > 0)
                        {
                            v.PctUsed = (int)Math.Round((v.UsedGB / v.TotalGB) * 100);
                        }
                        specs.Volumes.Add(v);
                    }
                }
            }
            catch { }

            // 7. Motherboard & BIOS
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string mfg = mo["Manufacturer"] != null ? mo["Manufacturer"].ToString().Trim() : "";
                        string prod = mo["Product"] != null ? mo["Product"].ToString().Trim() : "";
                        specs.Motherboard = (mfg + " " + prod).Trim();
                        break;
                    }
                }
            }
            catch { }

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT SMBIOSBIOSVersion FROM Win32_BIOS"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        specs.BiosVersion = mo["SMBIOSBIOSVersion"] != null ? mo["SMBIOSBIOSVersion"].ToString().Trim() : "";
                        break;
                    }
                }
            }
            catch { }

            // 7. Microsoft Office & Activation Detection
            specs.Office = DetectOffice();

            // 8. Installed Software Scan
            specs.InstalledSoftware = FetchInstalledApps();

            return specs;
        }

        private static OfficeInfo DetectOffice()
        {
            OfficeInfo info = new OfficeInfo();

            // 1. Check Click-to-Run (Office 365, Office 2016 / 2019 / 2021 / 2024 C2R)
            try
            {
                using (RegistryKey c2rKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration"))
                {
                    if (c2rKey != null)
                    {
                        info.IsInstalled = true;
                        object prodObj = c2rKey.GetValue("ProductReleaseIds");
                        object verObj = c2rKey.GetValue("VersionToReport");
                        object platObj = c2rKey.GetValue("Platform");
                        object pathObj = c2rKey.GetValue("InstallationPath");

                        if (prodObj != null) info.ProductName = prodObj.ToString().Trim();
                        if (verObj != null) info.Version = verObj.ToString().Trim();
                        if (platObj != null) info.Architecture = platObj.ToString().Trim();
                        if (pathObj != null) info.InstallPath = pathObj.ToString().Trim();
                    }
                }
            }
            catch { }

            // 2. Check Standard MSI / Traditional Office Installations (16.0, 15.0, 14.0)
            if (!info.IsInstalled || string.IsNullOrEmpty(info.ProductName) || info.ProductName == "Not Detected")
            {
                string[] versions = new string[] { "16.0", "15.0", "14.0" };
                foreach (string ver in versions)
                {
                    try
                    {
                        string path = null;
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\" + ver + @"\Common\InstallRoot"))
                        {
                            if (key != null) path = key.GetValue("Path") as string;
                        }
                        if (string.IsNullOrEmpty(path))
                        {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Office\" + ver + @"\Common\InstallRoot"))
                            {
                                if (key != null) path = key.GetValue("Path") as string;
                            }
                        }

                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        {
                            info.IsInstalled = true;
                            info.InstallPath = path;
                            string wordPath = Path.Combine(path, "WINWORD.EXE");
                            string excelPath = Path.Combine(path, "EXCEL.EXE");
                            string testExe = File.Exists(wordPath) ? wordPath : (File.Exists(excelPath) ? excelPath : null);
                            if (testExe != null)
                            {
                                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(testExe);
                                info.ProductName = !string.IsNullOrEmpty(fvi.ProductName) ? fvi.ProductName : ("Microsoft Office " + ver);
                                info.Version = fvi.ProductVersion;
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }

            // 3. Fallback scan in Uninstall registry for friendly name if needed
            if (!info.IsInstalled || string.IsNullOrEmpty(info.ProductName) || info.ProductName == "Not Detected")
            {
                try
                {
                    string[] uninstallKeys = new string[]
                    {
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                        @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                    };
                    foreach (string uKey in uninstallKeys)
                    {
                        using (RegistryKey parent = Registry.LocalMachine.OpenSubKey(uKey))
                        {
                            if (parent == null) continue;
                            foreach (string subName in parent.GetSubKeyNames())
                            {
                                using (RegistryKey sub = parent.OpenSubKey(subName))
                                {
                                    if (sub == null) continue;
                                    string disp = sub.GetValue("DisplayName") as string;
                                    if (!string.IsNullOrEmpty(disp) && (disp.IndexOf("Microsoft Office", StringComparison.OrdinalIgnoreCase) >= 0 || disp.IndexOf("Microsoft 365", StringComparison.OrdinalIgnoreCase) >= 0))
                                    {
                                        if (disp.IndexOf("Proofing", StringComparison.OrdinalIgnoreCase) >= 0 || disp.IndexOf("Language Pack", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                                        info.IsInstalled = true;
                                        info.ProductName = disp;
                                        string ver = sub.GetValue("DisplayVersion") as string;
                                        if (!string.IsNullOrEmpty(ver)) info.Version = ver;
                                        break;
                                    }
                                }
                            }
                            if (info.IsInstalled && info.ProductName != "Not Detected") break;
                        }
                    }
                }
                catch { }
            }

            // 4. Activation Check via WMI (SoftwareLicensingProduct)
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name, Description, LicenseStatus, PartialProductKey FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        string name = mo["Name"] != null ? mo["Name"].ToString() : "";
                        if (name.IndexOf("Office", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int status = Convert.ToInt32(mo["LicenseStatus"]);
                            info.IsInstalled = true;
                            info.LicenseName = name;
                            info.LicenseChannel = mo["Description"] != null ? mo["Description"].ToString() : "";
                            info.PartialKey = mo["PartialProductKey"] != null ? mo["PartialProductKey"].ToString() : "";
                            info.IsActivated = (status == 1);
                            info.LicenseStatus = (status == 1) ? "Licensed (Activated)" :
                                (status == 0 ? "Unlicensed" : "Notification / Grace (" + status + ")");
                            break;
                        }
                    }
                }
            }
            catch { }

            // 5. Fallback Check via OfficeSoftwareProtectionProduct (Office 2010/2013)
            if (string.IsNullOrEmpty(info.PartialKey))
            {
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                        "SELECT Name, Description, LicenseStatus, PartialProductKey FROM OfficeSoftwareProtectionProduct WHERE PartialProductKey IS NOT NULL"))
                    {
                        foreach (ManagementObject mo in searcher.Get())
                        {
                            string name = mo["Name"] != null ? mo["Name"].ToString() : "";
                            int status = Convert.ToInt32(mo["LicenseStatus"]);
                            info.IsInstalled = true;
                            info.LicenseName = name;
                            info.LicenseChannel = mo["Description"] != null ? mo["Description"].ToString() : "";
                            info.PartialKey = mo["PartialProductKey"] != null ? mo["PartialProductKey"].ToString() : "";
                            info.IsActivated = (status == 1);
                            info.LicenseStatus = (status == 1) ? "Licensed (Activated)" :
                                (status == 0 ? "Unlicensed" : "Notification / Grace (" + status + ")");
                            break;
                        }
                    }
                }
                catch { }
            }

            if (info.IsInstalled)
            {
                if (string.IsNullOrEmpty(info.ProductName) || info.ProductName == "Not Detected")
                {
                    if (!string.IsNullOrEmpty(info.LicenseName))
                        info.ProductName = info.LicenseName;
                    else
                        info.ProductName = "Microsoft Office (Installed)";
                }
                if (string.IsNullOrEmpty(info.LicenseStatus))
                {
                    info.LicenseStatus = "Installed (License details available via OSPP)";
                }
            }

            return info;
        }

        public static List<InstalledApp> FetchInstalledApps()
        {
            List<InstalledApp> list = new List<InstalledApp>();
            Dictionary<string, bool> seen = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            string[] subPaths = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string subPath in subPaths)
            {
                ScanUninstallKey(RegistryHive.LocalMachine, RegistryView.Registry64, subPath, list, seen);
                ScanUninstallKey(RegistryHive.LocalMachine, RegistryView.Registry32, subPath, list, seen);
            }

            ScanUninstallKey(RegistryHive.CurrentUser, RegistryView.Default, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", list, seen);

            list.Sort(delegate(InstalledApp a, InstalledApp b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        private static void ScanUninstallKey(RegistryHive hive, RegistryView view, string subPath, List<InstalledApp> list, Dictionary<string, bool> seen)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view))
                {
                    using (RegistryKey parent = baseKey.OpenSubKey(subPath))
                    {
                        if (parent == null) return;
                        foreach (string subName in parent.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey appKey = parent.OpenSubKey(subName))
                                {
                                    if (appKey == null) continue;

                                    string name = appKey.GetValue("DisplayName") as string;
                                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(name.Trim())) continue;
                                    name = name.Trim();

                                    object sysComp = appKey.GetValue("SystemComponent");
                                    if (sysComp != null && Convert.ToInt32(sysComp) == 1) continue;

                                    object parentKey = appKey.GetValue("ParentKeyName");
                                    if (parentKey != null && !string.IsNullOrEmpty(parentKey.ToString())) continue;

                                    if (seen.ContainsKey(name)) continue;
                                    seen[name] = true;

                                    string version = appKey.GetValue("DisplayVersion") as string;
                                    string publisher = appKey.GetValue("Publisher") as string;
                                    string installDate = appKey.GetValue("InstallDate") as string;

                                    InstalledApp app = new InstalledApp();
                                    app.Name = name;
                                    app.Version = !string.IsNullOrEmpty(version) ? version.Trim() : "—";
                                    app.Publisher = !string.IsNullOrEmpty(publisher) ? publisher.Trim() : "Unknown";

                                    if (!string.IsNullOrEmpty(installDate) && installDate.Length == 8)
                                    {
                                        app.InstallDate = string.Format("{0}-{1}-{2}",
                                            installDate.Substring(0, 4),
                                            installDate.Substring(4, 2),
                                            installDate.Substring(6, 2));
                                    }
                                    else
                                    {
                                        app.InstallDate = !string.IsNullOrEmpty(installDate) ? installDate : "—";
                                    }

                                    list.Add(app);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        public string ToFormattedReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("==================================================================");
            sb.AppendLine(string.Format("{0} {1} - System Hardware & Windows Specification Report", AppMeta.AppName, AppMeta.Version));
            sb.AppendLine("Developed by " + AppMeta.Developer);
            sb.AppendLine("Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("==================================================================");
            sb.AppendLine();

            sb.AppendLine("[OPERATING SYSTEM & LICENSE]");
            sb.AppendLine("  OS Edition:        " + OsName);
            sb.AppendLine("  Build Number:      " + OsBuild + " (" + OsVersion + ")");
            sb.AppendLine("  Architecture:      " + OsArch);
            sb.AppendLine("  Computer Name:     " + ComputerName);
            sb.AppendLine("  Current User:      " + UserName);
            sb.AppendLine("  Activation Status: " + ActivationStatus);
            if (!string.IsNullOrEmpty(ActivationChannel))
            {
                sb.AppendLine("  License Channel:   " + ActivationChannel);
            }
            if (!string.IsNullOrEmpty(PartialKey))
            {
                sb.AppendLine("  Partial Key:       *****-" + PartialKey);
            }
            sb.AppendLine();

            sb.AppendLine("[MICROSOFT OFFICE & ACTIVATION]");
            if (Office != null && Office.IsInstalled)
            {
                sb.AppendLine("  Product Name:      " + Office.ProductName);
                sb.AppendLine("  Version:           " + (string.IsNullOrEmpty(Office.Version) ? "N/A" : Office.Version));
                sb.AppendLine("  Activation State:  " + Office.LicenseStatus);
                if (!string.IsNullOrEmpty(Office.LicenseChannel))
                    sb.AppendLine("  License Channel:   " + Office.LicenseChannel);
                if (!string.IsNullOrEmpty(Office.PartialKey))
                    sb.AppendLine("  Partial Key:       *****-" + Office.PartialKey);
                if (!string.IsNullOrEmpty(Office.InstallPath))
                    sb.AppendLine("  Install Directory: " + Office.InstallPath);
            }
            else
            {
                sb.AppendLine("  No active Microsoft Office suite installation detected.");
            }
            sb.AppendLine();

            sb.AppendLine("[PROCESSOR (CPU)]");
            sb.AppendLine("  Processor:         " + CpuName);
            sb.AppendLine(string.Format("  Cores & Threads:   {0} Physical Cores / {1} Logical Threads", CpuCores, CpuThreads));
            sb.AppendLine("  Architecture:      " + CpuArch);
            sb.AppendLine();

            sb.AppendLine("[MEMORY (RAM)]");
            sb.AppendLine(string.Format("  Total Memory:      {0:F1} GB", TotalRamGB));
            sb.AppendLine(string.Format("  Used Memory:       {0:F1} GB ({1}% in use)", UsedRamGB, RamPctUsed));
            sb.AppendLine(string.Format("  Free Memory:       {0:F1} GB", FreeRamGB));
            sb.AppendLine();

            sb.AppendLine("[GRAPHICS CARDS (GPU)]");
            if (Gpus.Count == 0)
            {
                sb.AppendLine("  No discrete or integrated video controller detected.");
            }
            else
            {
                for (int i = 0; i < Gpus.Count; i++)
                {
                    sb.AppendLine(string.Format("  GPU #{0}:            {1}", i + 1, Gpus[i].Name));
                    sb.AppendLine(string.Format("    VRAM:            {0} MB", Gpus[i].VramMB));
                    sb.AppendLine(string.Format("    Driver Version:  {0}", Gpus[i].DriverVersion));
                }
            }
            sb.AppendLine();

            sb.AppendLine("[STORAGE DRIVES & DISK SPACE]");
            if (PhysicalDisks.Count > 0)
            {
                sb.AppendLine("  Physical Disks:");
                for (int i = 0; i < PhysicalDisks.Count; i++)
                {
                    sb.AppendLine("    - " + PhysicalDisks[i]);
                }
            }
            sb.AppendLine("  Logical Volumes:");
            for (int i = 0; i < Volumes.Count; i++)
            {
                VolumeInfo v = Volumes[i];
                sb.AppendLine(string.Format("    - {0} [{1}]: {2:F1} GB Free / {3:F1} GB Total ({4}% used, {5})",
                    v.DriveLetter, v.VolumeLabel, v.FreeGB, v.TotalGB, v.PctUsed, v.FileSystem));
            }
            sb.AppendLine();

            sb.AppendLine("[MOTHERBOARD & BIOS]");
            sb.AppendLine("  Motherboard:       " + (string.IsNullOrEmpty(Motherboard) ? "Generic / OEM" : Motherboard));
            sb.AppendLine("  BIOS Version:      " + (string.IsNullOrEmpty(BiosVersion) ? "Standard" : BiosVersion));
            sb.AppendLine();

            sb.AppendLine(string.Format("[INSTALLED SOFTWARE INVENTORY ({0} APPLICATIONS)]", InstalledSoftware != null ? InstalledSoftware.Count : 0));
            if (InstalledSoftware != null && InstalledSoftware.Count > 0)
            {
                for (int i = 0; i < InstalledSoftware.Count; i++)
                {
                    InstalledApp app = InstalledSoftware[i];
                    sb.AppendLine(string.Format("  {0,3}. {1,-40} | Version: {2,-15} | Publisher: {3}",
                        i + 1,
                        app.Name.Length > 40 ? app.Name.Substring(0, 37) + "..." : app.Name,
                        app.Version,
                        app.Publisher));
                }
            }
            else
            {
                sb.AppendLine("  No third-party applications found in uninstall registry.");
            }
            sb.AppendLine();

            sb.AppendLine("==================================================================");
            sb.AppendLine(string.Format("Report generated by {0} {1} | Developed by {2}", AppMeta.AppName, AppMeta.Version, AppMeta.Developer));
            sb.AppendLine("==================================================================");

            return sb.ToString();
        }
    }
}
