
using Ionic.BZip2;

namespace System.IO.Compression
{
    public class Archive : IDisposable
    {
        private Stream  dataStream = null;
        private dynamic archive    = null;

        private Stream GetDataStream(string filepath)
        {
            bool create = !File.Exists(filepath);

            /* Handle compression formats */
            if (filepath.EndsWith(".gz"))
            {
                return new GZipStream(File.Open(filepath,
                    create ? FileMode.Create : FileMode.Open),
                    create ? CompressionMode.Compress : CompressionMode.Decompress);
            }
            else if (filepath.EndsWith(".bz2"))
            {
                if (create)
                {
                    return new BZip2OutputStream(File.Open(filepath, FileMode.Create));
                }
                else
                {
                    return new BZip2InputStream(File.OpenRead(filepath));
                }
            }
            else return null;
        }

        public Archive(string filepath)
        {
            bool create = !File.Exists(filepath);

            dataStream = GetDataStream(filepath);

            string archivePath = filepath.Remove(filepath.LastIndexOf("."));

            /* Handle archive formats */
            if (archivePath.EndsWith(".zip"))
            {
                if (dataStream == null)
                {
                    this.archive = new ZipArchive(File.Open(filepath,
                        create ? FileMode.Create : FileMode.Open),
                        create ? ZipArchiveMode.Create : ZipArchiveMode.Read);
                }
                else
                {
                    this.archive = new ZipArchive(dataStream, 
                        create ? ZipArchiveMode.Create : ZipArchiveMode.Read);
                }
            }
            else if (archivePath.EndsWith(".tar"))
            {
                if (dataStream == null)
                {
                    this.archive = new TarArchive(File.Open(filepath,
                        create ? FileMode.Create : FileMode.Open),
                        create ? TarArchiveMode.Create : TarArchiveMode.Read);
                }
                else
                {
                    this.archive = new TarArchive(dataStream,
                        create ? TarArchiveMode.Create : TarArchiveMode.Read);

                    if (!dataStream.CanSeek)
                    {
                        try
                        {
                            dataStream.Dispose();
                        }
                        catch { }

                        this.archive.Reset(dataStream = GetDataStream(filepath));
                    }
                }
            }

            /* Assert that we have a valid archive */
            if (this.archive == null)
            {
                throw new Exception("Cannot find a matching archive decoder");
            }
        }

        public void CreateEntry(string name, Stream data)
        {
            var entry = archive.CreateEntry(name);

            if (data != null && data.Length != 0)
            {
                using (Stream dest = entry.Open())
                {
                    data.CopyTo(dest);
                }
            }
        }

        public void ExtractToDirectory(string directory)
        {
            if (archive is TarArchive)
            {
                ((TarArchive)archive).ExtractToDirectory(directory);
            }
            else if (archive is ZipArchive)
            {
                ((ZipArchive)archive).ExtractToDirectory(directory);
            }
        }

        public void Close()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            try
            { 
                this.archive.Dispose();
            }
            catch { }
        }
    }
}
