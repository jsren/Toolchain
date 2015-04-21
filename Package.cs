using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolchain
{
    [Serializable]
    public class Package : IEquatable<Package>
    {
        private readonly string product;
        private readonly Version version;
        private readonly Uri location;
        private readonly bool installed;

        private readonly Package[] dependecies;

        public string Product { get { return product; } }
        public Version Version { get { return version; } }
        public Uri Location { get { return location; } }
        public bool IsInstalled { get { return installed; } }

        public Package[] Dependecies { get { return dependecies; } }

        public Package(string product, Version version, Uri location, Package[] dependecies,
            bool installed)
        {
            this.product     = product;
            this.version     = version;
            this.location    = location;
            this.installed   = installed;
            this.dependecies = dependecies;
        }

        public override string ToString()
        {
            return string.Format("{0} v{1}", product, version);
        }

        public bool Equals(Package other)
        {
            if (other == null) return false;

            return this.product == other.product && this.version == other.version
                && this.location == other.location && this.dependecies == other.dependecies;
        }
    }
}
