// Runtime combot-part icon loader. Decodes the build-menu part icons straight from the
// player's OWN game files (TBD\structures{X,Y,Z}.tbd) at run time, so NO copyrighted game art
// is ever shipped inside the patcher. Every entry point is wrapped so any failure (missing
// files, a modded/localised install, an unexpected layout) returns null and the caller falls
// back to the plain text tree.
//
// Format (reverse-engineered, see research/tbd_icons.py + docs): the .tbd is a RIFF 'TBDF'
// container; a big DATA blob holds 32x32 tiles, and small "descriptor" resources compose each
// icon from (packed_xy, tile_ptr) pairs. The structures files are 8-bit indexed (1024-byte
// tiles); colour comes from a palette in GobjectPalettes.tbd (Rimtech blue / MilAgro red /
// Neuropa green). Transparent key = palette index 0 (chroma green) and the 0xFC fill.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MetalFatiguePatcher
{
    internal sealed class GameIcons
    {
        // Faction -> its structures file + GobjectPalettes index (the faction colour).
        static readonly (string Faction, string File, int Pal)[] FactionFiles =
        {
            ("Rimtech", "structuresX.tbd", 2),
            ("MilAgro", "structuresY.tbd", 3),
            ("Neuropa", "structuresZ.tbd", 4),
        };

        readonly Dictionary<string, List<Bitmap>> _parts;
        GameIcons(Dictionary<string, List<Bitmap>> parts) { _parts = parts; }

        /// <summary>Icons for a faction in TYPE order (index matches PartsData.IconIndex), or null.</summary>
        public IReadOnlyList<Bitmap> Faction(string faction)
            => _parts.TryGetValue(faction, out var l) ? l : null;

        Dictionary<string, Bitmap> _emblems;
        /// <summary>Faction emblem (Rimtech/MilAgro/Neuropa) decoded from Menu.tbd, or null.</summary>
        public Bitmap Emblem(string faction) => _emblems != null && _emblems.TryGetValue(faction, out var b) ? b : null;

        /// <summary>
        /// Load and decode every faction's part icons from the game folder next to mfatigueExe.
        /// Returns null on ANY problem so the UI can fall back to the text tree.
        /// </summary>
        public static GameIcons TryLoad(string mfatigueExePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mfatigueExePath)) return null;
                var tbd = Path.Combine(Path.GetDirectoryName(mfatigueExePath) ?? "", "TBD");
                var palFile = Path.Combine(tbd, "GobjectPalettes.tbd");
                if (!File.Exists(palFile)) return null;
                var palBytes = File.ReadAllBytes(palFile);

                var map = new Dictionary<string, List<Bitmap>>();
                foreach (var ff in FactionFiles)
                {
                    var path = Path.Combine(tbd, ff.File);
                    if (!File.Exists(path)) return null;
                    var pal = LoadPalette(palBytes, ff.Pal);
                    var imgs = DecodeStructures(File.ReadAllBytes(path))
                        .Where(im => im.W == 64 && im.H == 64)
                        .Select(im => ToBitmap(im, pal))
                        .ToList();
                    if (imgs.Count == 0) return null;
                    map[ff.Faction] = imgs;
                }
                var gi = new GameIcons(map);
                try { gi._emblems = LoadEmblems(Path.Combine(tbd, "Menu.tbd")); } catch { }   // emblems are optional
                return gi;
            }
            catch { return null; }
        }

        // ---------- decoder (port of the verified research/tbd_icons.py logic) ----------

        struct Img { public int W, H; public byte[] Idx; }

        static uint U(byte[] d, int o) => BitConverter.ToUInt32(d, o);

        static Dictionary<string, (int off, int size)> Chunks(byte[] d)
        {
            var m = new Dictionary<string, (int, int)>();
            int off = 12;
            while (off + 8 <= d.Length)
            {
                string c = Encoding.ASCII.GetString(d, off, 4);
                int sz = BitConverter.ToInt32(d, off + 4);
                if (!m.ContainsKey(c)) m[c] = (off + 8, sz);
                off += 8 + sz + (sz & 1);
            }
            return m;
        }

        static List<Img> DecodeStructures(byte[] d)
        {
            var ch = Chunks(d);
            var (to, ts) = ch["TYPE"];
            var (dataOff, ds) = ch["DATA"];

            var ent = new List<(uint hash, int off)>();
            for (int i = 0; i < (ts - 4) / 8; i++)
                ent.Add((U(d, to + 4 + 8 * i), (int)U(d, to + 4 + 8 * i + 4)));

            var offs = ent.Select(e => e.off).Distinct().OrderBy(x => x).ToList();
            int SizeOf(int o) { int i = offs.IndexOf(o); return i + 1 < offs.Count ? offs[i + 1] - o : ds - o; }

            int pix = offs.OrderByDescending(SizeOf).First();
            int plo = pix, phi = pix + SizeOf(pix);

            var cand = new List<uint>();
            foreach (var e in ent)
            {
                if (e.off == pix) continue;
                int s = SizeOf(e.off), n = s / 4;
                if (n < 2 || n > 30000) continue;
                for (int i = 0; i < n - 1; i++)
                {
                    uint a = U(d, dataOff + e.off + i * 4), b = U(d, dataOff + e.off + (i + 1) * 4);
                    if (b >= plo && b < phi && (a & 0xffff) < 2048 && (a >> 16) < 2048 && (a & 0x1f) == 0) cand.Add(b);
                }
            }
            if (cand.Count == 0) return new List<Img>();
            uint rem = cand.GroupBy(x => x % 1024).OrderByDescending(g => g.Count()).First().Key;

            var seen = new HashSet<string>();
            var all = new List<Img>();
            using (var md5 = System.Security.Cryptography.MD5.Create())
                foreach (var e in ent)
                {
                    if (e.off == pix) continue;
                    int s = SizeOf(e.off), n = s / 4;
                    var tiles = new List<(int x, int y, int pt)>();
                    int i = 0;
                    while (i + 1 < n)
                    {
                        uint xy = U(d, dataOff + e.off + i * 4), pt = U(d, dataOff + e.off + (i + 1) * 4);
                        if (pt >= plo && pt < phi && pt % 1024 == rem && (xy & 0x1f) == 0 && (xy & 0xffff) < 2048 && (xy >> 16) < 2048)
                        { tiles.Add(((int)(xy & 0xffff), (int)(xy >> 16), (int)pt)); i += 2; }
                        else i++;
                    }
                    if (tiles.Count == 0) continue;
                    int W = tiles.Max(t => t.x) + 32, H = tiles.Max(t => t.y) + 32;
                    if (W > 1024 || H > 1024 || W * H < 256) continue;

                    var idx = new byte[W * H];
                    for (int k = 0; k < idx.Length; k++) idx[k] = 0xfc;
                    foreach (var t in tiles)
                        for (int yy = 0; yy < 32; yy++)
                            Array.Copy(d, dataOff + t.pt + yy * 32, idx, (t.y + yy) * W + t.x, 32);

                    string key = W + "x" + H + ":" + Convert.ToBase64String(md5.ComputeHash(idx));
                    if (!seen.Add(key)) continue;
                    all.Add(new Img { W = W, H = H, Idx = idx });
                }
            return all;
        }

        static Color[] LoadPalette(byte[] g, int index)
        {
            var ch = Chunks(g);
            var (to, ts) = ch["TYPE"];
            var (dataOff, _) = ch["DATA"];
            var offs = new List<int>();
            for (int i = 0; i < (ts - 4) / 8; i++) offs.Add((int)U(g, to + 4 + 8 * i + 4));
            offs = offs.Distinct().OrderBy(x => x).ToList();
            int o = offs[index];
            var pal = new Color[256];
            for (int k = 0; k < 256; k++)
            {
                int b = dataOff + o + 8 + k * 4;
                pal[k] = Color.FromArgb(g[b], g[b + 1], g[b + 2]);
            }
            return pal;
        }

        static Bitmap ToBitmap(Img im, Color[] pal)
        {
            var bmp = new Bitmap(im.W, im.H, PixelFormat.Format32bppArgb);
            var data = bmp.LockBits(new Rectangle(0, 0, im.W, im.H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var buf = new byte[im.W * im.H * 4];
            for (int p = 0; p < im.W * im.H; p++)
            {
                byte ix = im.Idx[p];
                Color c = pal[ix];
                byte a = (ix == 0 || ix == 0xfc) ? (byte)0 : (byte)255;
                buf[p * 4 + 0] = c.B; buf[p * 4 + 1] = c.G; buf[p * 4 + 2] = c.R; buf[p * 4 + 3] = a;
            }
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            bmp.UnlockBits(data);
            return bmp;
        }

        static Dictionary<string, Bitmap> LoadEmblems(string menuPath)
        {
            if (!File.Exists(menuPath)) return null;
            var e = DecodeMenuEmblems(File.ReadAllBytes(menuPath));
            if (e.Count < 3) return null;
            return new Dictionary<string, Bitmap> { { "Rimtech", e[0] }, { "MilAgro", e[1] }, { "Neuropa", e[2] } };
        }

        // The 3 faction emblems are the first three 64x64 RGB565 images in Menu.tbd (verified,
        // in order Rimtech / MilAgro / Neuropa). Menu.tbd mixes 565 and 8-bit tiles, so a 565
        // image is told apart by its 2048-byte (vs 1024-byte) tile stride.
        static List<Bitmap> DecodeMenuEmblems(byte[] d)
        {
            var outp = new List<Bitmap>();
            var ch = Chunks(d);
            if (!ch.ContainsKey("TYPE") || !ch.ContainsKey("DATA")) return outp;
            var (to, ts) = ch["TYPE"];
            var (dataOff, ds) = ch["DATA"];
            var ent = new List<(uint hash, int off)>();
            for (int i = 0; i < (ts - 4) / 8; i++) ent.Add((U(d, to + 4 + 8 * i), (int)U(d, to + 4 + 8 * i + 4)));
            var offs = ent.Select(e => e.off).Distinct().OrderBy(x => x).ToList();
            int SizeOf(int o) { int i = offs.IndexOf(o); return i + 1 < offs.Count ? offs[i + 1] - o : ds - o; }
            int pix = offs.OrderByDescending(SizeOf).First();
            int plo = pix, phi = pix + SizeOf(pix);

            var cand = new List<uint>();
            foreach (var e in ent)
            {
                if (e.off == pix) continue;
                int s = SizeOf(e.off), n = s / 4;
                if (n < 2 || n > 30000) continue;
                for (int i = 0; i < n - 1; i++)
                {
                    uint a = U(d, dataOff + e.off + i * 4), b = U(d, dataOff + e.off + (i + 1) * 4);
                    if (b >= plo && b < phi && (a & 0xffff) < 2048 && (a >> 16) < 2048 && (a & 0x1f) == 0) cand.Add(b);
                }
            }
            if (cand.Count == 0) return outp;
            uint rem = cand.GroupBy(x => x % 1024).OrderByDescending(g => g.Count()).First().Key;

            foreach (var e in ent)
            {
                if (e.off == pix) continue;
                int s = SizeOf(e.off), n = s / 4;
                var tiles = new List<(int x, int y, int pt)>();
                int i = 0;
                while (i + 1 < n)
                {
                    uint xy = U(d, dataOff + e.off + i * 4), pt = U(d, dataOff + e.off + (i + 1) * 4);
                    if (pt >= plo && pt < phi && pt % 1024 == rem && (xy & 0x1f) == 0 && (xy & 0xffff) < 2048 && (xy >> 16) < 2048)
                    { tiles.Add(((int)(xy & 0xffff), (int)(xy >> 16), (int)pt)); i += 2; }
                    else i++;
                }
                if (tiles.Count == 0) continue;
                int W = tiles.Max(t => t.x) + 32, H = tiles.Max(t => t.y) + 32;
                if (W != 64 || H != 64) continue;
                var ptrs = tiles.Select(t => t.pt).Distinct().OrderBy(x => x).ToList();
                int minD = int.MaxValue;
                for (int k = 0; k + 1 < ptrs.Count; k++) { int dd = ptrs[k + 1] - ptrs[k]; if (dd > 0 && dd < minD) minD = dd; }
                if (minD != 2048) continue;   // 565 tiles are 2048 bytes apart; 8-bit are 1024

                var bmp = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
                var bd = bmp.LockBits(new Rectangle(0, 0, 64, 64), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                var buf = new byte[64 * 64 * 4];
                foreach (var t in tiles)
                    for (int yy = 0; yy < 32; yy++)
                        for (int xx = 0; xx < 32; xx++)
                        {
                            int so = dataOff + t.pt + (yy * 32 + xx) * 2;
                            ushort v = (ushort)(d[so] | (d[so + 1] << 8));
                            int px = ((t.y + yy) * 64 + (t.x + xx)) * 4;
                            buf[px + 0] = (byte)((v & 31) * 255 / 31);
                            buf[px + 1] = (byte)(((v >> 5) & 63) * 255 / 63);
                            buf[px + 2] = (byte)(((v >> 11) & 31) * 255 / 31);
                            buf[px + 3] = (byte)(v == 0 ? 0 : 255);   // black key = transparent
                        }
                Marshal.Copy(buf, 0, bd.Scan0, buf.Length);
                bmp.UnlockBits(bd);
                outp.Add(bmp);
                if (outp.Count >= 3) break;
            }
            return outp;
        }

        /// <summary>
        /// Classify a build-menu icon by the faction-coloured card it sits on: Arm=red, Torso=blue,
        /// Legs=green. Language-independent (the colour is in the art, not the text), so it is the
        /// validator GameVariant.Detect uses to confirm an icon→part mapping. Samples the card FRAME
        /// (outer ring of the opaque 48×48) — the silver leg mesh fills the centre and would wash a
        /// green card out to grey. Returns "Arm"/"Torso"/"Legs", or null if no channel dominates.
        /// </summary>
        public static string ClassifySlotColor(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var buf = new byte[w * h * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            bmp.UnlockBits(data);

            double rs = 0, gs = 0, bs = 0; int n = 0;
            // The card is ~48×48 in the top-left; its frame is the outer few pixels of that.
            bool OnFrame(int x, int y) =>
                (x >= 1 && x <= 46 && y >= 1 && y <= 46) &&
                ((x <= 4 || x >= 43) || (y <= 4 || y >= 43));
            for (int y = 0; y < h && y <= 46; y++)
                for (int x = 0; x < w && x <= 46; x++)
                {
                    if (!OnFrame(x, y)) continue;
                    int p = (y * w + x) * 4;
                    if (buf[p + 3] < 128) continue;        // skip transparent
                    bs += buf[p]; gs += buf[p + 1]; rs += buf[p + 2]; n++;
                }
            if (n == 0) return null;
            double r = rs / n, g = gs / n, b = bs / n;
            const double m = 12;
            if (r > g + m && r > b + m) return "Arm";
            if (b > r + m && b > g + m) return "Torso";
            if (g > r + m && g > b + m) return "Legs";
            return null;
        }

        /// <summary>Tight bounding box of the non-transparent pixels (the icons carry a big
        /// transparent right/bottom margin; cropping to this lets them fill their button).</summary>
        public static Rectangle OpaqueBounds(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var buf = new byte[w * h * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            bmp.UnlockBits(data);
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (buf[(y * w + x) * 4 + 3] > 40)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
            return maxX < 0 ? new Rectangle(0, 0, w, h) : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        }
    }
}
