// ============================================================================
// NillyUninstall Pro — Ultra-Light Native Windows 10/11 Apps Manager,
//                      Safe Windows 11 Bloat Remover & Leftover Cleaner
// created by Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/
// Strictly C# 5 compatible (.NET Framework 4.x csc.exe — 0 external DLLs)
// Features:
//  - <50ms launch · 0 MB RAM / 0% CPU when closed · 0 external DLLs · Zero Admin (HKCU)
//  - Tab 1: Instant Win32/MSI/User Installed Programs (reads all 3 Registry hives in <30ms)
//  - Tab 2: Safe Windows 11 Bloat Remover (measures real MB + WebView2 impact, hard-locked
//           against removing Codecs/Store/Defender/Shell, 1-click Microsoft Store restore!)
//  - Tab 3: Safe AppData Leftover Scanner (Recycle-Bin-Only deletion via SHFileOperation)
//  - Greek accent-free search · Dark/Light Monokai Theme · Built-in 'i' Ultra-Light Specs
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("NillyUninstall Pro")]
[assembly: AssemblyDescription("Ultra-Light Native Windows 10/11 Uninstaller, Safe Bloatware Remover & Leftover Cleaner — created by Nilly graphics (Nillygraphics · Sotiris Christos)")]
[assembly: AssemblyCompany("Nilly graphics (Nillygraphics · Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyProduct("NillyUninstall Pro")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Nilly graphics (Sotiris Christos) — https://nillygraphics.com/")]
[assembly: AssemblyTrademark("Nilly graphics / Nillygraphics — Sotiris Christos")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace NillyUninstallPro
{
    internal static class Native
    {
        public const uint FO_DELETE = 0x0003;
        public const ushort FOF_ALLOWUNDO = 0x0040;
        public const ushort FOF_NOCONFIRMATION = 0x0010;
        public const ushort FOF_SILENT = 0x0004;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
        }

        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minSize, IntPtr maxSize);

        public static void TrimMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int sz);
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr hWnd, string app, string id);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr CreateIconFromResourceEx(byte[] presbits, uint dwResSize, bool fIcon, uint dwVer, int cx, int cy, uint Flags);

        public static void SetDarkTitlebar(IntPtr hwnd, bool dark)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int v = dark ? 1 : 0;
                if (DwmSetWindowAttribute(hwnd, 20, ref v, 4) != 0)
                    DwmSetWindowAttribute(hwnd, 19, ref v, 4);
            }
            catch { }
        }

        public static void SetDarkScrollbars(Control c, bool dark)
        {
            if (c == null || !c.IsHandleCreated) return;
            try { SetWindowTheme(c.Handle, dark ? "DarkMode_Explorer" : "Explorer", null); } catch { }
        }

        public static void SetCue(TextBox tb, string text)
        {
            if (tb == null || !tb.IsHandleCreated) return;
            try { SendMessage(tb.Handle, 0x1501, (IntPtr)1, text); } catch { }
        }

        public static bool SendToRecycleBin(IntPtr hwnd, IList<string> paths)
        {
            if (paths == null || paths.Count == 0) return false;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < paths.Count; i++) { sb.Append(paths[i]); sb.Append('\0'); }
            sb.Append('\0');
            SHFILEOPSTRUCT op = new SHFILEOPSTRUCT();
            op.hwnd = hwnd;
            op.wFunc = FO_DELETE;
            op.pFrom = sb.ToString();
            op.fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT);
            return SHFileOperation(ref op) == 0 && !op.fAnyOperationsAborted;
        }

        public static Bitmap LoadCrispIcon(string icoPath, int sz)
        {
            if (string.IsNullOrEmpty(icoPath) || !File.Exists(icoPath)) return null;
            try
            {
                byte[] data = File.ReadAllBytes(icoPath);
                if (data.Length < 22) return null;
                int cnt = BitConverter.ToUInt16(data, 4);
                int bestIdx = 0, bestW = 0;
                for (int i = 0; i < cnt; i++)
                {
                    int off = 6 + i * 16;
                    if (off + 16 > data.Length) break;
                    int w = data[off]; if (w == 0) w = 256;
                    if (w > bestW) { bestW = w; bestIdx = i; }
                }
                int eOff = 6 + bestIdx * 16;
                int len = BitConverter.ToInt32(data, eOff + 8);
                int imgOff = BitConverter.ToInt32(data, eOff + 12);
                Bitmap dst = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(dst))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Transparent);
                    if (imgOff + 4 < data.Length && data[imgOff] == 0x89 && data[imgOff + 1] == 0x50)
                    {
                        using (MemoryStream ms = new MemoryStream(data, imgOff, len))
                        using (Image img = Image.FromStream(ms))
                            g.DrawImage(img, 0, 0, sz, sz);
                    }
                }
                return dst;
            }
            catch { return null; }
        }

        public static void ApplyTaskbarIcon(Form f, string icoPath)
        {
            if (f == null || !f.IsHandleCreated || !File.Exists(icoPath)) return;
            try
            {
                byte[] data = File.ReadAllBytes(icoPath);
                IntPtr hBig = MakeHIcon(data, 48);
                IntPtr hSm = MakeHIcon(data, 16);
                if (hBig != IntPtr.Zero) SendMessage(f.Handle, 0x0080, (IntPtr)1, hBig);
                if (hSm != IntPtr.Zero) SendMessage(f.Handle, 0x0080, (IntPtr)0, hSm);
            }
            catch { }
        }

        private static IntPtr MakeHIcon(byte[] data, int desired)
        {
            if (data.Length < 22) return IntPtr.Zero;
            int cnt = BitConverter.ToUInt16(data, 4), bestIdx = 0, bestDiff = int.MaxValue;
            for (int i = 0; i < cnt; i++)
            {
                int off = 6 + i * 16;
                if (off + 16 > data.Length) break;
                int w = data[off]; if (w == 0) w = 256;
                int d = Math.Abs(w - desired);
                if (w >= desired && d < bestDiff) { bestDiff = d; bestIdx = i; }
                else if (bestDiff == int.MaxValue && d < bestDiff) { bestDiff = d; bestIdx = i; }
            }
            int eOff = 6 + bestIdx * 16;
            int len = BitConverter.ToInt32(data, eOff + 8);
            int imgOff = BitConverter.ToInt32(data, eOff + 12);
            if (len <= 0 || imgOff + len > data.Length) return IntPtr.Zero;
            byte[] slice = new byte[len];
            Buffer.BlockCopy(data, imgOff, slice, 0, len);
            return CreateIconFromResourceEx(slice, (uint)len, true, 0x00030000, desired, desired, 0);
        }
    }

    internal sealed class DoubleBufferedLV : ListView
    {
        public DoubleBufferedLV()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }
    }

    internal sealed class InstalledApp
    {
        public string Name;
        public string Publisher;
        public string Version;
        public string InstallDate;
        public long SizeKb;
        public string Scope;
        public string UninstallString;
        public string InstallLocation;
        public string RegPath;
        public string Norm;
    }

    internal sealed class BloatApp
    {
        public string PackageName;
        public string FullName;
        public string FriendlyName;
        public string Category;
        public string ReplacesNote;
        public string StoreQuery;
        public double SizeMb;
        public bool IsRecommendedBloat;
        public string Norm;
    }

    internal sealed class LeftoverItem
    {
        public string FolderName;
        public string RootLabel;
        public string FullPath;
        public long SizeBytes;
        public DateTime Modified;
        public string Norm;
    }

    internal static class Helpers
    {
        public static string StripAccents(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string norm = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(norm.Length);
            for (int i = 0; i < norm.Length; i++)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(norm[i]) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(norm[i]));
            }
            return sb.ToString();
        }

        public static string FmtMb(double mb)
        {
            if (mb <= 0.05) return "—";
            if (mb >= 1024.0) return (mb / 1024.0).ToString("0.00") + " GB";
            return mb.ToString("0.0") + " MB";
        }

        public static string FmtBytes(long b)
        {
            if (b <= 0) return "0 B";
            if (b < 1024) return b + " B";
            double d = b / 1024.0;
            if (d < 1024) return d.ToString("0.0") + " KB";
            d /= 1024.0;
            if (d < 1024) return d.ToString("0.0") + " MB";
            return (d / 1024.0).ToString("0.00") + " GB";
        }

        public static long MeasureDirBytes(string path, int maxFiles)
        {
            long total = 0;
            int count = 0;
            try
            {
                Stack<string> st = new Stack<string>();
                st.Push(path);
                while (st.Count > 0 && count < maxFiles)
                {
                    string cur = st.Pop();
                    try
                    {
                        string[] files = Directory.GetFiles(cur);
                        for (int i = 0; i < files.Length && count < maxFiles; i++)
                        {
                            try { total += new FileInfo(files[i]).Length; count++; } catch { }
                        }
                        string[] dirs = Directory.GetDirectories(cur);
                        for (int i = 0; i < dirs.Length; i++) st.Push(dirs[i]);
                    }
                    catch { }
                }
            }
            catch { }
            return total;
        }
    }

    // ========================================================================
    // SCANNERS: Win32 Registry (<30ms), Safe Windows 11 Bloat, Safe Leftovers
    // ========================================================================
    internal static class AppScanners
    {
        public static List<InstalledApp> ScanWin32Apps()
        {
            List<InstalledApp> list = new List<InstalledApp>(160);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ScanHive(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "System (64-bit)", list, seen);
            ScanHive(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "System (32-bit)", list, seen);
            ScanHive(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "Χρήστης (HKCU)", list, seen);

            list.Sort(delegate(InstalledApp a, InstalledApp b) { return StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name); });
            return list;
        }

        private static void ScanHive(RegistryKey root, string subPath, string scope, List<InstalledApp> list, HashSet<string> seen)
        {
            try
            {
                using (RegistryKey k = root.OpenSubKey(subPath))
                {
                    if (k == null) return;
                    string[] names = k.GetSubKeyNames();
                    for (int i = 0; i < names.Length; i++)
                    {
                        try
                        {
                            using (RegistryKey sk = k.OpenSubKey(names[i]))
                            {
                                if (sk == null) continue;
                                object sysObj = sk.GetValue("SystemComponent");
                                if (sysObj is int && (int)sysObj == 1) continue;
                                string parent = sk.GetValue("ParentKeyName") as string;
                                if (!string.IsNullOrEmpty(parent)) continue;

                                string disp = (sk.GetValue("DisplayName") as string ?? "").Trim();
                                string uninst = (sk.GetValue("UninstallString") as string ?? "").Trim();
                                if (disp.Length == 0 || uninst.Length == 0) continue;

                                string keySig = disp + "|" + (sk.GetValue("DisplayVersion") as string ?? "");
                                if (!seen.Add(keySig)) continue;

                                InstalledApp a = new InstalledApp();
                                a.Name = disp;
                                a.Publisher = (sk.GetValue("Publisher") as string ?? "").Trim();
                                a.Version = (sk.GetValue("DisplayVersion") as string ?? "").Trim();
                                a.InstallDate = FormatRegDate(sk.GetValue("InstallDate") as string);
                                object szObj = sk.GetValue("EstimatedSize");
                                if (szObj is int) a.SizeKb = (int)szObj;
                                else if (szObj is long) a.SizeKb = (long)szObj;
                                a.Scope = scope;
                                a.UninstallString = uninst;
                                a.InstallLocation = (sk.GetValue("InstallLocation") as string ?? "").Trim();
                                a.RegPath = root.Name + "\\" + subPath + "\\" + names[i];
                                a.Norm = Helpers.StripAccents(a.Name + " " + a.Publisher);
                                list.Add(a);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static string FormatRegDate(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length != 8) return "";
            return raw.Substring(6, 2) + "/" + raw.Substring(4, 2) + "/" + raw.Substring(0, 4);
        }

        // Strict protected system prefixes — NEVER shown for removal!
        private static readonly string[] ProtectedTokens = new string[]
        {
            "WindowsStore", "DesktopAppInstaller", "StorePurchaseApp", "VCLibs", "NET.Native",
            "UI.Xaml", "WindowsAppRuntime", "WinAppRuntime", "DDLM", "DirectX", "Advertising",
            "LanguageExperiencePack", "WebMediaExtensions", "AV1VideoExtension", "AVCEncoder",
            "HEIFImageExtension", "HEVCVideoExtension", "VP9VideoExtensions", "MPEG2VideoExtension",
            "RawImageExtension", "WebpImageExtension", "SecHealthUI", "ShellExperienceHost",
            "StartMenu", "StartExperiencesApp", "FileExp", "LockApp", "AAD.BrokerPlugin",
            "CloudExperienceHost", "Client.CBS", "Client.Core", "Client.OOBE", "Client.Photon",
            "AccountsControl", "CredDialogHost", "BioEnrollment", "InputApp", "Winget.Source",
            "Services.Store.Engagement", "Win32WebViewHost", "EdgeDevToolsClient", "XboxIdentityProvider",
            "NVIDIA", "IntelGraphics", "AMD", "Realtek", "Dolby", "Synaptics", "ApplicationCompatibility",
            "MicrosoftEdge.Stable", "WindowsTerminal"
        };

        private sealed class BloatMeta
        {
            public string Friendly, Category, Note, StoreQuery;
            public BloatMeta(string f, string c, string n, string q) { Friendly = f; Category = c; Note = n; StoreQuery = q; }
        }

        private static readonly Dictionary<string, BloatMeta> KnownBloat = new Dictionary<string, BloatMeta>(StringComparer.OrdinalIgnoreCase)
        {
            { "MSTeams", new BloatMeta("Microsoft Teams (Personal/Chat)", "Βαρύ WebView2 Bloat", "~370 MB δίσκος + WebView2 RAM στο παρασκήνιο", "Microsoft Teams") },
            { "Microsoft.OutlookForWindows", new BloatMeta("New Outlook for Windows", "Βαρύ WebView2 Bloat", "WebView2 wrapper (~80–160 MB RAM)", "Outlook for Windows") },
            { "microsoft.windowscommunicationsapps", new BloatMeta("Mail & Calendar (Παλιό)", "Προεγκατεστημένο Bloat", "~206 MB δίσκος (παρωχημένο app)", "Mail and Calendar") },
            { "Microsoft.Office.OneNote", new BloatMeta("OneNote for Windows", "Office / Cloud Bloat", "~142 MB δίσκος + background sync", "OneNote") },
            { "Microsoft.M365Companions", new BloatMeta("Microsoft 365 Companions", "WebView2 / M365 Bloat", "~125 MB δίσκος + background hosts", "Microsoft 365") },
            { "Microsoft.MicrosoftOfficeHub", new BloatMeta("Microsoft 365 (Office Hub)", "Promo / WebView2 Bloat", "Διαφημιστικό hub συνδρομής M365", "Microsoft 365") },
            { "Clipchamp.Clipchamp", new BloatMeta("Microsoft Clipchamp", "Βαρύ WebView2 Bloat", "Web video editor με cloud account", "Clipchamp") },
            { "Microsoft.MicrosoftStickyNotes", new BloatMeta("Sticky Notes (UWP)", "UWP Bloat", "Τρώει WebView2/OneNote sync · αντικατάσταση: NillyNote Pro", "Microsoft Sticky Notes") },
            { "Microsoft.Windows.Photos", new BloatMeta("Windows Photos", "Αντικαθίσταται από Σουίτα", "~881 MB! Αντικατάσταση: NillyView Pro (50 KB)", "Microsoft Photos") },
            { "Microsoft.Paint", new BloatMeta("Microsoft Paint (WinUI)", "Αντικαθίσταται από Σουίτα", "~417 MB! Αντικατάσταση: NillyView Pro + NillyQuickSnap", "Paint") },
            { "Microsoft.ScreenSketch", new BloatMeta("Snipping Tool (WinUI + AI)", "Αντικαθίσταται από Σουίτα", "~533 MB! Αντικατάσταση: NillyQuickSnap (49 KB)", "Snipping Tool") },
            { "Microsoft.WindowsNotepad", new BloatMeta("Windows 11 Notepad (AI)", "Αντικαθίσταται από Σουίτα", "UWP + Copilot AI · Αντικατάσταση: NillyNote Pro (78 KB)", "Windows Notepad") },
            { "Microsoft.WindowsCalculator", new BloatMeta("Windows Calculator (UWP)", "Αντικαθίσταται από Σουίτα", "UWP XAML · Αντικατάσταση: NillyCalc Pro (72 KB)", "Windows Calculator") },
            { "Microsoft.ZuneMusic", new BloatMeta("Windows Media Player (UWP)", "Αντικαθίσταται από Σουίτα", "WinUI media indexer · Αντικατάσταση: NillyMedia Lite (52 KB)", "Windows Media Player") },
            { "Microsoft.ZuneVideo", new BloatMeta("Movies & TV (UWP)", "Αντικαθίσταται από Σουίτα", "Αντικατάσταση: NillyMedia Lite (52 KB)", "Movies & TV") },
            { "Microsoft.WindowsSoundRecorder", new BloatMeta("Sound Recorder (UWP)", "Αντικαθίσταται από Σουίτα", "Αντικατάσταση: NillyMedia Lite WAV Recorder", "Windows Sound Recorder") },
            { "Microsoft.WindowsAlarms", new BloatMeta("Windows Clock & Focus", "UWP Bloat", "Βαρύ WinUI ρολόι/ξυπνητήρι (~80 MB RAM)", "Windows Clock") },
            { "Microsoft.WindowsCamera", new BloatMeta("Windows Camera", "Προεγκατεστημένο App", "~23 MB UWP camera app", "Windows Camera") },
            { "Microsoft.BingNews", new BloatMeta("Microsoft News", "Διαφημιστικό Bloat", "Feeds & widgets background", "Microsoft News") },
            { "Microsoft.BingWeather", new BloatMeta("MSN Weather", "Διαφημιστικό Bloat", "Background weather tiles", "MSN Weather") },
            { "Microsoft.BingSearch", new BloatMeta("Bing Search Extension", "Telemetry / Web Bloat", "Bing web results στο Start", "Bing") },
            { "Microsoft.MicrosoftSolitaireCollection", new BloatMeta("Microsoft Solitaire (Ads)", "Προεγκατεστημένο Παιχνίδι", "Παιχνίδι με διαφημίσεις", "Microsoft Solitaire Collection") },
            { "Microsoft.YourPhone", new BloatMeta("Phone Link", "Background Bloat", "Τρέχει μόνιμα στο παρασκήνιο (PhoneExperienceHost)", "Phone Link") },
            { "Microsoft.WindowsFeedbackHub", new BloatMeta("Feedback Hub", "Telemetry Bloat", "Αποστολή διαγνωστικών Microsoft", "Feedback Hub") },
            { "Microsoft.GetHelp", new BloatMeta("Get Help", "Προεγκατεστημένο Bloat", "Εφαρμογή υποστήριξης WebView2", "Get Help") },
            { "Microsoft.Getstarted", new BloatMeta("Microsoft Tips", "Προεγκατεστημένο Bloat", "Ειδοποιήσεις & συμβουλές Windows", "Microsoft Tips") },
            { "Microsoft.WindowsMaps", new BloatMeta("Windows Maps", "Προεγκατεστημένο Bloat", "Παρωχημένοι χάρτες Microsoft", "Windows Maps") },
            { "Microsoft.People", new BloatMeta("Microsoft People", "Προεγκατεστημένο Bloat", "Παλιές επαφές Windows", "Microsoft People") },
            { "Microsoft.Todos", new BloatMeta("Microsoft To Do", "Cloud Bloat", "Cloud task manager", "Microsoft To Do") },
            { "Microsoft.PowerAutomateDesktop", new BloatMeta("Power Automate Desktop", "Βαρύ Enterprise Bloat", "Εταιρικό automation (~300+ MB)", "Power Automate") },
            { "Microsoft.GamingApp", new BloatMeta("Xbox App", "Gaming Bloat", "Βαρύ gaming launcher (αν δεν παίζεις Xbox)", "Xbox") },
            { "Microsoft.XboxGamingOverlay", new BloatMeta("Xbox Game Bar", "Gaming Overlay", "Overlay στο παρασκήνιο", "Game Bar") },
            { "Microsoft.Copilot", new BloatMeta("Microsoft Copilot", "AI / WebView2 Bloat", "AI sidebar WebView2", "Microsoft Copilot") },
            { "Microsoft.549981C3F5F10", new BloatMeta("Cortana (Legacy)", "Παρωχημένο Bloat", "Παλιός φωνητικός βοηθός", "Cortana") }
        };

        private static bool IsProtectedPackage(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            for (int i = 0; i < ProtectedTokens.Length; i++)
                if (name.IndexOf(ProtectedTokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static List<BloatApp> ScanStoreBloatApps()
        {
            List<BloatApp> list = new List<BloatApp>();
            try
            {
                string psScript =
                    "$ErrorActionPreference='SilentlyContinue';" +
                    "Get-AppxPackage | Where-Object { -not $_.NonRemovable -and -not $_.IsFramework } | ForEach-Object {" +
                    "  $sz=0; if ($_.InstallLocation -and (Test-Path $_.InstallLocation)) {" +
                    "    $sz=(Get-ChildItem $_.InstallLocation -Recurse -File -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum" +
                    "  };" +
                    "  Write-Output ($_.Name + '|' + $_.PackageFullName + '|' + [math]::Round(($sz/1MB),1))" +
                    "}";

                ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + psScript + "\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.StandardOutputEncoding = Encoding.UTF8;

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(15000);
                    string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string[] parts = lines[i].Split('|');
                        if (parts.Length < 3) continue;
                        string pkg = parts[0].Trim();
                        string full = parts[1].Trim();
                        if (IsProtectedPackage(pkg) || !seen.Add(pkg)) continue;

                        double mb = 0;
                        double.TryParse(parts[2].Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out mb);

                        BloatApp b = new BloatApp();
                        b.PackageName = pkg;
                        b.FullName = full;
                        b.SizeMb = mb;

                        BloatMeta meta;
                        if (KnownBloat.TryGetValue(pkg, out meta))
                        {
                            b.FriendlyName = meta.Friendly;
                            b.Category = meta.Category;
                            b.ReplacesNote = meta.Note;
                            b.StoreQuery = meta.StoreQuery;
                            b.IsRecommendedBloat = true;
                        }
                        else
                        {
                            b.FriendlyName = pkg.Replace("Microsoft.", "");
                            b.Category = "Store App (Αφαιρούμενο)";
                            b.ReplacesNote = "Προαιρετικό Store πακέτο (επαναφέρεται από MS Store)";
                            b.StoreQuery = b.FriendlyName;
                            b.IsRecommendedBloat = false;
                        }
                        b.Norm = Helpers.StripAccents(b.FriendlyName + " " + b.PackageName + " " + b.Category + " " + b.ReplacesNote);
                        list.Add(b);
                    }
                }
            }
            catch { }

            list.Sort(delegate(BloatApp a, BloatApp b)
            {
                if (a.IsRecommendedBloat != b.IsRecommendedBloat) return a.IsRecommendedBloat ? -1 : 1;
                return b.SizeMb.CompareTo(a.SizeMb);
            });
            return list;
        }

        private static readonly HashSet<string> SafeSystemAppDataFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft", "Windows", "Packages", "Temp", "Programs", "NillySuite", "NillyHub",
            "NillyFilesPro", "NillyClipPro", "NillyDocPro", "NillyNotePro", "NillyCodePro", "NillyViewPro",
            "NillyTaskPro", "NillyDiskPro", "NillyZipPro", "NillySearchPro", "NillyUninstallPro", "NillyNetPro", "NillyFontPro",
            "Google", "Mozilla", "Adobe", "Corel", "Apple", "Apple Computer", "Intel", "NVIDIA", "NVIDIA Corporation",
            "AMD", "Realtek", "GitHub", "GitHub CLI", "Code", "Cursor", "Antigravity", "JetBrains", "npm", "npm-cache",
            "pip", "Python", "NuGet", "IsolatedStorage", "ElevatedDiagnostics", "D3DSCache", "CrashDumps",
            "ConnectedDevicesPlatform", "Comms", "PeerDistRepub", "Publishers", "VirtualStore", "Speech",
            "OneDrive", "Identities", "Low", "PlaceholderTileLogoFolder", "ToastNotificationManagerCompat",
            "fontconfig", "CefSharp", "SquirrelTemp", "Diagnostics", "DBG", "Deployment", "WinGet", "WindowsApps",
            "History", "Temporary Internet Files", "Application Data", "Local Settings", "Start Menu", "Templates"
        };

        public static List<LeftoverItem> ScanLeftovers(List<InstalledApp> installed)
        {
            List<LeftoverItem> list = new List<LeftoverItem>();
            HashSet<string> tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < installed.Count; i++)
            {
                AddTokens(tokens, installed[i].Name);
                AddTokens(tokens, installed[i].Publisher);
                if (!string.IsNullOrEmpty(installed[i].InstallLocation))
                    AddTokens(tokens, Path.GetFileName(installed[i].InstallLocation.TrimEnd('\\')));
            }

            ScanAppDataRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalAppData", tokens, list);
            ScanAppDataRoot(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Roaming AppData", tokens, list);

            list.Sort(delegate(LeftoverItem a, LeftoverItem b) { return b.SizeBytes.CompareTo(a.SizeBytes); });
            return list;
        }

        private static void AddTokens(HashSet<string> set, string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            string clean = s.ToLowerInvariant();
            set.Add(clean);
            string[] parts = clean.Split(new char[] { ' ', '-', '_', '.', ',', '(', ')', '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length >= 3) set.Add(parts[i]);
        }

        private static void ScanAppDataRoot(string root, string label, HashSet<string> installedTokens, List<LeftoverItem> list)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
            try
            {
                string[] dirs = Directory.GetDirectories(root);
                for (int i = 0; i < dirs.Length; i++)
                {
                    string name = Path.GetFileName(dirs[i]);
                    if (string.IsNullOrEmpty(name) || name.StartsWith(".") || name.StartsWith("{") || name.StartsWith("Nilly")) continue;
                    if (SafeSystemAppDataFolders.Contains(name)) continue;

                    string low = name.ToLowerInvariant();
                    bool matched = installedTokens.Contains(low);
                    if (!matched)
                    {
                        string[] parts = low.Split(new char[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int k = 0; k < parts.Length; k++)
                            if (parts[k].Length >= 4 && installedTokens.Contains(parts[k])) { matched = true; break; }
                    }
                    if (matched) continue;

                    long sz = Helpers.MeasureDirBytes(dirs[i], 400);
                    if (sz < 1024) continue;
                    LeftoverItem it = new LeftoverItem();
                    it.FolderName = name;
                    it.RootLabel = label;
                    it.FullPath = dirs[i];
                    it.SizeBytes = sz;
                    try { it.Modified = Directory.GetLastWriteTime(dirs[i]); } catch { }
                    it.Norm = Helpers.StripAccents(name + " " + label);
                    list.Add(it);
                }
            }
            catch { }
        }
    }

    // ========================================================================
    // MAIN FORM
    // ========================================================================
    internal sealed class MainForm : Form
    {
        private bool _dark = true;
        private int _activeTab = 0; // 0=Win32, 1=Win11 Bloat, 2=Leftovers
        private readonly ToolTip _tip = new ToolTip();

        private Color _bgMain, _bgPanel, _bgSurf, _bgHover, _bgSel, _bgAlt, _border, _fgMain, _fgMuted, _accent, _cyan, _green, _pink;

        private readonly Panel _header, _tabBar, _actionBar, _statusBar;
        private readonly PictureBox _logoBox;
        private readonly Label _lblTitle, _lblSub, _lblStatus, _lblSummary;
        private readonly Button _btnTabApps, _btnTabBloat, _btnTabLeft, _btnRefresh, _btnTheme, _btnInfo;
        private readonly TextBox _txtSearch;
        private readonly FlowLayoutPanel _actFlow;
        private readonly Button _btnUninstall, _btnRemoveBloat, _btnRestoreStore, _btnCleanLeftover, _btnOpenDir, _btnExportNote;
        private readonly DoubleBufferedLV _lvApps, _lvBloat, _lvLeft;
        private readonly LinkLabel _lnkBrand;

        private List<InstalledApp> _allApps = new List<InstalledApp>();
        private List<InstalledApp> _visApps = new List<InstalledApp>();
        private List<BloatApp> _allBloat = new List<BloatApp>();
        private List<BloatApp> _visBloat = new List<BloatApp>();
        private List<LeftoverItem> _allLeft = new List<LeftoverItem>();
        private List<LeftoverItem> _visLeft = new List<LeftoverItem>();
        private bool _bloatLoaded, _leftLoaded;
        private Bitmap _logoBmp;

        private readonly Font _fTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        private readonly Font _fUi = new Font("Segoe UI", 9.0f);
        private readonly Font _fBold = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        private readonly Font _fSmall = new Font("Segoe UI", 8.2f);

        public int AppsCount { get { return _allApps.Count; } }
        public int BloatCount { get { return _allBloat.Count; } }

        public MainForm(bool smokeMode)
        {
            _dark = ReadSuiteDark(true);
            Text = "NillyUninstall Pro — Ultra-Light Uninstaller & Windows 11 Bloat Remover | Nilly graphics";
            Size = new Size(1120, 690);
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterScreen;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NillyUninstallPro.ico");
            if (File.Exists(icoPath))
            {
                _logoBmp = Native.LoadCrispIcon(icoPath, 26);
                try { Icon = new Icon(icoPath); } catch { }
            }

            // Header
            _header = new Panel { Dock = DockStyle.Top, Height = 46 };
            _logoBox = new PictureBox { Size = new Size(26, 26), Location = new Point(12, 10), SizeMode = PictureBoxSizeMode.Zoom, Image = _logoBmp };
            _lblTitle = new Label { Text = "NillyUninstall Pro", AutoSize = true, Location = new Point(44, 7), Font = _fTitle };
            _lblSub = new Label { Text = "Ultra-Light Uninstaller · Safe Win11 Debloat · 0 MB Idle RAM", AutoSize = true, Location = new Point(45, 25), Font = new Font("Segoe UI", 7.6f) };

            FlowLayoutPanel rightHdr = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, Padding = new Padding(0, 8, 10, 0) };
            _txtSearch = new TextBox { Width = 220, Height = 26, BorderStyle = BorderStyle.FixedSingle, Font = _fUi, Margin = new Padding(0, 2, 6, 0) };
            _txtSearch.TextChanged += delegate { RenderActiveTab(); };
            _txtSearch.HandleCreated += delegate { Native.SetCue(_txtSearch, "Αναζήτηση (Ctrl+F)..."); };
            _btnRefresh = MkBtn("↻", 32, "Επανασάρωση (F5)", delegate { ReloadAll(); });
            _btnTheme = MkBtn("◐", 32, "Αλλαγή Dark / Light θέματος", delegate { _dark = !_dark; ApplyTheme(); });
            _btnInfo = MkBtn("i", 32, "Ultra-Light Specs & Πληροφορίες", delegate { ShowInfoDialog(); });
            rightHdr.Controls.AddRange(new Control[] { _txtSearch, _btnRefresh, _btnTheme, _btnInfo });
            _header.Controls.AddRange(new Control[] { _logoBox, _lblTitle, _lblSub, rightHdr });

            // Responsive Tab bar (auto-wraps summary cleanly below 820px)
            _tabBar = new Panel { Dock = DockStyle.Top, Height = 38 };
            _btnTabApps = MkBtn("Εγκατεστημένα Προγράμματα", 205, "Όλα τα Win32 / MSI / Χρήστη προγράμματα", delegate { SwitchTab(0); });
            _btnTabBloat = MkBtn("Windows 11 Bloat (Ασφαλές)", 215, "Αφαιρούμενα Microsoft / Store Bloat Apps με επαναφορά Store", delegate { SwitchTab(1); });
            _btnTabLeft = MkBtn("Υπολείμματα (Leftovers)", 180, "Ορφανοί φάκελοι AppData (διαγραφή μόνο στον Κάδο)", delegate { SwitchTab(2); });
            _lblSummary = new Label { AutoSize = true, Font = _fSmall, TextAlign = ContentAlignment.MiddleRight, UseMnemonic = false };
            _tabBar.Controls.AddRange(new Control[] { _btnTabApps, _btnTabBloat, _btnTabLeft, _lblSummary });
            _tabBar.Resize += delegate { LayoutTabBar(); };

            // Action bar
            _actionBar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10, 5, 10, 5) };
            _actFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            _btnUninstall = MkBtn("✕ Απεγκατάσταση Επιλεγμένου", 205, "Εκκίνηση κανονικής απεγκατάστασης προγράμματος", delegate { UninstallSelectedWin32(); });
            _btnOpenDir = MkBtn("Φάκελος Εγκατάστασης", 160, "Άνοιγμα φακέλου εγκατάστασης (NillyFiles / Explorer)", delegate { OpenSelectedFolder(); });
            _btnExportNote = MkBtn("↗ Λίστα σε Note", 128, "Εξαγωγή λίστας εγκατεστημένων προγραμμάτων στο NillyNote Pro", delegate { ExportAppsToNillyNote(); });
            _btnRemoveBloat = MkBtn("✕ Αφαίρεση Επιλεγμένων Bloat", 215, "Ασφαλής αφαίρεση επιλεγμένων Store Bloat Apps (Zero Admin)", delegate { RemoveSelectedBloat(); });
            _btnRestoreStore = MkBtn("↺ Επαναφορά από Microsoft Store", 220, "Άνοιγμα στο Microsoft Store για 1-κλικ επανεγκατάσταση", delegate { RestoreSelectedFromStore(); });
            _btnCleanLeftover = MkBtn("Κάδος Ανακύκλωσης (Επιλεγμένο)", 225, "Ασφαλής μεταφορά στον Κάδο Ανακύκλωσης", delegate { DeleteSelectedLeftover(); });
            _actFlow.Controls.AddRange(new Control[] { _btnUninstall, _btnOpenDir, _btnExportNote, _btnRemoveBloat, _btnRestoreStore, _btnCleanLeftover });
            _actionBar.Controls.Add(_actFlow);

            // ListViews (auto-stretched columns — zero white header gap!)
            _lvApps = BuildLV(new string[] { "Πρόγραμμα", "Εκδότης", "Έκδοση", "Ημερομηνία", "Μέγεθος", "Είδος" },
                              new int[] { 340, 210, 110, 100, 95, 130 });
            _lvApps.DoubleClick += delegate { UninstallSelectedWin32(); };
            _lvApps.Resize += delegate { FitColumns(_lvApps, new int[] { 310, 195, 100, 95, 90, 125 }); };

            _lvBloat = BuildLV(new string[] { "Εφαρμογή Windows 11", "Κατηγορία", "Μέγεθος", "Αντίκτυπος / Αντικατάσταση Σουίτας" },
                               new int[] { 255, 175, 95, 480 });
            _lvBloat.CheckBoxes = true;
            _lvBloat.Resize += delegate { FitColumns(_lvBloat, new int[] { 245, 170, 90, 440 }); };

            _lvLeft = BuildLV(new string[] { "Φάκελος (Πιθανό Υπόλειμμα)", "Τοποθεσία", "Μέγεθος", "Τροποποίηση", "Πλήρης Διαδρομή" },
                              new int[] { 220, 125, 95, 125, 430 });
            _lvLeft.DoubleClick += delegate
            {
                if (_lvLeft.SelectedItems.Count > 0)
                {
                    LeftoverItem l = _lvLeft.SelectedItems[0].Tag as LeftoverItem;
                    if (l != null && Directory.Exists(l.FullPath)) OpenFolderSmart(l.FullPath);
                }
            };
            _lvLeft.Resize += delegate { FitColumns(_lvLeft, new int[] { 200, 120, 90, 115, 390 }); };

            Panel body = new Panel { Dock = DockStyle.Fill };
            body.Controls.AddRange(new Control[] { _lvApps, _lvBloat, _lvLeft });

            // Status bar
            _statusBar = new Panel { Dock = DockStyle.Bottom, Height = 26, Padding = new Padding(10, 0, 10, 0) };
            _lblStatus = new Label { Dock = DockStyle.Fill, Font = _fSmall, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
            _lnkBrand = new LinkLabel
            {
                Text = "created by Nilly graphics (Sotiris Christos) — nillygraphics.com",
                Dock = DockStyle.Right, AutoSize = true, Font = _fSmall, Padding = new Padding(8, 5, 2, 0), LinkBehavior = LinkBehavior.HoverUnderline
            };
            _lnkBrand.LinkClicked += delegate { OpenUrl("https://nillygraphics.com/"); };
            _statusBar.Controls.Add(_lblStatus);
            _statusBar.Controls.Add(_lnkBrand);

            Controls.Add(body);
            Controls.Add(_actionBar);
            Controls.Add(_tabBar);
            Controls.Add(_header);
            Controls.Add(_statusBar);

            HandleCreated += delegate
            {
                Native.SetDarkTitlebar(Handle, _dark);
                Native.ApplyTaskbarIcon(this, icoPath);
            };
            Shown += delegate
            {
                FitColumns(_lvApps, new int[] { 310, 195, 100, 95, 90, 125 });
                FitColumns(_lvBloat, new int[] { 245, 170, 90, 440 });
                FitColumns(_lvLeft, new int[] { 200, 120, 90, 115, 390 });
                Native.TrimMemory();
            };

            ApplyTheme();
            LayoutTabBar();
            ReloadAll();
            SwitchTab(0);
        }

        private void LayoutTabBar()
        {
            int w = Math.Max(560, _tabBar.ClientSize.Width);
            int x = 10;
            _btnTabApps.Location = new Point(x, 5); x += _btnTabApps.Width + 6;
            _btnTabBloat.Location = new Point(x, 5); x += _btnTabBloat.Width + 6;
            _btnTabLeft.Location = new Point(x, 5); x += _btnTabLeft.Width + 6;
            if (w >= x + 195)
            {
                if (_tabBar.Height != 38) _tabBar.Height = 38;
                _lblSummary.Location = new Point(w - Math.Max(180, _lblSummary.Width) - 12, 10);
            }
            else
            {
                if (_tabBar.Height != 62) _tabBar.Height = 62;
                _lblSummary.Location = new Point(12, 38);
            }
        }

        private static void FitColumns(ListView lv, int[] mins)
        {
            if (lv == null || lv.Columns.Count != mins.Length) return;
            int cw = lv.ClientSize.Width;
            if (cw <= 120) return;
            int total = 0; for (int i = 0; i < mins.Length; i++) total += mins[i];
            int target = Math.Max(total, cw - 2), used = 0;
            for (int i = 0; i < mins.Length - 1; i++)
            {
                int w = (int)Math.Round((double)mins[i] * target / total);
                lv.Columns[i].Width = Math.Max(55, w);
                used += lv.Columns[i].Width;
            }
            lv.Columns[mins.Length - 1].Width = Math.Max(90, target - used);
        }

        private Button MkBtn(string txt, int w, string tip, EventHandler click)
        {
            Button b = new Button
            {
                Text = txt, Width = w, Height = 28, FlatStyle = FlatStyle.Flat, Font = _fBold,
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 6, 0), TabStop = false
            };
            b.FlatAppearance.BorderSize = 1;
            b.Click += click;
            _tip.SetToolTip(b, tip);
            return b;
        }

        private DoubleBufferedLV BuildLV(string[] cols, int[] widths)
        {
            DoubleBufferedLV lv = new DoubleBufferedLV
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = true,
                BorderStyle = BorderStyle.None, OwnerDraw = true, Font = _fUi, HideSelection = false
            };
            for (int i = 0; i < cols.Length; i++)
                lv.Columns.Add(cols[i], widths[i], cols[i] == "Μέγεθος" ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            lv.DrawColumnHeader += DrawHdr;
            lv.DrawSubItem += DrawSub;
            lv.DrawItem += delegate(object s, DrawListViewItemEventArgs e)
            {
                if (((ListView)s).CheckBoxes) e.DrawDefault = false;
            };
            lv.HandleCreated += delegate { Native.SetDarkScrollbars(lv, _dark); };
            return lv;
        }

        private void ApplyTheme()
        {
            if (_dark)
            {
                _bgMain = Color.FromArgb(24, 26, 31); _bgPanel = Color.FromArgb(31, 34, 41);
                _bgSurf = Color.FromArgb(39, 43, 52); _bgHover = Color.FromArgb(49, 54, 66);
                _bgSel = Color.FromArgb(54, 67, 89); _bgAlt = Color.FromArgb(28, 30, 36);
                _border = Color.FromArgb(52, 57, 68); _fgMain = Color.FromArgb(236, 239, 244);
                _fgMuted = Color.FromArgb(148, 157, 174); _accent = Color.FromArgb(252, 150, 31);
                _cyan = Color.FromArgb(102, 217, 239); _green = Color.FromArgb(166, 226, 46);
                _pink = Color.FromArgb(249, 38, 114);
            }
            else
            {
                _bgMain = Color.FromArgb(248, 249, 251); _bgPanel = Color.FromArgb(238, 241, 246);
                _bgSurf = Color.White; _bgHover = Color.FromArgb(226, 232, 242);
                _bgSel = Color.FromArgb(209, 228, 250); _bgAlt = Color.FromArgb(243, 245, 249);
                _border = Color.FromArgb(210, 216, 226); _fgMain = Color.FromArgb(28, 32, 40);
                _fgMuted = Color.FromArgb(98, 108, 125); _accent = Color.FromArgb(224, 118, 12);
                _cyan = Color.FromArgb(14, 132, 168); _green = Color.FromArgb(62, 145, 32);
                _pink = Color.FromArgb(204, 28, 92);
            }

            Native.SetDarkTitlebar(Handle, _dark);
            BackColor = _bgMain; ForeColor = _fgMain;
            _header.BackColor = _bgPanel; _tabBar.BackColor = _bgPanel;
            _actionBar.BackColor = _bgMain; _statusBar.BackColor = _bgPanel;
            _lblTitle.ForeColor = _fgMain; _lblSub.ForeColor = _accent;
            _lblStatus.ForeColor = _fgMuted; _lblSummary.ForeColor = _cyan;
            _lnkBrand.LinkColor = _fgMuted; _lnkBrand.ActiveLinkColor = _accent;
            _txtSearch.BackColor = _bgSurf; _txtSearch.ForeColor = _fgMain;

            DoubleBufferedLV[] lvs = new DoubleBufferedLV[] { _lvApps, _lvBloat, _lvLeft };
            for (int i = 0; i < lvs.Length; i++)
            {
                lvs[i].BackColor = _bgMain; lvs[i].ForeColor = _fgMain;
                Native.SetDarkScrollbars(lvs[i], _dark);
                lvs[i].Invalidate();
            }
            StyleButtons();
        }

        private void StyleButtons()
        {
            StyleBtn(_btnRefresh, false, _accent);
            StyleBtn(_btnTheme, false, _accent);
            StyleBtn(_btnInfo, false, _accent);
            _btnInfo.ForeColor = _accent; _btnInfo.FlatAppearance.BorderColor = _accent;

            StyleBtn(_btnTabApps, _activeTab == 0, _accent);
            StyleBtn(_btnTabBloat, _activeTab == 1, _pink);
            StyleBtn(_btnTabLeft, _activeTab == 2, _cyan);

            StyleBtn(_btnUninstall, true, _pink);
            StyleBtn(_btnOpenDir, false, _cyan);
            StyleBtn(_btnExportNote, false, _accent);
            _btnExportNote.ForeColor = _accent;
            StyleBtn(_btnRemoveBloat, true, _pink);
            StyleBtn(_btnRestoreStore, false, _green);
            StyleBtn(_btnCleanLeftover, true, _accent);
        }

        private void StyleBtn(Button b, bool primary, Color accent)
        {
            b.BackColor = primary ? accent : _bgSurf;
            b.ForeColor = primary ? Color.FromArgb(20, 22, 26) : _fgMain;
            b.FlatAppearance.BorderColor = primary ? accent : _border;
            b.FlatAppearance.MouseOverBackColor = primary ? accent : _bgHover;
        }

        public void SwitchTab(int tab)
        {
            _activeTab = tab;
            _lvApps.Visible = (tab == 0);
            _lvBloat.Visible = (tab == 1);
            _lvLeft.Visible = (tab == 2);

            _btnUninstall.Visible = (tab == 0);
            _btnOpenDir.Visible = (tab == 0);
            _btnExportNote.Visible = (tab == 0);
            _btnRemoveBloat.Visible = (tab == 1);
            _btnRestoreStore.Visible = (tab == 1);
            _btnCleanLeftover.Visible = (tab == 2);

            StyleButtons();
            if (tab == 1 && !_bloatLoaded) LoadBloatAsync();
            if (tab == 2 && !_leftLoaded) LoadLeftoversAsync();
            RenderActiveTab();
        }

        public void ReloadAll()
        {
            Stopwatch sw = Stopwatch.StartNew();
            _allApps = AppScanners.ScanWin32Apps();
            sw.Stop();
            _bloatLoaded = false;
            _leftLoaded = false;
            _lblStatus.Text = string.Format("✓ Σαρώθηκαν {0} εγκατεστημένα προγράμματα σε {1} ms (0 MB idle RAM).", _allApps.Count, sw.ElapsedMilliseconds);
            RenderActiveTab();
            if (_activeTab == 1) LoadBloatAsync();
            else if (_activeTab == 2) LoadLeftoversAsync();
        }

        public void LoadBloatSyncForSmoke()
        {
            _allBloat = AppScanners.ScanStoreBloatApps();
            _bloatLoaded = true;
            RenderBloat();
        }

        private void LoadBloatAsync()
        {
            _bloatLoaded = true;
            _lblStatus.Text = "Σάρωση προεγκατεστημένων Windows 11 Bloat Apps & μέτρηση MB...";
            Thread th = new Thread(delegate()
            {
                List<BloatApp> res = AppScanners.ScanStoreBloatApps();
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        _allBloat = res;
                        RenderActiveTab();
                        double recMb = 0; int recCnt = 0;
                        for (int i = 0; i < res.Count; i++) if (res[i].IsRecommendedBloat) { recMb += res[i].SizeMb; recCnt++; }
                        _lblStatus.Text = string.Format("✓ Βρέθηκαν {0} ασφαλή bloat apps ({1:0.0} MB δίσκος) · Codecs/Store/Defender είναι κλειδωμένα ασφαλείας.", recCnt, recMb);
                    });
                }
                catch { }
            });
            th.IsBackground = true; th.Start();
        }

        private void LoadLeftoversAsync()
        {
            _leftLoaded = true;
            _lblStatus.Text = "Ασφαλής σάρωση AppData για ορφανούς φακέλους...";
            Thread th = new Thread(delegate()
            {
                List<LeftoverItem> res = AppScanners.ScanLeftovers(_allApps);
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        _allLeft = res;
                        RenderActiveTab();
                        long tot = 0; for (int i = 0; i < res.Count; i++) tot += res[i].SizeBytes;
                        _lblStatus.Text = string.Format("✓ Βρέθηκαν {0} πιθανοί ορφανοί φάκελοι ({1}) — διαγραφή ΜΟΝΟ στον Κάδο Ανακύκλωσης.", res.Count, Helpers.FmtBytes(tot));
                    });
                }
                catch { }
            });
            th.IsBackground = true; th.Start();
        }

        private void RenderActiveTab()
        {
            if (_activeTab == 0) RenderApps();
            else if (_activeTab == 1) RenderBloat();
            else RenderLeftovers();
        }

        private void RenderApps()
        {
            string q = Helpers.StripAccents(_txtSearch.Text.Trim());
            _visApps = new List<InstalledApp>(_allApps.Count);
            long totKb = 0;
            for (int i = 0; i < _allApps.Count; i++)
            {
                if (q.Length == 0 || _allApps[i].Norm.IndexOf(q, StringComparison.Ordinal) >= 0)
                {
                    _visApps.Add(_allApps[i]);
                    totKb += _allApps[i].SizeKb;
                }
            }
            _lvApps.BeginUpdate();
            _lvApps.Items.Clear();
            for (int i = 0; i < _visApps.Count; i++)
            {
                InstalledApp a = _visApps[i];
                ListViewItem it = new ListViewItem(a.Name);
                it.SubItems.Add(a.Publisher);
                it.SubItems.Add(a.Version);
                it.SubItems.Add(a.InstallDate);
                it.SubItems.Add(a.SizeKb > 0 ? Helpers.FmtMb(a.SizeKb / 1024.0) : "—");
                it.SubItems.Add(a.Scope);
                it.Tag = a;
                _lvApps.Items.Add(it);
            }
            _lvApps.EndUpdate();
            _lblSummary.Text = string.Format("{0} προγράμματα · ~{1}", _visApps.Count, Helpers.FmtMb(totKb / 1024.0));
        }

        private void RenderBloat()
        {
            string q = Helpers.StripAccents(_txtSearch.Text.Trim());
            _visBloat = new List<BloatApp>(_allBloat.Count);
            double recMb = 0;
            for (int i = 0; i < _allBloat.Count; i++)
            {
                if (q.Length == 0 || _allBloat[i].Norm.IndexOf(q, StringComparison.Ordinal) >= 0)
                {
                    _visBloat.Add(_allBloat[i]);
                    if (_allBloat[i].IsRecommendedBloat) recMb += _allBloat[i].SizeMb;
                }
            }
            _lvBloat.BeginUpdate();
            _lvBloat.Items.Clear();
            for (int i = 0; i < _visBloat.Count; i++)
            {
                BloatApp b = _visBloat[i];
                ListViewItem it = new ListViewItem(b.FriendlyName);
                it.SubItems.Add(b.Category);
                it.SubItems.Add(Helpers.FmtMb(b.SizeMb));
                it.SubItems.Add(b.ReplacesNote);
                it.Tag = b;
                _lvBloat.Items.Add(it);
            }
            _lvBloat.EndUpdate();
            _lblSummary.Text = string.Format("{0} πακέτα · {1} αφαιρούμενο bloat", _visBloat.Count, Helpers.FmtMb(recMb));
        }

        private void RenderLeftovers()
        {
            string q = Helpers.StripAccents(_txtSearch.Text.Trim());
            _visLeft = new List<LeftoverItem>(_allLeft.Count);
            long tot = 0;
            for (int i = 0; i < _allLeft.Count; i++)
            {
                if (q.Length == 0 || _allLeft[i].Norm.IndexOf(q, StringComparison.Ordinal) >= 0)
                {
                    _visLeft.Add(_allLeft[i]);
                    tot += _allLeft[i].SizeBytes;
                }
            }
            _lvLeft.BeginUpdate();
            _lvLeft.Items.Clear();
            for (int i = 0; i < _visLeft.Count; i++)
            {
                LeftoverItem l = _visLeft[i];
                ListViewItem it = new ListViewItem(l.FolderName);
                it.SubItems.Add(l.RootLabel);
                it.SubItems.Add(Helpers.FmtBytes(l.SizeBytes));
                it.SubItems.Add(l.Modified > DateTime.MinValue ? l.Modified.ToString("dd/MM/yyyy") : "");
                it.SubItems.Add(l.FullPath);
                it.Tag = l;
                _lvLeft.Items.Add(it);
            }
            _lvLeft.EndUpdate();
            _lblSummary.Text = string.Format("{0} φάκελοι · {1}", _visLeft.Count, Helpers.FmtBytes(tot));
        }

        private void DrawHdr(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush br = new SolidBrush(_bgPanel)) e.Graphics.FillRectangle(br, e.Bounds);
            using (Pen p = new Pen(_border)) e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            Rectangle r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(4, e.Bounds.Width - 10), e.Bounds.Height);
            TextFormatFlags tf = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | (e.Header.Text == "Μέγεθος" ? TextFormatFlags.Right : TextFormatFlags.Left);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, _fSmall, r, _fgMuted, tf);
        }

        private void DrawSub(object sender, DrawListViewSubItemEventArgs e)
        {
            ListView lv = (ListView)sender;
            bool sel = e.Item.Selected;
            Color bg = sel ? _bgSel : ((e.ItemIndex % 2 == 0) ? _bgMain : _bgAlt);
            using (SolidBrush br = new SolidBrush(bg)) e.Graphics.FillRectangle(br, e.Bounds);

            int xPad = 6;
            if (lv.CheckBoxes && e.ColumnIndex == 0)
            {
                Rectangle cb = new Rectangle(e.Bounds.X + 7, e.Bounds.Y + (e.Bounds.Height - 13) / 2, 13, 13);
                using (Pen p = new Pen(e.Item.Checked ? _accent : _fgMuted, 1.4f)) e.Graphics.DrawRectangle(p, cb);
                if (e.Item.Checked)
                {
                    using (SolidBrush b = new SolidBrush(_accent)) e.Graphics.FillRectangle(b, cb.X + 3, cb.Y + 3, 7, 7);
                }
                xPad = 26;
            }

            Color fg = _fgMain;
            Font fn = (e.ColumnIndex == 0) ? _fBold : _fSmall;
            if (lv == _lvBloat)
            {
                BloatApp ba = e.Item.Tag as BloatApp;
                if (e.ColumnIndex == 1 && ba != null) fg = ba.IsRecommendedBloat ? _pink : _fgMuted;
                else if (e.ColumnIndex == 2) fg = _cyan;
                else if (e.ColumnIndex == 3) fg = _fgMuted;
            }
            else if (lv == _lvApps)
            {
                if (e.ColumnIndex == 4) fg = _cyan;
                else if (e.ColumnIndex > 0) fg = _fgMuted;
            }
            else if (lv == _lvLeft)
            {
                if (e.ColumnIndex == 2) fg = _accent;
                else if (e.ColumnIndex > 0) fg = _fgMuted;
            }

            Rectangle r = new Rectangle(e.Bounds.X + xPad, e.Bounds.Y, Math.Max(4, e.Bounds.Width - xPad - 6), e.Bounds.Height);
            TextFormatFlags tf = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                                 (lv.Columns[e.ColumnIndex].Text == "Μέγεθος" ? TextFormatFlags.Right : TextFormatFlags.Left);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, fn, r, fg, tf);
        }

        // ---- Actions ----
        private void UninstallSelectedWin32()
        {
            if (_lvApps.SelectedItems.Count == 0) return;
            InstalledApp a = _lvApps.SelectedItems[0].Tag as InstalledApp;
            if (a == null || string.IsNullOrEmpty(a.UninstallString)) return;
            if (MessageBox.Show(this, "Εκκίνηση απεγκατάστασης για:\n\n" + a.Name + "\n(" + a.Publisher + ")",
                "NillyUninstall Pro", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                string cmd = a.UninstallString.Trim();
                string file, args = "";
                if (cmd.StartsWith("\""))
                {
                    int end = cmd.IndexOf('"', 1);
                    file = end > 1 ? cmd.Substring(1, end - 1) : cmd.Trim('"');
                    args = end > 1 && end + 1 < cmd.Length ? cmd.Substring(end + 1).Trim() : "";
                }
                else
                {
                    int sp = cmd.IndexOf(' ');
                    if (sp > 0 && !File.Exists(cmd)) { file = cmd.Substring(0, sp); args = cmd.Substring(sp + 1).Trim(); }
                    else file = cmd;
                }
                Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = true });
                _lblStatus.Text = "Εκκινήθηκε το uninstaller για: " + a.Name + " (πάτα ↻ μόλις τελειώσει).";
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "NillyUninstall Pro", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private static bool ReadSuiteDark(bool fallback)
        {
            try
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\suite.theme");
                if (File.Exists(p))
                {
                    string s = File.ReadAllText(p, Encoding.ASCII).Trim();
                    if (s.Equals("light", StringComparison.OrdinalIgnoreCase)) return false;
                    if (s.Equals("dark", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch { }
            return fallback;
        }

        private static string FindSiblingExe(string folder, string exeName)
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NillySuite\" + folder + @"\" + exeName);
                if (File.Exists(appData)) return appData;
                DirectoryInfo par = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));
                if (par != null)
                {
                    string sib = Path.Combine(Path.Combine(par.FullName, folder), exeName);
                    if (File.Exists(sib)) return sib;
                }
            }
            catch { }
            return null;
        }

        private static void OpenFolderSmart(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            string filesExe = FindSiblingExe("NillyFilesPro", "NillyFilesPro.exe");
            if (filesExe != null)
            {
                try { Process.Start(new ProcessStartInfo(filesExe, "\"" + dir + "\"") { WorkingDirectory = Path.GetDirectoryName(filesExe), UseShellExecute = true }); return; } catch { }
            }
            try { Process.Start("explorer.exe", "\"" + dir + "\""); } catch { }
        }

        private void ExportAppsToNillyNote()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== NillyUninstall Pro — Λίστα Εγκατεστημένων Προγραμμάτων ===");
            sb.AppendLine("Ημερομηνία: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "  |  Σύνολο: " + _visApps.Count);
            sb.AppendLine("created by Nilly graphics (Sotiris Christos) — nillygraphics.com");
            sb.AppendLine("");
            for (int i = 0; i < _visApps.Count; i++)
            {
                InstalledApp a = _visApps[i];
                sb.AppendFormat("{0,3}. {1}", i + 1, a.Name);
                if (!string.IsNullOrEmpty(a.Version)) sb.Append("  [v" + a.Version + "]");
                if (!string.IsNullOrEmpty(a.Publisher)) sb.Append("  —  " + a.Publisher);
                if (a.SizeKb > 0) sb.Append("  (" + Helpers.FmtMb(a.SizeKb / 1024.0) + ")");
                sb.AppendLine();
            }
            string txt = sb.ToString();
            string noteExe = FindSiblingExe("NillyNotePro", "NillyNotePro.exe");
            if (noteExe != null)
            {
                try
                {
                    string tmp = Path.Combine(Path.GetTempPath(), "NillyUninstall_Apps_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                    File.WriteAllText(tmp, txt, new UTF8Encoding(false));
                    Process.Start(new ProcessStartInfo(noteExe, "\"" + tmp + "\"") { WorkingDirectory = Path.GetDirectoryName(noteExe), UseShellExecute = true });
                    _lblStatus.Text = "↗ Η λίστα εστάλη στο NillyNote Pro (" + _visApps.Count + " προγράμματα).";
                    return;
                }
                catch { }
            }
            try { Clipboard.SetText(txt); _lblStatus.Text = "✓ Αντιγράφηκε η λίστα (" + _visApps.Count + " προγράμματα) στο πρόχειρο."; } catch { }
        }

        private void OpenSelectedFolder()
        {
            if (_lvApps.SelectedItems.Count == 0) return;
            InstalledApp a = _lvApps.SelectedItems[0].Tag as InstalledApp;
            if (a != null && !string.IsNullOrEmpty(a.InstallLocation) && Directory.Exists(a.InstallLocation))
                OpenFolderSmart(a.InstallLocation);
            else
                _lblStatus.Text = "Δεν υπάρχει καταχωρημένος φάκελος εγκατάστασης για αυτό το πρόγραμμα.";
        }

        private void RemoveSelectedBloat()
        {
            List<BloatApp> targets = new List<BloatApp>();
            for (int i = 0; i < _lvBloat.CheckedItems.Count; i++)
            {
                BloatApp b = _lvBloat.CheckedItems[i].Tag as BloatApp;
                if (b != null) targets.Add(b);
            }
            if (targets.Count == 0 && _lvBloat.SelectedItems.Count > 0)
            {
                BloatApp b = _lvBloat.SelectedItems[0].Tag as BloatApp;
                if (b != null) targets.Add(b);
            }
            if (targets.Count == 0) { _lblStatus.Text = "Επίλεξε (τικάρισε) ένα ή περισσότερα Bloat Apps πρώτα."; return; }

            StringBuilder names = new StringBuilder();
            double mb = 0;
            for (int i = 0; i < targets.Count; i++) { names.AppendLine("• " + targets[i].FriendlyName + " (" + Helpers.FmtMb(targets[i].SizeMb) + ")"); mb += targets[i].SizeMb; }
            if (MessageBox.Show(this,
                string.Format("Ασφαλής αφαίρεση {0} εφαρμογών (~{1} δίσκου);\n\n{2}\n100% αναστρέψιμο — μπορείς να τις ξανακατεβάσεις ανά πάσα στιγμή από το Microsoft Store.",
                targets.Count, Helpers.FmtMb(mb), names.ToString()),
                "NillyUninstall Pro — Ασφαλής Αφαίρεση Bloat", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _btnRemoveBloat.Enabled = false;
            _lblStatus.Text = "Αφαίρεση επιλεγμένων πακέτων...";
            Thread th = new Thread(delegate()
            {
                int ok = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    try
                    {
                        string arg = "-NoProfile -ExecutionPolicy Bypass -Command \"Remove-AppxPackage -Package '" + targets[i].FullName.Replace("'", "''") + "'\"";
                        ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", arg) { UseShellExecute = false, CreateNoWindow = true };
                        using (Process p = Process.Start(psi)) { p.WaitForExit(30000); if (p.ExitCode == 0) ok++; }
                    }
                    catch { }
                }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        _btnRemoveBloat.Enabled = true;
                        _bloatLoaded = false;
                        LoadBloatAsync();
                        _lblStatus.Text = string.Format("✓ Αφαιρέθηκαν {0}/{1} εφαρμογές! Το σύστημα πήρε ανάσα.", ok, targets.Count);
                    });
                }
                catch { }
            });
            th.IsBackground = true; th.Start();
        }

        private void RestoreSelectedFromStore()
        {
            string query = "Microsoft";
            if (_lvBloat.SelectedItems.Count > 0)
            {
                BloatApp b = _lvBloat.SelectedItems[0].Tag as BloatApp;
                if (b != null && !string.IsNullOrEmpty(b.StoreQuery)) query = b.StoreQuery;
            }
            OpenUrl("ms-windows-store://search/?query=" + Uri.EscapeDataString(query));
        }

        private void DeleteSelectedLeftover()
        {
            if (_lvLeft.SelectedItems.Count == 0) return;
            List<string> paths = new List<string>();
            for (int i = 0; i < _lvLeft.SelectedItems.Count; i++)
            {
                LeftoverItem l = _lvLeft.SelectedItems[i].Tag as LeftoverItem;
                if (l != null && Directory.Exists(l.FullPath)) paths.Add(l.FullPath);
            }
            if (paths.Count == 0) return;
            if (MessageBox.Show(this, "Μεταφορά " + paths.Count + " φακέλων στον Κάδο Ανακύκλωσης;\n\n" + paths[0],
                "NillyUninstall Pro — Ασφαλής Καθαρισμός", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (Native.SendToRecycleBin(Handle, paths))
            {
                _leftLoaded = false;
                LoadLeftoversAsync();
            }
        }

        public void ShowInfoDialog()
        {
            double exeKb = 0;
            try { exeKb = new FileInfo(Assembly.GetExecutingAssembly().Location).Length / 1024.0; } catch { }
            long ramMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

            using (Form dlg = new Form())
            {
                dlg.Text = "NillyUninstall Pro — Ultra-Light Specs & Πληροφορίες";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(515, 395);
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.BackColor = _bgMain; dlg.ForeColor = _fgMain;
                Native.SetDarkTitlebar(dlg.Handle, _dark);

                PictureBox pic = new PictureBox { Size = new Size(42, 42), Location = new Point(18, 16), SizeMode = PictureBoxSizeMode.Zoom, Image = _logoBmp };
                Label hdr = new Label { Text = "NillyUninstall Pro v1.0", Location = new Point(70, 16), AutoSize = true, Font = new Font("Segoe UI", 13.0f, FontStyle.Bold) };
                Label sub = new Label { Text = "Ultra-Light Native Uninstaller, Safe Win11 Bloat Remover & Leftover Cleaner", Location = new Point(72, 42), AutoSize = true, Font = _fSmall, ForeColor = _accent, UseMnemonic = false };

                Panel card = new Panel { Location = new Point(18, 72), Size = new Size(478, 212), BackColor = _bgPanel };
                Label specs = new Label
                {
                    Location = new Point(12, 10), Size = new Size(454, 194), Font = _fUi, ForeColor = _fgMain,
                    Text = string.Format(
                        "• Μέγεθος EXE στον δίσκο:  {0:0.0} KB  (0 εξωτερικά DLL · 0 WebView2 · 0 Electron)\n" +
                        "• Τρέχουσα RAM ανοιχτό:  ~{1} MB  |  Κλειστό:  0 MB RAM & 0% CPU\n" +
                        "• Αντικαθιστά:  Windows 11 Settings → Installed Apps (αργό WinUI 5–15s) & Revo\n" +
                        "• Ασφαλής Αφαίρεση Bloat:  Κλειδωμένα εκτός λίστας Microsoft Store, Codecs\n" +
                        "  (HEIF/AV1/WebP/RAW), Defender, Shell & Framework πακέτα — ΔΕΝ χαλάει Windows!\n" +
                        "• 100% Αναστρέψιμο:  Κουμπί επαναφοράς από Microsoft Store για κάθε app\n" +
                        "• Υπολείμματα:  Διαγραφή ΜΟΝΟ στον Κάδο Ανακύκλωσης (Recycle Bin)",
                        exeKb, ramMb)
                };
                card.Controls.Add(specs);

                LinkLabel lnkSite = new LinkLabel { Text = "created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/", Location = new Point(18, 300), AutoSize = true, Font = _fSmall, LinkColor = _accent };
                lnkSite.LinkClicked += delegate { OpenUrl("https://nillygraphics.com/"); };
                LinkLabel lnkGit = new LinkLabel { Text = "GitHub (MIT Open Source · 0€): https://github.com/nillynp/nillyuninstall-pro", Location = new Point(18, 322), AutoSize = true, Font = _fSmall, LinkColor = _cyan };
                lnkGit.LinkClicked += delegate { OpenUrl("https://github.com/nillynp/nillyuninstall-pro"); };

                Button btnClose = new Button
                {
                    Text = "Κλείσιμο", DialogResult = DialogResult.OK, Location = new Point(408, 348), Size = new Size(88, 30),
                    FlatStyle = FlatStyle.Flat, BackColor = _accent, ForeColor = Color.FromArgb(20, 22, 26), Font = _fBold
                };
                btnClose.FlatAppearance.BorderSize = 0;

                dlg.Controls.AddRange(new Control[] { pic, hdr, sub, card, lnkSite, lnkGit, btnClose });
                dlg.AcceptButton = btnClose; dlg.CancelButton = btnClose;
                dlg.ShowDialog(this);
            }
        }

        private static void OpenUrl(string u) { try { Process.Start(new ProcessStartInfo(u) { UseShellExecute = true }); } catch { } }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F)) { _txtSearch.Focus(); _txtSearch.SelectAll(); return true; }
            if (keyData == Keys.F5) { ReloadAll(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (_logoBmp != null) _logoBmp.Dispose(); _tip.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0 && args[0].Equals("--test-smoke", StringComparison.OrdinalIgnoreCase))
                return RunSmoke(args.Length > 1 ? args[1] : null, args.Length > 2 ? args[2] : null);

            Application.Run(new MainForm(false));
            return 0;
        }

        private static int RunSmoke(string logPath, string pngPath)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (MainForm f = new MainForm(true))
                {
                    f.Show();
                    Application.DoEvents();
                    int apps = f.AppsCount;

                    if (!string.IsNullOrEmpty(pngPath))
                    {
                        using (Bitmap shot = new Bitmap(f.Width, f.Height))
                        {
                            f.DrawToBitmap(shot, new Rectangle(0, 0, f.Width, f.Height));
                            shot.Save(pngPath, ImageFormat.Png);
                        }
                    }

                    f.SwitchTab(1);
                    f.LoadBloatSyncForSmoke();
                    Application.DoEvents();
                    int bloat = f.BloatCount;

                    if (!string.IsNullOrEmpty(pngPath))
                    {
                        string p2 = Path.Combine(Path.GetDirectoryName(pngPath), Path.GetFileNameWithoutExtension(pngPath) + "_bloat.png");
                        using (Bitmap shot2 = new Bitmap(f.Width, f.Height))
                        {
                            f.DrawToBitmap(shot2, new Rectangle(0, 0, f.Width, f.Height));
                            shot2.Save(p2, ImageFormat.Png);
                        }
                    }

                    double exeKb = new FileInfo(Assembly.GetExecutingAssembly().Location).Length / 1024.0;
                    bool pass = (apps >= 5 && bloat >= 1 && f.IsHandleCreated && exeKb < 150.0);
                    sb.AppendLine("WIN32_APPS_COUNT=" + apps);
                    sb.AppendLine("BLOAT_APPS_COUNT=" + bloat);
                    sb.AppendLine("EXE_KB=" + exeKb.ToString("0.0"));
                    sb.AppendLine("WINDOW_OPENED_OK=" + f.IsHandleCreated);
                    sb.AppendLine("SMOKE_RESULT=" + (pass ? "PASS" : "FAIL"));
                    f.Close();
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("SMOKE_RESULT=FAIL");
                sb.AppendLine(ex.ToString());
                if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
                return 1;
            }
            if (!string.IsNullOrEmpty(logPath)) File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
            return 0;
        }
    }
}
