namespace Unity.Entities
{
    using System;

    internal static class HashUtility
    {
        public static unsafe uint Fletcher32(ushort* data, int count)
        {
            uint num = 0xff;
            uint num2 = 0xff;
            while (true)
            {
                if (count <= 0)
                {
                    num = (num & 0xffff) | (num >> 0x10);
                    return (((uint) (((num2 & 0xffff) | (num2 >> 0x10)) << 0x10)) | num);
                }
                int num3 = (count < 0x167) ? count : 0x167;
                int index = 0;
                while (true)
                {
                    if (index >= num3)
                    {
                        num = (num & 0xffff) + (num >> 0x10);
                        num2 = (num2 & 0xffff) + (num2 >> 0x10);
                        count -= num3;
                        data += num3;
                        break;
                    }
                    num += data[index];
                    num2 += num;
                    index++;
                }
            }
        }
    }
}

