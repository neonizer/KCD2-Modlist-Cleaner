using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaveCleanerTray
{
    static class Program
    {
        private static NotifyIcon trayIcon;
        private static FileSystemWatcher watcher;
        private static string basePath;
        private static bool paused = false;
        private static string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SaveCleanerTray.log");

        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Saved Games", "kingdomcome2", "saves");

                Log("Application started.");

                trayIcon = new NotifyIcon
                {
                    Icon = new Icon(new MemoryStream(ModlistCleanerTray.Properties.Resources.save_icon)),
                    Text = "Save Cleaner",
                    Visible = true
                };

                var contextMenu = new ContextMenuStrip();

                var pauseItem = new ToolStripMenuItem("Pause Auto-Clean")
                {
                    Checked = paused,
                    CheckOnClick = true
                };
                pauseItem.CheckedChanged += (s, e) => paused = pauseItem.Checked;

                var cleanAllItem = new ToolStripMenuItem("Clean All Saves Now");
                cleanAllItem.Click += (s, e) => CleanAllExisting();

                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (s, e) => Application.Exit();

                contextMenu.Items.Add(pauseItem);
                contextMenu.Items.Add(cleanAllItem);
                contextMenu.Items.Add(exitItem);
                trayIcon.ContextMenuStrip = contextMenu;

                StartWatching();
                Application.ApplicationExit += (s, e) => trayIcon.Dispose();
                Application.Run();
            }
            catch (Exception ex)
            {
                Log($"Critical error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show("A critical error occurred. Please check the log file.", "Save Cleaner Tray Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void StartWatching()
        {
            try
            {
                watcher = new FileSystemWatcher(basePath, "*.whs")
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };
                watcher.Created += OnFileCreated;
                Log("FileSystemWatcher started.");
            }
            catch (Exception ex)
            {
                Log($"Failed to start FileSystemWatcher: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (paused) return;
            Task.Run(() =>
            {
                try
                {
                    if (!WaitForFile(e.FullPath, TimeSpan.FromSeconds(5))) return;
                    ProcessFile(e.FullPath);
                    Log($"File processed: {e.FullPath}");
                }
                catch (Exception ex)
                {
                    Log($"Error processing file '{e.FullPath}': {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        private static bool WaitForFile(string path, TimeSpan timeout)
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start) < timeout)
            {
                try { using var s = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); return true; }
                catch (IOException) { Task.Delay(200).Wait(); }
            }
            return false;
        }

        private static void CleanAllExisting()
        {
            try
            {
                if (!Directory.Exists(basePath)) return;
                foreach (var playlineDir in Directory.GetDirectories(basePath, "playline*"))
                {
                    foreach (var file in Directory.GetFiles(playlineDir, "*.whs"))
                    {
                        ProcessFile(file);
                        Log($"File cleaned during Clean All: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error during Clean All: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void ProcessFile(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                uint signature = BitConverter.ToUInt32(bytes, 0);
                int oldDescLen = BitConverter.ToInt32(bytes, 4);
                string description = Encoding.UTF8.GetString(bytes, 8, oldDescLen);
                string cleanedDescription = Regex.Replace(
                    description,
                    @"<UsedMods>.*?</UsedMods>",
                    "<UsedMods></UsedMods>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                byte[] newDescBytes = Encoding.UTF8.GetBytes(cleanedDescription);
                using var fs = File.Open(path, FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(fs, Encoding.UTF8);
                writer.Write(signature);
                writer.Write(newDescBytes.Length);
                writer.Write(newDescBytes);
                int remainingStart = 8 + oldDescLen;
                writer.Write(bytes, remainingStart, bytes.Length - remainingStart);
            }
            catch (Exception ex)
            {
                Log($"Error processing file '{path}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void Log(string message)
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] {message}\n");
        }
    }
}
