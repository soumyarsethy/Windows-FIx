# WinFixPro - Portable Windows Repair & Optimization Toolkit (v2.7)

> **Developed by Soumya Ranjan Sethy**  
> *A lightweight, portable, zero-install toolkit for troubleshooting and repairing Windows systems.*

**WinFixPro** is a standalone, lightweight (~85 KB), portable utility designed for IT technicians, system administrators, and everyday users to diagnose, repair, and optimize Windows systems.

No installation, no third-party runtimes (Python, Node, or external dependencies) are required. Just copy **`WinFixPro.exe`** onto any USB flash drive and run it on any Windows computer.

---

## 🚀 Key Highlights

- **100% Portable**: Single-file executable (`WinFixPro.exe`). Run directly from a USB stick, external drive, or folder.
- **Universal Windows Compatibility**: Runs natively on **Windows 11, Windows 10, Windows 8.1, and Windows 7** (both 64-bit and 32-bit).
- **Zero Runtime Dependencies**: Built with native Windows .NET Framework 4.0/4.8 components pre-installed on every modern Windows system.
- **Modern Dark UI**: Fluid, high-DPI aware dark interface with live logging console, categorized fix cards, and visual progress tracking.
- **Live Activity Console**: Streams real-time command output line-by-line with color-coded severity (Info, Success, Warning, Error) and 1-click log export.
- **Task Safety & Control**: Includes a "Cancel Task" button to abort long-running tasks, plus pre-repair System Restore Point creation.
- **Administrator Elevation Detector**: Automatically detects non-elevated user mode and offers a 1-click `⚡ Relaunch as Admin` button.

---

## 🛠️ Repair Categories & Capabilities

### ⚡ 1. 1-Click Master Repair Wizard
Automates the most critical repair sequence in a single click:
1. **Flush & Re-register DNS**
2. **Reset Winsock Catalog**
3. **Clean Temporary & Junk Files**
4. **Fix Stuck Print Spooler Queue**
5. **Unblock Malware-Restricted System Tools**
6. **Reset Windows Update & BITS Components**
7. **Resync Windows Clock & Time Services**
8. **DISM Component Store Health Check & Restore**
9. **SFC System File Scannow**
10. **SSD TRIM & Drive Optimization**

---

### 🛡️ 2. System Health & Integrity
- **SFC System File Checker (`sfc /scannow`)**: Scans protected Windows system files and replaces corrupted or missing binaries with official cached copies.
- **DISM Component Store Repair (`dism /restorehealth`)**: Repairs corrupted Windows component store files using Windows Update or local servicing sources.
- **DISM Quick Health Check (`dism /checkhealth` & `/scanhealth`)**: Quickly verifies whether component store corruption exists without touching files.
- **Create System Restore Point**: Automatically turns on restore protection for drive `C:` and captures an instant restore snapshot (`WinFixPro_PreRepair`).
- **Check Disk (`chkdsk C: /scan`)**: Online NTFS integrity check without requiring a system reboot.
- **Rebuild Icon & Thumbnail Cache**: Kills explorer, wipes corrupted `IconCache.db` and thumbnail databases, and restarts the shell.

---

### 🌐 3. Network & Internet Repair
- **Flush & Register DNS (`ipconfig /flushdns`)**: Clears stale resolver records to fix "Site Cannot Be Reached" errors.
- **Reset Winsock Catalog (`netsh winsock reset`)**: Fixes corrupted Layered Service Providers (LSP) and socket connectivity issues.
- **Reset TCP/IP Protocol Stack (`netsh int ip reset`)**: Restores original IPv4/IPv6 networking parameters.
- **Release & Renew IP Address (`ipconfig /release` & `/renew`)**: Requests fresh DHCP lease from your router.
- **Reset Windows Firewall Rules (`netsh advfirewall reset`)**: Restores factory firewall rules, clearing rogue malware blocking.
- **Restart All Network Adapters**: Power-cycles physical Wi-Fi and Ethernet adapters via PowerShell.
- **Network Diagnostic Ping Test**: Live ICMP latency tests to Gateway, Google DNS (`8.8.8.8`), Cloudflare (`1.1.1.1`), and web domain DNS resolutions.

---

### 🔄 4. Windows Update & Store Fixes
- **Reset Windows Update Components**: Stops `wuauserv`, `bits`, `cryptSvc`, and `msiserver`; backs up & purges corrupted `SoftwareDistribution` and `catroot2` cache directories; restarts services.
- **Reset Microsoft Store Cache (`wsreset.exe`)**: Clears corrupted Windows Store cache without wiping installed applications.
- **Re-register All Built-in Windows Apps**: PowerShell AppX re-registration for Start Menu, Taskbar, Settings, and default apps.
- **Restart Windows Explorer**: Instantly restarts `explorer.exe` to resolve frozen taskbars or black screens.
- **Rebuild Windows Search Index**: Restarts Windows Search (`WSearch`) service to resolve search bar freezes.

---

### ⚙️ 5. Windows Services & Hardware
- **Fix Stuck Print Spooler Queue**: Stops the `Spooler` service, wipes jammed `.spl` and `.shd` print spool files, and restarts the printer engine.
- **Resync & Fix Windows Clock**: Re-registers the `w32time` service, binds to `time.windows.com`, and forces an immediate NTP time synchronization.
- **Restart Windows Audio Engine**: Restarts `Audiosrv` and `AudioEndpointBuilder` to recover from silent audio output drops.
- **Unblock Restricted System Tools**: Removes malware registry policies that disable Task Manager (`DisableTaskMgr`), Registry Editor (`DisableRegistryTools`), or Command Prompt (`DisableCMD`).

---

### 🧹 6. Disk Cleanup & Optimization
- **Clean Temporary & Junk Files**: Empties `%TEMP%`, `C:\Windows\Temp`, error logs, and dump files, recovering gigabytes of space.
- **Optimize & TRIM All Drives (`defrag C: /O`)**: Executes SSD TRIM commands to restore flash write performance and optimizes HDDs.
- **Clear Windows Error Reporting (WER)**: Deletes stale crash logs in `ReportArchive` and `ReportQueue`.
- **Launch Windows Disk Cleanup (`cleanmgr.exe`)**: Opens elevated native cleanup for drive `C:`.

---

### 📊 7. Diagnostics & System Reports
- **Generate Laptop Battery Health Report (`powercfg /batteryreport`)**: Creates an extensive HTML battery degradation and life expectancy report and opens it in your default web browser.
- **Export Full System Diagnostic Summary**: Gathers OS build, processor specs, total/free disk space on all partitions, and saves a text report directly to your Desktop.
- **Scan Event Viewer for Critical Errors**: Queries the Windows System log for critical and error entries recorded in the last 24 hours.

---

## 📁 File Structure

```text
E:\Backup\Windows FIx\
├── WinFixPro.exe          <-- The standalone portable software (~85 KB)
├── Run-As-Admin.bat       <-- Quick-launcher to run with full Admin rights
├── build.bat              <-- 1-click build script (recompiles in ~1 second)
├── app.manifest           <-- Windows 7-11 compatibility & PerMonitorV2 DPI manifest
├── app.ico                <-- Embedded multi-resolution application icon
├── README.md              <-- Complete user guide & documentation
└── src/
    ├── Program.cs         <-- Entry point and global exception trapping
    ├── MainForm.cs        <-- Dark GUI layout, sidebar navigation & real-time console
    ├── FixEngine.cs       <-- Background process execution, cancellation & live logging
    ├── FixModules.cs      <-- All categorized repair modules and actions
    └── Theme.cs           <-- Modern controls, button styles & dark color palette
```

---

## 💻 How to Use on Multiple Systems

1. Copy **`WinFixPro.exe`** (or the entire folder) to any **USB flash drive**.
2. Plug the USB flash drive into the target Windows computer.
3. Right-click **`WinFixPro.exe`** and select **Run as administrator** (or double-click `Run-As-Admin.bat`).
4. Select **⚡ 1-Click Repair** for automated health check and tuneup, or browse specific categories on the sidebar to fix individual issues.
5. Watch the live terminal at the bottom for progress. When finished, you can click **💾 Save Log** to keep a record of the repairs.

---

## 🔨 How to Recompile (Optional)

If you ever modify the source code in `src/`, simply double-click **`build.bat`**. It uses the built-in Microsoft C# compiler (`csc.exe`) located in `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`. No IDE or external downloads are necessary!
