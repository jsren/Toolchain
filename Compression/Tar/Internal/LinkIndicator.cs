
namespace System.IO.Compression
{
    internal enum LinkIndicator
    {
        File             = '0',
        HardLink         = '1',
        SymbolicLink     = '2',
        CharacterSpecial = '3',
        BlockSpecial     = '4',
        Directory        = '5',
        FIFO             = '6',
        ContiguousFile   = '7',
        GlobalHeaderEx   = 'g',
        FileHeaderEx     = 'x',

        GnuDumpDirectory = 'D',
        GnuLongLink      = 'K',
        GnuLongName      = 'L',
        GnuMultiVolume   = 'M',
        GnuSparse        = 'S',
        [Obsolete]
        GnuVolumeHeader  = 'V',

        SolarisHeaderEx = 'X'
    }
}