using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Toolchain.Common;

namespace Toolchain.Installers
{
    public class BinutilsInstaller
    {
        private Process Execute(string program, string args = "", string workingDir = null)
        {
            var @return = Process.Start(new ProcessStartInfo(program)
            {
                Arguments        = args,
                WorkingDirectory = workingDir ?? Environment.CurrentDirectory,
                CreateNoWindow   = true,
            });
            @return.WaitForExit();

            return @return;
        }

        public async Task InstallPackage(Package package, ArgumentSet args)
        {
            if (package.Product != "binutils") {
                throw new ArgumentException("Invalid package for installer: " + package.Product);
            }

            /* Configure whether to build current and subsequent packages */
            bool performBuild = args.GetValues("PerformBuild")[0] == "true";
            if (performBuild && package.Dependecies.Any())
            {
                args.SetValue("PerformBuild", "false");
            }

            /* Begin installing dependencies. */
            var installTasks =
                package.Dependecies.Select((pkg) => this.InstallPackage(pkg, args));

            /* Get the installation directory and package. */
            string installDir  = args.GetValues("d:install")[0];
            string buildDir    = args.GetValues("d:build")[0];
            string packagePath = Path.Combine(Path.GetTempPath(), 
                String.Format("{0}-{1}.package", package.Product, Path.GetTempFileName()));

            // Download package
            using (WebClient client = new WebClient())
            {
                Console.WriteLine("Beginning download of package '{0}'", package);
                client.DownloadFileCompleted += (s, e) =>
                {
                    Console.WriteLine("Download of package '{0}' complete", package);
                };
                await client.DownloadFileTaskAsync(package.Location, packagePath);
            }

            /* Wait until the dependencies have been installed */
            await Task.WhenAll(installTasks);

            // Unpack package - auto-detect archive type
            using (Archive archive = new Archive(packagePath))
            {
                archive.ExtractToDirectory(buildDir);
            }

            /* Only perform build if requested */
            if (performBuild)
            {
                var configureArgs =
                    String.Format("{0} --target={1} --prefix=\"{2}\" {3}",
                        Path.Combine(Path.GetFullPath(buildDir), "configure"),
                        args.GetValues("Target")[0],
                        Path.GetFullPath(installDir),
                        args.GetValues("BuildParameters")[0]
                    );

                int exitCode;

                /* Begin configure */
                if ((exitCode = Execute("bash", args: configureArgs).ExitCode) != 0)
                {
                    throw new Exception("Error configuring binutils: " + exitCode.ToString());
                }
                /* Begin build */
                if ((exitCode = Execute("make", workingDir: Path.GetFullPath(buildDir)).ExitCode) != 0)
                {
                    throw new Exception("Error building binutils: " + exitCode.ToString());
                }
                /* Begin install */
                if ((exitCode = Execute("make", args: "install", workingDir: Path.GetFullPath(buildDir)).ExitCode) != 0)
                {
                    throw new Exception("Error installing binutils: " + exitCode.ToString());
                }
            }
        }
    }
}
