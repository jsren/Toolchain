using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Toolchain.Common
{
    internal static class Extensions
    {
        public static string ReadResponse(this WebResponse response)
        {
            using (Stream instream = response.GetResponseStream())
            {
                var reader = new StreamReader(instream);
                return reader.ReadToEnd();
            }
        }

        public static string[] ReadResponseLines(this WebResponse response)
        {
            List<string> lines = new List<string>();

            using (Stream instream = response.GetResponseStream())
            {
                var reader = new StreamReader(instream);

                while (true)
                {
                    try
                    {
                        string nextLine = reader.ReadLine();
                        if (nextLine == null) break;

                        lines.Add(nextLine);
                    }
                    catch { break; }
                }
            }
            return lines.ToArray();
        }

    }
}
