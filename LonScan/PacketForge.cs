using System;
using System.Reflection;
using System.Text;

namespace LonScan
{
    internal class PacketForge
    {
        internal static string ToString(object pdu, string indent = "")
        {
            StringBuilder sb = new StringBuilder();

            var fields = pdu.GetType().GetFields();

            foreach (var field in fields)
            {
                Type ft = field.FieldType;

                foreach (var att in field.GetCustomAttributes(typeof(PacketForgeAttribute), true))
                {
                    if (!(att is PacketForgeAttribute pfAtt))
                    {
                        continue;
                    }

                    string typeString = "";
                    string nameString = "";
                    string valueString = "";
                    string infoString = "";

                    nameString = field.Name.PadRight(16);


                    if (pfAtt is PacketFieldBoolAttribute)
                    {
                        bool value = false;
                        if (ft == typeof(bool))
                        {
                            value = (bool)field.GetValue(pdu);
                        }
                        else if (ft == typeof(int))
                        {
                            value = (int)field.GetValue(pdu) > 0;
                        }
                        else if (ft == typeof(uint))
                        {
                            value = (uint)field.GetValue(pdu) > 0;
                        }

                        typeString = "bool";
                        valueString = value ? "yes" : "no";
                    }
                    else if (pfAtt is PacketFieldUnsignedAttribute)
                    {
                        PacketFieldUnsignedAttribute attrib = (PacketFieldUnsignedAttribute)pfAtt;

                        int value = 0;
                        if (ft == typeof(int))
                        {
                            value = (int)field.GetValue(pdu);
                        }
                        else if (ft == typeof(uint))
                        {
                            value = (int)(uint)field.GetValue(pdu);
                        }

                        typeString = "int";
                        valueString = value.ToString();
                        infoString = "";// "(" + attrib.Width + " bit)";
                    }
                    else if (pfAtt is PacketFieldEnumAttribute)
                    {
                        typeString = ft.Name;
                        valueString = Enum.GetName(ft, field.GetValue(pdu));
                    }
                    else if (pfAtt is PacketFieldSduAttribute)
                    {
                        string data = "";

                        BitConverter.ToString((byte[])field.GetValue(pdu)).Replace("-", " ");

                        typeString = "byte[]";
                        valueString = data;
                    }
                    else if (pfAtt is PacketFieldDataAttribute)
                    {
                        string data = BitConverter.ToString((byte[])field.GetValue(pdu)).Replace("-", " ");

                        typeString = "byte[]";
                        valueString = "[ " + data + " ]";
                    }
                    else if (pfAtt is PacketFieldSubtypeAttribute)
                    {
                        PacketFieldSubtypeAttribute attrib = (PacketFieldSubtypeAttribute)att;

                        object sub = field.GetValue(pdu);

                        if (sub != null)
                        {
                            valueString = sub.GetType().Name + Environment.NewLine + ToString(sub, indent + "  ").TrimEnd(new[] { '\r', '\n' });
                        }
                        else
                        {
                            valueString = field.FieldType.Name + " (null)";
                        }
                    }

                    sb.AppendLine(indent + /*" "+ typeString.PadRight(16) + */" " + nameString.PadRight(24) + " = " + valueString + " " + infoString);
                }
            }

            return sb.ToString();
        }
    }

    internal class PacketForgeAttribute : Attribute
    {
    }

    internal class PacketFieldBoolAttribute : PacketForgeAttribute
    {
    }

    internal class PacketFieldEnumAttribute : PacketForgeAttribute
    {
    }

    internal class PacketFieldUnsignedAttribute : PacketForgeAttribute
    {
        internal int Width;

        public PacketFieldUnsignedAttribute()
        {
        }

        public PacketFieldUnsignedAttribute(int width)
        {
            this.Width = width;
        }
    }

    internal class PacketFieldSduAttribute : PacketForgeAttribute
    {
    }
    internal class PacketFieldDataAttribute : PacketForgeAttribute
    {
    }

    internal class PacketFieldSubtypeAttribute : PacketForgeAttribute
    {
        public Type[] ValidTypes;

        public PacketFieldSubtypeAttribute(params Type[] types)
        {
            ValidTypes = types;
        }
    }
}