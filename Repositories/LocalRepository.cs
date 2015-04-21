using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolchain.Repositories
{
    public class LocalRepository : IRepository
    {
        public string Name { get { return "Local Repository"; } }

        public Uri URI { get; private set; }

        public LocalRepository(string path)
        {
            this.URI = new Uri(System.IO.Path.GetFullPath(path));

            MasterRepository.RegisterLocal(this);
        }

        public Task<IEnumerable<string>> QueryProducts()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Package>> QueryPackages()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Package>> QueryPackages(string product)
        {
            throw new NotImplementedException();
        }
    }
}
