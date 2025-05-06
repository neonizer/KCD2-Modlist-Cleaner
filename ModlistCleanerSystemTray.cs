using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static NotifyIcon trayIcon;
    static FileSystemWatcher watcher;
    static bool cleanOnlyNew = true;
    static string savesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KingdomComeDeliverance", "user", "savegames");

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        trayIcon = new NotifyIcon()
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Modlist Cleaner"
        };

        var contextMenu = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("Only Clean New Saves") { Checked = true };
        var exitItem = new ToolStripMenuItem("Exit");

        toggleItem.Click += (s, e) => toggleItem.Checked = cleanOnlyNew = !toggleItem.Checked;
        exitItem.Click += (s, e) => { trayIcon.Visible = false; Application.Exit(); };

        contextMenu.Items.Add(toggleItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        trayIcon.ContextMenuStrip = contextMenu;

        StartWatching();

        Application.Run();
    }

    static void StartWatching()
    {
        watcher = new FileSystemWatcher(savesRoot, "*.whs")
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        watcher.Created += OnNewSave;
        watcher.Changed += OnNewSave;
    }

    static void OnNewSave(object sender, FileSystemEventArgs e)
    {
        Task.Delay(1000).ContinueWith(_ =>
        {
            try
            {
                if (cleanOnlyNew)
                {
                    CleanModlist(e.FullPath);
                }
                else
                {
                    var allSaves = Directory.GetFiles(savesRoot, "*.whs", SearchOption.AllDirectories);
                    foreach (var save in allSaves)
                        CleanModlist(save);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cleaning save: " + ex.Message);
            }
        });
    }

    static void CleanModlist(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        string content = Encoding.ASCII.GetString(data);

        int start = content.IndexOf("<UsedMods>");
        int end = content.IndexOf("</UsedMods>", start);

        if (start >= 0 && end > start)
        {
            int length = (end + 11) - start;
            string empty = "<UsedMods></UsedMods>";
            string padded = empty + new string(' ', length - empty.Length);
            byte[] newBytes = Encoding.ASCII.GetBytes(padded);

            Array.Copy(newBytes, 0, data, start, newBytes.Length);
            File.WriteAllBytes(path, data);
        }
    }
}
