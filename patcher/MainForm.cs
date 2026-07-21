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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MetalFatiguePatcher
{
    public class MainForm : Form
    {
        TextBox _pathBox, _log;
        RadioButton _srcAuto, _srcSteam, _srcGog;
        RadioButton _prof2x, _prof4x, _prof8x, _profUnleashed, _profCheats, _profCheatsAll;
        Label _profDesc, _bannerTitle, _bannerSub, _exeLabel, _compatLabel, _credits, _svStatus;
        LinkLabel _contactLink, _licenseLink;

        /// <summary>
        /// Where users can report an unsupported build. While this is empty the
        /// "contact us" link stays hidden, so no placeholder URL can ever ship.
        /// </summary>
        const string ContactUrl = "";
        GroupBox _srcGroup, _profGroup, _svGroup;
        CheckBox _sharedVision;
        readonly ToolTip _tips = new ToolTip();
        Button _browseBtn, _patchBtn, _restoreBtn, _exitBtn;
        Panel[] _flagCells;
        Panel _banner;
        PictureBox _mascot;

        /// <summary>
        /// Normal mascot, plus an optional alternate shown in cheat mode. Drop a
        /// "logo_cheat.png" next to logo.png and it is picked up automatically.
        /// While it is absent, cheat mode deliberately shows a loud missing-texture
        /// placeholder (see MakeMissingMascot) so the gap cannot be shipped unnoticed.
        /// </summary>
        Image _logo, _logoCheat;
        Image _missingMascot;

        /// <summary>
        /// Loud stand-in for a missing cheat mascot: the classic magenta/black
        /// missing-texture check pattern. Drawn at runtime so no asset is required.
        /// </summary>
        static Image MakeMissingMascot(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int c = size / 8;
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                        using (var b = new SolidBrush((x + y) % 2 == 0 ? Color.Magenta : Color.Black))
                            g.FillRectangle(b, x * c, y * c, c, c);

                using (var f = new Font("Segoe UI", size * 0.42f, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var shadow = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                {
                    var r = new RectangleF(0, 0, size, size);
                    g.DrawString("?", f, shadow, new RectangleF(3, 3, size, size), sf);
                    g.DrawString("?", f, Brushes.White, r, sf);
                }
            }
            return bmp;
        }

        /// <summary>Path whose installed shared-vision state we already mirrored into the box.</summary>
        string _svSyncedPath;

        // Preserves the shared-vision tick across a detour into a cheat profile.
        bool _svForcedOff, _svBeforeCheat;

        // Easter egg: click the banner 5x to unlock the cheat variants.
        int _bannerClicks;
        bool _cheatUnlocked;
        const int UnlockClicks = 5;

        public MainForm()
        {
            ClientSize = new Size(760, 684);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            _logo = LoadEmbedded("MetalFatiguePatcher.logo.png");
            _logoCheat = LoadEmbedded("MetalFatiguePatcher.logo_cheat.png");   // optional

            BuildBanner();

            int y = 126;   // below the 116px banner + 10px gap

            // 1. Game source — radios, the MFatigue.exe path chooser, and the
            // compatibility status all live in this one frame.
            _srcGroup = new GroupBox { Location = new Point(12, y), Size = new Size(736, 118) };
            _srcAuto  = new RadioButton { Checked = true, Location = new Point(14, 24), AutoSize = true };
            _srcSteam = new RadioButton { Text = "Steam", Location = new Point(220, 24), AutoSize = true };
            _srcGog   = new RadioButton { Text = "GOG",   Location = new Point(330, 24), AutoSize = true };
            // Picking a source runs the search right away — no extra button needed.
            foreach (var rb in new[] { _srcAuto, _srcSteam, _srcGog })
                rb.CheckedChanged += (s, e) => { if (((RadioButton)s).Checked) Detect(); };
            // MFatigue.exe path chooser — second row.
            _exeLabel  = new Label   { Location = new Point(14, 59), AutoSize = true };
            _pathBox   = new TextBox { Location = new Point(110, 54), Size = new Size(494, 24) };
            _browseBtn = new Button  { Location = new Point(612, 53), Size = new Size(108, 26) };
            _browseBtn.Click += (s, e) => Browse();
            _pathBox.TextChanged += (s, e) => UpdateCompat();
            // Compatibility status (+ optional contact link) — third row.
            _compatLabel = new Label { Location = new Point(14, 90), Size = new Size(706, 20), AutoEllipsis = true };
            _contactLink = new LinkLabel
            {
                Location = new Point(454, 90), Size = new Size(266, 20),
                TextAlign = ContentAlignment.MiddleRight, Visible = false
            };
            _contactLink.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start(ContactUrl); } catch { }
            };
            _srcGroup.Controls.AddRange(new Control[] {
                _srcAuto, _srcSteam, _srcGog, _exeLabel, _pathBox, _browseBtn, _compatLabel, _contactLink });
            Controls.Add(_srcGroup);
            y += 128;   // 118px frame + 10px gap (same total as before — nothing below shifts)

            // 3. Version
            _profGroup = new GroupBox { Location = new Point(12, y), Size = new Size(736, 132) };
            var orange = Color.FromArgb(186, 106, 16);
            // all four main modes on one row
            _prof2x        = new RadioButton { Location = new Point(14, 24),  AutoSize = true };
            _prof4x        = new RadioButton { Checked = true, Location = new Point(210, 24), AutoSize = true };
            _prof8x        = new RadioButton { Location = new Point(420, 24), AutoSize = true };
            _profUnleashed = new RadioButton { Location = new Point(620, 24), AutoSize = true };
            // hidden cheat row
            _profCheats    = new RadioButton { Location = new Point(14, 50),  AutoSize = true, Visible = false, ForeColor = orange };
            _profCheatsAll = new RadioButton { Location = new Point(300, 50), AutoSize = true, Visible = false, ForeColor = orange };
            _profDesc = new Label { Location = new Point(14, 78), Size = new Size(708, 46), ForeColor = Color.DimGray };
            foreach (var rb in new[] { _prof2x, _prof4x, _prof8x, _profUnleashed, _profCheats, _profCheatsAll })
                rb.CheckedChanged += (s, e) => UpdateProfDesc();
            _profGroup.Controls.AddRange(new Control[] { _prof2x, _prof4x, _prof8x, _profUnleashed, _profCheats, _profCheatsAll, _profDesc });
            Controls.Add(_profGroup);
            y += 142;

            // 3. Shared vision — optional add-on, framed like the sections above.
            // (The cheat variants turn the fog off entirely, so it is disabled there.)
            _svGroup = new GroupBox { Location = new Point(12, y), Size = new Size(736, 58) };
            _sharedVision = new CheckBox { Location = new Point(14, 24), AutoSize = true };
            _tips.SetToolTip(_sharedVision, "");
            // Shows what the installed exe actually has — the checkbox itself is the
            // desired state, this is the current one (same idea as the version line).
            _svStatus = new Label { Location = new Point(300, 26), AutoSize = true, Visible = false };
            _svGroup.Controls.AddRange(new Control[] { _sharedVision, _svStatus });
            Controls.Add(_svGroup);
            y += 68;

            // 4. Buttons
            _patchBtn = new Button
            {
                Location = new Point(12, y), Size = new Size(140, 34),
                BackColor = Color.FromArgb(60, 120, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            _patchBtn.Click += (s, e) => DoPatch();
            _restoreBtn = new Button { Location = new Point(160, y), Size = new Size(230, 34) };
            _restoreBtn.Click += (s, e) => DoRestore();
            _exitBtn = new Button { Location = new Point(628, y), Size = new Size(120, 34) };
            _exitBtn.Click += (s, e) => Close();
            Controls.AddRange(new Control[] { _patchBtn, _restoreBtn, _exitBtn });
            y += 44;

            // 5. Log
            _log = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Location = new Point(12, y), Size = new Size(736, 104),
                BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.Gainsboro,
                Font = new Font("Consolas", 9f)
            };
            Controls.Add(_log);
            y += 112;

            // Credits + GPL "Appropriate Legal Notices" (see Lang: credits.legal)
            _credits = new Label
            {
                Location = new Point(12, y), Size = new Size(736, 32),
                ForeColor = Color.Gray, Font = new Font("Segoe UI", 7.5f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_credits);
            y += 32;

            _licenseLink = new LinkLabel
            {
                Location = new Point(12, y), Size = new Size(736, 16),
                Font = new Font("Segoe UI", 7.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkColor = Color.FromArgb(90, 110, 150)
            };
            _licenseLink.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start("https://www.gnu.org/licenses/gpl-3.0.html"); } catch { }
            };
            Controls.Add(_licenseLink);

            ApplyLanguage();
            Detect();
        }

        // ---------- banner ----------

        void BuildBanner()
        {
            _banner = new Panel { Location = new Point(0, 0), Size = new Size(ClientSize.Width, 116) };
            _banner.Paint += (s, e) =>
            {
                var top = _cheatUnlocked ? Color.FromArgb(120, 62, 8)   : Color.FromArgb(24, 34, 54);
                var bot = _cheatUnlocked ? Color.FromArgb(206, 126, 22) : Color.FromArgb(44, 62, 96);
                using (var b = new LinearGradientBrush(_banner.ClientRectangle, top, bot, 90f))
                    e.Graphics.FillRectangle(b, _banner.ClientRectangle);
                using (var p = new Pen(_cheatUnlocked ? Color.FromArgb(250, 196, 80) : Color.FromArgb(80, 110, 160), 2))
                    e.Graphics.DrawLine(p, 0, _banner.Height - 1, _banner.Width, _banner.Height - 1);
            };

            _mascot = new PictureBox
            {
                Image = _logo, SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(12, 10), Size = new Size(102, 102),
                BackColor = Color.Transparent, Cursor = Cursors.Hand
            };
            var pic = _mascot;

            _bannerTitle = new Label
            {
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(114, 34)
            };
            _bannerSub = new Label
            {
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(185, 200, 225), BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(116, 64)
            };

            // language selector: clickable flags (auto-detected, user can override)
            _flagCells = new Panel[Lang.Codes.Length];
            var tip = new ToolTip();
            const int perRow = 5, cellW = 44, cellH = 33;   // 10 languages -> 5x2
            int fx = ClientSize.Width - 12 - perRow * cellW;
            for (int i = 0; i < Lang.Codes.Length; i++)
            {
                int idx = i;
                var cell = new Panel
                {
                    Location = new Point(fx + (i % perRow) * cellW, 28 + (i / perRow) * cellH),
                    Size = new Size(40, 29), Cursor = Cursors.Hand
                };
                var fp = new PictureBox
                {
                    Image = LoadEmbedded("MetalFatiguePatcher.flag_" + Lang.Codes[i] + ".png"),
                    SizeMode = PictureBoxSizeMode.AutoSize,
                    Location = new Point(2, 2), Cursor = Cursors.Hand, BackColor = Color.Transparent
                };
                tip.SetToolTip(fp, Lang.Names[i]);
                tip.SetToolTip(cell, Lang.Names[i]);
                EventHandler pick = (s, e) => { Lang.Current = (Lang.L)idx; ApplyLanguage(); };
                cell.Click += pick; fp.Click += pick;
                cell.Controls.Add(fp);
                _flagCells[i] = cell;
                _banner.Controls.Add(cell);
            }

            _banner.Controls.AddRange(new Control[] { pic, _bannerTitle, _bannerSub });
            Controls.Add(_banner);

            // The easter egg lives on the mascot only — poking the robot is the secret,
            // and the hand cursor over him is the only hint. Clicking elsewhere on the
            // banner does nothing.
            EventHandler poke = (s, e) => MascotClicked();
            pic.Click += poke;
            // Fast clicking raises DoubleClick for every second click, which would otherwise
            // be swallowed and make the easter egg need more clicks than the robot promises.
            pic.DoubleClick += poke;
        }

        /// <summary>Poking the robot mascot 5x unlocks the cheat variants.</summary>
        void MascotClicked()
        {
            if (_cheatUnlocked) return;
            _bannerClicks++;
            if (_bannerClicks >= UnlockClicks) { UnlockCheats(true); return; }
            ShowBotLine();
        }

        /// <summary>The robot in the banner talks back (Warcraft-style click responses).</summary>
        void ShowBotLine()
        {
            _bannerSub.Text = Lang.T("bot.click" + _bannerClicks);
            bool lastWarning = _bannerClicks == UnlockClicks - 1;
            _bannerSub.ForeColor = lastWarning
                ? Color.FromArgb(255, 214, 120)     // ominous gold for the final warning
                : Color.FromArgb(206, 222, 245);
            _bannerSub.Font = new Font("Segoe UI", 8.5f, lastWarning ? FontStyle.Bold : FontStyle.Regular);
        }

        /// <summary>
        /// Reveals the cheat variants. Triggered by the banner easter egg (announce: true)
        /// or silently when the selected exe already carries a cheat patch.
        /// Deliberately does NOT call ApplyLanguage() — that would recurse via UpdateCompat.
        /// </summary>
        void UnlockCheats(bool announce)
        {
            if (_cheatUnlocked) return;
            _cheatUnlocked = true;
            _profCheats.Visible = true;
            _profCheatsAll.Visible = true;
            _bannerTitle.Text = Lang.T("banner.cheatTitle");
            _bannerSub.Text = Lang.T("banner.cheatSub");
            _bannerSub.ForeColor = Color.FromArgb(255, 236, 190);
            _bannerSub.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            // Swap in the alternate mascot. If it was never supplied, show a loud
            // missing-texture placeholder instead of silently keeping the normal one,
            // so a forgotten asset is impossible to overlook.
            if (_mascot != null)
            {
                if (_logoCheat != null)
                {
                    _mascot.Image = _logoCheat;
                }
                else
                {
                    _mascot.Image = _missingMascot ?? (_missingMascot = MakeMissingMascot(144));
                    Log("[dev] logo_cheat.png is missing — showing placeholder mascot.");
                }
            }
            _banner.Invalidate();
            if (announce)
            {
                Log(Lang.T("msg.unlocked"));
                try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
            }
        }

        // ---------- localization ----------

        void ApplyLanguage()
        {
            Text                 = Lang.T("window.title");
            _bannerTitle.Text    = _cheatUnlocked ? Lang.T("banner.cheatTitle") : Lang.T("banner.title");
            // keep the robot's line if the user is mid-easter-egg
            if (_cheatUnlocked)                      _bannerSub.Text = Lang.T("banner.cheatSub");
            else if (_bannerClicks > 0)              ShowBotLine();
            else                                     _bannerSub.Text = Lang.T("banner.sub");
            _srcGroup.Text       = Lang.T("grp.source");
            _srcAuto.Text        = Lang.T("src.auto");
            _exeLabel.Text       = Lang.T("lbl.exe");
            _browseBtn.Text      = Lang.T("btn.browse");
            _profGroup.Text      = Lang.T("grp.version");
            _svGroup.Text        = Lang.T("grp.sharedvision");
            _prof2x.Text         = Lang.ProfileTitle("balanced2x");
            _prof4x.Text         = Lang.ProfileTitle("balanced4x");
            _prof8x.Text         = Lang.ProfileTitle("balanced8x");
            _profUnleashed.Text  = Lang.ProfileTitle("unleashed");
            _profCheats.Text     = Lang.ProfileTitle("cheats");
            _profCheatsAll.Text  = Lang.ProfileTitle("cheats_all");
            _patchBtn.Text       = Lang.T("btn.patch");
            _restoreBtn.Text     = Lang.T("btn.restore");
            _exitBtn.Text        = Lang.T("btn.exit");
            _credits.Text        = Lang.T("credits.legal") + "\n" + Lang.T("credits.thanks");
            _licenseLink.Text    = Lang.T("credits.license");
            _sharedVision.Text   = Lang.T("sv.label");
            _tips.SetToolTip(_sharedVision, Lang.T("sv.hint"));
            // The checkbox auto-sizes per language, so park the status right after it.
            _svStatus.Left = _sharedVision.Right + 16;

            // highlight the active language flag
            if (_flagCells != null)
                for (int i = 0; i < _flagCells.Length; i++)
                    _flagCells[i].BackColor = (i == (int)Lang.Current)
                        ? Color.FromArgb(250, 214, 140)
                        : Color.Transparent;

            UpdateProfDesc();
            UpdateCompat();
        }

        /// <summary>
        /// Checks whether the selected MFatigue.exe is a build this patcher supports and
        /// enables/disables the version choice accordingly.
        /// </summary>
        void UpdateCompat()
        {
            if (_compatLabel == null) return;

            string profKey; bool exact;
            var c = Patcher.Check(_pathBox.Text.Trim(), out profKey, out exact);
            bool canPatch;

            // Already running a cheat build? Then there is nothing left to hide.
            if (profKey == "cheats" || profKey == "cheats_all") UnlockCheats(false);

            switch (c)
            {
                case Patcher.Compat.Pristine:
                    _compatLabel.Text = Lang.T(exact ? "compat.exact" : "compat.ok");
                    _compatLabel.ForeColor = Color.FromArgb(22, 120, 52);
                    canPatch = true;
                    break;
                case Patcher.Compat.PatchedByUs:
                    _compatLabel.Text = profKey != null
                        ? string.Format(Lang.T("compat.patched"), Lang.ProfileTitle(profKey))
                        : Lang.T("compat.patchedUnknown");
                    _compatLabel.ForeColor = Color.FromArgb(176, 108, 12);
                    canPatch = true;
                    break;
                case Patcher.Compat.Missing:
                    _compatLabel.Text = Lang.T("compat.missing");
                    _compatLabel.ForeColor = Color.DimGray;
                    canPatch = false;
                    break;
                default:
                    // We recognise our own patch but have no clean original to work from.
                    _compatLabel.Text = profKey != null
                        ? Lang.T("compat.patchedNoBackup")
                        : Lang.T("compat.unsupported");
                    _compatLabel.ForeColor = Color.FromArgb(192, 32, 32);
                    canPatch = false;
                    break;
            }

            _contactLink.Text = Lang.T("compat.contact");
            // Hidden until a real contact address is configured (see ContactUrl).
            _contactLink.Visible = c == Patcher.Compat.Unsupported
                                   && profKey == null
                                   && !string.IsNullOrEmpty(ContactUrl);
            // Only give up width for the contact link when it is actually shown.
            // (Widths are relative to the "1. Game source" frame the label now lives in.)
            _compatLabel.Width = _contactLink.Visible ? 432 : 706;

            // Grey out the version choice + patch button when we can't safely patch.
            _profGroup.Enabled = canPatch;
            _patchBtn.Enabled = canPatch;

            var path = _pathBox.Text.Trim();
            bool svInstalled = !string.IsNullOrEmpty(path) && Patcher.HasSharedVision(path);

            // Reflect what is already installed — but only once per file, so the user
            // can still tick/untick freely afterwards without being overridden.
            if (path != _svSyncedPath)
            {
                _svSyncedPath = path;
                _sharedVision.Checked = svInstalled;
            }
            UpdateSharedVisionState();

            // Show the state of the *installed* exe next to the box (the box itself is
            // the desired state). Only meaningful once something of ours is installed.
            bool isPatched = c == Patcher.Compat.PatchedByUs || profKey != null;
            _svStatus.Visible = isPatched;
            if (isPatched)
            {
                _svStatus.Text = Lang.T(svInstalled ? "sv.on" : "sv.off");
                _svStatus.ForeColor = svInstalled ? Color.FromArgb(176, 108, 12) : Color.Gray;
            }

            // Only offer Restore when the backup actually verifies as a clean original.
            _restoreBtn.Enabled = Patcher.HasValidBackup(path);
        }

        static Image LoadEmbedded(string resourceName)
        {
            try
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    if (s != null) return Image.FromStream(s);
            }
            catch { }
            return null;
        }

        // ---------- logic ----------

        Profile Selected
        {
            get
            {
                if (_profCheatsAll != null && _profCheatsAll.Checked) return PatchData.ByKey("cheats_all");
                if (_profCheats != null && _profCheats.Checked)       return PatchData.ByKey("cheats");
                if (_prof2x.Checked)        return PatchData.ByKey("balanced2x");
                if (_prof8x.Checked)        return PatchData.ByKey("balanced8x");
                if (_profUnleashed.Checked) return PatchData.ByKey("unleashed");
                return PatchData.ByKey("balanced4x");
            }
        }

        void UpdateProfDesc()
        {
            _profDesc.Text = Selected.Description;
            UpdateSharedVisionState();
        }

        /// <summary>
        /// The cheat variants switch the fog off entirely, so shared vision is meaningless
        /// there — clear the box and grey it out until a normal version is chosen again.
        /// </summary>
        void UpdateSharedVisionState()
        {
            if (_sharedVision == null) return;
            bool cheat = (_profCheats != null && _profCheats.Checked)
                      || (_profCheatsAll != null && _profCheatsAll.Checked);

            // Remember the user's choice while a cheat profile forces the box off, and
            // hand it back when they switch away again — otherwise the tick would be
            // silently lost and the next Patch would quietly drop an installed add-on.
            if (cheat && !_svForcedOff)
            {
                _svForcedOff = true;
                _svBeforeCheat = _sharedVision.Checked;
                _sharedVision.Checked = false;
            }
            else if (!cheat && _svForcedOff)
            {
                _svForcedOff = false;
                _sharedVision.Checked = _svBeforeCheat;
            }

            // Dim the whole "3." box (title + checkbox + status) when cheats are on
            // or no compatible exe is selected, matching the version group.
            _svGroup.Enabled = !cheat && _profGroup.Enabled;
        }

        void Log(string s) => _log.AppendText(s + Environment.NewLine);

        void Detect()
        {
            string exe = null;
            try
            {
                if (_srcSteam.Checked) exe = GameFinder.FindSteam();
                else if (_srcGog.Checked) exe = GameFinder.FindGog();
                else exe = GameFinder.AutoDetect();
            }
            catch (Exception ex) { Log(string.Format(Lang.T("msg.searchError"), ex.Message)); }

            if (exe != null) { _pathBox.Text = exe; Log(string.Format(Lang.T("msg.found"), exe)); }
            else Log(Lang.T("msg.notFound"));
        }

        void Browse()
        {
            using (var d = new OpenFileDialog { Filter = "MFatigue.exe|MFatigue.exe|*.exe|*.exe" })
            {
                if (d.ShowDialog() == DialogResult.OK) _pathBox.Text = d.FileName;
            }
        }

        void DoPatch()
        {
            var path = _pathBox.Text.Trim();
            if (!File.Exists(path))
            {
                MessageBox.Show(Lang.T("msg.exeMissing"), Lang.T("ttl.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.Equals(Path.GetFileName(path), PatchData.TargetFileName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(Lang.T("msg.wrongName"), Lang.T("ttl.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                new Patcher(path).Apply(Selected, _sharedVision.Checked, Log);
                _svSyncedPath = null;   // force a re-read of the installed state
                UpdateCompat();
                MessageBox.Show(string.Format(Lang.T("msg.patchOk"), Selected.Title),
                    Lang.T("ttl.done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                Log(string.Format(Lang.T("log.error"), "access denied"));
                MessageBox.Show(Lang.T("msg.denied"), Lang.T("ttl.denied"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Log(string.Format(Lang.T("log.error"), ex.Message));
                MessageBox.Show(ex.Message, Lang.T("ttl.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void DoRestore()
        {
            var path = _pathBox.Text.Trim();
            if (!string.Equals(Path.GetFileName(path), PatchData.TargetFileName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(Lang.T("msg.wrongName"), Lang.T("ttl.error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                new Patcher(path).Restore(Log);
                _svSyncedPath = null;
                UpdateCompat();
                MessageBox.Show(Lang.T("msg.restored"), Lang.T("ttl.done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log(string.Format(Lang.T("log.error"), ex.Message));
                MessageBox.Show(ex.Message, Lang.T("ttl.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
