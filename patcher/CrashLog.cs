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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace MetalFatiguePatcher
{
    /// <summary>
    /// Last line of defence: turns an unhandled exception into a text file the user can attach to a
    /// bug report, instead of the bare .NET crash dialog that tells us nothing.
    ///
    /// The report is written next to the patcher exe (it runs elevated, so that folder is writable
    /// even under Program Files) and falls back to %TEMP% if that ever fails — a crash handler that
    /// throws would be worse than none.
    ///
    /// Everything it needs is pushed in beforehand (<see cref="Note"/>, <see cref="Context"/>) and
    /// kept in plain fields. Nothing here reads the UI: by the time this runs the UI thread may be
    /// the one that died, and touching a control from another thread can block on a SendMessage that
    /// will never be answered.
    ///
    /// Paths are scrubbed of the Windows user name before they are written — these files end up on a
    /// public issue tracker.
    /// </summary>
    public static class CrashLog
    {
        const int KeptLines = 200;
        const int KeepDays = 14;   // older logs are dropped at startup

        // Spelled out on purpose: this file lands next to the exe and gets attached to bug reports,
        // so its name alone has to say which program wrote it and that it is an error log.
        const string FilePrefix = "MetalFatigueRetrofitPatcher-ErrorLog-";

        // Everything from one day lands in one file, so the thing to keep small is the number of
        // entries, not the number of files. A fault in a paint or layout handler fires again on every
        // redraw and would otherwise append the same block hundreds of times, leaving a log too big
        // to attach. Filtered by signature rather than by a plain count: a bare "first N only" would
        // keep N copies of one fault and then discard the next, different one — which is exactly the
        // one worth having. The count is only a backstop behind that.
        const int MaxEntriesPerSession = 10;

        static readonly object Gate = new object();
        static readonly Queue<string> Recent = new Queue<string>();
        static readonly List<KeyValuePair<string, string>> Facts = new List<KeyValuePair<string, string>>();
        static bool _inHandler;    // a crash while reporting a crash must not recurse
        static bool _toldUser;     // one dialog per session
        static int _written;
        static string _lastReport; // so a suppressed repeat can still point at real evidence
        static int _mainThreadId;
        static readonly HashSet<string> Seen = new HashSet<string>();

        /// <summary>Route both exception paths here. Must run before the first form is created.</summary>
        public static void Install()
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            try { Prune(); } catch { }

            // Explicit rather than load-bearing: this is already the default once a ThreadException
            // handler is attached without a debugger. Stating it keeps a future debugger session from
            // quietly changing which of the two handlers below sees the exception.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // A failure on the UI thread leaves the process perfectly alive, so we report it and let
            // the user carry on — same call the default WinForms dialog makes, except we keep the
            // evidence. A failure on any other thread is already fatal to the process by .NET's own
            // rules; there is nothing to keep running.
            Application.ThreadException += (s, e) => Handle(e.Exception, "ui thread", false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Handle(e.ExceptionObject as Exception, ThreadLabel(), true);
        }

        /// <summary>
        /// AppDomain.UnhandledException is not only for worker threads: anything thrown on the main
        /// thread before or after the message loop — the MainForm constructor, most of all — arrives
        /// there too. Labelling all of it "background thread" would send triage looking in the wrong
        /// place, so name the thread it actually came from.
        /// </summary>
        static string ThreadLabel()
        {
            try
            {
                return System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId
                    ? "main thread, outside the message loop (startup or shutdown)"
                    : "background thread";
            }
            catch { return "unknown thread"; }
        }

        /// <summary>Mirror of the on-screen log. The last lines before a crash are usually the whole story.</summary>
        public static void Note(string line)
        {
            if (line == null) return;
            lock (Gate)
            {
                Recent.Enqueue(line);
                while (Recent.Count > KeptLines) Recent.Dequeue();
            }
        }

        /// <summary>A named fact about the current state (game path, chosen version, ...). Latest wins.</summary>
        public static void Context(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            lock (Gate)
            {
                for (int i = 0; i < Facts.Count; i++)
                    if (Facts[i].Key == key)
                    {
                        Facts[i] = new KeyValuePair<string, string>(key, value ?? "");
                        return;
                    }
                Facts.Add(new KeyValuePair<string, string>(key, value ?? ""));
            }
        }

        static void Handle(Exception ex, string source, bool fatal)
        {
            bool tell;
            lock (Gate)
            {
                if (_inHandler) return;
                _inHandler = true;
                // Say it once. A fault in a paint or layout handler fires again on every redraw, and
                // a stack of modal dialogs would be worse than the bug. The files still pile up to
                // MaxReports, so a second, different fault is not lost.
                tell = !_toldUser || fatal;
                _toldUser = true;
            }

            try
            {
                string path = null;
                bool fresh;
                lock (Gate) fresh = Seen.Add(Signature(ex)) && _written < MaxEntriesPerSession;

                if (fresh)
                {
                    try { path = Write(ex, source); _written++; _lastReport = path; } catch { }
                }
                else
                {
                    // Suppressed, not failed. Point at the last file we did write instead of claiming
                    // nothing could be saved — a repeat says the same thing as the report already on
                    // disk, and sending the user hunting for a file that exists is worse.
                    path = _lastReport;
                }

                if (tell)
                {
                    string text = path != null
                        ? string.Format(Lang.T(fatal ? "crash.msg" : "crash.msgContinue"), path)
                        : string.Format(Lang.T(fatal ? "crash.msgNoFile" : "crash.msgNoFileContinue"), Scrub(Describe(ex)));

                    var buttons = path != null ? MessageBoxButtons.OKCancel : MessageBoxButtons.OK;
                    var answer = MessageBox.Show(text, Lang.T("crash.title"), buttons, MessageBoxIcon.Error);

                    if (path != null && answer == DialogResult.OK)
                        try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + path + "\""); } catch { }
                }
            }
            catch { }
            finally
            {
                lock (Gate) { _inHandler = false; }
            }

            // Only for the non-UI case: .NET is tearing the process down regardless, and exiting
            // here keeps that from happening halfway through with no explanation.
            if (fatal) Environment.Exit(1);
        }

        /// <summary>
        /// Append the report to today's log and return its full path. One file per day, not one per
        /// fault: a folder full of near-identical .txt files is worse to hand over than a single one,
        /// and it keeps a user who hits something twice in a week from having to pick which file
        /// matters. Every entry carries its own timestamp, so the file stays readable as it grows.
        /// Throws only if no location worked.
        /// </summary>
        public static string Write(Exception ex, string source)
        {
            string name = FilePrefix + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".txt";
            string entry = Compose(ex, source);

            foreach (var dir in Locations())
            {
                try
                {
                    var full = Path.Combine(dir, name);

                    bool startFresh = true;
                    try { startFresh = !File.Exists(full) || new FileInfo(full).Length == 0; } catch { }

                    File.AppendAllText(full, startFresh ? Header() + entry : entry, Encoding.UTF8);
                    return full;
                }
                catch { }
            }
            throw new IOException("no writable location for the crash report");
        }

        /// <summary>Written once, when the day's file is created.</summary>
        static string Header()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Metal Fatigue Retrofit — error log");
            sb.AppendLine("Please attach this file to a bug report: " + MainForm.IssuesUrl);
            sb.AppendLine("Each entry below is one error, newest at the bottom.");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Drop error logs older than <see cref="KeepDays"/> at startup, so the folder next to the exe
        /// does not accumulate forever. The date comes from the file name, not from its timestamp: a
        /// log that was copied or restored keeps its name but not its modification date, and deleting
        /// by name is the only way to be sure we only ever remove files this class wrote. Anything
        /// whose name does not parse is left alone.
        /// </summary>
        static void Prune()
        {
            var cutoff = DateTime.Now.Date.AddDays(-KeepDays);

            foreach (var dir in Locations())
            {
                string[] files;
                try { files = Directory.GetFiles(dir, FilePrefix + "*.txt"); }
                catch { continue; }

                foreach (var f in files)
                {
                    try
                    {
                        var stem = Path.GetFileNameWithoutExtension(f);
                        if (stem.Length <= FilePrefix.Length) continue;

                        DateTime day;
                        if (!DateTime.TryParseExact(stem.Substring(FilePrefix.Length), "yyyy-MM-dd",
                                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
                            continue;

                        if (day < cutoff) File.Delete(f);
                    }
                    catch { }
                }
            }
        }

        static IEnumerable<string> Locations()
        {
            string exeDir = null;
            try { exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); } catch { }
            if (!string.IsNullOrEmpty(exeDir)) yield return exeDir;

            string temp = null;
            try { temp = Path.GetTempPath(); } catch { }
            if (!string.IsNullOrEmpty(temp)) yield return temp;
        }

        static string Compose(Exception ex, string source)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)
                          + "   —   Metal Fatigue Retrofit " + Version());
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            sb.AppendLine("where:   " + source);
            sb.AppendLine("os:      " + Safe(() => Environment.OSVersion.VersionString)
                                      + (Safe(() => Environment.Is64BitOperatingSystem.ToString()) == "True" ? " / 64-bit" : " / 32-bit")
                                      + (Safe(() => Environment.Is64BitProcess.ToString()) == "True" ? ", 64-bit process" : ", 32-bit process"));
            sb.AppendLine("clr:     " + Safe(() => Environment.Version.ToString()));
            sb.AppendLine("ui lang: " + Lang.Codes[(int)Lang.Current]
                                      + " (system " + Safe(() => CultureInfo.CurrentUICulture.Name) + ")");
            sb.AppendLine();

            lock (Gate)
            {
                if (Facts.Count > 0)
                {
                    sb.AppendLine("--- state ---");
                    foreach (var f in Facts)
                        sb.AppendLine(f.Key.PadRight(9) + Scrub(f.Value));
                    sb.AppendLine();
                }

                if (Recent.Count > 0)
                {
                    sb.AppendLine("--- last " + Recent.Count + " log line(s) ---");
                    foreach (var line in Recent) sb.AppendLine(Scrub(line));
                    sb.AppendLine();
                }
            }

            sb.AppendLine("--- exception ---");
            sb.AppendLine(Scrub(Describe(ex)));
            return sb.ToString();
        }

        /// <summary>
        /// What makes two faults "the same one". Type plus the topmost stack frame: the message often
        /// carries a varying value (a path, an index) and would let one repeating fault through again
        /// and again, while the frame is what actually identifies the site.
        ///
        /// Taken from the innermost exception, because a wrapper contributes nothing: every
        /// TargetInvocationException carries the same generic top frame, and signing on that would
        /// collapse unrelated faults into one and suppress all but the first.
        /// </summary>
        static string Signature(Exception ex)
        {
            if (ex == null) return "<none>";
            try
            {
                var e = ex;
                while (e.InnerException != null) e = e.InnerException;

                var trace = e.StackTrace ?? "";
                int nl = trace.IndexOf('\n');
                var top = (nl >= 0 ? trace.Substring(0, nl) : trace).Trim();
                return e.GetType().FullName + " @ " + top;
            }
            catch { return ex.GetType().FullName; }
        }

        static string Describe(Exception ex)
        {
            if (ex == null) return "(no exception object — the runtime did not hand one over)";

            var sb = new StringBuilder();
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e != ex) sb.AppendLine().AppendLine("--- caused by ---");
                sb.AppendLine(e.GetType().FullName + ": " + e.Message);
                if (!string.IsNullOrEmpty(e.StackTrace)) sb.AppendLine(e.StackTrace);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Take the Windows user name out of anything we write. Real names live in profile paths far
        /// more often than people expect, and these reports are meant to be pasted in public.
        /// </summary>
        static string Scrub(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try
            {
                // The profile path first, and into a sentinel: writing "%USERPROFILE%" straight away
                // would hand the second pass a token containing the literal word USER to chew on.
                const string Sentinel = "PROFILE";
                var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(profile)) s = ReplaceNoCase(s, profile, Sentinel, false);

                // The bare account name only where it stands alone. Without a boundary check an
                // account called "Al" turns System.Globalization into System.Glob%USER%ization, and
                // the stack frames are exactly what the report exists to carry. Short names are left
                // entirely alone — the profile pass above already covers where they actually appear.
                var user = Environment.UserName;
                if (!string.IsNullOrEmpty(user) && user.Length >= 3)
                    s = ReplaceNoCase(s, user, "%USER%", true);

                s = s.Replace(Sentinel, "%USERPROFILE%");
            }
            catch { }
            return s;
        }

        /// <summary>
        /// Case-insensitive replace. With <paramref name="wholeWord"/> a hit only counts when it is
        /// not glued to a letter or digit on either side, so a short needle cannot shred unrelated
        /// identifiers.
        /// </summary>
        static string ReplaceNoCase(string haystack, string needle, string with, bool wholeWord)
        {
            if (string.IsNullOrEmpty(needle)) return haystack;

            var sb = new StringBuilder();
            int at = 0;
            while (true)
            {
                int hit = haystack.IndexOf(needle, at, StringComparison.OrdinalIgnoreCase);
                if (hit < 0) { sb.Append(haystack, at, haystack.Length - at); break; }

                int end = hit + needle.Length;
                bool glued = wholeWord
                    && ((hit > 0 && IsWordChar(haystack[hit - 1]))
                        || (end < haystack.Length && IsWordChar(haystack[end])));

                sb.Append(haystack, at, hit - at);
                // A rejected hit is copied back from the haystack, not from the needle: the match was
                // case-insensitive, so re-emitting the needle would rewrite "Globalization" as
                // "GlobAlization" for an account named "Al".
                if (glued) sb.Append(haystack, hit, needle.Length);
                else sb.Append(with);
                at = end;
            }
            return sb.ToString();
        }

        static bool IsWordChar(char c) => char.IsLetterOrDigit(c);

        static string Version()
        {
            var v = Safe(() =>
            {
                var a = Assembly.GetExecutingAssembly().GetName().Version;
                return a == null ? "?" : string.Format("v{0}.{1}.{2}", a.Major, a.Minor, a.Build);
            });
            return v ?? "v?";
        }

        static string Safe(Func<string> f)
        {
            try { return f() ?? "?"; } catch { return "?"; }
        }
    }
}
