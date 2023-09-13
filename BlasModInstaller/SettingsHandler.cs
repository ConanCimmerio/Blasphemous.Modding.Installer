﻿using Newtonsoft.Json;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BlasModInstaller
{
    internal class SettingsHandler
    {
        private readonly string _configPath;

        public Config Config { get; private set; }

        public SettingsHandler(string configPath)
        {
            _configPath = configPath;

            LoadConfigSettings();
            Core.UIHandler.DebugLogSetVisible(Config.DebugMode);
        }

        public void LoadConfigSettings()
        {
            if (File.Exists(_configPath))
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configPath));
            }
            else
            {
                Config = new Config();
                SaveConfigSettings();
            }
        }

        public void SaveConfigSettings()
        {
            File.WriteAllText(_configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
        }

        public void LoadWindowSettings()
        {
            Core.UIHandler.WindowState = Properties.Settings.Default.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
            Core.UIHandler.Location = Properties.Settings.Default.Location;
            Core.UIHandler.Size = Properties.Settings.Default.Size;
        }

        public void SaveWindowSettings()
        {
            FormWindowState windowState = Core.UIHandler.WindowState;
            Rectangle windowBounds = Core.UIHandler.RestoreBounds;

            if (windowState == FormWindowState.Maximized)
            {
                Properties.Settings.Default.Location = windowBounds.Location;
                Properties.Settings.Default.Size = windowBounds.Size;
                Properties.Settings.Default.Maximized = true;
            }
            else if (windowState == FormWindowState.Minimized)
            {
                Properties.Settings.Default.Location = windowBounds.Location;
                Properties.Settings.Default.Size = windowBounds.Size;
                Properties.Settings.Default.Maximized = false;
            }
            else
            {
                Properties.Settings.Default.Location = Core.UIHandler.Location;
                Properties.Settings.Default.Size = Core.UIHandler.Size;
                Properties.Settings.Default.Maximized = false;
            }

            Properties.Settings.Default.Save();
        }
    }
}
