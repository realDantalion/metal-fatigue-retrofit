// Metal Fatigue Retrofit
// Copyright (C) 2026 Dantalion (github.com/realDantalion)
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MetalFatiguePatcher
{
    /// <summary>Locates MFatigue.exe via GOG / Steam registry entries, or common paths.</summary>
    public static class GameFinder
    {
        static readonly string Exe = PatchData.TargetFileName;

        public static string FindGog()
        {
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var games = baseKey.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                    {
                        if (games == null) continue;
                        foreach (var id in games.GetSubKeyNames())
                        {
                            using (var g = games.OpenSubKey(id))
                            {
                                var path = g?.GetValue("path") as string;
                                if (string.IsNullOrEmpty(path)) continue;
                                var exe = Path.Combine(path, Exe);
                                if (File.Exists(exe)) return exe;
                            }
                        }
                    }
                }
                catch { /* registry view missing */ }
            }
            return null;
        }

        public static string FindSteam()
        {
            string steam = null;
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var k = baseKey.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                        steam = steam ?? k?.GetValue("InstallPath") as string;
                }
                catch { }
            }
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                    steam = steam ?? k?.GetValue("SteamPath") as string;
            }
            catch { }

            if (string.IsNullOrEmpty(steam)) return null;

            var libs = new List<string> { steam };
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s*\"([^\"]+)\""))
                    libs.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
            }
            foreach (var lib in libs)
            {
                var exe = Path.Combine(lib, "steamapps", "common", "Metal Fatigue", Exe);
                if (File.Exists(exe)) return exe;
            }
            return null;
        }

        public static string AutoDetect() => FindGog() ?? FindSteam();
    }
}
