namespace Unity.Entities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Unity.Collections.LowLevel.Unsafe;

    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentGroupArray<T> : IDisposable where T: struct
    {
        internal ComponentGroupArrayData m_Data;
        internal ComponentGroupArray(ComponentGroupArrayStaticCache cache)
        {
            this.m_Data = new ComponentGroupArrayData(cache);
        }

        public void Dispose()
        {
        }

        public int Length =>
            this.m_Data.m_Length;
        public T this[int index]
        {
            get
            {
                this.m_Data.CheckAccess();
                if ((index < this.m_Data.m_MinIndex) || (index > this.m_Data.m_MaxIndex))
                {
                    this.m_Data.FailOutOfRangeError(index);
                }
                if ((index < this.m_Data.CacheBeginIndex) || (index >= this.m_Data.CacheEndIndex))
                {
                    this.m_Data.UpdateCache(index);
                }
                T output = default(T);
                byte* valuePtr = (byte*) ref UnsafeUtility.AddressOf<T>(ref output);
                this.m_Data.PatchPtrs(index, valuePtr);
                this.m_Data.PatchManagedPtrs(index, valuePtr);
                return output;
            }
        }
        public ComponentGroupEnumerator<T, T> GetEnumerator() => 
            new ComponentGroupEnumerator<T, T>(this.m_Data);
        [StructLayout(LayoutKind.Sequential)]
        public struct ComponentGroupEnumerator<U> : IEnumerator<U>, IEnumerator, IDisposable where U: struct
        {
            private ComponentGroupArrayData m_Data;
            private int m_Index;
            internal ComponentGroupEnumerator(ComponentGroupArrayData arrayData)
            {
                this.m_Data = arrayData;
                this.m_Index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                bool flag2;
                this.m_Index++;
                if ((this.m_Index >= this.m_Data.CacheBeginIndex) && (this.m_Index < this.m_Data.CacheEndIndex))
                {
                    flag2 = true;
                }
                else if (this.m_Index >= this.m_Data.m_Length)
                {
                    flag2 = false;
                }
                else
                {
                    this.m_Data.CheckAccess();
                    this.m_Data.UpdateCache(this.m_Index);
                    flag2 = true;
                }
                return flag2;
            }

            public void Reset()
            {
                this.m_Index = -1;
            }

            public U Current
            {
                get
                {
                    this.m_Data.CheckAccess();
                    if ((this.m_Index < this.m_Data.m_MinIndex) || (this.m_Index > this.m_Data.m_MaxIndex))
                    {
                        this.m_Data.FailOutOfRangeError(this.m_Index);
                    }
                    U output = default(U);
                    byte* valuePtr = (byte*) ref UnsafeUtility.AddressOf<U>(ref output);
                    this.m_Data.PatchPtrs(this.m_Index, valuePtr);
                    this.m_Data.PatchManagedPtrs(this.m_Index, valuePtr);
                    return output;
                }
            }
            object IEnumerator.Current =>
                this.Current;
        }
    }
}

