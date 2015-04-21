using System.Linq;

namespace System.IO.Compression
{
    public class TarArchiveEntry : IDisposable
    {
        /// <summary>
        /// Index for random access streams. 
        /// </summary>
        private int index = -1;
        /// <summary>
        /// Item stream for sequential access.
        /// </summary>
        private TarIOFileItemStream stream;

        TarIOFileHeader header;

        public TarArchive Archive { get; private set; }
        public string FullName { get; private set; }
        public string Name { get; private set; }
        public DateTime LastModified { get; set; }
        public TarEntryType Type { get; private set; }

        internal TarArchiveEntry(TarArchive archive, string name, int index)
        {
            this.Archive  = archive;
            this.FullName = name;
            this.index    = index;

            LinkIndicator type;
            string realname;

            if (name.EndsWith("/") || name.EndsWith("\\"))
            {
                type     = LinkIndicator.Directory;
                realname = name.TrimEnd('/','\\');
            }
            else
            {
                type     = LinkIndicator.File;
                realname = name;
            }
            this.header = new TarIOFileHeader()
            {
                Filename = name,
                Type     = type
            };

        }

        internal TarArchiveEntry(TarArchive archive, TarIOFileHeader header)
        {
            this.Archive      = archive;
            this.FullName     = header.Filename;
            this.Name         = header.Filename.Split('/', '\\').Last();
            this.LastModified = header.LastModified;

            switch (header.Type)
            {
                case LinkIndicator.File:
                case LinkIndicator.ContiguousFile:
                    this.Type = TarEntryType.File;
                    break;

                case LinkIndicator.HardLink:
                case LinkIndicator.SymbolicLink:
                case LinkIndicator.CharacterSpecial:
                case LinkIndicator.BlockSpecial:
                case LinkIndicator.FIFO:
                    this.Type = TarEntryType.Link;
                    break;

                case LinkIndicator.Directory:
                    this.Type = TarEntryType.Directory;
                    break;

                case LinkIndicator.GlobalHeaderEx:
                case LinkIndicator.FileHeaderEx:
                case LinkIndicator.GnuDumpDirectory:
                case LinkIndicator.GnuLongLink:
                case LinkIndicator.GnuLongName:
                case LinkIndicator.GnuMultiVolume:
                case LinkIndicator.GnuSparse:
                case LinkIndicator.GnuVolumeHeader:
                case LinkIndicator.SolarisHeaderEx:
                    this.Type = TarEntryType.Metadata;
                    break;

                default:
                    this.Type = TarEntryType.Other;
                    break;
            }

            this.header = header;
        }

        internal TarArchiveEntry(TarArchive archive, TarIOFileHeader header, int index) 
            : this(archive, header)
        {
            this.index = index;
        }

        internal TarArchiveEntry(TarArchive archive, TarIOFileHeader header, TarIOFileItemStream stream)
            : this(archive, header)
        {
            this.stream = stream;
        }


        public Stream Open()
        {
            if (this.Archive.AccessMode == StreamAccessMode.Sequential)
            {
                Stream output = this.stream;
                this.stream = null;

                if (output == null)
                {
                    throw new IOException("The entry is currently open or has been closed. It cannot be reopened.");
                }
                else return output;
            }
            else if (this.Archive.AccessMode == StreamAccessMode.Random)
            {
                return Archive.BlockManager.OpenStream(index);
            }
            else throw new NotImplementedException("Unknown access mode");
        }

        public void Delete()
        {
            if (this.Archive.AccessMode != StreamAccessMode.Random)
            {
                throw new StreamAccessException("Cannot delete entries during non-random access.");
            }
            else Archive.BlockManager.DeleteEntry(index);
        }

        public override string ToString()
        {
            return this.FullName;
        }


        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
            }
        }
    }
}
