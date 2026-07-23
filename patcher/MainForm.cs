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

        // --- 2.0 cheat tab ---
        TabControl _tabs;
        TabPage _tabPatch, _tabCheats;
        GroupBox _cheatGroup, _globalGroup, _unlockGroup;
        Label _scopeNote, _unlockNote, _partsForLabel, _crewsNote;
        RadioButton _scopePlayer, _scopeAll;
        RadioButton _partsScopePlayer, _partsScopeAll;
        CheckBox _cheatFog, _cheatBuild, _cheatTurbo, _cheatCrews;
        TreeView _unlockTree;
        bool _treeCascading;   // guards the parent<->child check cascade against recursion
        Label _svFogNote, _fogSvNote;   // "disabled because ..." notes for the fog/shared-vision clash
        bool _fogSvSyncing;    // guards the fog <-> shared-vision mutual-exclusion cascade
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

        // True while the banner shows its cheat theme (driven by the active tab).
        bool _cheatUnlocked;

        public MainForm()
        {
            ClientSize = new Size(760, 744);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            _logo = LoadEmbedded("MetalFatiguePatcher.logo.png");
            _logoCheat = LoadEmbedded("MetalFatiguePatcher.logo_cheat.png");   // optional

            BuildBanner();

            // Tabs: "Patch" (the bug-fix, unchanged) and "Cheats" (2.0 — individually selectable).
            _tabs = new TabControl { Location = new Point(12, 126), Size = new Size(736, 396) };
            _tabPatch = new TabPage();
            _tabCheats = new TabPage();
            _tabs.TabPages.Add(_tabPatch);
            _tabs.TabPages.Add(_tabCheats);
            // The banner turns to cheat mode (orange + cheat mascot) on the Cheats tab.
            _tabs.SelectedIndexChanged += (s, e) => SetCheatBanner(_tabs.SelectedTab == _tabCheats);
            Controls.Add(_tabs);
            var tabPatch = _tabPatch;
            var tabCheats = _tabCheats;

            int y = 12;   // tab-local vertical walk

            // 1. Game source — radios, the MFatigue.exe path chooser, and the
            // compatibility status all live in this one frame.
            _srcGroup = new GroupBox { Location = new Point(12, y), Size = new Size(708, 118) };
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
            tabPatch.Controls.Add(_srcGroup);
            y += 128;

            // 2. Version
            _profGroup = new GroupBox { Location = new Point(12, y), Size = new Size(708, 100) };
            _prof2x        = new RadioButton { Location = new Point(14, 24),  AutoSize = true };
            _prof4x        = new RadioButton { Checked = true, Location = new Point(210, 24), AutoSize = true };
            _prof8x        = new RadioButton { Location = new Point(420, 24), AutoSize = true };
            _profUnleashed = new RadioButton { Location = new Point(600, 24), AutoSize = true };
            // Cheats moved to their own tab in 2.0; these two fields are kept only so the old
            // easter-egg / detection code still compiles. They are never shown or selectable.
            _profCheats    = new RadioButton { Visible = false };
            _profCheatsAll = new RadioButton { Visible = false };
            _profDesc = new Label { Location = new Point(14, 54), Size = new Size(680, 40), ForeColor = Color.DimGray };
            foreach (var rb in new[] { _prof2x, _prof4x, _prof8x, _profUnleashed })
                rb.CheckedChanged += (s, e) => UpdateProfDesc();
            _profGroup.Controls.AddRange(new Control[] { _prof2x, _prof4x, _prof8x, _profUnleashed, _profDesc });
            tabPatch.Controls.Add(_profGroup);
            y += 110;

            // 3. Shared vision — optional add-on, framed like the sections above.
            _svGroup = new GroupBox { Location = new Point(12, y), Size = new Size(708, 74) };
            _sharedVision = new CheckBox { Location = new Point(14, 24), AutoSize = true };
            _tips.SetToolTip(_sharedVision, "");
            _svStatus = new Label { Location = new Point(300, 26), AutoSize = true, Visible = false };
            _svFogNote = new Label
            {
                Text = "Disabled — \"No fog of war\" (Cheats tab) already reveals the whole map.",
                Location = new Point(14, 48), Size = new Size(680, 16), ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8f), Visible = false
            };
            _sharedVision.CheckedChanged += (s, e) => UpdateSharedVisionState();
            _svGroup.Controls.AddRange(new Control[] { _sharedVision, _svStatus, _svFogNote });
            tabPatch.Controls.Add(_svGroup);

            BuildCheatTab(tabCheats);

            // everything below the tabs
            y = 126 + _tabs.Height + 10;

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

        // ---------- cheat tab (2.0) ----------

        void BuildCheatTab(TabPage tab)
        {
            var orange = Color.FromArgb(186, 106, 16);

            // Scope: player only vs everyone (AI included). Parts/superweapon unlocks are always
            // local-player only, so the scope governs the resource/build cheats.
            // Scoped cheats — the "me only / all players" switch governs exactly these.
            _cheatGroup = new GroupBox { Location = new Point(12, 10), Size = new Size(708, 84), ForeColor = orange };
            _scopePlayer = new RadioButton { Checked = true, Location = new Point(14, 20), AutoSize = true };
            _scopeAll = new RadioButton { Location = new Point(120, 20), AutoSize = true };
            _cheatBuild = new CheckBox { Location = new Point(14, 48), AutoSize = true };
            _cheatTurbo = new CheckBox { Location = new Point(160, 48), AutoSize = true };
            // Fog sits on the right, right next to its "disabled because shared vision is on" note.
            _cheatFog   = new CheckBox { Location = new Point(300, 48), AutoSize = true };
            _fogSvNote = new Label
            {
                Location = new Point(470, 50), Size = new Size(232, 14), ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8f), Visible = false
            };
            _scopeNote = new Label
            {
                Location = new Point(300, 22), Size = new Size(400, 24), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8f)
            };
            _cheatFog.CheckedChanged += (s, e) => UpdateSharedVisionState();
            _cheatGroup.Controls.AddRange(new Control[] { _scopePlayer, _scopeAll, _scopeNote, _cheatFog, _cheatBuild, _cheatTurbo, _fogSvNote });
            tab.Controls.Add(_cheatGroup);

            // Always-global cheats — no scope, so they live in their own little section.
            _globalGroup = new GroupBox { Location = new Point(12, 102), Size = new Size(708, 48), ForeColor = orange };
            _cheatCrews = new CheckBox { Location = new Point(14, 20), AutoSize = true };
            // The crews cheat includes the crew-name fix, so it also lifts the ~50 combot limit even
            // on a non-Maximum version. Say so, since that overlaps with what the Version tab does.
            _crewsNote = new Label
            {
                Location = new Point(230, 22), Size = new Size(470, 16), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8f)
            };
            _globalGroup.Controls.AddRange(new Control[] { _cheatCrews, _crewsNote });
            tab.Controls.Add(_globalGroup);

            // Unlock tree: combot parts (by faction) + superweapons, each a checkable node.
            _unlockGroup = new GroupBox { Location = new Point(12, 158), Size = new Size(708, 200), ForeColor = orange };
            // Parts get their own scope: the AI does use foreign parts (confirmed in testing), so
            // "all players" is a real option here, separate from the resource-cheat scope above.
            _partsForLabel = new Label { Location = new Point(14, 22), AutoSize = true, ForeColor = Color.DimGray };
            _partsScopePlayer = new RadioButton { Checked = true, Location = new Point(48, 20), AutoSize = true };
            _partsScopeAll = new RadioButton { Location = new Point(140, 20), AutoSize = true };
            _unlockTree = new TreeView
            {
                Location = new Point(14, 46), Size = new Size(680, 110),
                CheckBoxes = true, ShowRootLines = true, HideSelection = true
            };
            BuildUnlockTree();
            _unlockTree.AfterCheck += (s, e) =>
            {
                if (_treeCascading) return;
                _treeCascading = true;
                foreach (TreeNode c in e.Node.Nodes) c.Checked = e.Node.Checked;
                _treeCascading = false;
            };
            // Two things worth stating up front (both are "won't fix", just expectations):
            _unlockNote = new Label
            {
                Location = new Point(14, 160), Size = new Size(680, 32), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8f)
            };
            _unlockGroup.Controls.AddRange(new Control[] { _partsForLabel, _partsScopePlayer, _partsScopeAll, _unlockTree, _unlockNote });
            tab.Controls.Add(_unlockGroup);
        }

        void BuildUnlockTree()
        {
            // Preserve ticks across a rebuild (e.g. a language switch re-labels the nodes).
            var wasChecked = new System.Collections.Generic.HashSet<uint>(GatherUnlockAddrs());

            _treeCascading = true;   // suppress the cascade handler while we repopulate
            _unlockTree.BeginUpdate();
            _unlockTree.Nodes.Clear();
            foreach (var faction in PartsData.Factions)
            {
                var fnode = new TreeNode(FactionLabel(faction));
                foreach (var slot in PartsData.Slots)
                    foreach (var p in PartsData.Parts)
                        if (p.Faction == faction && p.Slot == slot)
                            fnode.Nodes.Add(new TreeNode(p.Name + "  (" + Lang.T("slot." + slot.ToLowerInvariant()) + ")")
                                { Tag = p.Addr, Checked = wasChecked.Contains(p.Addr) });
                _unlockTree.Nodes.Add(fnode);
            }
            var sw = new TreeNode(Lang.T("tree.superweapons"));
            foreach (var s in PartsData.Superweapons)
                sw.Nodes.Add(new TreeNode(s.Name) { Tag = s.Addr, Checked = wasChecked.Contains(s.Addr) });
            _unlockTree.Nodes.Add(sw);
            _unlockTree.EndUpdate();
            _treeCascading = false;
        }

        static string FactionLabel(string key)
        {
            switch (key)
            {
                case "MilAgro": return "Mil-Agro";
                case "Hedoth":  return "Hedoth (Alien)";
                default:        return key;
            }
        }

        /// <summary>Collect the descriptor addresses of every checked part/superweapon leaf.</summary>
        System.Collections.Generic.List<uint> GatherUnlockAddrs()
        {
            var addrs = new System.Collections.Generic.List<uint>();
            foreach (TreeNode top in _unlockTree.Nodes)
                foreach (TreeNode leaf in top.Nodes)
                    if (leaf.Checked && leaf.Tag is uint a)
                        addrs.Add(a);
            return addrs;
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
                // Negative Y on purpose: both mascot PNGs carry ~11.7% transparent
                // padding on top, so the box is lifted out of the panel by exactly that
                // much. Only transparent pixels get clipped; the robot starts at y=4.
                Location = new Point(12, -11), Size = new Size(132, 132),
                BackColor = Color.Transparent
            };
            var pic = _mascot;

            _bannerTitle = new Label
            {
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(146, 34)
            };
            _bannerSub = new Label
            {
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(185, 200, 225), BackColor = Color.Transparent,
                AutoSize = true, Location = new Point(148, 64)
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

            // Version tag, bottom-right of the banner. Read from the assembly (single source of
            // truth = the .csproj <Version>), so it never drifts.
            var ver = new Label
            {
                Text = VersionString(),
                Font = new Font("Segoe UI", 7.5f), BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(200, 212, 228), AutoSize = true
            };
            _banner.Controls.AddRange(new Control[] { pic, _bannerTitle, _bannerSub, ver });
            ver.Location = new Point(_banner.Width - ver.Width - 8, _banner.Height - ver.Height - 4);
            ver.BringToFront();
            Controls.Add(_banner);
        }

        /// <summary>"v1.1.0" from the assembly version — no hard-coded string to keep in sync.</summary>
        static string VersionString()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v == null ? "" : string.Format("v{0}.{1}.{2}", v.Major, v.Minor, v.Build);
        }

        /// <summary>
        /// The banner follows the active tab: on the Cheats tab it turns orange and shows the
        /// cheat mascot; on the Patch tab it is the normal blue banner. (This replaced the old
        /// click-the-robot easter egg.)
        /// </summary>
        void SetCheatBanner(bool cheat)
        {
            if (_cheatUnlocked == cheat) return;
            _cheatUnlocked = cheat;
            // Title stays "Metal Fatigue Retrofit" in both modes; only the subtitle + theme change.
            _bannerTitle.Text = Lang.T("banner.title");
            _bannerSub.Text = Lang.T(cheat ? "banner.cheatSub" : "banner.sub");
            _bannerSub.ForeColor = cheat ? Color.FromArgb(255, 236, 190) : Color.FromArgb(185, 200, 225);
            // Swap in the alternate mascot. If it was never supplied, show a loud missing-texture
            // placeholder instead of silently keeping the normal one.
            if (_mascot != null)
            {
                if (!cheat) _mascot.Image = _logo;
                else if (_logoCheat != null) _mascot.Image = _logoCheat;
                else
                {
                    _mascot.Image = _missingMascot ?? (_missingMascot = MakeMissingMascot(144));
                    Log("[dev] logo_cheat.png is missing — showing placeholder mascot.");
                }
            }
            _banner.Invalidate();
        }

        // ---------- localization ----------

        void ApplyLanguage()
        {
            Text                 = Lang.T("window.title") + "  " + VersionString();
            _bannerTitle.Text    = Lang.T("banner.title");
            _bannerSub.Text      = _cheatUnlocked ? Lang.T("banner.cheatSub") : Lang.T("banner.sub");
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

            // --- 2.0 cheat tab ---
            if (_tabPatch != null)
            {
                _tabPatch.Text        = Lang.T("tab.patch");
                _tabCheats.Text       = Lang.T("tab.cheats");
                _cheatGroup.Text      = Lang.T("tab.cheats");
                _globalGroup.Text     = Lang.T("grp.globalcheats");
                _unlockGroup.Text     = Lang.T("grp.unlock");
                _scopePlayer.Text     = Lang.T("scope.me");
                _scopeAll.Text        = Lang.T("scope.all");
                _scopeNote.Text       = Lang.T("scope.note");
                _cheatFog.Text        = Lang.T("cheat.fog");
                _cheatBuild.Text      = Lang.T("cheat.build");
                _cheatTurbo.Text      = Lang.T("cheat.turbo");
                _cheatCrews.Text      = Lang.T("cheat.crews");
                _crewsNote.Text       = Lang.T("cheat.crews.note");
                _partsForLabel.Text   = Lang.T("unlock.for");
                _partsScopePlayer.Text = Lang.T("scope.me");
                _partsScopeAll.Text   = Lang.T("scope.all");
                _unlockNote.Text      = Lang.T("unlock.note");
                _svFogNote.Text       = Lang.T("note.svfog");
                _fogSvNote.Text       = Lang.T("note.fogsv");
                // The parts scope radios sit after the "For:" label, which resizes per language.
                _partsScopePlayer.Left = _partsForLabel.Right + 6;
                _partsScopeAll.Left    = _partsScopePlayer.Right + 12;
                // Re-label the tree nodes (slot suffixes + "Superweapons") in the new language.
                BuildUnlockTree();
            }

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
            _svGroup.Enabled = canPatch;
            _patchBtn.Enabled = canPatch;

            var path = _pathBox.Text.Trim();
            bool svInstalled = !string.IsNullOrEmpty(path) && Patcher.HasSharedVision(path);

            // Reflect what is already installed — but only once per file, so the user
            // can still tick/untick freely afterwards without being overridden.
            if (path != _svSyncedPath)
            {
                _svSyncedPath = path;
                _sharedVision.Checked = svInstalled;
                RestoreFromExe(path, profKey, c);
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
                if (_prof2x.Checked)        return PatchData.ByKey("balanced2x");
                if (_prof8x.Checked)        return PatchData.ByKey("balanced8x");
                if (_profUnleashed.Checked) return PatchData.ByKey("unleashed");
                return PatchData.ByKey("balanced4x");
            }
        }

        /// <summary>The extra sites for the currently ticked cheats and part/superweapon unlocks.</summary>
        System.Collections.Generic.List<PatchSite> ComposeExtras()
        {
            var features = new System.Collections.Generic.HashSet<string>();
            if (_cheatFog.Checked)   features.Add(PatchData.CheatFog);
            if (_cheatBuild.Checked) features.Add(PatchData.CheatFreeBuild);
            if (_cheatTurbo.Checked) features.Add(PatchData.CheatTurbo);
            if (_cheatCrews.Checked) features.Add(PatchData.CheatCrews);

            var extras = PatchData.CheatFeatureSites(features, _scopeAll.Checked);
            extras.AddRange(PatchData.PartsUnlockSites(GatherUnlockAddrs(), _partsScopeAll.Checked));
            return extras;
        }

        void UpdateProfDesc()
        {
            _profDesc.Text = Selected.Description;
            UpdateSharedVisionState();
        }

        /// <summary>
        /// "No fog of war" reveals the whole map, which makes "share vision with allies"
        /// meaningless — so the two are mutually exclusive. Whichever is ticked disables the
        /// other and shows a short note saying why. Runs across both tabs.
        /// </summary>
        void UpdateSharedVisionState()
        {
            if (_sharedVision == null || _cheatFog == null) return;
            if (_fogSvSyncing) return;
            _fogSvSyncing = true;
            try
            {
                bool fog = _cheatFog.Checked;
                bool sv = _sharedVision.Checked;

                // Fog wins if it is on: shared vision is moot with the whole map revealed.
                if (fog)
                {
                    if (sv) _sharedVision.Checked = false;
                    _sharedVision.Enabled = false;
                    _svFogNote.Visible = true;
                }
                else
                {
                    // Only re-enable the box if the whole group is enabled (compatible exe present).
                    _sharedVision.Enabled = _svGroup.Enabled;
                    _svFogNote.Visible = false;
                }

                if (sv && !fog)
                {
                    _cheatFog.Enabled = false;
                    _fogSvNote.Visible = true;
                }
                else
                {
                    _cheatFog.Enabled = true;
                    _fogSvNote.Visible = false;
                }
            }
            finally { _fogSvSyncing = false; }
        }

        /// <summary>
        /// Restore every UI setting from what the exe actually carries, so loading a file can
        /// never silently drop an installed cheat / unlock on the next patch. A pristine (or
        /// unknown) file resets everything to defaults.
        /// </summary>
        void RestoreFromExe(string path, string versionKey, Patcher.Compat c)
        {
            // Version radio from detection; default 4x when none is detected.
            switch (versionKey)
            {
                case "balanced2x": _prof2x.Checked = true; break;
                case "balanced8x": _prof8x.Checked = true; break;
                case "unleashed":  _profUnleashed.Checked = true; break;
                default:           _prof4x.Checked = true; break;
            }

            PatchData.Installed inst = null;
            if (c == Patcher.Compat.PatchedByUs || c == Patcher.Compat.Unsupported)
                try { inst = PatchData.DetectInstalled(File.ReadAllBytes(path)); } catch { }
            inst = inst ?? new PatchData.Installed();   // pristine / unreadable -> all off

            _cheatFog.Checked        = inst.Fog;
            _cheatBuild.Checked      = inst.FreeBuild;
            _cheatTurbo.Checked      = inst.Turbo;
            _cheatCrews.Checked      = inst.Crews;
            _scopeAll.Checked        = inst.CheatScopeAll;
            _scopePlayer.Checked     = !inst.CheatScopeAll;
            _partsScopeAll.Checked   = inst.PartsScopeAll;
            _partsScopePlayer.Checked = !inst.PartsScopeAll;

            // Restore the part/superweapon tree from the unlocked descriptor addresses.
            var set = new System.Collections.Generic.HashSet<uint>(inst.UnlockedAddrs);
            _treeCascading = true;
            foreach (TreeNode top in _unlockTree.Nodes)
            {
                bool allOn = top.Nodes.Count > 0;
                foreach (TreeNode leaf in top.Nodes)
                {
                    bool on = leaf.Tag is uint a && set.Contains(a);
                    leaf.Checked = on;
                    if (!on) allOn = false;
                }
                top.Checked = allOn;   // the faction parent reflects "all its parts on"
            }
            _treeCascading = false;
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
                new Patcher(path).Apply(Selected, _sharedVision.Checked, ComposeExtras(), Log);
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
