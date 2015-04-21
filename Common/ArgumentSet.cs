using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolchain.Common
{
    class Argument
    {
        public bool Present { get; private set; }
        public string[] Values { get; private set; }

        public Argument(bool present, string[] values)
        {
            this.Present = present;
            this.Values  = values;
        }
    }

    public class ArgumentSet
    {
        private Dictionary<string, Argument> args 
            = new Dictionary<string, Argument>();

        private string[] Positional;

        public string this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                else if (Positional.Length > index)
                {
                    return Positional[index];
                }
                else return null;
            }
        }

        public int PositionalCount { get { return this.Positional.Length; } }

        public bool FlagIsSet(string flag)
        {
            return args.ContainsKey(flag) && args[flag].Present;
        }

        public string[] GetValues(string option)
        {
            if (!args.ContainsKey(option))
            {
                throw new ArgumentException("Unknown option '" + option + '\'');
            }
            else return args[option].Values;
        }

        public void AddFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
            {
                throw new ArgumentException("Parameter 'flag' null or white space");
            }
            args.Add(flag, new Argument(false, null));
        }
        public void AddOption(string Switch, params string[] defaultValues)
        {
            if (string.IsNullOrWhiteSpace(Switch))
            {
                throw new ArgumentException("Parameter 'Switch' null or white space");
            }
            args.Add(Switch, new Argument(false, defaultValues));
        }
        public void AddLinkedFlags(params string[] flags)
        {
            if (flags == null) throw new ArgumentNullException("flags");

            Argument arg = new Argument(false, null);

            foreach (string flag in flags)
            {
                if (string.IsNullOrWhiteSpace(flag))
                {
                    throw new ArgumentException("A given flag name was null or white space");
                }
                else args.Add(flag, arg);
            }
        }
        public void AddLinkedOptions(string[] names, params string[] values)
        {
            if (names == null) throw new ArgumentNullException("names");

            Argument arg = new Argument(false, values);

            foreach (string option in names)
            {
                if (string.IsNullOrWhiteSpace(option))
                {
                    throw new ArgumentException("A given option name was null or white space");
                }
                else args.Add(option, arg);
            }
        }
        

        public void SetValue(string Switch, bool value)
        {
            SetValue(Switch, new Argument(true, null));
        }

        public void SetValue(string Switch, params string[] values)
        {
            SetValue(Switch, new Argument(true, values));
        }


        private void SetValue(string Switch, Argument @new)
        {
            if (string.IsNullOrWhiteSpace(Switch))
            {
                throw new ArgumentException("Parameter 'Switch' null or white space");
            }

            if (args.ContainsKey(Switch))
            {
                Argument @default = args[Switch];

                foreach (string key in args.Keys.ToArray())
                {
                    if (args[key] == @default)
                    {
                        args[key] = @new;
                    }
                }
            }
            else args.Add(Switch, @new);
        }

        private bool isSwitch(string arg)
        {
            return arg.StartsWith("-") || arg.StartsWith("/");
        }
        private string getSwitch(string arg)
        {
            string output = arg.Remove(0, 1);

            if (String.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentException("Flag or option name null or empty");
            }
            else return output;
        }

        private void AddValue(string option, string value)
        {
            if (this.args.ContainsKey(option))
            {
                string[] values = args[option].Values;
                if (values == null)
                {
                    values = new string[] { value };
                }
                else
                {
                    int len = values.Length;
                    Array.Resize(ref values, len + 1);

                    values[len] = value;
                }
                SetValue(option, new Argument(true, values));
            }
            else
            {
                args.Add(option, new Argument(true, new string[] { value }));
            }
        }

        public void Parse(string[] args)
        {
            bool   positional   = true;
            string currentParam = null;

            List<string> positionals = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (positional)
                {
                    if (!isSwitch(arg))
                    {
                        positionals.Add(arg);
                    }
                    else
                    {
                        positional = false;
                        if (i + 1 == args.Length || isSwitch(args[i+1]))
                        {
                            SetValue(getSwitch(args[i]), new Argument(true, null));
                        }
                        else currentParam = getSwitch(args[i]);
                    }
                }
                else
                {
                    if (currentParam == null)
                    {
                        if (!isSwitch(args[i])) {
                            throw new Exception("Unexpected argument at index " + i.ToString());
                        }
                        else if (i + 1 == args.Length || isSwitch(args[i+1]))
                        {
                            SetValue(getSwitch(args[i]), new Argument(true, null));
                            currentParam = null;
                        }
                        else currentParam = getSwitch(args[i]);
                    }
                    // Option with a value
                    else
                    {
                        AddValue(currentParam, args[i+1]);
                        currentParam = null;
                    }
                }
            }
            this.Positional = positionals.ToArray();
        }
    }
}
