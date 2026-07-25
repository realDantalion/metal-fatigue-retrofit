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
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MetalFatiguePatcher
{
    public class MainForm : Form
    {
        TextBox _pathBox, _log;
        RadioButton _srcAuto, _srcSteam, _srcGog;
        RadioButton _prof2x, _prof4x, _prof8x, _profUnleashed, _profCheats, _profCheatsAll;
        Label _profDesc, _bannerTitle, _bannerSub, _exeLabel, _compatLabel, _credits, _svStatus, _infoLegacy;
        Label _infoBuild, _infoVariant, _infoInstalled;   // exe read-out area
        LinkLabel _contactLink, _licenseLink, _reportLink;
        PatchData.Installed _lastInstalled;                // what the current exe carries (for the read-out)
        string _reportKind;                                // "version" | "language" | null — drives the report link

        /// <summary>
        /// Where users can report an unsupported build. While this is empty the
        /// "contact us" link stays hidden, so no placeholder URL can ever ship.
        /// </summary>
        const string ContactUrl = "";

        /// <summary>Repository issue tracker — the report link points here (a real URL, unlike
        /// ContactUrl). A query pre-selects the right template for unknown builds vs unknown
        /// language patches.</summary>
        const string IssuesUrl = "https://github.com/realDantalion/metal-fatigue-retrofit/issues";
        GroupBox _srcGroup, _profGroup, _svGroup;
        CheckBox _sharedVision;

        // --- 2.0 cheat tab ---
        TabControl _tabs;
        TabPage _tabPatch, _tabCheats, _tabExperimental, _tabMusic;
        GroupBox _cheatGroup, _globalGroup, _unlockGroup;
        Label _scopeNote, _unlockNote, _partsForLabel, _crewsNote;
        RadioButton _scopePlayer, _scopeAll;
        RadioButton _partsScopePlayer, _partsScopeAll;
        CheckBox _cheatFog, _cheatBuild, _cheatTurbo, _cheatCrews;
        TreeView _unlockTree;
        bool _treeCascading;   // guards the parent<->child check cascade against recursion
        // Icon section-list: shown instead of the tree once the game's part icons decode from the
        // user's OWN files (see GameIcons). _iconToggles != null means we are in icon mode; every
        // unlock read/write branches on that, and any decode failure just leaves the tree up.
        FlowLayoutPanel _unlockList;
        System.Collections.Generic.List<CheckBox> _iconToggles;
        GameIcons _gameIcons;
        GameVariant _variant;              // which localised build the icons came from (maps IconIndex)
        GameVariant.Match _variantMatch;   // last detection result (for the exe-info panel / report)
        string _iconsPath;                 // the exe path our current icon/variant state reflects
        readonly System.Collections.Generic.HashSet<string> _collapsed = new System.Collections.Generic.HashSet<string>();
        Panel _unlockOverlay;                 // full-tab overlay the list pops into on hover
        System.Windows.Forms.Timer _hoverTimer;
        System.Windows.Forms.Timer _pulseTimer;
        bool _listBig;
        Label _svFogNote, _fogSvNote;   // "disabled because ..." notes for the fog/shared-vision clash
        bool _fogSvSyncing;    // guards the fog <-> shared-vision mutual-exclusion cascade

        // --- experimental tab (features that change core behaviour and may break things) ---
        GroupBox _expWarnGroup, _expSpeedGroup, _expSoonGroup;
        Label _expWarn, _expSoon;
        CheckBox _expSpeed;
        TrackBar _expSpeedBar;
        Label _expSpeedFactorLbl, _expSpeedValue, _expSpeedExample, _expSpeedForLbl, _expSpeedNote;
        RadioButton _expSpeedPlayer, _expSpeedAll;
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
        Image _logo, _logoCheat, _logoExperimental, _logoMusic;
        GroupBox _musicGroup;
        Label _musicIntro, _musicStatus, _musicTime, _musicVolLbl;
        Label _musicLegendActive, _musicLegendPending, _musicLegendMismatch;
        FlowLayoutPanel _musicLegend;
        Button _musicPickFolder, _musicPickZip, _musicRemove, _musicPlay, _musicPlayStop;
        DataGridView _musicGrid;
        TrackBar _musicSeek, _musicVol;
        Timer _musicTimer;
        MusicPreview _musicPreview;
        System.Collections.Generic.List<MusicImport.Candidate> _musicTracks =
            new System.Collections.Generic.List<MusicImport.Candidate>();
        bool _musicSeeking;
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

        // The banner theme is driven by the active tab: blue (Patch), orange (Cheats),
        // Neuropa faction green (Experimental).
        enum BannerTheme { Patch, Cheats, Experimental, Music }
        BannerTheme _bannerTheme = BannerTheme.Patch;

        public MainForm()
        {
            ClientSize = new Size(760, 782);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            _logo = LoadEmbedded("MetalFatiguePatcher.logo.png");
            _logoCheat = LoadEmbedded("MetalFatiguePatcher.logo_cheat.png");   // optional
            _logoExperimental = LoadEmbedded("MetalFatiguePatcher.logo_experimental.png");   // optional (Neuropa mascot)
            _logoMusic = LoadEmbedded("MetalFatiguePatcher.logo_music.png");                 // optional (Rimtech, with violin)

            BuildBanner();

            // Tabs: "Patch" (the bug-fix, unchanged) and "Cheats" (2.0 — individually selectable).
            _tabs = new TabControl { Location = new Point(12, 126), Size = new Size(736, 434) };
            _tabPatch = new TabPage();
            _tabCheats = new TabPage();
            _tabExperimental = new TabPage();
            _tabMusic = new TabPage();
            _tabs.TabPages.Add(_tabPatch);
            _tabs.TabPages.Add(_tabCheats);
            _tabs.TabPages.Add(_tabExperimental);
            _tabs.TabPages.Add(_tabMusic);
            // The banner theme follows the active tab: blue (Patch), orange (Cheats),
            // Neuropa faction green (Experimental).
            _tabs.SelectedIndexChanged += (s, e) => SetBannerTheme(
                _tabs.SelectedTab == _tabCheats       ? BannerTheme.Cheats :
                _tabs.SelectedTab == _tabExperimental ? BannerTheme.Experimental :
                _tabs.SelectedTab == _tabMusic        ? BannerTheme.Music :
                                                        BannerTheme.Patch);
            Controls.Add(_tabs);
            var tabPatch = _tabPatch;
            var tabCheats = _tabCheats;

            int y = 12;   // tab-local vertical walk

            // 1. Game source — radios, the MFatigue.exe path chooser, and the
            // compatibility status all live in this one frame.
            // 194 = the three-line worst case (label 82+50, then three 17px info rows, then padding).
            _srcGroup = new GroupBox { Location = new Point(12, y), Size = new Size(708, 194) };
            _srcAuto  = new RadioButton { Checked = true, Location = new Point(14, 24), AutoSize = true };
            _srcSteam = new RadioButton { Text = "Steam", Location = new Point(220, 24), AutoSize = true };
            _srcGog   = new RadioButton { Text = "GOG",   Location = new Point(330, 24), AutoSize = true };
            // Picking a source runs the search right away — no extra button needed.
            foreach (var rb in new[] { _srcAuto, _srcSteam, _srcGog })
                rb.CheckedChanged += (s, e) => { if (((RadioButton)s).Checked) Detect(); };
            // MFatigue.exe path chooser — second row.
            _exeLabel  = new Label   { Location = new Point(14, 59), AutoSize = true };
            // The frame is 708 wide and clips its children, so nothing may reach past 694
            // (= 708 - the same 14px margin the left edge uses). The Browse button was the
            // visible casualty: it ended at 720 and lost its right border to the frame.
            _pathBox   = new TextBox { Location = new Point(110, 54), Size = new Size(468, 24) };
            _browseBtn = new Button  { Location = new Point(586, 53), Size = new Size(108, 26) };
            _browseBtn.Click += (s, e) => Browse();
            _pathBox.TextChanged += (s, e) => UpdateCompat();
            // Read-out area: everything we can learn about the chosen exe. Populated by UpdateCompat.
            // Row 1 = compatibility status (coloured) + a report link when we can't fully support it.
            // The status line grows to fit its text — up to three lines, which is what the longest
            // message (patched by a superseded release) needs in German/French/Russian/Japanese at
            // the narrower 472px width the report link leaves. A status the user has to act on is
            // the worst thing to truncate. The frame reserves that worst case permanently and the
            // rows below follow the label, so short messages leave the slack at the bottom of the
            // frame instead of a hole in the middle, and nothing outside the frame ever moves.
            _compatLabel = new Label { Location = new Point(14, 82), Size = new Size(560, 18), AutoEllipsis = true };
            _reportLink = new LinkLabel
            {
                Location = new Point(494, 82), Size = new Size(200, 18),
                TextAlign = ContentAlignment.MiddleRight, Visible = false,
                LinkColor = Color.FromArgb(80, 130, 200)
            };
            _reportLink.LinkClicked += (s, e) => OpenReport();
            // Rows 2-4 = build, language variant, and what is already installed. Caption + value
            // are baked into each label's text in UpdateCompat (localised).
            // Tops here are the one-line case; ReflowReadout repositions them to follow the label.
            _infoBuild     = new Label { Location = new Point(14, 104), Size = new Size(680, 16), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.25f) };
            _infoVariant   = new Label { Location = new Point(14, 121), Size = new Size(680, 16), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.25f) };
            _infoInstalled = new Label { Location = new Point(14, 138), Size = new Size(680, 16), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.25f) };
            // Fourth row, shown only when an older release's layout is detected AND a clean backup
            // exists — i.e. the case that repairs itself, so this is advice, not a warning. It can
            // never coincide with the three-line compat message, which covers the no-backup case.
            _infoLegacy    = new Label { Location = new Point(14, 155), Size = new Size(680, 16), Visible = false,
                                         ForeColor = Color.FromArgb(176, 108, 12), Font = new Font("Segoe UI", 8.25f) };
            // Kept only so the old easter-egg/detection code still compiles; never shown.
            _contactLink = new LinkLabel { Visible = false };
            _srcGroup.Controls.AddRange(new Control[] {
                _srcAuto, _srcSteam, _srcGog, _exeLabel, _pathBox, _browseBtn,
                _compatLabel, _reportLink, _infoBuild, _infoVariant, _infoInstalled, _infoLegacy });
            tabPatch.Controls.Add(_srcGroup);
            y += 204;

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
            BuildExperimentalTab(_tabExperimental);
            BuildMusicTab(_tabMusic);

            // everything below the tabs
            y = 126 + _tabs.Height + 10;

            // 4. Buttons
            _patchBtn = new Button
            {
                Location = new Point(12, y), Size = new Size(140, 34),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            _patchBtn.Click += (s, e) => DoPatch();
            // Colour follows the enabled state, so hook it here rather than at every Enabled = ...
            _patchBtn.EnabledChanged += (s, e) => StylePatchButton();
            StylePatchButton();
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
            _cheatGroup = new GroupBox { Location = new Point(12, 10), Size = new Size(708, 90), ForeColor = orange };
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
                Location = new Point(14, 68), Size = new Size(690, 16), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8f)
            };
            _cheatFog.CheckedChanged += (s, e) => UpdateSharedVisionState();
            _cheatGroup.Controls.AddRange(new Control[] { _scopePlayer, _scopeAll, _scopeNote, _cheatFog, _cheatBuild, _cheatTurbo, _fogSvNote });
            tab.Controls.Add(_cheatGroup);

            // Always-global cheats — no scope, so they live in their own little section.
            _globalGroup = new GroupBox { Location = new Point(12, 106), Size = new Size(708, 48), ForeColor = orange };
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
            _unlockGroup = new GroupBox { Location = new Point(12, 160), Size = new Size(708, 200), ForeColor = orange };
            // Parts get their own scope: the AI does use foreign parts (confirmed in testing), so
            // "all players" is a real option here, separate from the resource-cheat scope above.
            _partsForLabel = new Label { Location = new Point(14, 22), AutoSize = true, ForeColor = Color.DimGray };
            _partsScopePlayer = new RadioButton { Checked = true, Location = new Point(48, 20), AutoSize = true };
            _partsScopeAll = new RadioButton { Location = new Point(140, 20), AutoSize = true };
            _unlockTree = new TreeView
            {
                Location = new Point(14, 46), Size = new Size(680, 86),
                CheckBoxes = true, ShowRootLines = true, HideSelection = true
            };
            BuildUnlockTree();
            _unlockTree.AfterCheck += (s, e) =>
            {
                if (_treeCascading) return;
                _treeCascading = true;
                foreach (TreeNode c in e.Node.Nodes) c.Checked = e.Node.Checked;
                if (e.Node.Tag is uint addr) SyncSharedDescriptor(addr, e.Node.Checked, e.Node);
                _treeCascading = false;
            };
            // Two things worth stating up front (both are "won't fix", just expectations):
            // Tall enough for the longest translation: the two sentences can wrap to ~4 lines in
            // German/Spanish/etc. A fixed 2-line box clipped the second (alien) sentence there.
            _unlockNote = new Label
            {
                Location = new Point(14, 136), Size = new Size(680, 62), ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8f)
            };
            // Icon section-list occupies the same spot as the tree; hidden until icons load.
            _unlockList = new FlowLayoutPanel
            {
                Location = new Point(14, 44), Size = new Size(680, 124),
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoScroll = true, Visible = false, BackColor = Color.FromArgb(30, 30, 30)
            };
            _unlockGroup.Controls.AddRange(new Control[] { _partsForLabel, _partsScopePlayer, _partsScopeAll, _unlockTree, _unlockList, _unlockNote });
            tab.Controls.Add(_unlockGroup);

            // Hover-to-enlarge: the cramped list pops into this full-tab overlay while the mouse is
            // over it, and snaps back when the cursor leaves (checked by a small timer, so moving
            // across child toggles doesn't count as "leaving").
            _unlockOverlay = new Panel
            {
                Location = new Point(8, 6), Size = new Size(714, 358), Visible = false,
                BackColor = Color.FromArgb(26, 26, 30), BorderStyle = BorderStyle.FixedSingle
            };
            tab.Controls.Add(_unlockOverlay);
            _hoverTimer = new System.Windows.Forms.Timer { Interval = 160 };
            _hoverTimer.Tick += (s, e) =>
            {
                if (!_unlockOverlay.RectangleToScreen(_unlockOverlay.ClientRectangle).Contains(Cursor.Position)) HideBig();
            };
            _unlockList.MouseEnter += (s, e) => ShowBig();

            // Gentle pulse of the selected toggles' borders.
            _pulseTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _pulseTimer.Tick += (s, e) =>
            {
                if (_iconToggles == null) return;
                IconToggle.Advance();
                foreach (var cb in _iconToggles) if (cb.Checked) cb.Invalidate();
            };
            _pulseTimer.Start();
        }

        /// <summary>
        /// Music tab: put back the Rimtech soundtrack the re-release is missing. Unlike every other
        /// tab this one needs files from the user — the patcher ships no audio and names no source.
        ///
        /// The list lives here rather than in a dialog, because it is not a one-off question: it IS
        /// the state of the feature. It shows what is on disk right now, so loading an already-patched
        /// game reconstructs the arrangement the same way every other tab restores its settings.
        ///
        /// Importing only copies files. The 16-byte table edit rides along with the normal Patch
        /// button like every other change — writing the exe straight from here would be the one place
        /// in the tool where a setting takes effect without pressing Patch.
        /// </summary>
        void BuildMusicTab(TabPage tab)
        {
            var violet = Color.FromArgb(88, 48, 150);
            _musicGroup = new GroupBox { Location = new Point(12, 6), Size = new Size(708, 380), ForeColor = violet };

            // y=26, not 20: a GroupBox draws its caption on the frame line, and text starting any
            // higher runs into it.
            _musicIntro = new Label
            {
                Location = new Point(14, 26), Size = new Size(680, 42),
                ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.25f)
            };

            _musicPickFolder = new Button { Location = new Point(14, 74), Size = new Size(150, 26) };
            _musicPickZip    = new Button { Location = new Point(172, 74), Size = new Size(150, 26) };
            _musicRemove     = new Button { Location = new Point(544, 74), Size = new Size(150, 26) };
            _musicPickFolder.Click += (s, e) => ImportMusic(false);
            _musicPickZip.Click    += (s, e) => ImportMusic(true);
            _musicRemove.Click     += (s, e) => RemoveMusic();

            // A grid, not a ListView: the per-row buttons are the discoverable way to reorder, and
            // only a grid gives them to us without owner-drawing and hit-testing by hand.
            _musicGrid = new DataGridView
            {
                Location = new Point(14, 108), Size = new Size(680, 192),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                // Text columns may be widened — a long file name is worth more room. The two button
                // columns are pinned further down: stretching a button changes nothing but its looks.
                AllowUserToResizeColumns = true, AllowUserToOrderColumns = false,
                RowHeadersVisible = false, MultiSelect = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.FixedSingle,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                Font = new Font("Segoe UI", 8.25f), ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                // Vertical only: the columns are sized to fit, and a horizontal bar would just be a
                // sign that they are not.
                ScrollBars = ScrollBars.Vertical,
            };
            _musicGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "slot", Width = 62 });
            _musicGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "file", Width = 228 });
            _musicGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "exp",  Width = 72,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _musicGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "act",  Width = 72,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _musicGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "match", Width = 112 });
            // Only the reorder arrows live in the rows — they act on that row and nothing else.
            // Playback belongs to the transport below, next to stop and the seek bar, because those
            // three are one control surface for one thing that is playing.
            _musicGrid.Columns.Add(new DataGridViewButtonColumn { Name = "up",   Width = 34, Text = "▲", UseColumnTextForButtonValue = true, HeaderText = "",
                Resizable = DataGridViewTriState.False });
            _musicGrid.Columns.Add(new DataGridViewButtonColumn { Name = "down", Width = 34, Text = "▼", UseColumnTextForButtonValue = true, HeaderText = "",
                Resizable = DataGridViewTriState.False });
            _musicGrid.CellContentClick += MusicGridClick;
            _musicGrid.SelectionChanged += (s, e) => UpdateMusicTransport();

            _musicPlay = new Button { Location = new Point(14, 308), Size = new Size(70, 26), Text = "▶", Enabled = false };
            _musicPlay.Click += (s, e) =>
            {
                var t = _musicGrid.CurrentRow?.Tag as MusicImport.Candidate;
                if (t != null) PlayMusicRow(t);
            };
            _musicPlayStop = new Button { Location = new Point(90, 308), Size = new Size(70, 26), Text = "■", Enabled = false };
            _musicPlayStop.Click += (s, e) => { _musicPreview?.Stop(); UpdateMusicTransport(); };
            // AutoSize = false is load-bearing: a TrackBar ignores the height you give it and grows to
            // 45px, which silently covered the status line underneath it.
            _musicSeek = new TrackBar
            {
                AutoSize = false,
                Location = new Point(166, 306), Size = new Size(330, 28),
                Minimum = 0, Maximum = 1000, TickStyle = TickStyle.None, Enabled = false
            };
            _musicVolLbl = new Label { Location = new Point(506, 312), AutoSize = true, Text = "🔊", ForeColor = Color.DimGray };
            _musicVol = new TrackBar
            {
                AutoSize = false,
                Location = new Point(528, 306), Size = new Size(86, 28),
                Minimum = 0, Maximum = 100, Value = 70, TickStyle = TickStyle.None
            };
            // Volume is remembered on the preview, so it survives switching tracks and is applied
            // again whenever a new output device is opened.
            _musicVol.ValueChanged += (s, e) =>
            {
                if (_musicPreview != null) _musicPreview.Volume = _musicVol.Value / 100.0;
            };
            _musicSeek.MouseDown += (s, e) => _musicSeeking = true;
            _musicSeek.MouseUp += (s, e) =>
            {
                _musicSeeking = false;
                if (_musicPreview != null && _musicPreview.Length > 0)
                    _musicPreview.SeekTo(_musicPreview.Length * _musicSeek.Value / 1000.0);
            };
            _musicTime = new Label { Location = new Point(622, 312), Size = new Size(72, 18), ForeColor = Color.DimGray };

            // A colour that is never explained is a puzzle. The legend flows left-to-right so the
            // three entries stay side by side whatever their length in the current language.
            _musicLegend = new FlowLayoutPanel
            {
                Location = new Point(12, 340), Size = new Size(684, 20),
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = false
            };
            _musicLegendActive   = LegendDot(Color.FromArgb(22, 120, 52));
            _musicLegendPending  = LegendDot(Color.Gray);
            _musicLegendMismatch = LegendDot(Color.FromArgb(176, 108, 12));
            _musicLegend.Controls.AddRange(new Control[] { _musicLegendActive, _musicLegendPending, _musicLegendMismatch });

            _musicStatus = new Label
            {
                Location = new Point(14, 362), Size = new Size(680, 18), Font = new Font("Segoe UI", 8.25f)
            };

            _musicTimer = new Timer { Interval = 200 };
            _musicTimer.Tick += (s, e) => UpdateMusicTransport();
            _musicTimer.Start();

            _musicGroup.Controls.AddRange(new Control[] {
                _musicIntro, _musicPickFolder, _musicPickZip, _musicRemove,
                _musicGrid, _musicPlay, _musicPlayStop, _musicSeek,
                _musicVolLbl, _musicVol, _musicTime, _musicLegend, _musicStatus });
            tab.Controls.Add(_musicGroup);
        }

        /// <summary>
        /// The "Experimental" tab: a home for features that touch core game behaviour and may
        /// break saves / multiplayer. Framed in the Neuropa faction green. First resident is the

        /// <summary>
        /// Import ten user-supplied OGG files as Rimtech's soundtrack. Everything is confirmed before
        /// anything is written: the mapping is shown, the files go in first, and only once they are
        /// verified on disk does the exe get its 16-byte table edit. Failing halfway leaves the game
        /// exactly as it was.
        /// </summary>
        void ImportMusic(bool fromZip)
        {
            var exe = _pathBox.Text.Trim();
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return;

            string source;
            if (fromZip)
            {
                using (var d = new OpenFileDialog { Filter = "Zip|*.zip", CheckFileExists = true })
                {
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    source = d.FileName;
                }
            }
            else
            {
                using (var d = new FolderBrowserDialog())
                {
                    if (d.ShowDialog(this) != DialogResult.OK) return;
                    source = d.SelectedPath;
                }
            }

            var scan = MusicImport.Collect(source);
            try
            {
                if (scan.Error != null)
                {
                    string msg = scan.Error == "music.err.wrongFormat"
                        ? string.Format(Lang.T(scan.Error), scan.OtherFormats.Count)
                        : scan.Error == "music.err.count"
                            ? string.Format(Lang.T(scan.Error), scan.Tracks.Count)
                            : Lang.T(scan.Error);
                    MessageBox.Show(this, msg, Lang.T("tab.music"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                MusicImport.Assign(scan.Tracks);

                // Copy the files and stop. The table edit is applied by the Patch button along with
                // everything else — this is the only sane place for it, because Apply rebuilds the
                // exe from the clean backup and would otherwise throw a directly-written table away.
                MusicImport.Install(exe, scan.Tracks, Log);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Lang.T("tab.music"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                MusicImport.CleanTemp(scan);
                Detect();
                UpdateMusicState();   // the grid mirrors the disk, so refresh it here and not by luck
            }
        }

        /// <summary>Write (or rewrite) the track-range table edit on top of whatever is patched now.</summary>
        void ApplyMusicTablePatch(string exe)
        {
            var data = File.ReadAllBytes(exe);
            foreach (var s in PatchData.RimtechMusicSites())
            {
                for (int i = 0; i < s.Patched.Length; i++) data[s.Offset + i] = s.Patched[i];
            }
            File.WriteAllBytes(exe, data);
        }

        /// <summary>One legend entry: a coloured bullet plus its caption, in the colour it explains.</summary>
        static Label LegendDot(Color c) => new Label
        {
            AutoSize = true, ForeColor = c, Margin = new Padding(0, 2, 22, 0),
            Font = new Font("Segoe UI", 8.25f)
        };

        /// <summary>Redraw the grid from what is actually on disk — the tab shows state, not a plan.</summary>
        void FillMusicGrid()
        {
            if (_musicGrid == null) return;
            var exe = _pathBox.Text.Trim();
            _musicTracks = string.IsNullOrEmpty(exe) || !File.Exists(exe)
                ? new System.Collections.Generic.List<MusicImport.Candidate>()
                : MusicImport.LoadInstalled(exe);

            int keep = _musicGrid.CurrentRow?.Index ?? -1;
            _musicGrid.Rows.Clear();
            foreach (var t in _musicTracks.OrderBy(x => x.Slot))
            {
                int i = _musicGrid.Rows.Add(
                    string.Format("Track{0:00}", t.Slot), t.Name,
                    FmtTime(t.SlotSeconds), FmtTime(t.Seconds),
                    Lang.T(t.Confidence == MusicImport.Confidence.Exact ? "music.match.exact"
                         : t.Confidence == MusicImport.Confidence.Duration ? "music.match.duration"
                         : "music.match.uncertain"),
                    null, null, null);
                _musicGrid.Rows[i].Tag = t;

                // Three readable states, so the list answers "what is going on" at a glance:
                //   amber  — the length does not match this slot, worth a look
                //   grey   — the files are here but the game is not using them yet
                //   green  — in place and active
                bool active = _lastInstalled != null && _lastInstalled.RimtechMusic;
                _musicGrid.Rows[i].DefaultCellStyle.ForeColor =
                    t.Confidence == MusicImport.Confidence.Uncertain ? Color.FromArgb(176, 108, 12)
                  : active ? Color.FromArgb(22, 120, 52)
                  : Color.Gray;
            }
            if (keep >= 0 && keep < _musicGrid.Rows.Count)
                _musicGrid.CurrentCell = _musicGrid.Rows[keep].Cells[0];
        }

        static string FmtTime(double s) => string.Format("{0}:{1:00}", (int)(s / 60), (int)(s % 60));

        void MusicGridClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var t = _musicGrid.Rows[e.RowIndex].Tag as MusicImport.Candidate;
            if (t == null) return;

            switch (_musicGrid.Columns[e.ColumnIndex].Name)
            {
                case "up":   MoveMusicRow(e.RowIndex, -1); break;
                case "down": MoveMusicRow(e.RowIndex, +1); break;
            }
        }

        /// <summary>
        /// Swap two tracks' slots and move the files to match, then re-judge both against their new
        /// reference length. Re-judging rather than flagging "changed" matters: swap a pair back and
        /// the rows go black again, because the order really is the matched one once more.
        /// </summary>
        void MoveMusicRow(int row, int delta)
        {
            int other = row + delta;
            if (other < 0 || other >= _musicGrid.Rows.Count) return;
            var a = _musicGrid.Rows[row].Tag as MusicImport.Candidate;
            var b = _musicGrid.Rows[other].Tag as MusicImport.Candidate;
            if (a == null || b == null) return;

            _musicPreview?.Stop();
            int tmp = a.Slot; a.Slot = b.Slot; b.Slot = tmp;
            try
            {
                MusicImport.Rearrange(_pathBox.Text.Trim(), _musicTracks, Log);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Lang.T("tab.music"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            FillMusicGrid();
            UpdateMusicState();
            if (other < _musicGrid.Rows.Count) _musicGrid.CurrentCell = _musicGrid.Rows[other].Cells[0];
        }

        void PlayMusicRow(MusicImport.Candidate t)
        {
            try
            {
                if (_musicPreview == null) _musicPreview = new MusicPreview(_pathBox.Text.Trim());
                _musicPreview.Volume = _musicVol.Value / 100.0;
                _musicPreview.Play(t.Path);
            }
            catch (Exception ex)
            {
                // A broken preview must never block the import — it is an aid, not a gate.
                _musicTime.Text = "—";
                Log("[dev] preview: " + ex.Message);
            }
            UpdateMusicTransport();
        }

        void UpdateMusicTransport()
        {
            if (_musicSeek == null) return;
            bool on = _musicPreview != null && _musicPreview.IsPlaying;
            // Play needs something to play: greyed out until a row is picked, so the button never
            // looks available while it would do nothing.
            _musicPlay.Enabled = _musicGrid.CurrentRow?.Tag is MusicImport.Candidate;
            _musicPlayStop.Enabled = on;
            _musicSeek.Enabled = on;
            if (on && _musicPreview.Length > 0)
            {
                if (!_musicSeeking)
                    _musicSeek.Value = Math.Max(0, Math.Min(1000,
                        (int)(_musicPreview.Position / _musicPreview.Length * 1000)));
                _musicTime.Text = FmtTime(_musicPreview.Position) + " / " + FmtTime(_musicPreview.Length);
            }
            else if (!on) _musicTime.Text = "";
        }

        /// <summary>Take the music back out: the table first, then the files it pointed at.</summary>
        void RemoveMusic()
        {
            var exe = _pathBox.Text.Trim();
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return;
            try
            {
                // The preview keeps a file handle open; delete would fail on whatever is loaded and
                // that track would quietly survive the removal.
                _musicPreview?.Stop();

                var data = File.ReadAllBytes(exe);
                foreach (var s in PatchData.RimtechMusicSites())
                    for (int i = 0; i < s.Original.Length; i++) data[s.Offset + i] = s.Original[i];
                File.WriteAllBytes(exe, data);

                System.Collections.Generic.List<string> failed;
                int n = MusicImport.Remove(exe, out failed);
                Log(string.Format("{0}  ({1})", Lang.T("music.remove"), n));

                if (failed.Count > 0)
                    MessageBox.Show(this, string.Format(Lang.T("music.err.locked"), string.Join(", ", failed)),
                                    Lang.T("tab.music"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Lang.T("tab.music"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Detect(); UpdateMusicState(); }
        }

        /// <summary>Reflect what is installed right now: files present AND the table pointing at them.</summary>
        void UpdateMusicState()
        {
            if (_musicStatus == null) return;
            var path = _pathBox.Text.Trim();
            bool haveFiles = !string.IsNullOrEmpty(path) && MusicImport.FilesPresent(path);
            bool installed = haveFiles && _lastInstalled != null && _lastInstalled.RimtechMusic;

            // Reconstruct the real arrangement, not just "something is there": if a file sits on a
            // slot whose reference length it does not match, say so — that is exactly the failure
            // this feature can have, and it is otherwise inaudible until you play as Rimtech.
            int odd = 0;
            if (haveFiles)
                odd = MusicImport.LoadInstalled(path)
                        .Count(t => t.Confidence == MusicImport.Confidence.Uncertain);

            // Two independent facts, so say both. Which state the feature is in comes first — that is
            // what the user came to find out — and a length mismatch is appended, not substituted.
            // Reporting only the mismatch used to hide whether the exe was patched at all.
            bool tablePatched = _lastInstalled != null && _lastInstalled.RimtechMusic;

            string text;
            Color colour;
            if (installed)
            { text = Lang.T("music.status.installed"); colour = Color.FromArgb(22, 120, 52); }
            else if (tablePatched)
            {
                // The exe points at tracks 24–33 but they are not on disk. The game folds anything
                // above the file count down by ten, so Rimtech would play Neuropa's music — worse
                // than doing nothing, and completely silent about it. Say so plainly.
                text = Lang.T("music.status.orphan"); colour = Color.FromArgb(192, 32, 32);
            }
            else if (haveFiles)
            { text = Lang.T("music.status.ready");     colour = Color.FromArgb(176, 108, 12); }
            else
            { text = Lang.T("music.status.none");      colour = Color.DimGray; }

            if (haveFiles && odd > 0)
            {
                text += "   " + string.Format(Lang.T("music.status.check"), odd);
                colour = Color.FromArgb(176, 108, 12);
            }
            _musicStatus.Text = text;
            _musicStatus.ForeColor = colour;

            _musicRemove.Enabled = haveFiles;
            FillMusicGrid();

            bool canPatch = Patcher.HasValidBackup(path) || Patcher.LooksPristine(path);
            _musicPickFolder.Enabled = _musicPickZip.Enabled = canPatch;
        }

        /// <summary>
        /// movement-speed multiplier (see PatchData.MoveSpeedSites).
        /// </summary>
        void BuildExperimentalTab(TabPage tab)
        {
            var green = Color.FromArgb(36, 132, 70);   // Neuropa faction green (#3AB451 family)

            // Read-first warning — these features can damage saved games / multiplayer.
            _expWarnGroup = new GroupBox { Location = new Point(12, 10), Size = new Size(708, 84), ForeColor = green };
            _expWarn = new Label
            {
                Location = new Point(14, 22), Size = new Size(684, 56), ForeColor = Color.FromArgb(150, 96, 20)
            };
            _expWarnGroup.Controls.Add(_expWarn);
            tab.Controls.Add(_expWarnGroup);

            // --- movement speed ---------------------------------------------------------------
            _expSpeedGroup = new GroupBox { Location = new Point(12, 102), Size = new Size(708, 186), ForeColor = green };
            _expSpeed = new CheckBox { Location = new Point(14, 22), AutoSize = true };

            _expSpeedFactorLbl = new Label { Location = new Point(34, 62), AutoSize = true, ForeColor = Color.DimGray };
            // Discrete notches rather than a free slider: each stop is a factor we can reason about,
            // and it maps 1:1 to the float baked into the patch.
            _expSpeedBar = new TrackBar
            {
                Location = new Point(96, 54), Size = new Size(300, 45),
                Minimum = 0, Maximum = PatchData.SpeedFactors.Length - 1,
                TickFrequency = 1, SmallChange = 1, LargeChange = 1, Value = 1
            };
            _expSpeedValue = new Label
            {
                Location = new Point(406, 60), Size = new Size(70, 26), ForeColor = green,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold)
            };
            _expSpeedExample = new Label
            {
                Location = new Point(482, 64), Size = new Size(216, 30), ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8f)
            };
            _expSpeedBar.ValueChanged += (s, e) => UpdateSpeedLabels();

            _expSpeedForLbl = new Label { Location = new Point(34, 112), AutoSize = true, ForeColor = Color.DimGray };
            _expSpeedPlayer = new RadioButton { Checked = true, Location = new Point(80, 110), AutoSize = true };
            _expSpeedAll = new RadioButton { Location = new Point(180, 110), AutoSize = true };

            _expSpeedNote = new Label
            {
                Location = new Point(14, 140), Size = new Size(684, 40), ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8f)
            };

            // The whole feature is off until it's ticked, so the knobs follow the checkbox.
            _expSpeed.CheckedChanged += (s, e) => UpdateSpeedEnabled();

            _expSpeedGroup.Controls.AddRange(new Control[]
            {
                _expSpeed, _expSpeedFactorLbl, _expSpeedBar, _expSpeedValue, _expSpeedExample,
                _expSpeedForLbl, _expSpeedPlayer, _expSpeedAll, _expSpeedNote
            });
            tab.Controls.Add(_expSpeedGroup);

            // Slim footer: more experimental features are expected here later.
            _expSoonGroup = new GroupBox { Location = new Point(12, 296), Size = new Size(708, 62), ForeColor = green };
            _expSoon = new Label
            {
                Location = new Point(14, 24), Size = new Size(684, 30), ForeColor = Color.DimGray
            };
            _expSoonGroup.Controls.Add(_expSoon);
            tab.Controls.Add(_expSoonGroup);

            UpdateSpeedEnabled();
        }

        /// <summary>The multiplier the speed slider currently sits on.</summary>
        double SpeedFactor => PatchData.SpeedFactors[
            Math.Min(Math.Max(_expSpeedBar.Value, 0), PatchData.SpeedFactors.Length - 1)];

        /// <summary>Snap the slider to the ladder entry nearest an arbitrary factor (e.g. one read
        /// back out of an already-patched exe).</summary>
        void SetSpeedFactor(double f)
        {
            int best = 1;
            double bestD = double.MaxValue;
            for (int i = 0; i < PatchData.SpeedFactors.Length; i++)
            {
                double d = Math.Abs(PatchData.SpeedFactors[i] - f);
                if (d < bestD) { bestD = d; best = i; }
            }
            _expSpeedBar.Value = best;
        }

        void UpdateSpeedLabels()
        {
            if (_expSpeedValue == null) return;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            double f = SpeedFactor;
            _expSpeedValue.Text = f.ToString("0.0", inv) + "×";
            _expSpeedExample.Text = string.Format(inv, Lang.T("exp.speed.example"),
                                                  f.ToString("0.0", inv),
                                                  (28.0 * f).ToString("0.#", inv),
                                                  (15.5 * f).ToString("0.#", inv));
        }

        void UpdateSpeedEnabled()
        {
            if (_expSpeed == null) return;
            bool on = _expSpeed.Checked;
            _expSpeedFactorLbl.Enabled = on;
            _expSpeedBar.Enabled = on;
            _expSpeedValue.Enabled = on;
            _expSpeedExample.Enabled = on;
            _expSpeedForLbl.Enabled = on;
            _expSpeedPlayer.Enabled = on;
            _expSpeedAll.Enabled = on;
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

        /// <summary>Collect the descriptor addresses of every checked part/superweapon.</summary>
        System.Collections.Generic.List<uint> GatherUnlockAddrs()
        {
            var addrs = new System.Collections.Generic.List<uint>();
            if (_iconToggles != null)   // icon mode
            {
                foreach (var cb in _iconToggles)
                    if (cb.Checked && cb.Tag is uint a) addrs.Add(a);
            }
            else                        // tree fallback
            {
                foreach (TreeNode top in _unlockTree.Nodes)
                    foreach (TreeNode leaf in top.Nodes)
                        if (leaf.Checked && leaf.Tag is uint a)
                            addrs.Add(a);
            }
            return addrs;
        }

        // ---------- icon section-list (shown instead of the tree when GameIcons decodes) ----------

        /// <summary>Try to load the game's part icons from the chosen exe's folder and switch the
        /// unlock UI from the text tree to the icon section-list. Any failure keeps the tree.</summary>
        void EnsureIcons(string exePath)
        {
            // Re-read whenever the exe path changes (e.g. switching the Steam/GOG source), so an
            // English install and a German-patched one show their own icons. Same path -> no work.
            if (string.IsNullOrWhiteSpace(exePath) || exePath == _iconsPath) return;
            _iconsPath = exePath;

            var gi = GameIcons.TryLoad(exePath);
            var match = gi != null ? GameVariant.Detect(gi) : null;

            if (gi == null || match == null || match.Variant == null)
            {
                // Undecodable or an unknown/mismatched build: fall back to the text tree so we never
                // show wrong icons. The exe-info panel surfaces this + a report link.
                _variantMatch = match;
                _gameIcons = null;
                _variant = null;
                if (_iconToggles != null) ShowTreeView();   // we were in icon mode -> revert
                return;
            }

            _gameIcons = gi;
            _variant = match.Variant;
            _variantMatch = match;
            BuildUnlockList();
            _unlockTree.Visible = false;
            _unlockList.Visible = true;
            _unlockNote.Location = new Point(14, 172);   // list is taller than the tree was
            _unlockNote.Size = new Size(680, 26);
        }

        /// <summary>Switch the unlock UI back to the plain text tree (unknown build / decode failure).</summary>
        void ShowTreeView()
        {
            _iconToggles = null;
            BuildUnlockTree();
            _unlockList.Visible = false;
            _unlockTree.Visible = true;
            _unlockNote.Location = new Point(14, 136);   // tree layout
            _unlockNote.Size = new Size(680, 62);
        }

        /// <summary>Language switch / initial fill: rebuild whichever unlock view is active.</summary>
        void RebuildUnlockView()
        {
            if (_gameIcons != null) BuildUnlockList();
            else BuildUnlockTree();
        }

        /// <summary>A scaled copy of a faction's icon at the given catalogue index, or null.</summary>
        Image IconFor(string faction, int index, int size)
        {
            // Hedoth has no own structures file, but the alien icons live in every faction file,
            // so borrow them from MilAgro.
            var src = faction == "Hedoth" ? "MilAgro" : faction;
            var list = _gameIcons?.Faction(src);
            if (list == null) return null;
            // Translate the canonical (English) catalogue index into this build's file index; a
            // localisation may have dropped the icon (-1) or the list may be short.
            int fi = _variant != null ? _variant.MapIcon(index) : index;
            if (fi < 0 || fi >= list.Count) return null;
            try
            {
                var bmp = list[fi];
                var box = GameIcons.OpaqueBounds(bmp);   // crop off the transparent right/bottom padding
                var outp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(outp))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bmp, new Rectangle(0, 0, size, size), box, GraphicsUnit.Pixel);
                }
                return outp;
            }
            catch { return null; }
        }

        /// <summary>One toggle button for a part/superweapon: its icon, or a text fallback.</summary>
        CheckBox MakeToggle(string faction, int iconIndex, uint addr, string name, bool @checked)
        {
            var cb = new IconToggle
            {
                Size = new Size(54, 54), Margin = new Padding(3),
                Tag = addr, Checked = @checked, Icon = IconFor(faction, iconIndex, 54)
            };
            if (cb.Icon == null) { cb.Text = name; cb.Font = new Font("Segoe UI", 7f); }   // text fallback when no icon
            _tips.SetToolTip(cb, name);
            // Attached after the initializer set Checked, so building the list never fires this.
            cb.CheckedChanged += (s, e) =>
            {
                if (_treeCascading) return;
                _treeCascading = true;
                SyncSharedDescriptor(addr, ((CheckBox)s).Checked, s);
                _treeCascading = false;
            };
            _iconToggles.Add(cb);
            return cb;
        }

        /// <summary>Keep the twins of a shared descriptor in step. A few parts are listed under two
        /// factions but are a single class in the game — the JetPack torso appears under MilAgro and
        /// Neuropa yet is one descriptor (CJetPackTorso, 0x5731b0). Unlocking is per descriptor, so
        /// the twins cannot be toggled apart; mirroring them is honest about that instead of letting
        /// one box claim a state the game will not honour.</summary>
        void SyncSharedDescriptor(uint addr, bool chk, object origin)
        {
            if (_iconToggles != null)   // icon mode
            {
                foreach (var cb in _iconToggles)
                    if (!ReferenceEquals(cb, origin) && cb.Tag is uint a && a == addr && cb.Checked != chk)
                        cb.Checked = chk;
                return;
            }
            foreach (TreeNode f in _unlockTree.Nodes)
                foreach (TreeNode n in f.Nodes)
                    if (!ReferenceEquals(n, origin) && n.Tag is uint a && a == addr && n.Checked != chk)
                        n.Checked = chk;
        }

        /// <summary>Populate the icon section-list (collapsible faction sections) from PartsData +
        /// the decoded icons, keeping ticks and collapse state.</summary>
        void BuildUnlockList()
        {
            var wasChecked = new System.Collections.Generic.HashSet<uint>(GatherUnlockAddrs());
            _iconToggles = new System.Collections.Generic.List<CheckBox>();
            var orange = Color.FromArgb(200, 130, 40);
            int rowW = 654;

            _unlockList.SuspendLayout();
            _unlockList.Controls.Clear();

            // A collapsible section: one custom-drawn header (chevron + optional faction emblem +
            // title) that shows/hides its body controls. Single control + MouseUp = robust clicks
            // (Click would drop every 2nd rapid press to DoubleClick); the FlowLayoutPanel skips
            // hidden children, so hiding them truly collapses the space.
            void AddSection(string key, string title, Image emblem, System.Action<System.Collections.Generic.List<Control>> buildBody)
            {
                int hh = emblem != null ? 30 : 24;
                var header = new Panel { Width = 340, Height = hh, Margin = new Padding(2, 6, 2, 2), Cursor = Cursors.Hand };
                header.Paint += (s, e) =>
                {
                    var gg = e.Graphics;
                    using (var f = new Font("Segoe UI", 10f, FontStyle.Bold))
                    {
                        gg.DrawString(_collapsed.Contains(key) ? "▶" : "▼", f, Brushes.Gainsboro, 2, (hh - 19) / 2f);
                        int tx = 24;
                        if (emblem != null) { gg.DrawImage(emblem, tx, 2, 26, 26); tx += 32; }
                        gg.DrawString(title, f, Brushes.Gainsboro, tx, (hh - 19) / 2f);
                    }
                };
                _unlockList.Controls.Add(header);
                var body = new System.Collections.Generic.List<Control>();
                buildBody(body);
                foreach (var c in body) { c.Visible = !_collapsed.Contains(key); _unlockList.Controls.Add(c); }
                header.MouseUp += (s, e) =>
                {
                    if (e.Button != MouseButtons.Left) return;
                    bool now = !_collapsed.Contains(key);
                    if (now) _collapsed.Add(key); else _collapsed.Remove(key);
                    foreach (var c in body) c.Visible = !now;
                    header.Invalidate();
                };
            }

            foreach (var faction in PartsData.Factions)
            {
                bool any = false;
                foreach (var pp in PartsData.Parts) if (pp.Faction == faction) { any = true; break; }
                if (!any) continue;
                string fac = faction;
                AddSection("F:" + fac, FactionLabel(fac), _gameIcons?.Emblem(fac), body =>
                {
                    foreach (var slot in PartsData.Slots)
                    {
                        var parts = new System.Collections.Generic.List<PartInfo>();
                        foreach (var p in PartsData.Parts) if (p.Faction == fac && p.Slot == slot) parts.Add(p);
                        if (parts.Count == 0) continue;

                        var head = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Margin = new Padding(14, 4, 2, 0), Cursor = Cursors.Hand };
                        int basicIdx = slot == "Arm" ? 9 : slot == "Legs" ? 48 : 27;   // BasicHand / BasicLegs / BasicTorso
                        var bimg = fac == "Hedoth" ? null : IconFor(fac, basicIdx, 22);
                        if (bimg != null) head.Controls.Add(new PictureBox { Image = bimg, Size = new Size(22, 22), SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 1, 6, 0), Cursor = Cursors.Hand });
                        head.Controls.Add(new Label { Text = Lang.T("slot." + slot.ToLowerInvariant()), AutoSize = true, ForeColor = orange, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Margin = new Padding(0, 4, 0, 0), Cursor = Cursors.Hand });
                        body.Add(head);

                        var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoSize = true, Width = rowW, Margin = new Padding(18, 0, 0, 4) };
                        var slotToggles = new System.Collections.Generic.List<CheckBox>();
                        foreach (var p in parts)
                        {
                            var t = MakeToggle(p.Faction, p.IconIndex, p.Addr, p.Name, wasChecked.Contains(p.Addr));
                            slotToggles.Add(t); row.Controls.Add(t);
                        }
                        body.Add(row);

                        // Clicking the slot sub-header toggles the whole slot (all on -> all off, else all on).
                        // MouseUp, not Click: WinForms turns every 2nd rapid press on the same spot into a
                        // DoubleClick, which would swallow half the clicks -> "same pixel, sometimes works".
                        MouseEventHandler toggleSlot = (s, e) =>
                        {
                            if (e.Button != MouseButtons.Left) return;
                            bool allOn = slotToggles.Count > 0;
                            foreach (var t in slotToggles) if (!t.Checked) { allOn = false; break; }
                            foreach (var t in slotToggles) t.Checked = !allOn;
                        };
                        head.MouseUp += toggleSlot;
                        foreach (Control chld in head.Controls) chld.MouseUp += toggleSlot;
                    }
                });
            }

            AddSection("SW", Lang.T("tree.superweapons"), null, body =>
            {
                var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoSize = true, Width = rowW, Margin = new Padding(18, 0, 0, 4) };
                foreach (var sw in PartsData.Superweapons) row.Controls.Add(MakeToggle(sw.Faction, sw.IconIndex, sw.Addr, sw.Name, wasChecked.Contains(sw.Addr)));
                body.Add(row);
            });

            _unlockList.ResumeLayout();
            AttachHover(_unlockList);   // hovering ANY control enlarges the list, so clicks are consistent
            if (_listBig) SetRowWidths(_unlockList.ClientSize.Width - 8);
        }

        /// <summary>Wire MouseEnter -> ShowBig on every control in the list so the enlarge is
        /// consistent no matter which part of the list the cursor approaches.</summary>
        void AttachHover(Control c)
        {
            foreach (Control ch in c.Controls)
            {
                ch.MouseEnter += (s, e) => ShowBig();
                AttachHover(ch);
            }
        }

        /// <summary>Pop the list into the full-tab overlay (bigger, easier to see) while hovered.</summary>
        void ShowBig()
        {
            if (_listBig || _gameIcons == null || _unlockList == null) return;
            _listBig = true;
            _unlockGroup.Controls.Remove(_unlockList);
            _unlockOverlay.Controls.Add(_unlockList);
            _unlockList.Location = new Point(6, 6);
            _unlockList.Size = new Size(_unlockOverlay.ClientSize.Width - 12, _unlockOverlay.ClientSize.Height - 12);
            SetRowWidths(_unlockList.ClientSize.Width - 8);
            _unlockOverlay.Visible = true;
            _unlockOverlay.BringToFront();
            _hoverTimer.Start();
        }

        /// <summary>Snap the list back into its small spot when the cursor leaves the overlay.</summary>
        void HideBig()
        {
            if (!_listBig) return;
            _listBig = false;
            _hoverTimer.Stop();
            _unlockOverlay.Controls.Remove(_unlockList);
            _unlockOverlay.Visible = false;
            _unlockGroup.Controls.Add(_unlockList);
            _unlockList.Location = new Point(14, 44);
            _unlockList.Size = new Size(680, 124);
            SetRowWidths(654);
            _unlockList.BringToFront();
        }

        void SetRowWidths(int w)
        {
            foreach (Control c in _unlockList.Controls)
                if (c is FlowLayoutPanel f && f.WrapContents) f.Width = w;
        }

        // ---------- banner ----------

        void BuildBanner()
        {
            _banner = new Panel { Location = new Point(0, 0), Size = new Size(ClientSize.Width, 116) };
            _banner.Paint += (s, e) =>
            {
                // One faction per tab, sampled from the game's own faction art and darkened by a
                // uniform 0.48 — the mascots are bright metallic, so a darker banner is what makes
                // them read. The old orange Cheats banner was the worst of the lot at 1.9:1 contrast
                // against its (already red) mascot; Mil-Agro red brings that to 5.7:1.
                // Violet is deliberately NOT a faction colour: the music tab is a general import, not
                // a Rimtech feature, and Rimtech blue would also sit too close to the Patch tab.
                // Keep in step with BannerAccent-style values documented in research-notes.
                Color top, bot, line;
                switch (_bannerTheme)
                {
                    case BannerTheme.Cheats:         // Mil-Agro red   #D43923 x0.48
                        top = Color.FromArgb(34, 9, 5); bot = Color.FromArgb(101, 27, 16); line = Color.FromArgb(151, 40, 24); break;
                    case BannerTheme.Experimental:   // Neuropa green  #227F15 x0.48
                        top = Color.FromArgb(5, 20, 3); bot = Color.FromArgb(16, 60, 10); line = Color.FromArgb(24, 89, 14); break;
                    case BannerTheme.Music:          // neutral violet #3E1580 x0.48
                        top = Color.FromArgb(9, 3, 20); bot = Color.FromArgb(29, 10, 61); line = Color.FromArgb(43, 14, 91); break;
                    default:                         // Rimtech blue   #004FD4 x0.48
                        top = Color.FromArgb(0, 12, 34); bot = Color.FromArgb(0, 37, 101); line = Color.FromArgb(0, 55, 151); break;
                }
                using (var b = new LinearGradientBrush(_banner.ClientRectangle, top, bot, 90f))
                    e.Graphics.FillRectangle(b, _banner.ClientRectangle);
                using (var p = new Pen(line, 2))
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
        /// The banner follows the active tab: blue on Patch, orange on Cheats, Neuropa-green on
        /// Experimental. Only the subtitle, its colour, the mascot and the gradient change; the
        /// title stays "Metal Fatigue Retrofit" throughout. (This replaced the old easter egg.)
        /// </summary>
        void SetBannerTheme(BannerTheme theme)
        {
            if (_bannerTheme == theme) return;
            _bannerTheme = theme;
            _bannerTitle.Text = Lang.T("banner.title");
            string subKey; Color subColor;
            switch (theme)
            {
                case BannerTheme.Cheats:
                    subKey = "banner.cheatSub"; subColor = Color.FromArgb(255, 236, 190); break;
                case BannerTheme.Experimental:
                    subKey = "banner.expSub";   subColor = Color.FromArgb(205, 245, 214); break;
                case BannerTheme.Music:
                    subKey = "banner.musicSub"; subColor = Color.FromArgb(214, 200, 245); break;
                default:
                    subKey = "banner.sub";      subColor = Color.FromArgb(185, 200, 225); break;
            }
            _bannerSub.Text = Lang.T(subKey);
            _bannerSub.ForeColor = subColor;
            if (_mascot != null) _mascot.Image = MascotFor(theme);
            _banner.Invalidate();
        }

        /// <summary>
        /// Deliberately neutral, and the same on every tab. Tinting this button per tab was tried and
        /// reverted: patching is a single action over all tabs at once, and a button that changes
        /// colour with the tab reads as "patch this tab", which is exactly the wrong idea.
        /// </summary>
        static readonly Color PatchButtonColor = Color.FromArgb(74, 74, 74);

        /// <summary>
        /// Restyle the Patch button for its enabled state. A FlatStyle button keeps whatever BackColor
        /// it was given when disabled — only the caption greys out — so the disabled Patch button
        /// still read as clickable. Repainting it here is what actually makes "disabled" visible.
        /// </summary>
        void StylePatchButton()
        {
            if (_patchBtn == null) return;
            if (_patchBtn.Enabled)
            {
                _patchBtn.BackColor = PatchButtonColor;
                _patchBtn.ForeColor = Color.White;
                _patchBtn.FlatAppearance.BorderColor = ControlPaint.Dark(PatchButtonColor, 0.15f);
                _patchBtn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(PatchButtonColor, 0.20f);
                _patchBtn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(PatchButtonColor, 0.10f);
                _patchBtn.Cursor = Cursors.Hand;
            }
            else
            {
                _patchBtn.BackColor = Color.FromArgb(214, 214, 214);
                _patchBtn.ForeColor = Color.FromArgb(128, 128, 128);
                _patchBtn.FlatAppearance.BorderColor = Color.FromArgb(188, 188, 188);
                _patchBtn.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Mascot for a banner theme. The Cheats and Experimental mascots are optional drop-in
        /// PNGs (logo_cheat.png / logo_experimental.png); while one is absent, a loud
        /// missing-texture placeholder is shown so the gap cannot ship unnoticed.
        /// </summary>
        Image MascotFor(BannerTheme theme)
        {
            switch (theme)
            {
                case BannerTheme.Cheats:
                    if (_logoCheat != null) return _logoCheat;
                    Log("[dev] logo_cheat.png is missing — showing placeholder mascot.");
                    return _missingMascot ?? (_missingMascot = MakeMissingMascot(144));
                case BannerTheme.Experimental:
                    if (_logoExperimental != null) return _logoExperimental;
                    Log("[dev] logo_experimental.png is missing — showing placeholder mascot.");
                    return _missingMascot ?? (_missingMascot = MakeMissingMascot(144));
                case BannerTheme.Music:
                    if (_logoMusic != null) return _logoMusic;
                    Log("[dev] logo_music.png is missing — showing placeholder mascot.");
                    return _missingMascot ?? (_missingMascot = MakeMissingMascot(144));
                default:
                    return _logo;
            }
        }

        // ---------- localization ----------

        void ApplyLanguage()
        {
            Text                 = Lang.T("window.title") + "  " + VersionString();
            _bannerTitle.Text    = Lang.T("banner.title");
            _bannerSub.Text      = Lang.T(_bannerTheme == BannerTheme.Cheats       ? "banner.cheatSub"
                                        : _bannerTheme == BannerTheme.Experimental ? "banner.expSub"
                                        : _bannerTheme == BannerTheme.Music        ? "banner.musicSub"
                                        :                                            "banner.sub");
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
                _tabExperimental.Text = Lang.T("tab.experimental");
                _tabMusic.Text        = Lang.T("tab.music");
                _expWarnGroup.Text    = Lang.T("grp.expwarn");
                _expWarn.Text         = Lang.T("exp.warning");
                _expSoonGroup.Text    = Lang.T("grp.expsoon");
                _expSoon.Text         = Lang.T("exp.soon");
                _expSpeedGroup.Text   = Lang.T("grp.expspeed");
                _musicGroup.Text      = Lang.T("tab.music");
                _musicIntro.Text      = Lang.T("music.intro");
                _musicPickFolder.Text = Lang.T("music.pickFolder");
                _musicPickZip.Text    = Lang.T("music.pickZip");
                _musicRemove.Text     = Lang.T("music.remove");
                _musicLegendActive.Text   = "● " + Lang.T("music.legend.active");
                _musicLegendPending.Text  = "● " + Lang.T("music.legend.pending");
                _musicLegendMismatch.Text = "● " + Lang.T("music.legend.mismatch");
                _musicGrid.Columns["slot"].HeaderText  = Lang.T("music.dlg.slot");
                _musicGrid.Columns["file"].HeaderText  = Lang.T("music.dlg.file");
                _musicGrid.Columns["exp"].HeaderText   = Lang.T("music.dlg.expected");
                _musicGrid.Columns["act"].HeaderText   = Lang.T("music.dlg.actual");
                _musicGrid.Columns["match"].HeaderText = Lang.T("music.dlg.match");
                _expSpeed.Text        = Lang.T("exp.speed");
                _expSpeedFactorLbl.Text = Lang.T("exp.speed.factor");
                _expSpeedForLbl.Text  = Lang.T("unlock.for");
                _expSpeedPlayer.Text  = Lang.T("scope.me");
                _expSpeedAll.Text     = Lang.T("scope.all");
                _expSpeedNote.Text    = Lang.T("exp.speed.note");
                // "For:" resizes per language, so park the scope radios right after it.
                _expSpeedPlayer.Left  = _expSpeedForLbl.Right + 6;
                _expSpeedAll.Left     = _expSpeedPlayer.Right + 12;
                UpdateSpeedLabels();
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
                // Re-label the active unlock view (tree nodes or icon-list headers) for the language.
                RebuildUnlockView();
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
                    // Just "already patched" — the version/cheats specifics are in the read-out below.
                    _compatLabel.Text = Lang.T("compat.patched");
                    _compatLabel.ForeColor = Color.FromArgb(176, 108, 12);
                    canPatch = true;
                    break;
                case Patcher.Compat.Missing:
                    _compatLabel.Text = Lang.T("compat.missing");
                    _compatLabel.ForeColor = Color.DimGray;
                    canPatch = false;
                    break;
                default:
                    // We recognise our own patch but have no clean original to work from. If the file
                    // still carries a superseded release's cave layout, name it: "restore the original"
                    // is actionable, where "unknown version" would send them hunting the wrong problem.
                    // Only worth saying here — with a clean backup, re-patching rebuilds from it and
                    // wipes the old layout by itself.
                    var legacy = PatchData.DetectLegacyLayout(_pathBox.Text.Trim());
                    _compatLabel.Text = legacy != null
                        ? string.Format(Lang.T("compat.legacyLayout"), legacy)
                        : profKey != null
                            ? Lang.T("compat.patchedNoBackup")
                            : Lang.T("compat.unsupported");
                    _compatLabel.ForeColor = Color.FromArgb(192, 32, 32);
                    canPatch = false;
                    break;
            }

            // Grey out the version choice + patch button when we can't safely patch.
            _profGroup.Enabled = canPatch;
            _svGroup.Enabled = canPatch;
            _patchBtn.Enabled = canPatch;

            var path = _pathBox.Text.Trim();
            bool svInstalled = !string.IsNullOrEmpty(path) && Patcher.HasSharedVision(path);

            // Once a real game folder is known, decode its part icons + detect the language variant
            // and switch the unlock UI from the text tree to the icon list (stays on the tree on any
            // failure). Must run BEFORE we fill the read-out, which reports the detected variant.
            EnsureIcons(path);

            // Reflect what is already installed — but only once per file, so the user
            // can still tick/untick freely afterwards without being overridden.
            if (path != _svSyncedPath)
            {
                _svSyncedPath = path;
                _sharedVision.Checked = svInstalled;
                RestoreFromExe(path, profKey, c);
            }
            UpdateSharedVisionState();

            FillExeInfo(c, profKey, path, svInstalled);

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

        /// <summary>
        /// Fill the exe read-out (build, language variant, what is already installed) and decide
        /// whether to offer a report link. Runs after EnsureIcons so the detected variant is current.
        /// Categories only for "installed" — listing every cheat would overflow the row.
        /// </summary>
        void FillExeInfo(Patcher.Compat c, string profKey, string path, bool svInstalled)
        {
            bool haveFile = c != Patcher.Compat.Missing && !string.IsNullOrEmpty(path);
            // Pristine / our-patch / our-patch-without-backup are all the supported Nightdive build.
            bool knownBuild = c == Patcher.Compat.Pristine || c == Patcher.Compat.PatchedByUs
                              || (c == Patcher.Compat.Unsupported && profKey != null);

            // --- build ---
            if (!haveFile)
                _infoBuild.Text = Lang.T("info.build") + " —";
            else if (knownBuild)
                _infoBuild.Text = Lang.T("info.build") + " " + Lang.T("info.build.nightdive");
            else
            {
                long size = 0; try { size = new FileInfo(path).Length; } catch { }
                _infoBuild.Text = Lang.T("info.build") + " " + Lang.T("info.build.unknown")
                                  + (size > 0 ? string.Format(" ({0:n0} B)", size) : "");
            }

            // --- language variant (from the icon-set detection) ---
            string variantVal;
            if (!haveFile) variantVal = "—";
            else if (_variant != null) variantVal = Lang.T(_variant.NameKey);
            else if (_variantMatch != null && _variantMatch.Variant == null) variantVal = Lang.T("variant.unknown");
            else variantVal = "—";   // no TBD folder / could not decode
            _infoVariant.Text = Lang.T("info.language") + " " + variantVal;

            // --- what is already installed (categories, not individual items) ---
            string installedVal;
            if (!haveFile) installedVal = "—";
            else
            {
                var cats = new System.Collections.Generic.List<string>();
                if (profKey != null) cats.Add(Lang.T("info.cat.patch") + " (" + Lang.ProfileTitle(profKey) + ")");
                else if (svInstalled) cats.Add(Lang.T("info.cat.patch"));
                var inst = _lastInstalled;
                if (inst != null)
                {
                    if (inst.Fog || inst.FreeBuild || inst.Turbo || inst.Crews || inst.PartsUnlock)
                        cats.Add(Lang.T("info.cat.cheats"));
                    if (inst.MoveSpeed) cats.Add(Lang.T("info.cat.experimental"));
                }
                installedVal = cats.Count > 0 ? string.Join(", ", cats) : Lang.T("info.installed.none");
            }
            _infoInstalled.Text = Lang.T("info.installed") + " " + installedVal;

            // Carrying a superseded cave layout but still holding a clean backup: patching again
            // rebuilds from that backup and drops the old layout on its own, so all this needs is a
            // nudge. Without a backup the red compat line above says it instead, and far more firmly.
            var legacyWithBackup = c == Patcher.Compat.PatchedByUs
                ? PatchData.DetectLegacyLayout(_pathBox.Text.Trim()) : null;
            _infoLegacy.Visible = legacyWithBackup != null;
            if (legacyWithBackup != null)
                _infoLegacy.Text = string.Format(Lang.T("info.legacyHint"), legacyWithBackup);

            // --- report link: an unknown exe build, or a decoded-but-unrecognised language patch ---
            if (c == Patcher.Compat.Unsupported && profKey == null) _reportKind = "version";
            else if (haveFile && _variant == null && _variantMatch != null && _variantMatch.Variant == null) _reportKind = "language";
            else _reportKind = null;
            _reportLink.Visible = _reportKind != null;
            _reportLink.Text = Lang.T("info.report");
            _compatLabel.Width = _reportLink.Visible ? 472 : 680;   // yield room only when the link shows
            ReflowReadout();
        }

        /// <summary>Size the status line to its text and slide the info rows under it. The frame
        /// already reserves the three-line worst case, so this only ever moves rows inside it.</summary>
        void ReflowReadout()
        {
            if (_compatLabel == null || _infoBuild == null) return;

            int h = TextRenderer.MeasureText(
                _compatLabel.Text, _compatLabel.Font,
                new Size(_compatLabel.Width, 0), TextFormatFlags.WordBreak).Height;
            _compatLabel.Height = Math.Min(50, Math.Max(18, h));

            int top = _compatLabel.Bottom + 4;
            _infoBuild.Top     = top;
            _infoVariant.Top   = top + 17;
            _infoInstalled.Top = top + 34;
            _infoLegacy.Top    = top + 51;
        }

        /// <summary>Open the GitHub issue tracker, pre-selecting the template that fits what we
        /// could not support (unknown build vs unknown language patch). A diagnostic snapshot is
        /// copied to the clipboard so the report is actionable — the issue form asks to paste it.</summary>
        void OpenReport()
        {
            try { Clipboard.SetText(BuildReportDiagnostics()); } catch { }
            try
            {
                var url = IssuesUrl + "/new";
                if (_reportKind == "version") url += "?template=unsupported_build.yml";
                else if (_reportKind == "language") url += "?template=unsupported_language.yml";
                System.Diagnostics.Process.Start(url);
            }
            catch { }
        }

        /// <summary>A copy-paste snapshot that identifies an unrecognised build or language patch:
        /// file size + SHA-256 for an unknown exe, decoded icon counts for an unknown language.</summary>
        string BuildReportDiagnostics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Metal Fatigue Retrofit " + VersionString() + "  (" + _reportKind + " report)");
            var path = _pathBox.Text.Trim();
            sb.AppendLine("exe: " + path);
            try
            {
                if (File.Exists(path))
                {
                    sb.AppendLine("size: " + new FileInfo(path).Length + " bytes");
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    using (var fs = File.OpenRead(path))
                        sb.AppendLine("sha256: " + BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant());
                }
            }
            catch { }
            if (_variantMatch != null)
            {
                var fc = _variantMatch.FactionIconCounts;
                sb.AppendLine(string.Format("icon counts R/M/N: {0}/{1}/{2}", fc[0], fc[1], fc[2]));
                sb.AppendLine("detected variant: " + (_variant != null ? _variant.Key : "<none matched>"));
            }
            return sb.ToString();
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
            if (_expSpeed.Checked)
                extras.AddRange(PatchData.MoveSpeedSites(SpeedFactor, _expSpeedAll.Checked));

            // Imported music has to ride along with every patch. Apply always rebuilds from the clean
            // backup, so leaving it out would silently drop the table edit while the files stayed on
            // disk — and Rimtech would then play Neuropa's tracks. Keyed on the files being present,
            // never on a checkbox: the table is only ever valid together with them.
            if (MusicImport.FilesPresent(_pathBox.Text.Trim()))
                extras.AddRange(PatchData.RimtechMusicSites());

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
            _lastInstalled = inst;   // reused by the exe-info read-out (UpdateCompat)

            _cheatFog.Checked        = inst.Fog;
            _cheatBuild.Checked      = inst.FreeBuild;
            _cheatTurbo.Checked      = inst.Turbo;
            _cheatCrews.Checked      = inst.Crews;
            _scopeAll.Checked        = inst.CheatScopeAll;
            _scopePlayer.Checked     = !inst.CheatScopeAll;
            _partsScopeAll.Checked   = inst.PartsScopeAll;
            _partsScopePlayer.Checked = !inst.PartsScopeAll;

            _expSpeed.Checked          = inst.MoveSpeed;
            _expSpeedAll.Checked       = inst.MoveSpeedScopeAll;
            _expSpeedPlayer.Checked    = !inst.MoveSpeedScopeAll;
            SetSpeedFactor(inst.MoveSpeed ? inst.MoveSpeedFactor : 2.0);
            UpdateSpeedEnabled();
            UpdateMusicState();

            // Restore the part/superweapon selection from the unlocked descriptor addresses.
            var set = new System.Collections.Generic.HashSet<uint>(inst.UnlockedAddrs);
            if (_iconToggles != null)   // icon mode
            {
                foreach (var cb in _iconToggles)
                    if (cb.Tag is uint a) cb.Checked = set.Contains(a);
            }
            else                        // tree fallback
            {
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
