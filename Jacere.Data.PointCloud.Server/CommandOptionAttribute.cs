using System;
using JetBrains.Annotations;

namespace Jacere.Data.PointCloud.Server
{
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class CommandOptionAttribute : Attribute
    {
        public CommandOptionAttribute(string option, string shortOption = null)
        {
            if (shortOption != null)
            {
                if (shortOption.Length != 1 || shortOption[0] < 'a' || shortOption[0] > 'z')
                {
                    throw new ArgumentException("Short option must be a single letter [a-z]", nameof(shortOption));
                }
            }

            Option = option;
            ShortOption = shortOption;
        }

        public string Option { get; }

        public string ShortOption { get; }
    }
}
