using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RotdToolkit.Lso
{
    public enum LsoValueKind
    {
        Undefined,
        Null,
        Bool,
        Int,
        Double,
        String,
        Array
    }

    public sealed class LsoValue
    {
        public LsoValueKind Kind;
        public bool Bool;
        public long Int;
        public double Double;
        public string String;
        public List<LsoValue> Array;

        public static LsoValue FromBool(bool v) => new LsoValue { Kind = LsoValueKind.Bool, Bool = v };
        public static LsoValue FromInt(long v) => new LsoValue { Kind = LsoValueKind.Int, Int = v };
        public static LsoValue FromDouble(double v) => new LsoValue { Kind = LsoValueKind.Double, Double = v };

        public bool IsNumeric => Kind == LsoValueKind.Int || Kind == LsoValueKind.Double;

        public double AsDouble() => Kind == LsoValueKind.Int ? Int : Double;
    }

    public sealed class LsoField
    {
        public string Name;
        public LsoValue Value;
    }

    /// <summary>
    /// Road of the Dead (Flash) local shared object (.sol) reader/writer.
    /// Format: [00 BF][u32 length]["TCSO"][u16 a][u32 b][u16 nameLen][name][u32 version]
    ///         then repeated: [u29 (nameLen&lt;&lt;1|1)][name][AMF3 value][00]
    /// The round-trip is byte-exact for the game's saves.
    /// </summary>
    public sealed class LsoDocument
    {
        public string ObjectName { get; private set; }
        public uint Version { get; private set; }
        public List<LsoField> Fields { get; } = new List<LsoField>();
        public byte[] Extra1 { get; private set; } = new byte[] { 0x00, 0x04 };
        public byte[] Extra2 { get; private set; } = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        private static readonly byte[] TCSO = { (byte)'T', (byte)'C', (byte)'S', (byte)'O' };

        public static LsoDocument Load(string path)
        {
            return Load(File.ReadAllBytes(path));
        }

        public static LsoDocument Load(byte[] b)
        {
            if (b.Length < 20 || b[0] != 0x00 || b[1] != 0xBF)
            {
                throw new InvalidDataException("不是有效的 Flash 本地存档 (.sol) 文件。");
            }

            var doc = new LsoDocument();
            int i = 6;
            for (int k = 0; k < 4; k++)
            {
                if (b[i + k] != TCSO[k])
                {
                    throw new InvalidDataException("缺少 TCSO 签名，可能不是 .sol 文件。");
                }
            }
            i += 4;

            doc.Extra1 = new byte[] { b[i], b[i + 1] };
            i += 2;
            doc.Extra2 = new byte[] { b[i], b[i + 1], b[i + 2], b[i + 3] };
            i += 4;

            int nameLen = (b[i] << 8) | b[i + 1];
            i += 2;
            doc.ObjectName = Encoding.UTF8.GetString(b, i, nameLen);
            i += nameLen;

            doc.Version = ReadU32(b, ref i);

            while (i < b.Length)
            {
                int raw = ReadU29(b, ref i);
                int len = raw >> 1;
                if (len == 0)
                {
                    break;
                }

                string name = Encoding.UTF8.GetString(b, i, len);
                i += len;

                var value = ReadValue(b, ref i);
                doc.Fields.Add(new LsoField { Name = name, Value = value });

                if (i < b.Length && b[i] == 0x00)
                {
                    i++;
                }
            }

            return doc;
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Serialize());
        }

        public byte[] Serialize()
        {
            var rest = new MemoryStream();
            rest.Write(TCSO, 0, TCSO.Length);
            rest.Write(Extra1, 0, Extra1.Length);
            rest.Write(Extra2, 0, Extra2.Length);

            var nameBytes = Encoding.UTF8.GetBytes(ObjectName);
            rest.WriteByte((byte)(nameBytes.Length >> 8));
            rest.WriteByte((byte)(nameBytes.Length & 0xFF));
            rest.Write(nameBytes, 0, nameBytes.Length);

            WriteU32(rest, Version);

            foreach (var f in Fields)
            {
                var fb = Encoding.UTF8.GetBytes(f.Name);
                WriteU29(rest, (fb.Length << 1) | 1);
                rest.Write(fb, 0, fb.Length);
                WriteValue(rest, f.Value);
                rest.WriteByte(0x00);
            }

            var restBytes = rest.ToArray();
            var outBytes = new byte[6 + restBytes.Length];
            outBytes[0] = 0x00;
            outBytes[1] = 0xBF;
            WriteU32Big(outBytes, 2, (uint)restBytes.Length);
            Buffer.BlockCopy(restBytes, 0, outBytes, 6, restBytes.Length);
            return outBytes;
        }

        // ---- value (AMF3) ----

        private static LsoValue ReadValue(byte[] b, ref int i)
        {
            byte t = b[i++];
            switch (t)
            {
                case 0x00: return new LsoValue { Kind = LsoValueKind.Undefined };
                case 0x01: return new LsoValue { Kind = LsoValueKind.Null };
                case 0x02: return LsoValue.FromBool(false);
                case 0x03: return LsoValue.FromBool(true);
                case 0x04:
                    {
                        int raw = ReadU29(b, ref i);
                        return LsoValue.FromInt(SignExtend29(raw));
                    }
                case 0x05:
                    {
                        double d = BitConverter.ToDouble(new[] { b[i + 7], b[i + 6], b[i + 5], b[i + 4], b[i + 3], b[i + 2], b[i + 1], b[i] }, 0);
                        i += 8;
                        return LsoValue.FromDouble(d);
                    }
                case 0x06:
                    {
                        int raw = ReadU29(b, ref i);
                        int len = raw >> 1;
                        string s = Encoding.UTF8.GetString(b, i, len);
                        i += len;
                        return new LsoValue { Kind = LsoValueKind.String, String = s };
                    }
                case 0x09:
                    {
                        int raw = ReadU29(b, ref i);
                        int count = (raw >> 1) + 1;
                        var arr = new List<LsoValue>(count);
                        for (int k = 0; k < count; k++)
                        {
                            arr.Add(ReadValue(b, ref i));
                        }
                        return new LsoValue { Kind = LsoValueKind.Array, Array = arr };
                    }
                default:
                    throw new InvalidDataException($"不支持的 AMF3 标记 0x{t:X2} (偏移 {i - 1})。");
            }
        }

        private static void WriteValue(Stream s, LsoValue v)
        {
            switch (v.Kind)
            {
                case LsoValueKind.Undefined: s.WriteByte(0x00); break;
                case LsoValueKind.Null: s.WriteByte(0x01); break;
                case LsoValueKind.Bool: s.WriteByte(v.Bool ? (byte)0x03 : (byte)0x02); break;
                case LsoValueKind.Int:
                    s.WriteByte(0x04);
                    WriteU29(s, unchecked((int)(v.Int & 0x1FFFFFFF)));
                    break;
                case LsoValueKind.Double:
                    {
                        var bytes = BitConverter.GetBytes(v.Double);
                        Array.Reverse(bytes);
                        s.Write(bytes, 0, 8);
                        break;
                    }
                case LsoValueKind.String:
                    {
                        s.WriteByte(0x06);
                        var sb = Encoding.UTF8.GetBytes(v.String ?? string.Empty);
                        WriteU29(s, (sb.Length << 1) | 1);
                        s.Write(sb, 0, sb.Length);
                        break;
                    }
                case LsoValueKind.Array:
                    {
                        s.WriteByte(0x09);
                        int n = v.Array?.Count ?? 0;
                        WriteU29(s, ((n - 1) << 1) | 1);
                        if (v.Array != null)
                        {
                            foreach (var item in v.Array)
                            {
                                WriteValue(s, item);
                            }
                        }
                        break;
                    }
            }
        }

        // ---- helpers ----

        private static int SignExtend29(int raw)
        {
            if ((raw & 0x10000000) != 0)
            {
                return raw | unchecked((int)0xE0000000);
            }
            return raw;
        }

        private static int ReadU29(byte[] b, ref int i)
        {
            int v = 0;
            for (int k = 0; k < 3; k++)
            {
                byte c = b[i++];
                v = (v << 7) | (c & 0x7F);
                if ((c & 0x80) == 0)
                {
                    return v;
                }
            }
            byte last = b[i++];
            return (v << 8) | last;
        }

        private static void WriteU29(Stream s, int value)
        {
            int u = value & 0x1FFFFFFF;
            if (u < 0x80)
            {
                s.WriteByte((byte)u);
            }
            else if (u < 0x4000)
            {
                s.WriteByte((byte)(0x80 | (u >> 7)));
                s.WriteByte((byte)(u & 0x7F));
            }
            else if (u < 0x200000)
            {
                s.WriteByte((byte)(0x80 | (u >> 14)));
                s.WriteByte((byte)(0x80 | ((u >> 7) & 0x7F)));
                s.WriteByte((byte)(u & 0x7F));
            }
            else
            {
                s.WriteByte((byte)(0x80 | (u >> 22)));
                s.WriteByte((byte)(0x80 | ((u >> 15) & 0x7F)));
                s.WriteByte((byte)(0x80 | ((u >> 8) & 0x7F)));
                s.WriteByte((byte)(u & 0xFF));
            }
        }

        private static uint ReadU32(byte[] b, ref int i)
        {
            uint v = (uint)((b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3]);
            i += 4;
            return v;
        }

        private static void WriteU32(Stream s, uint v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        private static void WriteU32Big(byte[] b, int offset, uint v)
        {
            b[offset] = (byte)(v >> 24);
            b[offset + 1] = (byte)(v >> 16);
            b[offset + 2] = (byte)(v >> 8);
            b[offset + 3] = (byte)v;
        }
    }
}
