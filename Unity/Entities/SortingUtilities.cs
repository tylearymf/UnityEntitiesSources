namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size=1)]
    internal struct SortingUtilities
    {
        public static unsafe void InsertSorted(int* data, int length, int newValue)
        {
            while (true)
            {
                if ((length <= 0) || (newValue >= data[length - 1]))
                {
                    data[length] = newValue;
                    return;
                }
                data[length] = data[length - 1];
                length--;
            }
        }

        public static unsafe void InsertSorted(ComponentType* data, int length, ComponentType newValue)
        {
            while (true)
            {
                if ((length <= 0) || (newValue >= data[length - 1]))
                {
                    data[length] = newValue;
                    return;
                }
                data[length] = data[length - 1];
                length--;
            }
        }

        public static unsafe void InsertSorted(ComponentTypeInArchetype* data, int length, ComponentType newValue)
        {
            ComponentTypeInArchetype archetype = new ComponentTypeInArchetype(newValue);
            while (true)
            {
                if ((length <= 0) || (archetype >= data[length - 1]))
                {
                    data[length] = archetype;
                    return;
                }
                data[length] = data[length - 1];
                length--;
            }
        }
    }
}

