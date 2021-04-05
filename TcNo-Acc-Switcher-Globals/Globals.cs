﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;

namespace TcNo_Acc_Switcher_Globals
{
    public class Globals
    {
        public string WorkingDirectory { get; set; }
        public DateTime UpdateLastChecked { get; set; } = DateTime.Now;

        // Read existing settings. If they don't exist, create them.
        // --> Reads from current directory. This will only work with the main TcNo Account Switcher app.
        //     Other apps will need to find the correct directory first.
        public static Globals LoadExisting(string fromDir)
        {
            var globalsFile = Path.Combine(fromDir, "globals.json");

            Globals g;
            if (File.Exists(globalsFile))
                g = JsonConvert.DeserializeObject<Globals>(File.ReadAllText(globalsFile));
            else
            {
                g = new Globals();
                File.WriteAllText(globalsFile, JsonConvert.SerializeObject(g));
            }

            g.WorkingDirectory = fromDir;
            return g;
        }

        public static void Save(Globals g)
        {
            var globalsFile = Path.Combine(g.WorkingDirectory, "globals.json");
            File.WriteAllText(globalsFile, JsonConvert.SerializeObject(g, Formatting.Indented));
        }

        // Saves the last time an update was checked.
        public void LastCheckedNow()
        {
            UpdateLastChecked = DateTime.Now;
        }

        // Did the account switcher check for an update within the last day?
        public bool NeedsUpdateCheck()
        {
            return (UpdateLastChecked < DateTime.Now.AddDays(-1));
        }
        // Was the account switcher launched within the last few minutes?
        // It reports launches, so I know how many people are using it from where, but it won't count launches < 5 mins apart.
        public bool NeedsUpdateCheck_Launch()
        {
            return (UpdateLastChecked < DateTime.Now.AddMinutes(-5));
        }

        // Launch main software and check for updates if not already running
        public void RunUpdateCheck()
        {
            var mainExeName = "TcNo Account Switcher.exe";
            var mainExeFullName = Path.Combine(WorkingDirectory, mainExeName);
            if (!File.Exists(mainExeFullName)) return;
            var pList = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(mainExeName).ToLower());
            if (pList.Length > 0) return; // Return because software is already running.

            try
            {
                var processName = mainExeFullName;
                var startInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    Arguments = "-updatecheck"
                };

                Process.Start(startInfo);
            }
            catch (Exception)
            {
                Console.WriteLine(@"Failed to start for update check.");
            }
        }
        
        /// <summary>
        /// Exception handling for all programs
        /// </summary>
        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log Unhandled Exception
            var exceptionStr = e.ExceptionObject.ToString();
            Directory.CreateDirectory("Errors");
            var filePath = $"Errors\\AccSwitcher-Crashlog-{DateTime.Now:dd-MM-yy_hh-mm-ss.fff}.txt";
            using (var sw = File.AppendText(filePath))
            {
                sw.WriteLine(DateTime.Now.ToString(CultureInfo.InvariantCulture) + "\t" + Strings.ErrUnhandledCrash + ": " + exceptionStr + Environment.NewLine + Environment.NewLine);
            }
            Console.WriteLine(Strings.ErrUnhandledException + Path.GetFullPath(filePath));
            //MessageBox.Show(Strings.ErrUnhandledException + Path.GetFullPath(filePath), Strings.ErrUnhandledExceptionHeader, MessageBoxButton.OK, MessageBoxImage.Error);
            Console.WriteLine(Strings.ErrSubmitCrashlog);
            //MessageBox.Show(Strings.ErrSubmitCrashlog, Strings.ErrUnhandledExceptionHeader, MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }

    public class TrayUser
    {
        public string Name { get; set; } = ""; // Name to display on list
        public string Arg { get; set; } = "";  // Argument used to switch to this account

        /// <summary>
        /// Reads Tray_Users.json, and returns a dictionary of strings, with a list of TrayUsers attached to them.
        /// </summary>
        /// <returns>Dictionary of keys, and associated lists of tray users</returns>
        public static Dictionary<string, List<TrayUser>> ReadTrayUsers()
        {
            if (!File.Exists("Tray_Users.json")) return new();
            var json = File.ReadAllText("Tray_Users.json");
            return JsonConvert.DeserializeObject<Dictionary<string, List<TrayUser>>>(json);
        }

        /// <summary>
        /// Adds a user to the beginning of the [Key]s list of TrayUsers. Moves to position 0 if exists.
        /// </summary>
        /// <param name="trayUsers">Reference to Dictionary of keys & list of TrayUsers</param>
        /// <param name="key">Key to add user to</param>
        /// <param name="newUser">user to add to aforementioned key in dictionary</param>
        public static void AddUser(ref Dictionary<string, List<TrayUser>> trayUsers, string key, TrayUser newUser)
        {
            // Create key and add item if doesn't exist already
            if (!trayUsers.ContainsKey(key))
            {
                trayUsers.Add(key, new List<TrayUser>(new[] {newUser}));
                return;
            }

            // If key contains -> Remove it
            trayUsers[key] = trayUsers[key].Where(x => x.Arg != newUser.Arg).ToList();
            // Add item into first slot
            trayUsers[key].Insert(0, newUser);
            // Shorten list to be a max of 3
            while (trayUsers[key].Count > 3) trayUsers[key].RemoveAt(trayUsers[key].Count - 1);
        }

        /// <summary>
        /// Saves trayUsers list to file.
        /// </summary>
        public static void SaveUsers(Dictionary<string, List<TrayUser>> trayUsers) => File.WriteAllText("Tray_Users.json", JsonConvert.SerializeObject(trayUsers));
    }
}
