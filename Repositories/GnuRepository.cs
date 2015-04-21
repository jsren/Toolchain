using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Toolchain.Common;

namespace Toolchain.Repositories
{
    public class GnuRepository : IRepository
    {
        public string Name { get { return "GNU Repository"; } }

        public Uri URI { get { return new Uri("ftp://ftp.gnu.org/gnu/"); } }

        private Regex detailsFormat = new Regex(
            @"([-d])[rwx-]+ +\d+ +\d+ +\d+ +\d+ +(\w+ \d+ +[\d:]+) +(.*)");

        private Regex packageFormat = new Regex(
            @"(.*?)-(\d+)\.(\d+\w?)(\.(\d+\w?))?(-(\d+)\.(\d+\w?)(\.(\d+\w?))?)?\.(.*)");


        public async Task<IEnumerable<string>> QueryProducts()
        {   
            WebRequest directoryListRequst = FtpWebRequest.Create(URI);
            directoryListRequst.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            string[] fileDetails = null;

            await Task.Run((Action)delegate
            {
                using (WebResponse response = directoryListRequst.GetResponse())
                {
                    fileDetails = response.ReadResponseLines();
                }
            });

            List<string> output = new List<string>();

            foreach (string detail in fileDetails)
            {
                Match match = detailsFormat.Match(detail);
                if (match.Success)
                {
                    if (match.Groups[1].Value == "d")
                    {
                        output.Add(match.Groups[3].Value);
                    }
                }
            }
            return output;
        }

        public async Task<IEnumerable<Package>> QueryPackages()
        {
            List<Package> packages = new List<Package>();
            
            var tasks = (await this.QueryProducts()).Select((s) => this.QueryPackages(s));

            foreach (var pkgs in await Task.WhenAll(tasks))
            {
                packages.AddRange(pkgs);
            }
            return packages.AsReadOnly();
        }

        public async Task<IEnumerable<Package>> QueryPackages(string packageName)
        {
            WebRequest req = FtpWebRequest.Create(new Uri(URI, packageName + '/'));
            req.Method = WebRequestMethods.Ftp.ListDirectory;

            string[] files = null;

            await Task.Run((Action)delegate
            {
                using (WebResponse response = req.GetResponse())
                {
                    files = response.ReadResponseLines();
                }
            });

            List<Package> output = new List<Package>();

            foreach (string file in files)
            {
                try
                {
                    Match packageMatch = packageFormat.Match(file);

                    if (!packageMatch.Success) continue;

                    string product = packageMatch.Groups[1].Value;

                    Version[] vers = ParseVersions(packageMatch);

                    if (vers.Length == 2 && vers[0] != vers[1])
                    {
                        output.Add(new Package(product, vers[1], new Uri(req.RequestUri, file),
                            new Package[] { new Package(null, vers[0], null, null, false) }, false));
                    }
                    else
                    {
                        output.Add(new Package(product, vers[0], new Uri(req.RequestUri, file),
                            new Package[0], false));
                    }
                }
                catch { }
            }

            /* Now resolve package dependencies */
            for (int i = 0; i < output.Count; i++)
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
            }
            // Return results
            return output.AsReadOnly();
        }

        private Version[] ParseVersions(Match match)
        {
            Stage stage;
            int v1, v2, v3 = 0;
            string s1, s2, s3;

            int count = (match.Groups[6].Success ? 2 : 1);

            Version[] output = new Version[count];

            for (int i = 0, n = 0; i < count; i++, n += 5)
            {
                stage = Stage.Release;

                s1 = match.Groups[n + 2].Value;
                s2 = match.Groups[n + 3].Value;

                if (match.Groups[n + 5].Success)
                {
                    s3 = match.Groups[n + 5].Value;

                    if (s3.EndsWith("a"))
                    {
                        stage = Stage.Alpha;
                        s3 = s3.Remove(s3.Length - 1);
                    }
                    else if (s3.EndsWith("b"))
                    {
                        stage = Stage.Beta;
                        s3 = s3.Remove(s3.Length - 1);
                    }
                    else stage = Stage.Release;

                    v3 = int.Parse(s3);
                }
                else
                {
                    if (s2.EndsWith("a"))
                    {
                        stage = Stage.Alpha;
                        s2 = s2.Remove(s2.Length - 1);
                    }
                    else if (s2.EndsWith("b"))
                    {
                        stage = Stage.Beta;
                        s2 = s2.Remove(s2.Length - 1);
                    }
                    else stage = Stage.Release;
                }

                v2 = int.Parse(s2);
                v1 = int.Parse(s1);

                output[i] = new Version(v1, v2, v3, stage);
            }
            return output;
        }
    }
}
