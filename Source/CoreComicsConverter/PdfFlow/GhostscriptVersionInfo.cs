﻿//
// GhostscriptVersionInfo.cs
// This file is part of Ghostscript.NET library
//
// Author: Josip Habjan (habjan@gmail.com, http://www.linkedin.com/in/habjan) 
// Copyright (c) 2013-2016 by Josip Habjan. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using Microsoft.Win32;

namespace CoreComicsConverter.PdfFlow
{
    public class GhostscriptVersionInfo
    {
        private static readonly Version MinVersion = new Version(9, 50);

        private static readonly Version MaxVersion = new Version(9, 52);

        private static readonly string[] hklmSubKeyNames = new[]
        {
            "SOFTWARE\\GPL Ghostscript\\",
            "SOFTWARE\\Artifex Ghostscript\\"
        };

        public GhostscriptVersionInfo(Version version, string gsExe)
        {
            Version = version;

            Exe = gsExe;
        }

        /// <summary>
        /// Gets Ghostscript version.
        /// </summary>
        public Version Version { get; }

        public string Exe { get; }

        /// <summary>
        /// Returns GhostscriptVersionInfo string.
        /// </summary>
        public override string ToString()
        {
            return $"Version: {Version}, Exe: {Exe}";
        }

        /// <summary>
        /// Gets installed Ghostscript versions list.
        /// </summary>
        /// <returns>A GhostscriptVersionInfo list of the Ghostscript installations found on the local system.</returns>
        public static List<GhostscriptVersionInfo> GetInstalledVersions()
        {
            var versionsMap = new Dictionary<Version, GhostscriptVersionInfo>();

            using var hklm32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            var x64 = Environment.Is64BitProcess;

            // 64 bit exe requires 64 bit process. 32 bit exe can run in both 32 bit and 64 bit process
            if (x64)
            {
                AddGhostscriptVersions(hklm32, versionsMap, x64);

                using var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                // 64 bit exe will overrule 32 bit exe with the same version.
                AddGhostscriptVersions(hklm64, versionsMap, x64);
            }
            else
            {
                AddGhostscriptVersions(hklm32, versionsMap, x64);
            }

            return versionsMap.Values.AsList();
        }

        private static void AddGhostscriptVersions(RegistryKey hklm, Dictionary<Version, GhostscriptVersionInfo> versionsMap, bool x64)
        {
            foreach (var subKeyName in hklmSubKeyNames)
            {
                using var rkGs = hklm.OpenSubKey(subKeyName);
                if (rkGs == null)
                {
                    continue;
                }

                // Each sub-key represents a version of the installed Ghostscript library
                foreach (var versionKey in rkGs.GetSubKeyNames())
                {
                    try
                    {
                        using var rkVer = rkGs.OpenSubKey(versionKey);

                        // get the Ghostscript native library path
                        var gsDll = rkVer.GetValue("GS_DLL", string.Empty) as string;

                        if (!string.IsNullOrEmpty(gsDll) && File.Exists(gsDll))
                        {
                            string exe = null;

                            // 64 bit exe requires 64 bit process. 
                            if (x64 && gsDll.EndsWith("gsdll64.dll"))
                            {
                                exe = "gswin64c.exe";
                            }

                            // 32 bit exe can run in both 32 bit and 64 bit process
                            if (exe == null && gsDll.EndsWith("gsdll32.dll"))
                            {
                                exe = "gswin32c.exe";
                            }

                            if (!string.IsNullOrEmpty(exe))
                            {
                                var bin = Path.GetDirectoryName(gsDll);
                                exe = Path.Combine(bin, exe);

                                if (File.Exists(exe))
                                {
                                    var fileVersion = FileVersionInfo.GetVersionInfo(exe);

                                    var version = new Version(fileVersion.FileVersion);

                                    versionsMap[version] = new GhostscriptVersionInfo(version, exe);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ProgressReporter.Warning(ex.TypeAndMessage());
                    }
                }
            }
        }

        public static GhostscriptVersionInfo GetInstalledVersion()
        {
            var gsVerList = GetInstalledVersions();

            if (gsVerList.Count == 0)
            {
                return null;
            }

            if (gsVerList.Count > 1)
            {
                gsVerList = gsVerList.OrderByDescending(g => g.Version).AsList();
            }

            foreach (var gsVer in gsVerList)
            {
                if ((MinVersion == null || gsVer.Version >= MinVersion) && (MaxVersion == null || gsVer.Version <= MaxVersion))
                {
                    return gsVer;
                }
            }

            ProgressReporter.Warning($"No Ghostscript version >= {MinVersion} and =< {MaxVersion}");

            gsVerList.ForEach(g => ProgressReporter.Warning($" Found {g.Version} -> {g.Exe}"));

            return null;
        }
    }
}
