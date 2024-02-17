﻿using BlasModInstaller.Mods;
using BlasModInstaller.Sorting;
using BlasModInstaller.UIHolding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace BlasModInstaller.Loading
{
    internal class ModLoader : ILoader
    {
        private readonly string _localDataPath;
        private readonly string _remoteDataPath;
        private readonly IUIHolder _uiHolder;
        private readonly ISorter _sorter;
        private readonly List<Mod> _mods;
        private readonly SectionType _modType;

        private bool _loadedData;

        public ModLoader(string localDataPath, string remoteDataPath, IUIHolder uiHolder, ISorter sorter, List<Mod> mods, SectionType modType)
        {
            _localDataPath = localDataPath;
            _remoteDataPath = remoteDataPath;
            _uiHolder = uiHolder;
            _sorter = sorter;
            _mods = mods;
            _modType = modType;
        }

        public void LoadAllData()
        {
            _uiHolder.AdjustPageWidth();
            if (_loadedData)
                return;

            LoadLocalMods();
            LoadRemoteMods();
            _loadedData = true;
        }

        private void LoadLocalMods()
        {
            if (File.Exists(_localDataPath))
            {
                string json = File.ReadAllText(_localDataPath);
                ModData[] localData = JsonConvert.DeserializeObject<ModData[]>(json);

                for (int i = 0; i < localData.Length; i++)
                {
                    _mods.Add(new Mod(localData[i], _uiHolder.SectionPanel, _modType));
                }
            }

            Core.UIHandler.Log($"Loaded {_mods.Count} local mods");
            _uiHolder.SetBackgroundColor();
            _sorter.Sort();
        }

        private async void LoadRemoteMods()
        {
            var newMods = new List<Mod>();

            using (HttpClient client = new HttpClient())
            {
                string json = await client.GetStringAsync(_remoteDataPath);
                ModData[] remoteData = JsonConvert.DeserializeObject<ModData[]>(json);

                foreach (var data in remoteData)
                {
                    Core.UIHandler.Log($"Getting latest release for {data.name}");
                    Octokit.Release latestRelease = await Core.GithubHandler.GetLatestReleaseAsync(data.githubAuthor, data.githubRepo);
                    if (latestRelease is null)
                        return;

                    Version latestVersion = GithubHandler.CleanSemanticVersion(latestRelease.TagName);
                    string latestDownloadURL = latestRelease.Assets[0].BrowserDownloadUrl;
                    DateTimeOffset latestReleaseDate = latestRelease.CreatedAt;

                    Mod localMod = FindMod(data.name);
                    ModData fullData = new ModData(data, latestVersion.ToString(), latestDownloadURL, latestReleaseDate);

                    if (localMod != null)
                    {
                        localMod.Data = fullData;
                        localMod.UpdateUI();
                        newMods.Add(localMod);
                    }
                    else
                    {
                        newMods.Add(new Mod(fullData, _uiHolder.SectionPanel, _modType));
                    }
                }

                Core.UIHandler.Log($"Loaded {remoteData.Length} global mods");
                _mods.Clear();
                _mods.AddRange(newMods);
            }

            SaveLocalData();
            _uiHolder.SetBackgroundColor();
            _sorter.Sort();
        }

        private void SaveLocalData()
        {
            File.WriteAllText(_localDataPath, JsonConvert.SerializeObject(_mods.Select(x => x.Data)));
        }

        private Mod FindMod(string name)
        {
            return _mods.Find(x => x.Data.name == name);
        }

        public IEnumerable<string> GetUnusedDlls(Mod mod)
        {
            if (mod.Data.requiredDlls == null || mod.Data.requiredDlls.Length == 0)
                return Enumerable.Empty<string>();

            return mod.Data.requiredDlls
                .Where(d => !_mods.Where(m => m.RequiresDll(d) && m.Installed).Any());
        }

        // Get list of mods that this mod requires that need to be enabled
        public IEnumerable<Mod> GetModDependencies(Mod mod)
        {
            return _mods.Where(x => mod.HasDependency(x.Data.name) && !x.Enabled);
        }

        // Get list of mods that require this mod that need to be disabled
        public IEnumerable<Mod> GetModDependents(Mod mod)
        {
            return _mods.Where(x => x.HasDependency(mod.Data.name) && x.Enabled);
        }
    }
}
