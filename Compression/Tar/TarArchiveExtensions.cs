using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public static class TarArchiveExtensions
    {
        public static void ExtractToDirectory(this TarArchive archive, string directory)
        {
            if (directory == null) {
                throw new ArgumentNullException("directory");
            }
            if (!Directory.Exists(directory)) {
                throw new DirectoryNotFoundException(directory);
            }

            foreach (TarArchiveEntry entry in archive.Entries)
            {
                string path = Path.GetFullPath(Path.Combine(directory, entry.FullName));

                if (entry.Type == TarEntryType.Directory)
                {
                    Directory.CreateDirectory(path);
                }
                else if (entry.Type == TarEntryType.File)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (FileStream stream = File.Open(path, FileMode.OpenOrCreate))
                    {
                        stream.SetLength(0);
                        entry.Open().CopyTo(stream);
                    }
                }
                else throw new NotImplementedException();
            }
        }
    }
}
