using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        public uint Prior = 0;
        public uint AltPath = 0;
        public uint DeltaBl = 1;
        public LonNPdu NPDU = new LonNPdu();

        public int Length => 1 + NPDU.Length;
        public byte[] SDU => CombineBits(new BitInfo(0, 1), new BitInfo(Prior, 1), new BitInfo(AltPath, 1), new BitInfo(DeltaBl, 6));
        public byte[] FrameBytes => Concat(SDU, NPDU.DataBytes);

        public static LonPPdu FromData(byte[] data, int offset, int length)
        {
            LonPPdu pdu = new LonPPdu();

            ulong[] values = ExtractBits(data, offset, new BitInfo(1), new BitInfo(1), new BitInfo(1), new BitInfo(6));
            (pdu.Prior, pdu.AltPath, pdu.DeltaBl) = ((uint)values[1], (uint)values[2], (uint)values[3]);

            pdu.NPDU = LonNPdu.FromData(data, offset + 1, length - 1);

            return pdu;
        }
    }

    public class LonNPdu : LonProtocolBase
    {
        public uint Version = 0;
        public uint Domain = 0;
        /* address */
        public uint SourceSubnet = 0;
        public uint SourceNode = 0;
        public uint DestinationSubnet = 0;
        public uint DestinationNode = 0;
        public uint DestinationGroup = 0;
        public uint DestinationGroupMember = 0;
        public ulong DestinationNeuron = 0;


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

                return LonNPduFormat.Invalid;
            }
        }
        public LonNPduAddressFormat AddressFormat = 0;
        public LonNPduDomainLength DomainLength = 0;
        public LonPdu PDU = new LonPdu();

        public enum LonNPduFormat
        {
            TPDU = 0,
            SPDU = 1,
            AuthPDU = 2,
            Invalid = 3
        }

        public enum LonNPduAddressFormat
        {
            Subnet = 0,
            Group = 1,
            SubnetNode = 2,
            SubnetNodeGroup = 0x80 | 2,
            SubnetNeuron = 3,
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

        public byte[] AddressBytes
        {
            get
            {
                byte[] address = new byte[0];

                switch (AddressFormat)
                {
                    case LonNPduAddressFormat.Subnet:
                        address = CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8));
                        break;
                    case LonNPduAddressFormat.Group:
                        address = CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationGroup, 8));
                        break;
                    case LonNPduAddressFormat.SubnetNode:
                        address = CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8), new BitInfo(1, 1), new BitInfo(DestinationNode, 7));
                        break;
                    case LonNPduAddressFormat.SubnetNodeGroup:
                        address = CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(0, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8), new BitInfo(1, 1), new BitInfo(DestinationNode, 7), new BitInfo(DestinationGroup, 8), new BitInfo(DestinationGroupMember, 8));
                        break;
                    case LonNPduAddressFormat.SubnetNeuron:
                        address = CombineBits(new BitInfo(SourceSubnet, 8), new BitInfo(1, 1), new BitInfo(SourceNode, 7), new BitInfo(DestinationSubnet, 8), new BitInfo(DestinationNeuron, 8));
                        break;
                }
                return address;
            }
        }

        public byte[] SDU => Concat(CombineBits(new BitInfo(Version, 2), new BitInfo((int)PduFormat, 2), new BitInfo((int)AddressFormat, 2), new BitInfo((int)DomainLength, 2)), AddressBytes, DomainBytes);

        public byte[] DataBytes => Concat(SDU, PDU.FrameBytes);

        public static LonNPdu FromData(byte[] data, int offset, int length)
        {
            LonNPdu pdu = new LonNPdu();

            LonNPduFormat pduFormat;

            ulong[] values = ExtractBits(data, offset, new BitInfo(0, 2), new BitInfo(0, 2), new BitInfo(0, 2), new BitInfo(0, 2));
            (pdu.Version, pduFormat, pdu.AddressFormat, pdu.DomainLength) = ((uint)values[0], (LonNPduFormat)values[1], (LonNPduAddressFormat)values[2], (LonNPduDomainLength)values[3]);

            int addressLength = 0;

            switch (pdu.AddressFormat)
            {
                case LonNPduAddressFormat.Subnet:
                    {
                        addressLength = 3;
                        ulong[] addr = ExtractBits(data, offset+1, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8));
                        (pdu.SourceSubnet, pdu.SourceNode, pdu.SourceSubnet) = ((uint)addr[0], (uint)addr[2], (uint)addr[3]);
                        break;
                    }
                case LonNPduAddressFormat.Group:
                    {
                        addressLength = 3;
                        ulong[] addr = ExtractBits(data, offset+1, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8));
                        (pdu.SourceSubnet, pdu.SourceNode, pdu.DestinationGroup) = ((uint)addr[0], (uint)addr[2], (uint)addr[3]);
                        break;
                    }
                case LonNPduAddressFormat.SubnetNode:
                    {
                        addressLength = 4;
                        uint bType = 0;
                        ulong[] addr = ExtractBits(data, offset+1, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(1), new BitInfo(7));
                        (pdu.SourceSubnet, bType, pdu.SourceNode, pdu.DestinationSubnet, pdu.DestinationNode) = ((uint)addr[0], (uint)addr[1], (uint)addr[2], (uint)addr[3], (uint)addr[5]);

                        if(bType == 0)
                        {
                            pdu.AddressFormat = LonNPduAddressFormat.SubnetNodeGroup;
                            addressLength = 6;
                            addr = ExtractBits(data, offset+1, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(8));
                            (pdu.SourceSubnet, pdu.SourceNode, pdu.DestinationSubnet, pdu.DestinationNode, pdu.DestinationGroup, pdu.DestinationGroupMember) = ((uint)addr[0], (uint)addr[2], (uint)addr[3], (uint)addr[5], (uint)addr[6], (uint)addr[7]);
                        }
                        break;
                    }
                case LonNPduAddressFormat.SubnetNeuron:
                    {
                        addressLength = 9;
                        ulong[] addr = ExtractBits(data, offset+1, new BitInfo(8), new BitInfo(1), new BitInfo(7), new BitInfo(8), new BitInfo(48));
                        (pdu.SourceSubnet, pdu.SourceNode, pdu.DestinationSubnet, pdu.DestinationNeuron) = ((uint)addr[0], (uint)addr[2], (uint)addr[3], (uint)addr[4]);
                        break;
                    }
            }

            pdu.Domain = (uint)ExtractBits(data, offset + 1 + addressLength, new BitInfo((int)pdu.DomainLength * 8))[0];

            int pduOffset = offset + 1 + addressLength + (int)pdu.DomainLength;
            int pduLength = length - (1 + addressLength + (int)pdu.DomainLength);

            switch (pduFormat)
            {
                case LonNPduFormat.TPDU:
                    pdu.PDU = LonTPdu.FromData(data, pduOffset, pduLength);
                    break;
                case LonNPduFormat.SPDU:
                    pdu.PDU = LonSPdu.FromData(data, pduOffset, pduLength);
                    break;
                case LonNPduFormat.AuthPDU:
                    pdu.PDU = LonAuthPdu.FromData(data, pduOffset, pduLength);
                    break;
            }

            return pdu;
        }
    }

    public class LonPdu : LonProtocolBase
    {
        public uint TransNo = 0;

        public virtual int Length => FrameBytes.Length;
        public virtual byte[] SDU => new byte[0];
        public virtual byte[] Payload => new byte[0];
        public virtual byte[] FrameBytes => Concat(SDU, Payload);
    }

    public class LonTPdu : LonPdu
    {
        public uint Auth = 0;
        public uint ReminderLength = 0;
        public ulong ReminderMList = 0;
        public LonTPduType TPDUType = 0;
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
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength))[0];
                    break;
                case LonTPduType.RemMessage:
                    int msgBytes = data[offset + 1];
                    pdu.ReminderLength = (uint)(msgBytes * 8);
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength))[0];
                    pdu.APDU = LonAPdu.FromData(data, offset + 2 + msgBytes, length - 2 - msgBytes);
                    break;
            }

            return pdu;
        }
    }

    public class LonSPdu : LonPdu
    {
        public uint Auth = 0;
        public uint ReminderLength = 0;
        public ulong ReminderMList = 0;
        public LonSPduType SPDUType = 0;
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
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength))[0];
                    break;
                case LonSPduType.RemMessage:
                    int msgBytes = data[offset + 1];
                    pdu.ReminderLength = (uint)(msgBytes * 8);
                    pdu.ReminderMList = ExtractBits(data, offset + 2, new BitInfo(pdu.ReminderLength))[0];
                    pdu.APDU = LonAPdu.FromData(data, offset + 2 + msgBytes, length - 2 - msgBytes);
                    break;
            }

            return pdu;
        }
    }

    public class LonAuthPdu : LonPdu
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
            if (ExtractBits(data, offset, new BitInfo(2))[0] == 0)
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
        public byte[] Data = new byte[0];

        /// <summary>
        /// Generate an APDU for reading a NV from the device
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static LonAPdu GenerateNMNVFetch(int id)
        {
            return new LonAPduNetworkManagement
            {
                Code = (int)LonAPduNMType.NetworkVariableValueFetch,
                Data = new byte[] { (byte)id }
            };
        }

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
                Code = (int)LonAPduNMType.ReadMemory,
                Data = new byte[] { (byte)type, (byte)(address >> 8), (byte)(address & 0xFF), (byte)length }
            };
        }
    

        public override byte[] SDU => new byte[0];

        public override byte[] Payload => Data;

        public static LonAPdu FromData(byte[] data, int offset, int length)
        {
            LonAPdu pdu = null;

            if ((data[offset] & 0x80) != 0)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(1), new BitInfo(1), new BitInfo(14));
                pdu = new LonAPduNetworkVariable
                {
                    Direction = (LonAPduDirection)values[1],
                    Selector = (uint)values[2],
                    Data = ExtractBytes(data, offset + 2, length - 2)
                };
            }
            else if ((data[offset] & 0xC0) == 0)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(2), new BitInfo(6));
                pdu = new LonAPduGenericApplication
                {
                    Code = (uint)values[1],
                    Data = ExtractBytes(data, offset + 1, length - 1)
                };
            }
            else if ((data[offset] & 0xE0) == 3)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(3), new BitInfo(5));
                pdu = new LonAPduNetworkManagement
                {
                    Code = (uint)values[1],
                    Data = ExtractBytes(data, offset + 1, length - 1)
                };
            }
            else if ((data[offset] & 0xF0) == 5)
            {
                ulong[] values = ExtractBits(data, offset, new BitInfo(4), new BitInfo(4));
                pdu = new LonAPduNetworkDiagnostic
                {
                    Code = (uint)values[1],
                    Data = ExtractBytes(data, offset + 1, length - 1)
                };
            }
            else if ((data[offset] & 0xF0) == 4)
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
            RouterHalfEscapeCode = 30,
            ServicePinMessage = 31,
            NetworkManagementEscapeCode = 0x7D
        }
    }

    public class LonAPduNetworkVariable : LonAPdu
    {
        public uint Selector = 0;
        public LonAPduDirection Direction;

        public override byte[] SDU => CombineBits(new BitInfo(1, 1), new BitInfo((int)Direction, 1), new BitInfo(Selector, 14));
    }

    public class LonAPduGenericApplication : LonAPdu
    {
        public uint Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(0, 2), new BitInfo(Code, 6));
    }

    public class LonAPduNetworkManagement : LonAPdu
    {
        public uint Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(3, 3), new BitInfo(Code, 5));
    }

    public class LonAPduNetworkDiagnostic : LonAPdu
    {
        public uint Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(5, 4), new BitInfo(Code, 4));
    }

    public class LonAPduForeignFrame : LonAPdu
    {
        public uint Code = 0;

        public override byte[] SDU => CombineBits(new BitInfo(4, 4), new BitInfo(Code, 4));
    }

}
