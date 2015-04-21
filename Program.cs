using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Toolchain.Common;
using Toolchain.Repositories;

namespace Toolchain
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.WaitAll(StartMain(args));
        }

        static async Task StartMain(string[] args)
        {
            var @params = new ArgumentSet();

            @params.AddLinkedFlags("f:p", "format-plain");

            @params.AddOption("d:package", "packages/");
            @params.AddOption("d:install", "tools/");
            @params.AddOption("d:build",   null);
            @params.AddOption("version", "*");
            @params.AddOption("repo", "*");

            @params.Parse(args);

            var repo = new MasterRepository();
            MasterRepository.Register(new GnuRepository());


            if ("list".Equals(@params[0], StringComparison.CurrentCultureIgnoreCase))
            {
                IEnumerable<Object> output;

                if ("packages".Equals(@params[1], StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!String.IsNullOrWhiteSpace(@params[2]))
                    {
                        output = await repo.QueryPackages(@params[2]);
                    }
                    else output = await repo.QueryPackages();
                }
                else if ("products".Equals(@params[1], StringComparison.CurrentCultureIgnoreCase))
                {
                    output = await repo.QueryProducts();
                }
                else if ("installed".Equals(@params[1], StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    Console.WriteLine("[ERROR] Unknown command");
                    throw new Exception();
                }

                if (!@params.FlagIsSet("f:p"))
                {
                    Console.WriteLine("\n{0}:", @params[1]);
                    foreach (Object obj in output)
                    {
                        Console.WriteLine(" - " + obj.ToString());
                    }
                }
                else
                {
                    foreach (Object obj in output)
                    {
                        Console.WriteLine(obj.ToString());
                    }
                }
            }
            else if ("download".Equals(@params[0], StringComparison.CurrentCultureIgnoreCase))
            {

            }


            /*Package lastestBinutils = (from pkg in await repo.QueryPackages("binutils") 
                                   orderby pkg.Version select pkg).FirstOrDefault();

            var installer = new Installers.BinutilsInstaller();

            await Task.WhenAll(installer.InstallPackage(lastestBinutils, @params));*/

            using (Archive arch = new Archive(@"J:\Users\James\Downloads\binutils-2.24.tar.gz"))
            {
                arch.ExtractToDirectory(Directory.CreateDirectory("test").FullName);
            }
        }
    }
}
