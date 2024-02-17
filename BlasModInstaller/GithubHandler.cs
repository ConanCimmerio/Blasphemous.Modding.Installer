﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlasModInstaller
{
    internal class GithubHandler
    {
        private readonly GitHubClient _client;
        private string _installerLatestReleaseLink;

        /// <summary>
        /// Initializes a new github client and checks for a newer version of the installer
        /// </summary>
        public GithubHandler(string githubToken)
        {
            _client = CreateGithubClient(githubToken);
            CheckForNewerInstallerRelease();
        }

        /// <summary>
        /// Returns the lastest release version of the specified repository
        /// </summary>
        public async Task<Release> GetLatestReleaseAsync(string owner, string repo)
        {
            try
            {
                return await _client.Repository.Release.GetLatest(owner, repo);
            }
            catch (Exception e)
            {
                Core.UIHandler.Log($"Github failure: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns the entire contents of the specified repository
        /// </summary>
        public async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsAsync(string owner, string repo)
        {
            try
            {
                return await _client.Repository.Content.GetAllContents(owner, repo);
            }
            catch
            {
                Core.UIHandler.Log("API limit reached!");
                return null;
            }
        }

        /// <summary>
        /// Returns the contents of a directory in a repository
        /// </summary>
        public async Task<IReadOnlyList<RepositoryContent>> GetRepositoryDirectoryAsync(string owner, string repo, string path)
        {
            try
            {
                return await _client.Repository.Content.GetAllContents(owner, repo, path);
            }
            catch
            {
                Core.UIHandler.Log("API limit reached!");
                return null;
            }
        }

        /// <summary>
        /// Creates a new github client with the token
        /// </summary>
        private GitHubClient CreateGithubClient(string githubToken)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("BlasModInstaller"));

            if (!string.IsNullOrEmpty(githubToken))
            {
                client.Credentials = new Credentials(githubToken);
            }

            return client;
        }

        /// <summary>
        /// When starting the installer, check if there is a more recent version, and if so display the update panel
        /// </summary>
        /// <returns></returns>
        private async void CheckForNewerInstallerRelease()
        {
            Release latestRelease = await GetLatestReleaseAsync("BrandenEK", "Blasphemous-Mod-Installer");
            if (latestRelease is null)
                return;

            Version newestVersion = CleanSemanticVersion(latestRelease.TagName);

            if (newestVersion.CompareTo(Core.CurrentInstallerVersion) > 0)
            {
                _installerLatestReleaseLink = latestRelease.HtmlUrl;
                Core.UIHandler.UpdatePanelSetVisible(true);
            }

            Core.UIHandler.Log("Remaining api calls: " + (_client.GetLastApiInfo()?.RateLimit.Remaining + 1));
        }

        /// <summary>
        /// When clicking the link in the update panel, open the installer's latest release page
        /// </summary>
        public void OpenInstallerLink()
        {
            try
            {
                Process.Start(_installerLatestReleaseLink);
            }
            catch
            {
                MessageBox.Show("Link does not exist!", "Invalid Link");
            }
        }

        /// <summary>
        /// Strips out the 'v' and ensures it is a valid version
        /// </summary>
        public static Version CleanSemanticVersion(string version)
        {
            try
            {
                return new Version(version.ToLower().Replace("v", ""));
            }
            catch
            {
                return new Version(0, 1, 0);
            }
        }
    }
}
