namespace Unity.Entities
{
    using System;
    using Unity.Collections;

    internal static class CalculateReaderWriterDependency
    {
        public static bool Add(ComponentType type, NativeList<int> reading, NativeList<int> writing)
        {
            bool flag2;
            if (!type.RequiresJobDependency)
            {
                flag2 = false;
            }
            else if (type.AccessModeType != ComponentType.AccessMode.ReadOnly)
            {
                int index = reading.IndexOf<int, int>(type.TypeIndex);
                if (index != -1)
                {
                    reading.RemoveAtSwapBack(index);
                }
                if (writing.Contains<int, int>(type.TypeIndex))
                {
                    flag2 = false;
                }
                else
                {
                    writing.Add(type.TypeIndex);
                    flag2 = true;
                }
            }
            else if (reading.Contains<int, int>(type.TypeIndex))
            {
                flag2 = false;
            }
            else if (writing.Contains<int, int>(type.TypeIndex))
            {
                flag2 = false;
            }
            else
            {
                reading.Add(type.TypeIndex);
                flag2 = true;
            }
            return flag2;
        }
    }
}

