using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace LonScan
{
    public class LonProtocolBase
    {
        public class BitInfo
        {
            public ulong Value;
            public int Width;

            public BitInfo(int width)
            {
                this.Width = width;
            }
            public BitInfo(uint width)
            {
                this.Width = (int)width;
            }
            public BitInfo(int value, int width)
            {
                this.Value = (uint)value;
                this.Width = width;
            }
            public BitInfo(int value, uint width)
            {
                this.Value = (uint)value;
                this.Width = (int)width;
            }
            public BitInfo(uint value, int width)
            {
                this.Value = value;
                this.Width = width;
            }
            public BitInfo(uint value, uint width)
            {
                this.Value = value;
                this.Width = (int)width;
            }
            public BitInfo(ulong value, int width)
            {
                this.Value = value;
                this.Width = width;
            }
            public BitInfo(ulong value, uint width)
            {
                this.Value = value;
                this.Width = (int)width;
            }
        }

        public static byte[] Concat(params byte[][] arrays)
        {
            return arrays.SelectMany(x => x).ToArray();
        }

        public static byte[] ExtractBytes(byte[] data, int offset, int length)
        {
            byte[] ret = new byte[length];

            Array.Copy(data, offset, ret, 0, length);

            return ret;
        }

        public static ulong ExtractBits(byte[] data, int offset, BitInfo info)
        {
            return ExtractBits(data, offset, new[] { info })[0];
        }

        public static ulong[] ExtractBits(byte[] data, int offset, params BitInfo[] infos)
        {
            ulong[] values = new ulong[infos.Length];
            int pos = 0;
            int bitPos = 0;
            int bytePos = offset;

            foreach (BitInfo info in infos)
            {
                ulong dataBits = GetBits(data, bytePos);
                ulong mask = (1UL << info.Width) - 1;
                dataBits >>= 64 - bitPos - info.Width;
                dataBits &= mask;

                values[pos++] = dataBits;
                bitPos += info.Width;
                while(bitPos > 8)
                {
                    bitPos -= 8;
                    bytePos++;
                }
            }

            return values;
        }

        private static ulong GetBits(byte[] data, int offset)
        {
            ulong value = 0;

            for(int byteNum = 0; byteNum < 8; byteNum++)
            {
                value <<= 8;
                if (offset + byteNum < data.Length)
                {
                    value |= data[offset + byteNum];
                }
            }

            return value;
        }

        public static byte[] CombineBits(params BitInfo[] infos)
        {
            ulong value = 0;
            int bitPos = 0;

            foreach (BitInfo info in infos.Reverse())
            {
                ulong mask = (1UL << info.Width) - 1;

                value |= (info.Value & mask) << bitPos;
                bitPos += info.Width;
            }

            if (bitPos > 64)
            {
                throw new Exception("array larger than 8 bytes is not supported at the moment");
            }

            byte[] data = new byte[bitPos / 8];

            for (int pos = 0; pos < data.Length; pos++)
            {
                data[data.Length - pos - 1] = (byte)(value & 0xFF);
                value >>= 8;
            }

            return data;
        }
    }

    public class LonPPdu : LonProtocolBase
    {
        [PacketFieldBool]
        public uint Prior = 0;
        [PacketFieldBool]
        public uint AltPath = 0;
        [PacketFieldUnsigned(6)]
        public uint DeltaBl = 1;
        [PacketFieldSubtype(typeof(LonNPdu))]
        public LonNPdu NPDU = new LonNPdu();

        public int Length => 1 + NPDU.Length;
        [PacketFieldSdu]
        public byte[] SDU => CombineBits(new BitInfo(0, 1), new BitInfo(Prior, 1), new BitInfo(AltPath, 1), new BitInfo(DeltaBl, 6));
        public byte[] FrameBytes => Concat(SDU, NPDU.DataBytes);

        public static LonPPdu FromData(byte[] data, int offset, int length)
        {
            LonPPdu pdu = new LonPPdu();

            ulong[] values = ExtractBits(data, offset, new BitInfo(1), new BitInfo(1), new BitInfo(6));
            (pdu.Prior, pdu.AltPath, pdu.DeltaBl) = ((uint)values[0], (uint)values[1], (uint)values[2]);

            pdu.NPDU = LonNPdu.FromData(data, offset + 1, length - 1);

            return pdu;
        }
    }


    public enum LonNPduAddressFormat
    {
        Subnet = 0,
        Group = 1,
        SubnetNode = 2,
        SubnetNodeGroup = 2,
        SubnetNeuron = 3,
        Invalid = 0xFF
    }

    public class LonAddress : LonProtocolBase
    {
        [PacketFieldUnsigned(8)]
        public int SourceSubnet = 0;
        [PacketFieldUnsigned(7)]
        public int SourceNode = 0;

        internal virtual LonNPduAddressFormat AddressFormat => LonNPduAddressFormat.Invalid;

        public virtual byte[] SDU => new byte[0];

        public virtual bool ForNode(int node) => false;
        public virtual bool ForGroup(int subnet) => false;
        public virtual bool ForSubnet(int group) => false;
        public virtual bool ForNeuron(ulong neuron) => false;
    }

    public class LonAddressSubnet : LonAddress
    {
        [PacketFieldUnsigned(8)]
        public uint DestinationSubnet = 0;
        internal override LonNPduAddressFormat AddressFormat => LonNPduAddressFormat.Subnet;

        override public byte[] SDU => CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8));

        public static LonAddress FromData(byte[] data, int offset, int length)
        {
            ulong[] addr = ExtractBits(data, offset, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8));
            var (sourceSubnet, sourceNode, destSubnet) = ((int)addr[0], (int)addr[2], (uint)addr[3]);

            return new LonAddressSubnet { SourceSubnet = sourceSubnet, SourceNode = sourceNode, DestinationSubnet = destSubnet };
        }

        public override bool ForSubnet(int subnet) => DestinationSubnet == subnet;
    }

    public class LonAddressGroup : LonAddress
    {
        [PacketFieldUnsigned(8)]
        public uint DestinationGroup = 0;
        internal override LonNPduAddressFormat AddressFormat => LonNPduAddressFormat.Group;

        override public byte[] SDU => CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationGroup, 8));

        public static LonAddress FromData(byte[] data, int offset, int length)
        {
            ulong[] addr = ExtractBits(data, offset, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8));
            var (sourceSubnet, sourceNode, destinationGroup) = ((int)addr[0], (int)addr[2], (uint)addr[3]);
            return new LonAddressGroup { SourceSubnet = sourceSubnet, SourceNode = sourceNode, DestinationGroup = destinationGroup };
        }
        public override bool ForGroup(int group) => DestinationGroup == group;
    }

    public class LonAddressNode : LonAddress
    {
        [PacketFieldUnsigned(8)]
        public uint DestinationSubnet = 0;
        [PacketFieldUnsigned(7)]
        public uint DestinationNode = 0;
        internal override LonNPduAddressFormat AddressFormat => LonNPduAddressFormat.SubnetNode;

        override public byte[] SDU => CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8), new BitInfo(1, 1), new BitInfo(DestinationNode, 7));

        public static LonAddress FromData(byte[] data, int offset, int length)
        {
            ulong[] addr = ExtractBits(data, offset, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(1), new BitInfo(7));
            var (sourceSubnet, bType, sourceNode, destinationSubnet, destinationNode) = ((int)addr[0], (uint)addr[1], (int)addr[2], (uint)addr[3], (uint)addr[5]);

            if (bType == 0)
            {
                return LonAddressNodeGroup.FromData(data, offset, length);
            }
            return new LonAddressNode { SourceSubnet = sourceSubnet, SourceNode = sourceNode, DestinationSubnet = destinationSubnet, DestinationNode = destinationNode };
        }

        public override bool ForNode(int node) => DestinationNode == node;
        public override bool ForSubnet(int subnet) => DestinationSubnet == subnet;
    }

    public class LonAddressNodeGroup : LonAddress
    {
        [PacketFieldUnsigned(8)]
        public uint DestinationSubnet = 0;
        [PacketFieldUnsigned(7)]
        public uint DestinationNode = 0;
        [PacketFieldUnsigned(8)]
        public uint DestinationGroup = 0;
        [PacketFieldUnsigned(8)]
        public uint DestinationGroupMember = 0;
        internal override LonNPduAddressFormat AddressFormat => LonNPduAddressFormat.SubnetNodeGroup;

        override public byte[] SDU => CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(0, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8), new BitInfo(1, 1), new BitInfo(DestinationNode, 7), new BitInfo(DestinationGroup, 8), new BitInfo(DestinationGroupMember, 8));

        public static LonAddress FromData(byte[] data, int offset, int length)
        {
            ulong[] addr = ExtractBits(data, offset, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(8));
            var (sourceSubnet, sourceNode, destinationSubnet, destinationNode, destinationGroup, destinationGroupMember) = ((int)addr[0], (int)addr[2], (uint)addr[3], (uint)addr[5], (uint)addr[6], (uint)addr[7]);

            return new LonAddressNodeGroup { SourceSubnet = sourceSubnet, SourceNode = sourceNode, DestinationSubnet = destinationSubnet, DestinationNode = destinationNode, DestinationGroup = destinationGroup, DestinationGroupMember = destinationGroupMember };
        }
        public override bool ForNode(int node) => DestinationNode == node;
        public override bool ForSubnet(int subnet) => DestinationSubnet == subnet;
    }

    public class LonAddressNeuron : LonAddress
    {
        [PacketFieldUnsigned(8)]
        public uint DestinationSubnet = 0;
        [PacketFieldUnsigned(48)]
        public ulong DestinationNeuron = 0;
        internal override LonNPduAddressFormat AddressFormat => LonNPduAddressFormat.SubnetNeuron;

        override public byte[] SDU => CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8), new BitInfo(DestinationNeuron, 48));

        public static LonAddress FromData(byte[] data, int offset, int length)
        {
            ulong[] addr = ExtractBits(data, offset, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(48));
            var (sourceSubnet, sourceNode, destinationSubnet, destinationNeuron) = ((int)addr[0], (int)addr[2], (uint)addr[3], (uint)addr[4]);

            return new LonAddressNeuron { SourceSubnet = sourceSubnet, SourceNode = sourceNode, DestinationSubnet = destinationSubnet, DestinationNeuron = destinationNeuron };
        }

        public override bool ForNeuron(ulong neuron) => DestinationNeuron == neuron;
    }

    public class LonNPdu : LonProtocolBase
    {
        [PacketFieldUnsigned(2)]
        public uint Version = 0;
        [PacketFieldEnum]
        public LonNPduDomainLength DomainLength = 0;
        [PacketFieldUnsigned]
        public uint Domain = 0;

        [PacketFieldSubtype(typeof(LonAddressGroup), typeof(LonAddressNeuron), typeof(LonAddressNode), typeof(LonAddressNodeGroup), typeof(LonAddressSubnet))]
        public LonAddress Address;


        public LonNPduFormat PduFormat
        {
            get
            {
                if (PDU is LonSPdu)
                {
                    return LonNPduFormat.SPDU;
                }
                if (PDU is LonTPdu)
                {
                    return LonNPduFormat.TPDU;
                }
                if (PDU is LonAuthPdu)
                {
                    return LonNPduFormat.AuthPDU;
                }
                if (PDU is LonAPdu)
                {
                    return LonNPduFormat.APDU;
                }

                return LonNPduFormat.Invalid;
            }
        }


        [PacketFieldSubtype(typeof(LonTPdu), typeof(LonSPdu), typeof(LonAuthPdu), typeof(LonAPdu))]
        public LonPdu PDU = new LonPdu();

        public enum LonNPduFormat
        {
            TPDU = 0,
            SPDU = 1,
            AuthPDU = 2,
            APDU = 3,
            Invalid
        }


        public enum LonNPduDomainLength
        {
            Bits_0 = 0,
            Bits_8 = 1,
            Bits_24 = 2,
            Bits_48 = 3
        }

        public int Length => 1 + AddressBytes.Length + DomainBytes.Length + PDU.Length;

        public byte[] DomainBytes
        {
            get
            {
                int domainWidth = 0;
                switch (DomainLength)
                {
                    case LonNPduDomainLength.Bits_0:
                        domainWidth = 0;
                        break;
                    case LonNPduDomainLength.Bits_8:
                        domainWidth = 1;
                        break;
                    case LonNPduDomainLength.Bits_24:
                        domainWidth = 3;
                        break;
                    case LonNPduDomainLength.Bits_48:
                        domainWidth = 4;
                        break;
                }

                return CombineBits(new BitInfo(Domain, domainWidth * 8));
            }
        }

        public byte[] AddressBytes => Address.SDU;

        public byte[] SDU => Concat(CombineBits(new BitInfo(Version, 2), new BitInfo((int)PduFormat, 2), new BitInfo((int)Address.AddressFormat, 2), new BitInfo((int)DomainLength, 2)), AddressBytes, DomainBytes);

        public byte[] DataBytes => Concat(SDU, PDU.FrameBytes);

        public static LonNPdu FromData(byte[] data, int offset, int length)
        {
            LonNPdu pdu = new LonNPdu();

            LonNPduFormat pduFormat;
            LonNPduAddressFormat addressFormat;

            ulong[] values = ExtractBits(data, offset, new BitInfo(0, 2), new BitInfo(0, 2), new BitInfo(0, 2), new BitInfo(0, 2));
            (pdu.Version, pduFormat, addressFormat, pdu.DomainLength) = ((uint)values[0], (LonNPduFormat)values[1], (LonNPduAddressFormat)values[2], (LonNPduDomainLength)values[3]);

            offset++;
            length--;

            switch (addressFormat)
            {
                case LonNPduAddressFormat.Subnet:
                    pdu.Address = LonAddressSubnet.FromData(data, offset, length);
                    break;
                case LonNPduAddressFormat.Group:
                    pdu.Address = LonAddressGroup.FromData(data, offset, length);
                    break;
                case LonNPduAddressFormat.SubnetNode:
                    pdu.Address = LonAddressNode.FromData(data, offset, length);
                    break;
                case LonNPduAddressFormat.SubnetNeuron:
                    pdu.Address = LonAddressNeuron.FromData(data, offset, length);
                    break;
            }

            int addrLen = pdu.Address.SDU.Length;
            offset += addrLen;
            length -= addrLen;

            pdu.Domain = (uint)ExtractBits(data, offset, new BitInfo((int)pdu.DomainLength * 8));
            offset += (int)pdu.DomainLength;
            length -= (int)pdu.DomainLength;


            switch (pduFormat)
            {
                case LonNPduFormat.TPDU:
                    pdu.PDU = LonTPdu.FromData(data, offset, length);
                    break;
                case LonNPduFormat.SPDU:
                    pdu.PDU = LonSPdu.FromData(data, offset, length);
                    break;
                case LonNPduFormat.AuthPDU:
                    pdu.PDU = LonAuthPdu.FromData(data, offset, length);
                    break;
                case LonNPduFormat.APDU:
                    pdu.PDU = LonAPdu.FromData(data, offset, length);
                    break;
            }

            return pdu;
        }
    }

    public class LonPdu : LonProtocolBase
    {
        public virtual int Length => FrameBytes.Length;
        public virtual byte[] SDU => new byte[0];
        public virtual byte[] Payload => new byte[0];
        public virtual byte[] FrameBytes => Concat(SDU, Payload);
    }

    public class LonTransPdu : LonPdu
    {
        [PacketFieldUnsigned]
        public uint TransNo = 0;
    }

    public class LonTPdu : LonTransPdu
    {
        [PacketFieldBool]
        public uint Auth = 0;
        [PacketFieldUnsigned]
        public uint ReminderLength = 0;
        [PacketFieldUnsigned]
        public ulong ReminderMList = 0;
        [PacketFieldEnum]
        public LonTPduType TPDUType = 0;
        [PacketFieldSubtype(typeof(LonAPdu))]
        public LonAPdu APDU = new LonAPdu();

        public enum LonTPduType
        {
            ACKD = 0,
            UnACKDRpt = 1,
            ACK = 2,
            Reminder = 4,
            RemMessage = 5
        }

        public override byte[] SDU => CombineBits(new BitInfo(Auth, 1), new BitInfo((int)TPDUType, 3), new BitInfo(TransNo, 4));
        public override byte[] Payload
        {
            get
            {
                byte[] data = new byte[0];

                switch (TPDUType)
                {
                    case LonTPduType.ACKD:
                    case LonTPduType.UnACKDRpt:
                        data = APDU.FrameBytes;
                        break;
                    case LonTPduType.ACK:
                        data = new byte[] { 0 };
                        break;
                    case LonTPduType.Reminder:
                        data = Concat(new byte[] { (byte)ReminderLength }, CombineBits(new BitInfo(ReminderMList, (ReminderLength - 24) / 8)));
                        break;
                    case LonTPduType.RemMessage:
                        data = Concat(new byte[] { (byte)ReminderLength }, CombineBits(new BitInfo(ReminderMList, ReminderLength / 8)), APDU.FrameBytes);
                        break;
                }

                return data;
            }
        }

        public static LonTPdu FromData(byte[] data, int offset, int length)
        {
            LonTPdu pdu = new LonTPdu();

            LonTPduType pduType;

            ulong[] values = ExtractBits(data, offset, new BitInfo(1), new BitInfo(3), new BitInfo(4));
            (pdu.Auth, pduType, pdu.TransNo) = ((uint)values[0], (LonTPduType)values[1], (uint)values[2]);

            switch (pduType)
            {
                case LonTPduType.ACKD:
                case LonTPduType.UnACKDRpt:
                    pdu.APDU = LonAPdu.FromData(data, offset + 1, length - 1);
                    break;
                case LonTPduType.ACK:
                    break;
                case LonTPduType.Reminder:
                    pdu.ReminderLength = (uint)(24 + data[offset + 1] * 8);
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength));
                    break;
                case LonTPduType.RemMessage:
                    int msgBytes = data[offset + 1];
                    pdu.ReminderLength = (uint)(msgBytes * 8);
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength));
                    pdu.APDU = LonAPdu.FromData(data, offset + 2 + msgBytes, length - 2 - msgBytes);
                    break;
            }

            return pdu;
        }
    }

    public class LonSPdu : LonTransPdu
    {
        [PacketFieldBool]
        public uint Auth = 0;
        [PacketFieldUnsigned]
        public uint ReminderLength = 0;
        [PacketFieldUnsigned]
        public ulong ReminderMList = 0;
        [PacketFieldEnum]
        public LonSPduType SPDUType = 0;
        [PacketFieldSubtype]
        public LonAPdu APDU = new LonAPdu();

        public enum LonSPduType
        {
            Request = 0,
            Response = 2,
            Reminder = 4,
            RemMessage = 5
        }

        public override byte[] SDU => CombineBits(new BitInfo(Auth, 1), new BitInfo((int)SPDUType, 3), new BitInfo(TransNo, 4));

        public override byte[] Payload
        {
            get
            {
                byte[] data = new byte[0];

                switch (SPDUType)
                {
                    case LonSPduType.Request:
                    case LonSPduType.Response:
                        data = APDU.FrameBytes;
                        break;
                    case LonSPduType.Reminder:
                        data = Concat(new byte[] { (byte)ReminderLength }, CombineBits(new BitInfo(ReminderMList, (ReminderLength - 24) / 8)));
                        break;
                    case LonSPduType.RemMessage:
                        data = Concat(new byte[] { (byte)ReminderLength }, CombineBits(new BitInfo(ReminderMList, ReminderLength / 8)), APDU.FrameBytes);
                        break;
                }

                return data;
            }
        }

        public static LonSPdu FromData(byte[] data, int offset, int length)
        {
            LonSPdu pdu = new LonSPdu();

            ulong[] values = ExtractBits(data, offset, new BitInfo(1), new BitInfo(3), new BitInfo(4));
            (pdu.Auth, pdu.SPDUType, pdu.TransNo) = ((uint)values[0], (LonSPduType)values[1], (uint)values[2]);

            switch (pdu.SPDUType)
            {
                case LonSPduType.Request:
                    pdu.APDU = LonAPdu.FromData(data, offset + 1, length - 1);
                    break;
                case LonSPduType.Response:
                    pdu.APDU = LonAPdu.FromData(data, offset + 1, length - 1);
                    break;
                case LonSPduType.Reminder:
                    pdu.ReminderLength = (uint)(24 + data[offset + 1] * 8);
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength));
                    break;
                case LonSPduType.RemMessage:
                    int msgBytes = data[offset + 1];
                    pdu.ReminderLength = (uint)(msgBytes * 8);
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength));
                    pdu.APDU = LonAPdu.FromData(data, offset + 2 + msgBytes, length - 2 - msgBytes);
                    break;
            }

            return pdu;
        }
    }

    public class LonAuthPdu : LonTransPdu
    {
        public uint Fmt = 0;
        public ulong RandomBytes = 0;
        public uint Group = 0;
        public LonAuthPDUType AuthPDUType = 0;

        public enum LonAuthPDUType
        {
            Challenge = 0,
            Reply = 2
        }

        public override byte[] SDU => 
            (Fmt == 1) ? 
            CombineBits(new BitInfo(Fmt, 2), new BitInfo((int)AuthPDUType, 2), new BitInfo(TransNo, 4), new BitInfo(RandomBytes, 64), new BitInfo(Group, 8)) :
            CombineBits(new BitInfo(Fmt, 2), new BitInfo((int)AuthPDUType, 2), new BitInfo(TransNo, 4), new BitInfo(RandomBytes, 64));

        public static LonAuthPdu FromData(byte[] data, int offset, int length)
        {
            LonAuthPdu pdu = new LonAuthPdu();

            /* only if Fmt == 1 we have a group */
            if (ExtractBits(data, offset, new BitInfo(2)) == 0)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(2), new BitInfo(2), new BitInfo(4), new BitInfo(64));
                (pdu.Fmt, pdu.AuthPDUType, pdu.TransNo, pdu.RandomBytes) = ((uint)values[0], (LonAuthPDUType)values[1], (uint)values[2], (uint)values[3]);
            }
            else
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(2), new BitInfo(2), new BitInfo(4), new BitInfo(64), new BitInfo(8));
                (pdu.Fmt, pdu.AuthPDUType, pdu.TransNo, pdu.RandomBytes, pdu.Group) = ((uint)values[0], (LonAuthPDUType)values[1], (uint)values[2], (uint)values[3], (uint)values[4]);
            }
            return pdu;
        }
    }

    public class LonAPdu : LonPdu
    {
        [PacketFieldData]
        public byte[] Data = new byte[0];

        public override byte[] SDU => new byte[0];

        public override byte[] Payload => Data;

        public static LonAPdu FromData(byte[] data, int offset, int length)
        {
            LonAPdu pdu = null;

            if (ExtractBits(data, offset, new BitInfo(1)) != 0)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(1), new BitInfo(1), new BitInfo(14));
                pdu = new LonAPduNetworkVariable
                {
                    Direction = (LonAPduDirection)values[1],
                    Selector = (uint)values[2],
                    Data = ExtractBytes(data, offset + 2, length - 2)
                };
            }
            else if (ExtractBits(data, offset, new BitInfo(2)) == 0)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(2), new BitInfo(6));
                pdu = new LonAPduGenericApplication
                {
                    Code = (uint)values[1],
                    Data = ExtractBytes(data, offset + 1, length - 1)
                };
            }
            else if (ExtractBits(data, offset, new BitInfo(3)) == 3)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(3), new BitInfo(5));
                pdu = new LonAPduNetworkManagement
                {
                    Code = (LonAPduNMType)values[1],
                    Data = ExtractBytes(data, offset + 1, length - 1)
                };
            }
            else if (ExtractBits(data, offset, new BitInfo(4)) == 5)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(4), new BitInfo(4));
                pdu = new LonAPduNetworkDiagnostic
                {
                    Code = (LonAPduDType)values[1],
                    Data = ExtractBytes(data, offset + 1, length - 1)
                };
            }
            else if (ExtractBits(data, offset, new BitInfo(4)) == 4)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(4), new BitInfo(4));
                pdu = new LonAPduForeignFrame
                {
                    Code = (uint)values[1],
                    Data = ExtractBytes(data, offset + 1, length - 1)
                };
            }

            return pdu;
        }

        public class NVConfig
        {
            public uint Priority;
            public LonAPduDirection Direction;
            public uint NetVarSelector;
            public bool Unbound;

            public uint Turnaround;
            public uint Service;
            public uint Authenticated;
            public uint Address;

            public static NVConfig FromData(byte[] data, int offset = 0)
            {
                NVConfig config = new NVConfig();

                ulong[] values = ExtractBits(data, offset, new BitInfo(1), new BitInfo(1), new BitInfo(14), new BitInfo(1), new BitInfo(2), new BitInfo(1), new BitInfo(4));

                (config.Priority, config.Direction, config.NetVarSelector, config.Turnaround, config.Service, config.Authenticated, config.Address) = ((uint)values[0], (LonAPduDirection)values[1], (uint)values[2], (uint)values[3], (uint)values[4], (uint)values[5], (uint)values[6]);

                config.Unbound = config.NetVarSelector > 0x1FFF;
                if(config.Unbound)
                {
                    config.NetVarSelector = 0x3FFF-config.NetVarSelector;
                }

                return config;
            }

            public override string ToString()
            {
                return "Prio: " + Priority + ", Dir: " + Direction + " Selector: " + NetVarSelector.ToString().PadLeft(4) + " (" + (Unbound?"UnBound":"Bound  ") + ") Turn: " + Turnaround + " Service: " + Service + " Auth: " + Authenticated + " AddrTbl: " + Address;
            }
        }


        public enum LonAPduDirection
        {
            Incoming = 0,
            Outgoing = 1
        }

        public enum LonAPduType
        {
            GenericApplicationMessage,
            NetworkVariableMessage,
            NetworkManagementMessage,
            DiagnosticMessage,
            ForeignFrame
        }


        public enum LonAPduDType
        {
            QueryStatus = 1,
            ProxyStatus = 2,
            ClearStatus = 3,
            QueryTransceiverStatus = 4
        }

        public enum LonAPduNMType
        {
            QueryId = 1,
            RespondToQuery = 2,
            UpdateDomain = 3,
            LeaveDomain = 4,
            UpdateKey = 5,
            UpdateAddress = 6,
            QueryAddress = 7,
            QueryNetworkVariableConfig = 8,
            UpdateGroupAddress = 9,
            QueryDomain = 10,
            UpdateNetworkVariableConfig = 11,
            SetNodeMode = 12,
            ReadMemory = 13,
            WriteMemory = 14,
            ChecksumRecalculate = 15,
            Install = 16,
            MemoryRefresh = 17,
            QueryStandardNetworkVariableType = 18,
            NetworkVariableValueFetch = 19,
            RouterMode = 20,
            RouterClearGroupOrSubnetTable = 21,
            RouterGroupOrSubnetTableDownload = 22,
            RouterGroupDownload = 23,
            RouterSubnetForward = 24,
            RouterDoNotForwardGroup = 25,
            RouterDoNotForwardSubnet = 26,
            RouterGroupOrSubnetTableReport = 27,
            RouterStatus = 28,
            NetworkManagementEscapeCode = 29,
            RouterHalfEscapeCode = 30,
            ServicePinMessage = 31
        }
    }

    public class LonAPduNetworkVariable : LonAPdu
    {
        [PacketFieldUnsigned]
        public uint Selector = 0;
        [PacketFieldEnum]
        public LonAPduDirection Direction;

        public override byte[] SDU => CombineBits(new BitInfo(1, 1), new BitInfo((int)Direction, 1), new BitInfo(Selector, 14));
    }

    public class LonAPduGenericApplication : LonAPdu
    {
        [PacketFieldUnsigned]
        public uint Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(0, 2), new BitInfo(Code, 6));
    }

    public class LonAPduNetworkManagement : LonAPdu
    {
        [PacketFieldEnum]
        public LonAPduNMType Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(3, 3), new BitInfo((int)Code, 5));


        /// <summary>
        /// Generate an APDU for reading memory
        /// </summary>
        /// <param name="type">0: ram, 1: eeprom ro, 2: eeprom cfg</param>
        /// <param name="address">0x0000 - 0xFFFF</param>
        /// <param name="length">1-16 bytes</param>
        /// <returns></returns>
        public static LonAPdu GenerateNMMemoryRead(int type, uint address, int length)
        {
            return new LonAPduNetworkManagement
            {
                Code = LonAPduNMType.ReadMemory,
                Data = new byte[] { (byte)type, (byte)(address >> 8), (byte)(address & 0xFF), (byte)length }
            };
        }
    }

    public class LonAPduNetworkDiagnostic : LonAPdu
    {
        [PacketFieldEnum]
        public LonAPduDType Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(5, 4), new BitInfo((int)Code, 4));
    }

    public class LonAPduForeignFrame : LonAPdu
    {
        [PacketFieldUnsigned]
        public uint Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(4, 4), new BitInfo(Code, 4));
    }

}
