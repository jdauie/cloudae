using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jacere.Data.PointCloud.Server
{
    public class CommandOptionThing
    {
        public static void ProcessStuff(Type target)
        {
            ProcessStuff2(target, null);
        }

        public static void ProcessStuff(object instance)
        {
            ProcessStuff2(instance.GetType(), instance);
        }

        private static void ProcessStuff2(Type target, object instance)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Public;

            if (instance != null)
            {
                flags |= BindingFlags.Instance;
            }
            else
            {
                flags |= BindingFlags.Static;
            }

            var props = target.GetProperties(flags);
            
            var optionMap = new Dictionary<string, PropertyInfo>();
            var shortOptionMap = new Dictionary<char, string>();

            foreach (var prop in props)
            {
                var attr = (CommandOptionAttribute)prop.GetCustomAttributes(typeof(CommandOptionAttribute), false).SingleOrDefault();
                if (attr != null)
                {
                    optionMap.Add(attr.Option, prop);

                    if (attr.ShortOption != null)
                    {
                        shortOptionMap.Add(attr.ShortOption[0], attr.Option);
                    }
                }
            }

            var args = new Dictionary<string, string>();

            foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
            {
                if (arg.StartsWith("--"))
                {
                    var key = arg.Substring(2);
                    string value = null;

                    var splitIndex = key.IndexOf('=');
                    if (splitIndex != -1)
                    {
                        value = key.Substring(splitIndex + 1);
                        key = key.Substring(0, splitIndex);
                    }

                    args.Add(key, value);
                }
                else if (arg.StartsWith("-"))
                {
                    var key = arg.Substring(1);

                    var splitIndex = key.IndexOf('=');
                    if (splitIndex != -1)
                    {
                        var value = key.Substring(splitIndex + 1);
                        key = key.Substring(0, splitIndex);

                        if (key.Length != 1)
                        {
                            throw new Exception("Invalid short option format");
                        }

                        args.Add(shortOptionMap[key[0]], value);
                    }
                    else
                    {
                        foreach (var c in key)
                        {
                            args.Add(shortOptionMap[c], null);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unknown argument `{arg}`");
                }
            }

            foreach (var arg in args)
            {
                var prop = optionMap[arg.Key];
                var underlyingPropType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                if (underlyingPropType == typeof(bool))
                {
                    prop.SetValue(instance, true);
                }
                else if (underlyingPropType == typeof(string))
                {
                    prop.SetValue(instance, arg.Value);
                }
                else if (underlyingPropType == typeof(int))
                {
                    prop.SetValue(instance, int.Parse(arg.Value));
                }
                else if (underlyingPropType == typeof(double))
                {
                    prop.SetValue(instance, double.Parse(arg.Value));
                }
                else
                {
                    throw new Exception($"Unsupported type for `{prop.Name}`");
                }
            }
        }
    }
}
