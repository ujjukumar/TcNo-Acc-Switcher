using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

using System.Runtime.Versioning;

namespace TcNo_Acc_Switcher_Wrapper;

[SupportedOSPlatform("windows")]
class Program
{
    private const string RequiredMinWebview = "98.0";
    private const string RequiredMinDesktopRuntime = "8.0.0";
    private const string RequiredMinAspCore = "8.0.0";

    static void Main(string[] args)
    {
        string selfPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        string selfName = Path.GetFileNameWithoutExtension(selfPath);
        string operatingDir = Path.GetDirectoryName(selfPath) ?? "";
        
        Console.Title = $"Runtime Verifier - {selfName}";
        Console.WriteLine($"Verifying .NET versions before attempting to launch {selfName}");

        bool minWebviewMet = CheckWebView2();
        bool minDesktopMet = CheckDotNetRuntime("Microsoft.WindowsDesktop.App", RequiredMinDesktopRuntime);
        bool minAspMet = CheckDotNetRuntime("Microsoft.AspNetCore.App", RequiredMinAspCore);

        if (!minWebviewMet || !minDesktopMet || !minAspMet)
        {
            Console.WriteLine("Missing required runtimes. Launching installer...");
            string installerPath = Path.Combine(operatingDir, "_First_Run_Installer.exe");
            
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = $"net {selfName}",
                WorkingDirectory = operatingDir,
                UseShellExecute = true
            };
            
            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch installer: {ex.Message}");
            }
        }
        else
        {
            string targetExe = Path.Combine(operatingDir, $"{selfName}_main.exe");
            Console.WriteLine($"Running: {Path.GetFileName(targetExe)}");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = targetExe,
                WorkingDirectory = operatingDir,
                UseShellExecute = false
            };

            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch application: {ex.Message}");
            }
        }
    }

    static bool CheckWebView2()
    {
        string[] keys = 
        {
            @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
            @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
        };

        foreach (var keyPath in keys)
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath) ?? Registry.LocalMachine.OpenSubKey(keyPath);
            if (key?.GetValue("pv") is string version)
            {
                if (CompareVersions(version, RequiredMinWebview) >= 0) return true;
            }
        }
        return false;
    }

    static bool CheckDotNetRuntime(string runtimeName, string minVersion)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            foreach (var line in output.Split(Environment.NewLine))
            {
                if (line.StartsWith(runtimeName))
                {
                    var parts = line.Split(' ');
                    if (parts.Length > 1 && CompareVersions(parts[1], minVersion) >= 0)
                    {
                        return true;
                    }
                }
            }
        }
        catch { }
        return false;
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