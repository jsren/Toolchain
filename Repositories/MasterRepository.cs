using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolchain.Repositories
{
    internal sealed class MasterRepository : IRepository
    {
        static List<IRepository> remoteRepos;
        static List<IRepository> localRepos;

        static MasterRepository()
        {
            remoteRepos = new List<IRepository>();
            localRepos  = new List<IRepository>();
        }


        public static void Register(IRepository repo)
        {
            if (!(repo is LocalRepository))
            {
                remoteRepos.Add(repo);
            }
        }

        internal static void RegisterLocal(IRepository localRepo)
        {
            localRepos.Add(localRepo);
        }



        public string Name { get { return "Master Repository"; } }
        public Uri URI { get { throw new InvalidOperationException(); } }


        public async Task<IEnumerable<string>> QueryProducts()
        {
            List<string> products = new List<string>();

            foreach (IRepository repo in remoteRepos)
            {
                try
                {
                    products.AddRange(await repo.QueryProducts());
                } catch { }
            }
            return products.AsReadOnly();
        }

        public async Task<IEnumerable<Package>> QueryPackages()
        {
            List<Package> pkgs = new List<Package>();

            foreach (IRepository repo in remoteRepos)
            {
                try
                {
                    pkgs.AddRange(await repo.QueryPackages());
                } catch { }
            }
            return pkgs.AsReadOnly();
        }
        public async Task<IEnumerable<Package>> QueryPackages(string product)
        {
            List<Package> pkgs = new List<Package>();

            foreach (IRepository repo in remoteRepos)
            {
                try
                {
                    pkgs.AddRange(await repo.QueryPackages(product));
                } catch { }
            }
            return pkgs.AsReadOnly();
        }

        public async Task<IEnumerable<Package>> QueryLocalPackages()
        {
            List<Package> pkgs = new List<Package>();

            foreach (IRepository repo in localRepos)
            {
                try
                {
                    pkgs.AddRange(await repo.QueryPackages());
                } catch { }
            }
            return pkgs.AsReadOnly();
        }
        public async Task<IEnumerable<Package>> QueryLocalPackages(string product)
        {
            List<Package> pkgs = new List<Package>();

            foreach (IRepository repo in localRepos)
            {
                try
                {
                    pkgs.AddRange(await repo.QueryPackages(product));
                } catch { }
            }
            return pkgs.AsReadOnly();
        }
    }
}
