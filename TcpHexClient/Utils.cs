using System;
using System.Linq;

namespace TcpHex
{
    public static class Utils
    {
        public static string BytesToHexString(byte[] bytes)
        {
            return "0x" + string.Join(" 0x", Array.ConvertAll(bytes, b => b.ToString("X2")));
        }

        public static byte[] HexStringToBytes(string hexString, string separator)
        {
            hexString = hexString.Trim().Replace(separator, "");

            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException("HexString must have an even number of characters.");
            }

            byte[] byteArray = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return byteArray;
        }

        public static byte[] SetValue<T>(this byte[] cmd, int index, T value, bool reverse = true)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));
            if (index < 0 || index >= cmd.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            byte[] valueBytes = GetValueBytes(value);

            if (index + valueBytes.Length > cmd.Length)
            {
                throw new ArgumentException(
                    "The length of the byte array representing the value exceeds the specified length."
                );
            }

            if (reverse)
            {
                valueBytes = valueBytes.Reverse().ToArray();
            }

            Array.Copy(valueBytes, 0, cmd, index, valueBytes.Length);

            return cmd;
        }

        public static T GetValue<T>(this byte[] cmd, int index, bool reverse = true)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));
            if (index < 0 || index >= cmd.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            int length = GetTypeLength<T>();

            if (index + length > cmd.Length)
            {
                throw new ArgumentException(
                    "The length of the byte array representing the value exceeds the specified length."
                );
            }

            byte[] valueBytes = new byte[length];
            Array.Copy(cmd, index, valueBytes, 0, length);

            if (reverse)
            {
                valueBytes = valueBytes.Reverse().ToArray();
            }

            return BytesToValue<T>(valueBytes);
        }

        public static byte[] GetValueBytes<T>(T value)
        {
            byte[] valueBytes;

            if (typeof(T) == typeof(byte))
            {
                valueBytes = new[] { Convert.ToByte(value) };
            }
            else if (typeof(T) == typeof(ushort))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToUInt16(value));
            }
            else if (typeof(T) == typeof(short))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToInt16(value));
            }
            else if (typeof(T) == typeof(uint))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToUInt32(value));
            }
            else if (typeof(T) == typeof(int))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToInt32(value));
            }
            else if (typeof(T) == typeof(long))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToInt64(value));
            }
            else if (typeof(T) == typeof(ulong))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToUInt64(value));
            }
            else if (typeof(T) == typeof(float))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToSingle(value));
            }
            else if (typeof(T) == typeof(double))
            {
                valueBytes = BitConverter.GetBytes(Convert.ToDouble(value));
            }
            else
            {
                throw new ArgumentException("Unsupported type", nameof(value));
            }

            return valueBytes;
        }

        public static T BytesToValue<T>(byte[] valueBytes)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)valueBytes[0];
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)BitConverter.ToUInt16(valueBytes, 0);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)BitConverter.ToInt16(valueBytes, 0);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)BitConverter.ToUInt32(valueBytes, 0);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)BitConverter.ToInt32(valueBytes, 0);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)BitConverter.ToInt64(valueBytes, 0);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)BitConverter.ToUInt64(valueBytes, 0);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)BitConverter.ToSingle(valueBytes, 0);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)BitConverter.ToDouble(valueBytes, 0);
            }
            else
            {
                throw new ArgumentException("Unsupported type");
            }
        }

        private static int GetTypeLength<T>()
        {
            int length;

            if (typeof(T) == typeof(byte))
            {
                length = 1;
            }
            else if (typeof(T) == typeof(ushort))
            {
                length = 2;
            }
            else if (typeof(T) == typeof(short))
            {
                length = 2;
            }
            else if (typeof(T) == typeof(uint))
            {
                length = 4;
            }
            else if (typeof(T) == typeof(int))
            {
                length = 4;
            }
            else if (typeof(T) == typeof(long))
            {
                length = 8;
            }
            else if (typeof(T) == typeof(ulong))
            {
                length = 8;
            }
            else if (typeof(T) == typeof(float))
            {
                length = 4;
            }
            else if (typeof(T) == typeof(double))
            {
                length = 8;
            }
            else
            {
                throw new ArgumentException("Unsupported type");
            }

            return length;
        }
    }
}
