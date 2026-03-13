using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.Win32;

using System.Runtime.Versioning;

namespace TcNo_Acc_Switcher_Installer;

[SupportedOSPlatform("windows")]
class Program
{
    private const string RequiredMinWebview = "98.0";
    private const string RequiredMinDesktopRuntime = "8.0.0";
    private const string RequiredMinAspCore = "8.0.0";
    private const string RequiredMinVc = "14.30.30704";

    private const string WRuntimeUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
    private const string DRuntimeUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.22/windowsdesktop-runtime-8.0.22-win-x64.exe";
    private const string ARuntimeUrl = "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/8.0.22/aspnetcore-runtime-8.0.22-win-x64.exe";
    private const string CRuntimeUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";

    static async Task Main(string[] args)
    {
        string selfPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        string operatingDir = Path.GetDirectoryName(selfPath) ?? "";

        if (args.Contains("finalizeupdate"))
        {
            int fromIdx = Array.IndexOf(args, "finalizeupdate") + 1;
            if (fromIdx < args.Length - 1)
            {
                FinalizeUpdate(args[fromIdx], args[fromIdx + 1]);
                return;
            }
        }

        Console.Title = "TcNo Account Switcher - Runtime installer";
        Console.WriteLine("Welcome to the TcNo Account Switcher - Runtime installer");
        Console.WriteLine("------------------------------------------------------------------------\n");

        if (args.Contains("vc"))
        {
            await VerifyVc(operatingDir);
            return;
        }

        if (args.Contains("net"))
        {
            await VerifyNet(operatingDir);
            if (args.Length > 2) LaunchTcnoProgram(args[^1], operatingDir);
            return;
        }

        Console.WriteLine("This script will make sure WebView 2, .NET Runtime 8.0, Desktop Runtime 8.0,");
        Console.WriteLine("ASP.NET Core Runtime 8.0, and Visual C++ Redistributable 2015 - 2019 are");
        Console.WriteLine("installed. If they are missing, they will be downloaded.\n");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();

        await VerifyNet(operatingDir);

        if (!args.Contains("nostart"))
        {
            LaunchTcnoProgram("TcNo-Acc-Switcher.exe", operatingDir);
        }
    }

    static void FinalizeUpdate(string from, string to)
    {
        Console.Title = "TcNo Account Switcher - Finalizing Update...";
        Console.WriteLine($"Finalizing update...\nFrom: {from}\nTo: {to}");

        if (!Directory.Exists(from))
        {
            Console.WriteLine("Source path does not exist.");
            return;
        }

        MoveRecursive(from, to);

        string exePath = Path.Combine(to, "TcNo-Acc-Switcher.exe");
        if (File.Exists(exePath))
        {
            Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = to, UseShellExecute = true });
        }
    }

    static void MoveRecursive(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (string file in Directory.GetFiles(from))
        {
            string dest = Path.Combine(to, Path.GetFileName(file));
            File.Delete(dest);
            File.Move(file, dest);
        }
        foreach (string dir in Directory.GetDirectories(from))
        {
            MoveRecursive(dir, Path.Combine(to, Path.GetFileName(dir)));
        }
        Directory.Delete(from, true);
    }

    static async Task VerifyNet(string operatingDir)
    {
        bool minWebviewMet = CheckWebView2();
        bool minDesktopMet = CheckDotNetRuntime("Microsoft.WindowsDesktop.App", RequiredMinDesktopRuntime);
        bool minAspMet = CheckDotNetRuntime("Microsoft.AspNetCore.App", RequiredMinAspCore);
        bool minVcMet = CheckVc();

        if (!minWebviewMet || !minDesktopMet || !minAspMet || !minVcMet)
        {
            Console.WriteLine("One or more runtimes are missing. Starting installation...");
            
            bool useWinget = IsWingetAvailable();
            string installerDir = Path.Combine(operatingDir, "runtime_installers");
            Directory.CreateDirectory(installerDir);

            if (!minWebviewMet) await InstallRuntime("WebView2", WRuntimeUrl, Path.Combine(installerDir, "WebView2Setup.exe"), null);
            if (!minDesktopMet) await InstallRuntime(".NET Desktop Runtime", DRuntimeUrl, Path.Combine(installerDir, "desktop_runtime.exe"), "Microsoft.DotNet.DesktopRuntime.8", useWinget);
            if (!minAspMet) await InstallRuntime("ASP.NET Core Runtime", ARuntimeUrl, Path.Combine(installerDir, "aspnet_runtime.exe"), "Microsoft.DotNet.AspNetCore.8", useWinget);
            if (!minVcMet) await InstallRuntime("VC++ Redistributable", CRuntimeUrl, Path.Combine(installerDir, "vc_redist.exe"), "Microsoft.VCRedist.2015+.x64", useWinget);
        }
        else
        {
            Console.WriteLine("All runtimes are already installed.");
        }
    }

    static async Task VerifyVc(string operatingDir)
    {
        if (!CheckVc())
        {
            string installerDir = Path.Combine(operatingDir, "runtime_installers");
            Directory.CreateDirectory(installerDir);
            await InstallRuntime("VC++ Redistributable", CRuntimeUrl, Path.Combine(installerDir, "vc_redist.exe"), "Microsoft.VCRedist.2015+.x64", IsWingetAvailable());
        }
    }

    static async Task InstallRuntime(string name, string url, string localPath, string? wingetId, bool useWinget = false)
    {
        if (useWinget && wingetId != null)
        {
            Console.WriteLine($"Installing {name} via winget...");
            if (RunCommand("winget", $"install --id {wingetId} --silent --accept-package-agreements --accept-source-agreements") == 0)
            {
                return;
            }
            Console.WriteLine($"Winget install failed for {name}, falling back to download.");
        }

        Console.WriteLine($"Downloading {name}...");
        using HttpClient client = new HttpClient();
        var data = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(localPath, data);

        Console.WriteLine($"Installing {name}...");
        ProcessStartInfo psi = new ProcessStartInfo(localPath, "/install /passive /norestart") { UseShellExecute = true };
        var p = Process.Start(psi);
        p?.WaitForExit();
    }

    static void LaunchTcnoProgram(string program, string operatingDir)
    {
        string exeName = program.EndsWith(".exe") ? program : $"{program}_main.exe";
        string fullPath = Path.Combine(operatingDir, exeName);
        if (File.Exists(fullPath))
        {
            Process.Start(new ProcessStartInfo(fullPath) { WorkingDirectory = operatingDir, UseShellExecute = true });
        }
    }

    static bool CheckWebView2()
    {
        string[] keys = { @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" };
        foreach (var keyPath in keys)
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath) ?? Registry.LocalMachine.OpenSubKey(keyPath);
            if (key?.GetValue("pv") is string version && CompareVersions(version, RequiredMinWebview) >= 0) return true;
        }
        return false;
    }

    static bool CheckDotNetRuntime(string runtimeName, string minVersion)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("dotnet", "--list-runtimes") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var process = Process.Start(psi);
            if (process == null) return false;
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Split(Environment.NewLine).Any(line => line.StartsWith(runtimeName) && CompareVersions(line.Split(' ')[1], minVersion) >= 0);
        }
        catch { return false; }
    }

    static bool CheckVc()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DevDiv\VC\Servicing\14.0\RuntimeMinimum");
        if (key?.GetValue("Version") is string version) return CompareVersions(version, RequiredMinVc) >= 0;
        return false;
    }

    static bool IsWingetAvailable() => RunCommand("cmd.exe", "/c winget --version") == 0;

    static int RunCommand(string cmd, string args)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(cmd, args) { CreateNoWindow = true, UseShellExecute = false };
            using var p = Process.Start(psi);
            p?.WaitForExit();
            return p?.ExitCode ?? -1;
        }
        catch { return -1; }
    }

    static int CompareVersions(string v1, string v2)
    {
        var v1Parts = v1.Split('.').Select(p => int.TryParse(new string(p.Where(char.IsDigit).ToArray()), out int i) ? i : 0).ToArray();
        var v2Parts = v2.Split('.').Select(p => int.TryParse(new string(p.Where(char.IsDigit).ToArray()), out int i) ? i : 0).ToArray();
        for (int i = 0; i < Math.Min(v1Parts.Length, v2Parts.Length); i++)
        {
            if (v1Parts[i] > v2Parts[i]) return 1;
            if (v1Parts[i] < v2Parts[i]) return -1;
        }
        return v1Parts.Length.CompareTo(v2Parts.Length);
    }
}