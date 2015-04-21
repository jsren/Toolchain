using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public class TarArchive : IDisposable
    {
        private bool   leaveOpen;
        private Stream baseStream;

        public TarArchiveMode ArchiveMode { get; private set; }
        public StreamAccessMode AccessMode { get; private set; }

        internal TarIOBlockManager RandomAccess { get; private set; }
        private TarSequentialCollection SequentialAccess { get; set; }


        public TarArchive(Stream stream) 
            : this(stream, TarArchiveMode.Read) { }

        public TarArchive(Stream stream, TarArchiveMode mode) 
            : this(stream, mode, false) { }

        public TarArchive(Stream stream, TarArchiveMode mode, bool leaveOpen) 
            : this(stream, mode, stream.CanSeek ? StreamAccessMode.Random : StreamAccessMode.Sequential, leaveOpen)
        {
        }

        public TarArchive(Stream stream, TarArchiveMode mode, StreamAccessMode access, bool leaveOpen)
        {
            this.baseStream  = stream;
            this.ArchiveMode = mode;
            this.AccessMode  = access;
            this.leaveOpen   = leaveOpen;

            if (access == StreamAccessMode.Random)
            {
                if (!stream.CanSeek)
                {
                    throw new StreamAccessException("The given stream does not support random access.");
                }
                else this.RandomAccess = new TarIOBlockManager(this, baseStream);
            }
            else if (access == StreamAccessMode.Sequential)
            {
                SequentialAccess = new TarSequentialCollection(this, stream);
            }
            else throw new NotImplementedException("Unknown access type");


            if (mode == TarArchiveMode.Read || mode == TarArchiveMode.Update)
            {
                this.ParseFile();
            }
        }
        ~TarArchive()
        {
            this.Dispose(false);
        }

        private void ParseFile()
        {
            byte[] blockBuffer = new byte[TarIOBlockManager.BLOCK_SIZE];
            long   position    = 0;

            while (true)
            {
                try
                {
                    using (var reader = new BinaryReader(baseStream, ASCIIEncoding.ASCII, true))
                    {
                        var  header = new TarIOFileHeader(reader);
                        long blocks = (header.Size + TarIOBlockManager.BLOCK_SIZE - 1) / TarIOBlockManager.BLOCK_SIZE;

                        position += TarIOBlockManager.BLOCK_SIZE;

                        

                        this.RealEntries.Add(new TarArchiveEntry(this, header, this.RealEntries.Count));
                        this.RandomAccess.RegisterEntry(position, header.Size);

                        for (long i = 0; i < blocks; i++)
                        {
                            baseStream.Read(blockBuffer, 0, blockBuffer.Length);
                            position += TarIOBlockManager.BLOCK_SIZE;
                        }
                    }
                }
                catch (EndOfStreamException) { break; }
            }
            this.BlockManager.Position = position;
            this.RealEntries.RemoveRange(this.RealEntries.Count - 2, 2);

            if (!baseStream.CanSeek)
            {
                // Force the base stream to be reset
                this.baseStream.Dispose();
            }
        }

        public TarArchiveEntry CreateEntry(string entryName)
        {
            switch (this.AccessMode)
            {
                case StreamAccessMode.Random:
                    return this.RandomAccess.CreateEntry(entryName);
                case StreamAccessMode.Sequential:
                    return this.RealEntries.CreateEntry(entryName);
                default:
                    throw new NotImplementedException();
            }
        }
        public TarArchiveEntry CreateEntry(string entryName, Stream data)
        {
            switch (this.AccessMode)
            {
                case StreamAccessMode.Random:
                    return this.BlockManager.CreateEntry(entryName, data);
                case StreamAccessMode.Sequential:
                    return this.RealEntries.CreateEntry(entryName, data);
                default:
                    throw new NotImplementedException();
            }
        }
        public TarArchiveEntry CreateEntryFromFile(string filepath, string entryName)
        {
            switch (this.AccessMode)
            {
                case StreamAccessMode.Random:
                    return this.BlockManager.CreateEntryFromFile(filepath, entryName);
                case StreamAccessMode.Sequential:
                    return this.RealEntries.CreateEntryFromFile(filepath, entryName);
                default:
                    throw new NotImplementedException();
            }
        }


        public IEnumerable<TarArchiveEntry> Entries
        {
            get
            {
                if (this.AccessMode == StreamAccessMode.Sequential)
                {
                    return new TarSequentialCollection(this, baseStream);
                }
                else if (this.AccessMode == StreamAccessMode.Random)
                {
                    return this.BlockManager.Entries;
                }
                else throw new NotImplementedException("Unknown access mode");
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.BlockManager.Dispose();

                if (leaveOpen) { this.baseStream.Flush(); }
                else           { this.baseStream.Dispose(); }
            }
        }
    }
}
