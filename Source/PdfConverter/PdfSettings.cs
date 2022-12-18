﻿using CbzMage.Shared.Helpers;
using PdfConverter.AppVersions;
using System.Diagnostics;

namespace PdfConverter
{
    public class PdfSettings
    {
        public static Settings Settings => new();

        private readonly SettingsHelper _settingsHelper = new();

        public void CreateSettings()
        {
            _settingsHelper.CreateSettings(nameof(PdfSettings), Settings);

            ConfigureSettings();
        }

        private void ConfigureSettings()
        {
            if (!string.IsNullOrEmpty(Settings.GhostscriptPath))
            {
                if (!File.Exists(Settings.GhostscriptPath))
                {
                    throw new Exception($"{nameof(Settings.GhostscriptPath)} [{Settings.GhostscriptPath}] does not exist");
                }

                var version = FileVersionInfo.GetVersionInfo(Settings.GhostscriptPath).FileVersion;
                if (version == null)
                {
                    ProgressReporter.Warning($"{Settings.GhostscriptPath} does not contain any version information.");
                }
                else
                {
                    var appVersion = new AppVersion(Settings.GhostscriptPath, new Version(version));

                    // Throws if version is invalid
                    appVersion = GetValidGhostscriptVersion(new List<AppVersion> { appVersion });

                    Settings.SetGhostscriptVersion(appVersion.Version);
                }
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var versionList = AppVersionManager.GetInstalledVersionsOf(App.Ghostscript);
                var appVersion = GetValidGhostscriptVersion(versionList);

                Settings.GhostscriptPath = appVersion.Exe;
                Settings.SetGhostscriptVersion(appVersion.Version);
            }
            else
            {
                throw new Exception($"Ghostscript installation not found ({nameof(Settings.GhostscriptPath)} is \"{Settings.GhostscriptPath}\")");
            }

            //MinimumDpi
            if (Settings.MinimumDpi <= 0)
            {
                Settings.MinimumDpi = 300;
            }

            //MinimumHeight
            if (Settings.MinimumHeight <= 0)
            {
                Settings.MinimumHeight = 1920;
            }

            //MaximumHeight
            if (Settings.MaximumHeight <= 0)
            {
                Settings.MaximumHeight = 3840;
            }

            //JpgQuality
            if (Settings.JpgQuality <= 0)
            {
                Settings.JpgQuality = 95;
            }

            //NumberOfThreads
            Settings.GhostscriptReaderThreads =
                _settingsHelper.GetThreadCount(Settings.GhostscriptReaderThreads);
        }

        public static AppVersion GetValidGhostscriptVersion(List<AppVersion> gsVersions)
        {
            var gsVersion = gsVersions.OrderByDescending(gs => gs.Version).FirstOrDefault();

            if (gsVersion == null || gsVersion.Version < Settings.GhostscriptMinVersion)
            {
                var foundVersion = gsVersion != null
                    ? $". (found version {gsVersion.Version})"
                    : string.Empty;

                throw new Exception($"CbzMage requires Ghostscript version {Settings.GhostscriptMinVersion}+ is installed{foundVersion}");

            }
            return gsVersion!;
        }
    }
}
