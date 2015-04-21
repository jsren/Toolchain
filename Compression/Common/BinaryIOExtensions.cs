using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public static class BinaryIOExtensions
    {
        public static int ReadASCIIOctal64(this BinaryReader reader)
        {
            byte[] buffer = new byte[8];

            int chars = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                char c = reader.ReadChar();
                if (char.IsDigit(c))
                {
                    buffer[i] = (byte)(c - '0');
                    chars++;
                }
            }

            if (chars == 0) return 0;

            int output  = 0;
            int product = 1 << (3 * (chars - 1));

            for (int n = 0; n < chars; n++, product >>= 3)
            {
                if (buffer[n] == 0) continue;
                output += product * buffer[n];
            }
            return output;
        }

        public static long ReadASCIIOctal96(this BinaryReader reader)
        {
            byte[] buffer = new byte[12];

            int chars = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                char c = reader.ReadChar();
                if (char.IsDigit(c))
                {
                    buffer[i] = (byte)(c - '0');
                    chars++;
                }
            }

            if (chars == 0) return 0;

            long output = 0;
            long product = 1 << (3 * (chars - 1));

            for (int n = 0; n < chars; n++, product >>= 3)
            {
                if (buffer[n] == 0) continue;
                output += product * buffer[n];
            }
            return output;
        }
    }
}
