using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolchain
{
    public interface IRepository
    {
        string Name { get; }
        Uri URI { get; }

        Task<IEnumerable<string>>  QueryProducts();
        Task<IEnumerable<Package>> QueryPackages();
        Task<IEnumerable<Package>> QueryPackages(string product);
    }
}
