﻿using CbzMage.Shared.Helpers;
using MobiMetadata;
using System.Net;

namespace AzwConverter
{
    public class TitleSyncer
    {
        public int SyncBooksToTitles(Dictionary<string, FileInfo[]> books, Dictionary<string, FileInfo> titles, ArchiveDb archive)
        {
            var syncedBookCount = 0;
            var booksWithErrors = new List<string>();

            foreach (var book in books)
            {
                var bookId = book.Key;

                // Book is not in current titles
                if (!titles.ContainsKey(bookId))
                {
                    // Try the archive
                    if (archive.TryGetName(bookId, out var name))
                    {
                        Sync(name);
                    }
                    else
                    {
                        // Or scan the book file
                        var bookFiles = book.Value;
                        var azwFile = bookFiles.First(file => file.IsAzwFile());

                        using var stream = azwFile.Open(FileMode.Open);

                        var metadata = GetMetadata(stream, bookId);
                        if (metadata == null)
                        {
                            booksWithErrors.Add(bookId);
                            continue;
                        }

                        var title = CleanStr(metadata.MobiHeader.FullName);
                        var publisher = CleanStr(metadata.MobiHeader.ExthHeader.Publisher);

                        publisher = TrimPublisher(publisher);
                        Sync($"[{publisher}] {title}");
                    }

                    void Sync(string titleFile)
                    {
                        var file = Path.Combine(Settings.TitlesDir, titleFile);
                        File.WriteAllText(file, bookId);

                        // Add archived/scanned title to list of current titles
                        titles[bookId] = new FileInfo(file);
                        syncedBookCount++;
                    }
                }
            }

            foreach (var bookId in booksWithErrors)
            {
                books.Remove(bookId);
            }

            return syncedBookCount;
        }

        private static MobiMetadata.MobiMetadata GetMetadata(Stream stream, string bookId)
        {
            try
            {
                var pdbHeader = new PDBHead();
                var palmDocHeader = new PalmDOCHead();
                var mobiHeader = new MobiHead();
                var exthHeader = new EXTHHead();

                // Don't need anything from these two
                pdbHeader.SetAttrsToRead(null);
                palmDocHeader.SetAttrsToRead(null);

                // Get the fullname and the exth header
                mobiHeader.SetAttrsToRead(mobiHeader.FullNameOffsetAttr, mobiHeader.ExthFlagsAttr);

                // Get the publisher
                exthHeader.SetAttrsToRead(exthHeader.PublisherAttr);

                return new MobiMetadata.MobiMetadata(stream, pdbHeader, palmDocHeader, mobiHeader, exthHeader, throwIfNoExthHeader: true);
            }
            catch (Exception ex)
            {
                ProgressReporter.Error($"Error reading {bookId}.", ex);

                return null;
            }
        }

        private static string TrimPublisher(string publisher)
        {
            // Normalize publisher name
            foreach (var trimmedName in Settings.TrimPublishers)
            {
                if (publisher.StartsWith(trimmedName, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmedName;
                }
            }

            return publisher;
        }

        private static string CleanStr(string str)
        {
            str = WebUtility.HtmlDecode(str);
            return str.ToFileSystemString();
        }

        public int SyncTitlesToArchive(Dictionary<string, FileInfo> titles, ArchiveDb archive, Dictionary<string, FileInfo[]> books)
        {
            var idsToRemove = new List<string>();
            var archivedTitleCount = 0;

            foreach (var title in titles)
            {
                var bookId = title.Key;
                var titleFile = title.Value;

                archive.SetOrCreateName(bookId, titleFile.Name);

                // Delete title if no longer in books.
                if (!books.ContainsKey(bookId))
                {
                    archivedTitleCount++;

                    idsToRemove.Add(bookId);
                    titleFile.Delete();
                }
            }

            // Update current titles
            foreach (var bookId in idsToRemove)
            {
                titles.Remove(bookId);
            }

            return archivedTitleCount;
        }

        public string SyncConvertedTitle(string titleFile, FileInfo? convertedTitleFile)
        {
            if (convertedTitleFile != null)
            {
                convertedTitleFile.Delete();
            }

            var name = Path.GetFileName(titleFile);
            name = name.RemoveAnyMarker();

            var dest = Path.Combine(Settings.ConvertedTitlesDir, name);

            File.Copy(titleFile, dest);
            File.SetLastWriteTime(dest, DateTime.Now);

            return name;
        }
    }
}
