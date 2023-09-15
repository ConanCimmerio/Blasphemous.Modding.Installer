﻿using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BlasModInstaller
{
    public partial class UIHandler : Form
    {
        public static string DownloadsPath => Environment.CurrentDirectory + "\\downloads\\";

        public int MainSectionWidth => mainSection.Width;

        public UIHandler()
        {
            Directory.CreateDirectory(DownloadsPath);
            InitializeComponent();
        }

        private void OnFormOpen(object sender, EventArgs e)
        {
            Text = "Blasphemous Mod Installer v" + Core.CurrentInstallerVersion.ToString(3);
            Core.SettingsHandler.LoadWindowSettings();

            OpenSection(Core.SettingsHandler.Config.LastSection);
        }

        private void OnFormClose(object sender, FormClosingEventArgs e)
        {
            Core.SettingsHandler.SaveConfigSettings();
            Core.SettingsHandler.SaveWindowSettings();
        }

        public static bool PromptQuestion(string title, string question)
        {
            return MessageBox.Show(question, title, MessageBoxButtons.OKCancel) == DialogResult.OK;
        }

        // Update installer

        public void UpdatePanelSetVisible(bool visible) => warningSectionOuter.Visible = visible;

        // Debug status

        public void DebugLogSetVisible(bool visible) => debugLog.Visible = visible;

        // Validation screen

        private void ClickLocationButton(object sender, EventArgs e)
        {
            if (blasLocDialog.ShowDialog() == DialogResult.OK)
            {
                Core.SettingsHandler.Config.Blas1RootFolder = Path.GetDirectoryName(blasLocDialog.FileName);
                OpenSection(Core.SettingsHandler.Config.LastSection);
            }
        }

        private async void ClickToolsButton(object sender, EventArgs e)
        {
            await Core.CurrentPage.InstallTools();
            OpenSection(Core.SettingsHandler.Config.LastSection);
        }

        // ...

        public Panel GetUIElementByType(SectionType type)
        {
            switch (type)
            {
                case SectionType.Blas1Mods: return blas1modSection;
                case SectionType.Blas1Skins: return blas1skinSection;
                case SectionType.Blas2Mods: return blas2modSection;
                default: return null;
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (var page in Core.AllPages)
                {
                    page.UIHolder.AdjustPageWidth();
                }
            }
            catch (NullReferenceException)
            {
                Log("Pages not initialized yet, skipping resizing!");
            }
        }

        public void RemoveButtonFocus(object sender, EventArgs e)
        {
            titleLabel.Focus();
        }

        public void Log(string message)
        {
            if (Core.SettingsHandler.Config.DebugMode)
                debugLog.Text += message + "\r\n";
        }

        private void ShowSideButtonBorder(object sender, EventArgs e)
        {
            (sender as Button).FlatAppearance.BorderColor = Color.White;
        }

        private void HideSideButtonBorder(object sender, EventArgs e)
        {
            (sender as Button).FlatAppearance.BorderColor = Color.FromArgb(30, 30, 30);
            RemoveButtonFocus(null, null);
        }

        private void SetSortByBox(SortType sort)
        {
            sortByName.Checked = sort == SortType.Name;
            sortByAuthor.Checked = sort == SortType.Author;
            sortByInitialRelease.Checked = sort == SortType.InitialRelease;
            sortByLatestRelease.Checked = sort == SortType.LatestRelease;
        }

        private void OpenSection(SectionType section)
        {
            Core.SettingsHandler.Config.LastSection = section;
            var currentPage = Core.CurrentPage;

            // Update background and info
            titleLabel.Text = currentPage.Title;
            titleSectionInner.BackgroundImage = currentPage.Image;

            // Validate the status of mods
            bool folderValid = currentPage.Validator.IsRootFolderValid;
            bool toolsInstalled = currentPage.Validator.AreModdingToolsInstalled;
            bool validated = folderValid && toolsInstalled;
            Log("Modding status validation: " + validated);

            if (validated)
            {
                SetSortByBox(currentPage.CurrentSortType);
                currentPage.Loader.LoadAllData();
                validationSection.Visible = false;
            }
            else
            {
                validationSection.Visible = true;
                locationBtn.Enabled = !folderValid;
                locationBtn.Text = "Locate " + currentPage.Validator.ExeName;
                toolsBtn.Enabled = folderValid && !toolsInstalled;
            }

            // Show the correct page element
            currentPage.UIHolder.SectionPanel.Visible = validated;
            foreach (var page in Core.AllPages)
                if (page != currentPage)
                    page.UIHolder.SectionPanel.Visible = false;                

            // Only show side buttons under certain conditions
            divider1.Visible = validated;
            divider2.Visible = validated;
            sortSection.Visible = validated;

            installBtn.Visible = validated && currentPage.Grouper.CanInstall;
            uninstallBtn.Visible = validated && currentPage.Grouper.CanInstall;
            enableBtn.Visible = validated && currentPage.Grouper.CanEnable;
            disableBtn.Visible = validated && currentPage.Grouper.CanEnable;

            sortByName.Visible = validated && currentPage.Grouper.CanSortByCreation;
            sortByAuthor.Visible = validated && currentPage.Grouper.CanSortByCreation;
            sortByInitialRelease.Visible = currentPage.Grouper.CanSortByDate;
            sortByLatestRelease.Visible = currentPage.Grouper.CanSortByDate;
        }

        #region Side section top

        private void ClickedBlas1Mods(object sender, EventArgs e) => OpenSection(SectionType.Blas1Mods);

        private void ClickedBlas1Skins(object sender, EventArgs e) => OpenSection(SectionType.Blas1Skins);

        private void ClickedBlas2Mods(object sender, EventArgs e) => OpenSection(SectionType.Blas2Mods);

        private void ClickedSettings(object sender, EventArgs e) { }

        #endregion Side section top

        #region Side section middle

        private void ClickedSortByName(object sender, EventArgs e)
        {
            Core.CurrentPage.CurrentSortType = SortType.Name;
            Core.CurrentPage.Sorter.Sort();
        }

        private void ClickedSortByAuthor(object sender, EventArgs e)
        {
            Core.CurrentPage.CurrentSortType = SortType.Author;
            Core.CurrentPage.Sorter.Sort();
        }

        private void ClickedSortByInitialRelease(object sender, EventArgs e)
        {
            Core.CurrentPage.CurrentSortType = SortType.InitialRelease;
            Core.CurrentPage.Sorter.Sort();
        }

        private void ClickedSortByLatestRelease(object sender, EventArgs e)
        {
            Core.CurrentPage.CurrentSortType = SortType.LatestRelease;
            Core.CurrentPage.Sorter.Sort();
        }

        #endregion Side section middle

        #region Side section bottom

        private void ClickedInstallAll(object sender, EventArgs e)
        {
            Core.CurrentPage.Grouper.InstallAll();
        }

        private void ClickedUninstallAll(object sender, EventArgs e)
        {
            Core.CurrentPage.Grouper.UninstallAll();
        }

        private void ClickedEnableAll(object sender, EventArgs e)
        {
            Core.CurrentPage.Grouper.EnableAll();
        }

        private void ClickedDisableAll(object sender, EventArgs e)
        {
            Core.CurrentPage.Grouper.DisableAll();
        }

        private void ClickInstallerUpdateLink(object sender, LinkLabelLinkClickedEventArgs e) => Core.GithubHandler.OpenInstallerLink();

        #endregion Side section bottom
    }
}
