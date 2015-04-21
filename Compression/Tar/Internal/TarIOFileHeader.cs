using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal class TarIOFileHeader
    {
        private static readonly char[] usmagic = { 'u', 's', 't', 'a', 'r', '\0' };


        private uint checksum;

        internal string Filename { get; set; }
        internal int FileMode { get; set; }
        internal int OwnerUserID { get; set; }
        internal int GroupUserID { get; set; }
        internal long Size { get; set; }
        internal DateTime LastModified { get; set; }
        internal LinkIndicator Type { get; set; }
        internal string LinkedFileName { get; set; }
        internal string OwnerUserName { get; set; }
        internal string OwnerGroupName { get; set; }
        internal Version  DeviceVersion { get; set; }
        internal DateTime LastAccessed { get; set; }


        internal TarIOFileHeader()
        {

        }

        internal TarIOFileHeader(BinaryReader reader)
        {
            char[] strBuffer = new char[155];
            
            reader.Read(strBuffer, 0, 100);
            Filename     = new string(strBuffer, 0, 100).Trim('\0');
            FileMode     = reader.ReadASCIIOctal64();
            OwnerUserID  = reader.ReadASCIIOctal64();
            GroupUserID  = reader.ReadASCIIOctal64();
            Size         = reader.ReadASCIIOctal96();
            LastModified = new DateTime(1970, 1, 1).AddSeconds(reader.ReadASCIIOctal96());
            checksum     = (uint)reader.ReadASCIIOctal64();
            Type         = (LinkIndicator)reader.ReadByte();

            reader.Read(strBuffer, 0, 100);
            LinkedFileName = new string(strBuffer, 0, 100).Trim('\0');

            // Now read the UStar format, if the magic matches.
            reader.Read(strBuffer, 0, usmagic.Length);

            bool isUStar = true;

            for (int i = 0; i < usmagic.Length; i++)
            {
                if (usmagic[i] != strBuffer[i])
                {
                    isUStar = false;
                    break;
                }
            }
            // Just finish up if not UStar
            if (!isUStar)
            {
                reader.ReadBytes(TarIOBlockManager.BLOCK_SIZE - 257);
                return;
            }
            // Now read version info (discard this)
            reader.Read(strBuffer, 0, 2);

            reader.Read(strBuffer, 0, 32);
            OwnerUserName = new string(strBuffer, 0, 32).Trim('\0');

            reader.Read(strBuffer, 0, 32);
            OwnerGroupName = new string(strBuffer, 0, 32).Trim('\0');


            int major = reader.ReadASCIIOctal64();
            int minor = reader.ReadASCIIOctal64();
            DeviceVersion = new Version(major, minor);

            reader.Read(strBuffer, 0, 155);
            string prefix = new string(strBuffer, 0, 155).Trim('\0');

            if (!String.IsNullOrWhiteSpace(prefix))
            {
                Filename = prefix + '/' + Filename;
            }

            reader.ReadBytes(TarIOBlockManager.BLOCK_SIZE - 500);
            return;
        }

        internal long Serialise(Stream baseStream)
        {
            throw new NotImplementedException();
        }
    }
}
