using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression
{

    class TarIOEntry
    {
        public long Offset { get; set; }
        public long Length { get; set; }

        public TarArchiveEntry     Entry { get; set; }
        public TarIOFileItemStream Stream { get; set; }
    }

    internal sealed class TarIOBlockManager : IDisposable
    {
        internal const int BLOCK_SIZE = 512;

        private Stream           baseStream;
        private TarArchive       archive;
        private List<TarIOEntry> realEntries;

        internal TarIOBlockManager(TarArchive archive, Stream stream)
        {
            this.realEntries = new List<TarIOEntry>();
            this.archive     = archive;
            this.baseStream  = stream;
        }
        
        public IEnumerable<TarArchiveEntry> Entries
        {
            get { lock (this) { return realEntries.Select((e) => e.Entry); } }
        }


        internal TarArchiveEntry CreateEntry(string name)
        {
            lock (this)
            {
                this.realEntries.Add(new TarIOEntry()
                {
                    Length = BLOCK_SIZE,
                    Offset = baseStream.Length + BLOCK_SIZE,
                    Entry  = new TarArchiveEntry(archive, name, this.realEntries.Count)
                });

                baseStream.SetLength(baseStream.Length + BLOCK_SIZE * 2);
            }
            return this.realEntries.Last().Entry;
        }

        internal TarArchiveEntry CreateEntry(string name, Stream data)
        {
            lock (this)
            {
                TarIOEntry entry;
                this.realEntries.Add(entry = new TarIOEntry()
                {
                    Length = BLOCK_SIZE,
                    Offset = baseStream.Length + BLOCK_SIZE,
                    Entry  = new TarArchiveEntry(archive, name, this.realEntries.Count),
                });

                baseStream.Seek(0, SeekOrigin.End);
                baseStream.SetLength(baseStream.Length + BLOCK_SIZE * 2);

                var header = new TarIOFileHeader()
                {
                    Filename     = name,
                    LastModified = DateTime.Now,
                    Size         = data.Length,
                    Type         = LinkIndicator.File
                };
                header.Serialise(baseStream);

                entry.Stream = new TarIOFileItemStream(entry.Offset, entry.Length, baseStream);
                data.CopyTo(entry.Stream);

                return entry.Entry;
            }
        }

        public TarArchiveEntry CreateEntryFromFile(string filepath, string entryName)
        {
            var file = File.OpenRead(filepath);

            var header = new TarIOFileHeader()
            {
                Filename     = entryName,
                LastModified = File.GetLastWriteTime(filepath),
                Size         = file.Length,
                Type         = LinkIndicator.File
            };

            lock (this)
            {
                baseStream.Seek(0, SeekOrigin.End);

                long written = header.Serialise(baseStream);
                long delta   = TarIOBlockManager.BLOCK_SIZE - written;
                long padding = file.Length % TarIOBlockManager.BLOCK_SIZE;

                byte[] buffer = new byte[Math.Max(delta, padding)];
                baseStream.Write(buffer, 0, (int)delta);

                file.CopyTo(baseStream);
                baseStream.Write(buffer, 0, (int)padding);

                return new TarArchiveEntry(archive, header, null);
            }
        }

        internal Stream OpenStream(int index)
        {
            lock (this)
            {
                if (index < 0 || index >= this.realEntries.Count)
                    throw new ArgumentOutOfRangeException("index");

                TarIOEntry entry = realEntries[index];

                if (entry.Stream != null && entry.Stream.Closed)
                {
                    throw new InvalidOperationException(
                        "A stream for the requested resouce is already open");
                }
                else
                {
                    var @return = new TarIOFileItemStream(entry.Offset, entry.Length, this);
                    realEntries[index].Stream = @return;

                    return @return;
                }
            }
        }

        private void Move(long moveStart, long delta)
        {
            if (delta == 0) {
                return;
            }
            else if (moveStart + delta < 0 || delta % BLOCK_SIZE != 0) {
                throw new ArgumentException();
            }

            byte[] buffer = new byte[BLOCK_SIZE];

            if (delta < 0)
            {
                delta -= BLOCK_SIZE;

                lock (this)
                {
                    long blocks = (baseStream.Length - moveStart) / BLOCK_SIZE;

                    for (long l = 0; l < blocks; l++)
                    {
                        baseStream.Position = moveStart + l * BLOCK_SIZE;
                        baseStream.Read(buffer, 0, BLOCK_SIZE);

                        baseStream.Position += delta;
                        baseStream.Write(buffer, 0, BLOCK_SIZE);
                    }
                    // Now resize the stream
                    baseStream.SetLength(baseStream.Length + delta + BLOCK_SIZE);
                }
            }
            else if (delta > 0)
            {
                lock (this)
                {
                    long moveEnd = baseStream.Length - BLOCK_SIZE;
                    long blocks  = (baseStream.Length - moveStart) / BLOCK_SIZE;

                    // Now resize the stream
                    baseStream.SetLength(baseStream.Length + delta);

                    delta -= BLOCK_SIZE;

                    // Move bytes
                    for (long l = 0; l < blocks; l++)
                    {
                        baseStream.Position = moveEnd - l * BLOCK_SIZE;
                        baseStream.Read(buffer, 0, BLOCK_SIZE);

                        baseStream.Position += delta;
                        baseStream.Write(buffer, 0, BLOCK_SIZE);
                    }
                }
            }
        }

        internal void SaveEntry(TarIOFileItemStream stream)
        {
            lock (this)
            {
                // Alloc space
                long dataLength  = stream.Length - stream.PhysicalLength;         // Length of suffix
                long allocLength = (dataLength + BLOCK_SIZE - 1) & (-BLOCK_SIZE); // Block-adjusted length of suffix

                if (dataLength  == 0) return;                    // Skip if all data is already in the stream
                if (allocLength == 0) allocLength = BLOCK_SIZE;  // If dataLength < BLOCK_SIZE, round alloc length to BLOCK_SIZE

                // Make space for the new data
                Move(stream.Offset + stream.PhysicalLength, allocLength);
                
                // Save stream
                stream.SaveToPhysical();

                // Update positions
                foreach (TarIOEntry entry in realEntries)
                {
                    if (entry.Offset > stream.Offset)
                    {
                        entry.Offset += allocLength;
                    }
                    else if (f.Item1 == stream.Offset)
                    {
                        f.Item2 += dataLength;
                    }
                }
            }
        }

        internal void DeleteEntry(int index)
        {
            if (this.streams.ContainsKey(index) && !this.streams[index].Closed)
            {
                this.streams[index].Close(false);
            }
            var entry = this.Files[index];
            this.Files.RemoveAt(index);

            Move(entry.Item1 + entry.Item2, -entry.Item2);

            for (int i = index; i < Files.Count; i++)
            {
                Files[i].Item1 -= entry.Item2;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Stream s in streams.Values)
                {
                    try { s.Dispose(); }
                    catch { }
                }
            }
        }


        public TarArchiveEntry CreateEntry(string entryName)
        {
            LinkIndicator type = LinkIndicator.File;

            if (entryName.EndsWith("\\") || entryName.EndsWith("/"))
            {
                type = LinkIndicator.Directory;
            }

            lock (this)
            {
                int  index  = this.realEntries.Count;
                long offset = this.baseStream.Length + BLOCK_SIZE;

                var header = new TarIOFileHeader()
                {
                    Type         = type,
                    Filename     = entryName,
                    LastModified = DateTime.Now,
                    Size         = 0
                };

                this.baseStream.Seek(0, SeekOrigin.End);
                long written = header.Serialise(this.baseStream);
                long delta   = BLOCK_SIZE - written;

                byte[] buffer = new byte[delta];
                this.baseStream.Write(buffer, 0, buffer.Length);
            }
        }

        internal TarArchiveEntry CreateEntry(string entryName, Stream data)
        {
            throw new NotImplementedException();
        }

        internal TarArchiveEntry CreateEntryFromFile(string filepath, string entryName)
        {
            throw new NotImplementedException();
        }

    }
}
