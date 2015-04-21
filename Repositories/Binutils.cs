using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

using Toolchain.Common;

namespace Toolchain.Repositories
{

    public sealed class BinutilsRepository
    {
        private Uri location = new Uri("ftp://ftp.gnu.org/gnu/binutils/");

        public string Name { get { return "binutils"; } }

        public Uri URI { get { return location; } }

        private const string versReg = @"(\d+)\.(\d+\w?)(\.(\d+\w?))?";

        private Regex tarRegex   = new Regex(@"binutils-" + versReg + @"\.tar\.(.*)");
        private Regex patchRegex = new Regex(@"binutils-" + versReg + @"-" + versReg + @"\.patch\.(.*)");


        

        public IEnumerable<Package> QueryPackages()
        {
            /*WebRequest req = FtpWebRequest.Create(URI);
            req.Method = WebRequestMethods.Ftp.ListDirectory;

            string[] files;

            using (WebResponse response = req.GetResponse())
            {
                files = response.ReadResponseLines();
            }

            List<Package> output = new List<Package>();

            foreach (string file in files)
            {
                Match tarMatch   = tarRegex.Match(file);
                Match patchMatch = patchRegex.Match(file);

                if (tarMatch.Success)
                {
                    Version[] vers = ParseVersions(tarMatch);
                    output.Add(new Package("binutils", vers[0], new Uri(URI, file), new Package[0]));
                }
                else if (patchMatch.Success)
                {
                    Version[] vers = ParseVersions(patchMatch);

                    if (vers[0] != vers[1])
                    {
                        output.Add(new Package("binutils", vers[1], new Uri(URI, file),
                            new Package[] { new Package(null, vers[0], null, null) }));
                    }
                    else output.Add(new Package("binutils", vers[1], new Uri(URI, file), new Package[0]));
                }
            }*/

            /* Now resolve package dependencies */
            /*for (int i = 0; i < output.Count; i++)
            {
                Package pack = output[i];

                for (int n = 0; n < pack.Dependecies.Length; n++)
                {
                    bool found = false;

                    for (int t = 11; t < output.Count; t++)
                    {
                        if (pack.Dependecies[n].Version == output[t].Version)
                        {
                            pack.Dependecies[n] = output[t];
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        output.RemoveAt(i--);
                        break;
                    }
                }
            }*/
            // Return results
            throw new NotImplementedException();
            //return output.AsReadOnly();
        }
    }
}
