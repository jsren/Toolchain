using System.Collections.Generic;
using System.Text;

namespace System.IO.Compression
{
    internal class TarSequentialCollection : IEnumerable<TarArchiveEntry>
    {
        private IEnumerator<TarArchiveEntry> enumerator;

        internal long       Position   { get; set; }
        internal TarArchive Archive    { get; private set; }
        internal Stream     BaseStream { get; private set; }
        
        
        public TarSequentialCollection(TarArchive archive, Stream baseStream)
        {
            this.Archive    = archive;
            this.BaseStream = baseStream;
            this.enumerator = new TarEntryEnumerator(this);
        }

        public TarArchiveEntry CreateEntry(string name)
        {
            LinkIndicator type = LinkIndicator.File;

            if (name.EndsWith("\\") || name.EndsWith("/"))
            {
                type = LinkIndicator.Directory;
            }

            var header = new TarIOFileHeader()
            {
                Filename     = name,
                LastModified = DateTime.Now,
                Size         = 0,
                Type         = type
            };

            long written = header.Serialise(BaseStream);
            long delta   = TarIOBlockManager.BLOCK_SIZE - written;

            byte[] buffer = new byte[delta];
            BaseStream.Write(buffer, 0, (int)delta);

            Position += TarIOBlockManager.BLOCK_SIZE;
            return new TarArchiveEntry(Archive, header, null);
        }

        public TarArchiveEntry CreateEntry(string name, Stream data)
        {
            var header = new TarIOFileHeader()
            {
                Filename     = name,
                LastModified = DateTime.Now,
                Size         = data.Length,
                Type         = LinkIndicator.File
            };

            long written = header.Serialise(BaseStream);
            long delta   = TarIOBlockManager.BLOCK_SIZE - written;
            long padding = data.Length % TarIOBlockManager.BLOCK_SIZE;

            byte[] buffer = new byte[Math.Max(delta, padding)];
            BaseStream.Write(buffer, 0, (int)delta);

            data.CopyTo(BaseStream);
            BaseStream.Write(buffer, 0, (int)padding);

            Position += TarIOBlockManager.BLOCK_SIZE + (int)padding + data.Length;
            return new TarArchiveEntry(Archive, header, null);
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

            long written = header.Serialise(BaseStream);
            long delta   = TarIOBlockManager.BLOCK_SIZE - written;
            long padding = file.Length % TarIOBlockManager.BLOCK_SIZE;

            byte[] buffer = new byte[Math.Max(delta, padding)];
            BaseStream.Write(buffer, 0, (int)delta);

            file.CopyTo(BaseStream);
            BaseStream.Write(buffer, 0, (int)padding);

            Position += TarIOBlockManager.BLOCK_SIZE + (int)padding + file.Length;
            return new TarArchiveEntry(Archive, header, null);
        }


        private class TarEntryEnumerator : IEnumerator<TarArchiveEntry>
        {
            private BinaryReader reader;
            private TarSequentialCollection entries;

            public TarArchiveEntry Current { get; private set; }
            object Collections.IEnumerator.Current { get { return this.Current; } }

            
            public TarEntryEnumerator(TarSequentialCollection entries)
            {
                this.entries = entries;
                this.reader  = new BinaryReader(entries.BaseStream, Encoding.ASCII, true);
            }


            public bool MoveNext()
            {
                // Dispose of previous entry, if present
                if (Current != null) Current.Dispose();

                var nextHeader = new TarIOFileHeader(reader);
                entries.Position += TarIOBlockManager.BLOCK_SIZE;

                // Check for end of file blank header
                if (nextHeader.Filename.Length == 0 && 
                    nextHeader.Size == 0 && nextHeader.Type == 0)
                {
                    return false;
                }

                // Create new entry
                Current = new TarArchiveEntry(entries.Archive, nextHeader,
                    new TarIOFileItemStream(entries.Position, nextHeader.Size, entries.BaseStream));

                // Increment position by block-adjusted size
                entries.Position += (nextHeader.Size + TarIOBlockManager.BLOCK_SIZE - 1) & (-TarIOBlockManager.BLOCK_SIZE);

                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                reader.Dispose();
            }
        }


        public IEnumerator<TarArchiveEntry> GetEnumerator()
        {
            lock (this)
            {
                var  output     = this.enumerator;
                this.enumerator = null;

                if (output == null)
                {
                    throw new InvalidOperationException(
                        "Unable to create a new enumerator for a base stream with sequential access");
                }
                else return output;
            }
        }
        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            lock (this)
            {
                var  output     = this.enumerator;
                this.enumerator = null;

                if (output == null)
                {
                    throw new InvalidOperationException(
                        "Unable to create a new enumerator for a base stream with sequential access");
                }
                else return output;
            }
        }
    }
}
