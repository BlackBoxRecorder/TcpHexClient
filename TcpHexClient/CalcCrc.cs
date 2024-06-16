using System;
using System.Collections.Generic;
using System.Text;

namespace TcpHex
{
    public static class CalcCrc
    {
        public static ushort Crc16(byte[] data)
        {
            ushort crc = 0xFFFF; // 初始值为0xFFFF

            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];

                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0x8408;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }
    }
}
