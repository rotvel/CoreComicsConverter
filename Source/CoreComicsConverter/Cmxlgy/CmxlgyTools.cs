﻿using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CoreComicsConverter.Cmxlgy
{
    public class CmxlgyTools
    {
        public static int VerifyDownloadFiles(string[] files, out int pageCount)
        {
            var expected = 0;

            pageCount = GetDownloadPageCount(files);
            if (files.Length == pageCount + 1)
            {
                return -1;
            }

            foreach (var file in files)
            {
                var actual = GetDownloadPageNumber(file);
                if (actual != expected)
                {
                    //Did we skip ony page, like in most manga downloads
                    expected++;
                    if (actual != expected)
                    {
                        return expected;
                    }
                }
                expected++;
            }
            return -1;
        }

        //Terminology: Download is a comic downloaded using browser extension
        //Backup is a cbz archive downloaded from the backups page

        private static readonly Regex downloadMatcher = new Regex(@".*page\d{1,3}\.(jpe?g|png)$");

        public static bool IsDownload(string[] paths)
        {
            int padLen = paths.Length.ToString().Length;
            var zeros = "0".PadLeft(padLen, '0');
            var pageZero = $"page{zeros}";

            var firstFile = Path.GetFileNameWithoutExtension(paths[0]);
            if (firstFile != pageZero)
            {
                return false;
            }

            return paths.Select(p => Path.GetFileName(p)).All(IsDownload);
        }

        public static bool IsDownload(string pageName)
        {
            return downloadMatcher.IsMatch(pageName);
        }

        public static int GetDownloadPageNumber(string name)
        {
            var numberStart = name.IndexOf("page") + 4;
            var numberEnd = name.IndexOf('.', numberStart);

            var numberString = name.Substring(numberStart, numberEnd - numberStart);
            var number = int.Parse(numberString);

            return number;
        }

        public static int GetDownloadPageCount(string[] pages)
        {
            var consecutivePages = 0;
            var pagesSkippingOnePage = 0;

            for (var i = 1; i < pages.Length; i++)
            {
                var pageNumber = GetDownloadPageNumber(pages[i]);
                var prevPageNumber = GetDownloadPageNumber(pages[i - 1]);

                switch (pageNumber - prevPageNumber)
                {
                    case 1:
                        consecutivePages++;
                        break;
                    case 2:
                        pagesSkippingOnePage++;
                        break;
                    default:
                        return pages.Length;
                }
            }

            if (pagesSkippingOnePage > consecutivePages)
            {
                var pageCount = GetDownloadPageNumber(pages.Last());
                return pageCount;
            }

            return pages.Length;
        }

        //public static bool IsBackup(string name)
        //{
        //    if (name.IndexOf("--.cbz") != -1)
        //    {
        //        return false;
        //    }

        //    if (name.IndexOf('-') != -1)
        //    {
        //        if (name.IndexOf(' ') == -1)
        //        {
        //            return true;
        //        }

        //        // Handle "2014 - Name-of-book.cbz"
        //        var tokens = name.Split(new[] { " - " }, StringSplitOptions.None);
        //        if (tokens.Length == 2 && int.TryParse(tokens[0], out var _))
        //        {
        //            return IsBackup(tokens[1]);
        //        }
        //    }

        //    return false;
        //}

        //public static string TrimBackup(string name)
        //{
        //    var ext = Path.GetExtension(name);
        //    name = Path.GetFileNameWithoutExtension(name);

        //    var addReadMarker = false;

        //    if (name.EndsWith("--"))
        //    {
        //        addReadMarker = true;
        //        name = name.Substring(0, name.Length - 2);
        //    }

        //    name = name.Replace("--", "-");
        //    name = name.Replace("-", " ");
        //    name = name.Replace("   ", " - "); // !!! See special case in IsBackup
        //    name = name.Replace(" Vol ", " Vol. ");
        //    if (name.StartsWith("Vol "))
        //    {
        //        name = name.Replace("Vol ", "Vol. ");
        //    }

        //    var words = name.Split(' ');
        //    var hasVol = words.Length > 1 && words[words.Length - 2] == "Vol.";
        //    if (!hasVol)
        //    {
        //        var lastWord = words[words.Length - 1];
        //        if (int.TryParse(lastWord, out var number) && number > 999) // Don't want to remove Call-of-The-Suicide-Forest-2
        //        {
        //            name = string.Join(" ", words, 0, words.Length - 1);
        //        }
        //    }

        //    if (addReadMarker)
        //    {
        //        name = $"{name}--";
        //    }

        //    return name + ext;
        //}
    }
}
