using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LonScan
{
    public class XifFile
    {
        public string DeviceName = "(undefined)";
        public List<XifTag> Tags = new List<XifTag>();
        public List<XifVar> Vars = new List<XifVar>();

        private XifLineReader Reader;


        public XifFile(string file)
        {
            Reader = new XifLineReader(file);
            DeviceName = new FileInfo(file).Name; 


            /* ignore first 11 lines */
            for (int line = 1; line < 11; line++)
            {
                Reader.ReadLine();
            }

            if(Reader.CurrentLine != "*")
            {
                Console.WriteLine("Failed to read");
                return;
            }
            Reader.ReadLine();

            string selfDoc = ReadSelfDoc();
            Console.WriteLine("selfDoc: '" + selfDoc + "'");

            if(!string.IsNullOrEmpty(selfDoc))
            {
                string[] fields = selfDoc.Split(';');
                if(fields.Length > 1)
                {
                    DeviceName = fields.Last();
                }
            }
            Reader.ReadLine();

            bool done = false;
            while(!done && Reader.CurrentLine != null)
            {
                string[] fields = Reader.CurrentLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if(fields.Length == 0)
                {
                    break;
                }

                switch(fields[0])
                {
                    case "TAG":
                        Tags.Add(ReadTag());
                        break;
                    case "VAR":
                        Vars.Add(ReadVar());
                        break;
                }
            }

            try
            {
                FileInfo[] opData = new FileInfo(file).Directory.GetFiles("OperationalData.xml");
                if (opData.Length > 0)
                {
                    dynamic data = DynamicXml.Parse(File.ReadAllText(opData[0].FullName));

                    string dev = data.Device.SoftwareName;
                    string ver = data.Device.SoftwareVersion;

                    DeviceName = dev + " " + ver;
                }
            }
            catch (Exception ex)
            {

            }
        }

        public class XifTag
        {
            public string Name;
            public string Index;
            public string AvgRate;
            public string MaxRate;

            public string BindFlag;
        }

        public class XifVar
        {
            public string Name;
            public int Index;
            public int AvgRate;
            public int MaxRate;
            public int ArraySize;
            public bool TakeOffline;
            public bool Input;
            public bool Output => !Input;

            public string SelfDoc;


            public int SnvtIndex;

            public string BindFlag;

            public List<XifVarStructure> Structure = new List<XifVarStructure>();

            public enum XifVarStructType
            {
                Character = 0,
                Int8,
                Int16,
                Bitfield,
                Union,
                Typeless
            }

            public class XifVarStructure
            {
                public XifVarStructType Type;
                public int BitOffset;
                public int Size;
                public bool Signed;
                public int ArraySize;
            }
        }

        private XifVar ReadVar()
        {
            StringBuilder sb = new StringBuilder();

            XifVar var = new XifVar();
            string header;
            string avgRate;
            string maxRate;
            string arraySize;
            string index;
            (header, var.Name, index, avgRate, maxRate, arraySize, _) = Reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (header != "VAR")
            {
                return null;
            }
            var.Index = int.Parse(index);
            var.AvgRate = int.Parse(avgRate);
            var.MaxRate = int.Parse(maxRate);
            var.ArraySize = int.Parse(arraySize);


            var (takeOffline, _, _, dir, serviceType, _) = Reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var.TakeOffline = takeOffline == "1";
            var.Input = dir == "0";

            var.SelfDoc = ReadSelfDoc();

            var (svnt, _, elements, _) = Reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);


            var.SnvtIndex = int.Parse(svnt);

            int elems = int.Parse(elements);

            for(int pos = 0; pos < elems; pos++)
            {
                XifVar.XifVarStructure t = new XifVar.XifVarStructure();
                var (type, offset, size, signed, elemArraySize, _) = Reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                t.Type = (XifVar.XifVarStructType)int.Parse(type);
                t.BitOffset = int.Parse(offset);
                t.Size = int.Parse(size);
                t.Signed = signed == "1";
                t.ArraySize = int.Parse(elemArraySize);

                var.Structure.Add(t);
            }

            return var;
        }
        private XifTag ReadTag()
        {
            StringBuilder sb = new StringBuilder();

            XifTag tag = new XifTag();

            string header;
            (header, tag.Name, tag.Index, tag.AvgRate, tag.MaxRate, _) = Reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if(header != "TAG")
            {
                return null;
            }
            (_, tag.BindFlag, _) = Reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return tag;
        }

        private string ReadSelfDoc()
        {
            StringBuilder sb = new StringBuilder();
            if(Reader.CurrentLine.StartsWith("*"))
            {
                Reader.ReadLine();
            }

            while(Reader.CurrentLine.StartsWith("\""))
            {
                sb.Append(Reader.ReadLine().Substring(1));
            }

            return sb.ToString();
        }

        public class XifLineReader
        {
            private TextReader Reader;
            public string CurrentLine;
            public string NextLine;

            public XifLineReader(string file)
            {
                Reader = File.OpenText(file);

                CurrentLine = Reader.ReadLine();
                NextLine = Reader.ReadLine();
            }

            public string ReadLine()
            {
                string ret = CurrentLine;

                CurrentLine = NextLine;
                NextLine = Reader.ReadLine();

                //Console.WriteLine("> " + CurrentLine);

                return ret;
            }
        }

        internal LonDeviceConfig ToDeviceConfig()
        {
            LonDeviceConfig cfg = new LonDeviceConfig();

            cfg.Name = DeviceName;
            cfg.Addresses = new int[0];

            foreach(var v in Vars)
            {
                cfg.NvMap.Add(v.Index, new NvInfo { Name = v.Name, Type = LonStandardTypes.Get(v.SnvtIndex)  });
            }

            return cfg;
        }
    }
}
