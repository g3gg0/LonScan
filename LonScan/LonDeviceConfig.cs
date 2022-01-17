using Newtonsoft.Json;
using System.Collections.Generic;

namespace LonScan
{
    public class NvInfo
    {
        public string Name;
        public string Description;
        public string LonType
        {
            get => Type.Name;
            set
            {
                Type = LonStandardTypes.Get(value);
            }
        }
        internal LonStandardTypes.LonType Type;

        public NvInfo()
        {
        }

        public NvInfo(string name, string description, string format) : this(name, description, LonStandardTypes.Get(format))
        {
        }

        public NvInfo(string name, string description, LonStandardTypes.LonType type)
        {
            Name = name.Trim();
            Description = description.Trim();
            if (type != null)
            {
                Type = type;
            }
            else
            {
                Type = LonStandardTypes.Get("UNVT");
            }
        }
    }

    public class LonDeviceConfig
    {
        [JsonProperty(Order = 1)]
        public string Name { get; set; }

        [JsonProperty(Order = 2)]
        public int[] Addresses { get; set; }

        [JsonProperty(Order = 3)]
        public Dictionary<int, NvInfo> NvMap = new Dictionary<int, NvInfo>();

        internal NvInfo[] NvInfos
        {
            set
            {
                NvMap = new Dictionary<int, NvInfo>();
                for (int pos = 0; pos < value.Length; pos++)
                {
                    NvMap.Add(pos, value[pos]);
                }
            }
        }
    }
}
