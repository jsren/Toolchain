using System;

using Toolchain.Common;

namespace Toolchain
{
    public class Version : IComparable<Version>, IEquatable<Version>
    {
        private int major;
        private int minor;
        private int build;
        private int revision;
        private Stage stage;

        public int Major { get { return major; } }
        public int Minor { get { return minor; } }
        public int Build { get { return build; } }
        public int Revision { get { return revision; } }
        public Stage IsAlpha { get { return stage; } }

        public Version() : this(0, 0) {  }
        public Version(int major, int minor, Stage stage = Stage.Release)
        {
            this.major = major;
            this.minor = minor;
            this.stage = stage;

            if (major < 0 || minor < 0) {
                throw new ArgumentException("Version numbers cannot be negative");
            }

            this.build    = -1;
            this.revision = -1;
        }
        public Version(int major, int minor, int build, Stage stage = Stage.Release) 
            : this(major, minor, stage)
        {
            if (build < 0) {
                throw new ArgumentException("Version numbers cannot be negative");
            }
            else this.build = build;
        }
        public Version(int major, int minor, int build, int revision, Stage stage = Stage.Release)
            : this(major, minor, build, stage)
        {
            if (revision < 0) {
                throw new ArgumentException("Version numbers cannot be negative");
            }
            else this.revision = revision;
        }

        public int CompareTo(Version other)
        {
            if (other == null) return 1;

            if (this.major > other.major) {
                return 1;
            }
            else if (this.major < other.major) {
                return -1;
            }
            else if (this.minor > other.minor) {
                return 1;
            }
            else if (this.minor < other.minor) {
                return -1;
            }
            else if (this.build > other.build) {
                return 1;
            }
            else if (this.build < other.build) {
                return -1;
            }
            else if (this.revision > other.revision) {
                return 1;
            }
            else if (this.revision < other.revision) {
                return -1;
            }
            else if ((int)this.stage > (int)other.stage) {
                return 1;
            }
            else if ((int)this.stage < (int)other.stage) {
                return -1;
            }
            else return 0;
        }

        public bool Equals(Version other)
        {
            return this.CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            Version other = obj as Version;

            if (other == null) {
                return false;
            }
            else return this.CompareTo(other) == 0;
        }

        public static bool operator ==(Version one, Version tother)
        {
            return Object.Equals(one, tother);
        }

        public static bool operator !=(Version one, Version tother)
        {
            return !Object.Equals(one, tother);
        }

        public override int GetHashCode()
        {
            return major ^ minor ^ build ^ revision ^ (int)stage;
        }

        public override string ToString()
        {
            string output;
            if (this.build == -1)
            {
                output = String.Format("{0}.{1}", major.ToString(), minor.ToString());
            }
            else if (this.revision == -1)
            {
                output = String.Format("{0}.{1}.{2}", major.ToString(),
                    minor.ToString(), build.ToString());
            }
            else
            {
                output = String.Format("{0}.{1}.{2}.{3}", major.ToString(),
                    minor.ToString(), build.ToString(), revision.ToString());
            }

            if (this.stage == Stage.Alpha)
            {
                output += " (Alpha)";
            }
            else if (this.stage == Stage.Beta)
            {
                output += " (Beta)";
            }
            return output;
        }
    }
}
