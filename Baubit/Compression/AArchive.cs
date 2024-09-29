﻿using System.IO.Compression;

namespace Baubit.Compression
{
    public abstract class AArchive
    {
        public string FilePath { get; init; }
        protected AArchive(string filePath)
        {
            this.FilePath = filePath;
        }

        public async IAsyncEnumerable<ZipArchiveEntry> EnumerateEntriesAsync()
        {
            await Task.Yield();
            using (FileStream fileStream = new FileStream(FilePath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        yield return entry;
                    }
                }
            }
        }
    }
}
