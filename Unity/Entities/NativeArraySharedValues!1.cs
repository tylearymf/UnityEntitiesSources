namespace Unity.Entities
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeArraySharedValues<T> : IDisposable where T: struct, IComparable<T>
    {
        private NativeArray<int> m_Buffer;
        [ReadOnly]
        private readonly NativeArray<T> m_Source;
        private int m_SortedBuffer;
        public NativeArraySharedValues(NativeArray<T> source, Allocator allocator)
        {
            this.m_Buffer = new NativeArray<int>((source.Length * 4) + 1, allocator, NativeArrayOptions.ClearMemory);
            this.m_Source = source;
            this.m_SortedBuffer = 0;
        }

        private JobHandle MergeSortedLists(JobHandle inputDeps, int sortedCount, int outputBuffer)
        {
            JobHandle handle4;
            int arrayLength = this.m_Source.Length / (sortedCount * 2);
            JobHandle dependsOn = inputDeps;
            if (arrayLength > 4)
            {
                dependsOn = new MergeSortedPairs<T> { 
                    buffer = this.m_Buffer,
                    source = this.m_Source,
                    sortedCount = sortedCount,
                    outputBuffer = outputBuffer
                }.Schedule<MergeSortedPairs<T>>(arrayLength, (arrayLength + 1) / 8, inputDeps);
            }
            else
            {
                int num3 = 0;
                while (true)
                {
                    if (num3 >= arrayLength)
                    {
                        break;
                    }
                    MergeLeft<T> jobData = new MergeLeft<T> {
                        startIndex = (num3 * sortedCount) * 2,
                        buffer = this.m_Buffer,
                        source = this.m_Source,
                        leftCount = sortedCount,
                        rightCount = sortedCount,
                        outputBuffer = outputBuffer
                    };
                    dependsOn = jobData.Schedule<MergeLeft<T>>(sortedCount, 0x40, dependsOn);
                    MergeRight<T> right2 = new MergeRight<T> {
                        startIndex = (num3 * sortedCount) * 2,
                        buffer = this.m_Buffer,
                        source = this.m_Source,
                        leftCount = sortedCount,
                        rightCount = sortedCount,
                        outputBuffer = outputBuffer
                    };
                    dependsOn = right2.Schedule<MergeRight<T>>(sortedCount, 0x40, dependsOn);
                    num3++;
                }
            }
            int num2 = this.m_Source.Length - ((arrayLength * sortedCount) * 2);
            if (num2 <= sortedCount)
            {
                if (num2 <= 0)
                {
                    handle4 = dependsOn;
                }
                else
                {
                    handle4 = new CopyRemainder<T> { 
                        startIndex = (arrayLength * sortedCount) * 2,
                        buffer = this.m_Buffer,
                        source = this.m_Source,
                        outputBuffer = outputBuffer
                    }.Schedule<CopyRemainder<T>>(num2, (arrayLength + 1) / 8, dependsOn);
                }
            }
            else
            {
                JobHandle handle2 = new MergeLeft<T> { 
                    startIndex = (arrayLength * sortedCount) * 2,
                    buffer = this.m_Buffer,
                    source = this.m_Source,
                    leftCount = sortedCount,
                    rightCount = num2 - sortedCount,
                    outputBuffer = outputBuffer
                }.Schedule<MergeLeft<T>>(sortedCount, 0x40, dependsOn);
                handle4 = new MergeRight<T> { 
                    startIndex = (arrayLength * sortedCount) * 2,
                    buffer = this.m_Buffer,
                    source = this.m_Source,
                    leftCount = sortedCount,
                    rightCount = num2 - sortedCount,
                    outputBuffer = outputBuffer
                }.Schedule<MergeRight<T>>(num2 - sortedCount, 0x40, handle2);
            }
            return handle4;
        }

        private JobHandle Sort(JobHandle inputDeps)
        {
            int sortedCount = 1;
            int outputBuffer = 1;
            while (true)
            {
                inputDeps = this.MergeSortedLists(inputDeps, sortedCount, outputBuffer);
                sortedCount *= 2;
                outputBuffer ^= 1;
                NativeArray<T> source = this.m_Source;
                if (sortedCount >= source.Length)
                {
                    this.m_SortedBuffer = outputBuffer ^ 1;
                    return inputDeps;
                }
            }
        }

        private JobHandle ResolveSharedGroups(JobHandle inputDeps)
        {
            AssignSharedValues<T> jobData = new AssignSharedValues<T> {
                buffer = this.m_Buffer,
                source = this.m_Source,
                sortedBuffer = this.m_SortedBuffer
            };
            return jobData.Schedule<AssignSharedValues<T>>(inputDeps);
        }

        public JobHandle Schedule(JobHandle inputDeps)
        {
            JobHandle handle4;
            if (this.m_Source.Length <= 1)
            {
                handle4 = inputDeps;
            }
            else
            {
                JobHandle handle = new InitializeIndices<T> { buffer = this.m_Buffer }.Schedule<InitializeIndices<T>>(this.m_Source.Length, (this.m_Source.Length + 1) / 8, inputDeps);
                JobHandle handle2 = this.Sort(handle);
                handle4 = this.ResolveSharedGroups(handle2);
            }
            return handle4;
        }

        public unsafe NativeArray<int> GetSortedIndices()
        {
            NativeArray<int> arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(this.m_Buffer.GetUnsafeReadOnlyPtr<int>() + ((this.m_SortedBuffer * this.m_Source.Length) * 4), this.m_Source.Length, Allocator.Invalid);
            this.SortedIndicesSetSafetyHandle(ref arr);
            return arr;
        }

        private void SortedIndicesSetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<int>(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle<int>(this.m_Buffer));
        }

        public int SharedValueCount =>
            this.m_Buffer[this.m_Source.Length * 4];
        public int GetSharedIndexBySourceIndex(int index)
        {
            int num = (this.m_SortedBuffer ^ 1) * this.m_Source.Length;
            return this.m_Buffer[num + index];
        }

        public unsafe NativeArray<int> GetSharedIndexArray()
        {
            NativeArray<int> arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<int>(this.m_Buffer) + (((this.m_SortedBuffer ^ 1) * this.m_Source.Length) * 4), this.m_Source.Length, Allocator.Invalid);
            this.SharedIndexArraySetSafetyHandle(ref arr);
            return arr;
        }

        private void SharedIndexArraySetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<int>(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle<int>(this.m_Buffer));
        }

        public NativeArray<int> GetSharedValueIndicesBySourceIndex(int index)
        {
            int num = (this.m_SortedBuffer ^ 1) * this.m_Source.Length;
            int num2 = this.m_Buffer[num + index];
            return this.GetSharedValueIndicesBySharedIndex(num2);
        }

        public int GetSharedValueIndexCountBySourceIndex(int index)
        {
            int num2 = 2 * this.m_Source.Length;
            return this.m_Buffer[num2 + this.GetSharedIndexBySourceIndex(index)];
        }

        public unsafe NativeArray<int> GetSharedValueIndexCountArray()
        {
            NativeArray<int> arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<int>(this.m_Buffer) + ((2 * this.m_Source.Length) * 4), this.m_Source.Length, Allocator.Invalid);
            this.SharedValueIndexCountArraySetSafetyHandle(ref arr);
            return arr;
        }

        private void SharedValueIndexCountArraySetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<int>(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle<int>(this.m_Buffer));
        }

        public unsafe NativeArray<int> GetSharedValueIndicesBySharedIndex(int index)
        {
            int num = 2 * this.m_Source.Length;
            int num3 = 3 * this.m_Source.Length;
            int num5 = this.m_SortedBuffer * this.m_Source.Length;
            NativeArray<int> arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<int>(this.m_Buffer) + ((num5 + this.m_Buffer[num3 + index]) * 4), this.m_Buffer[num + index], Allocator.Invalid);
            this.SharedValueIndicesSetSafetyHandle(ref arr);
            return arr;
        }

        private void SharedValueIndicesSetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<int>(ref arr, NativeArrayUnsafeUtility.GetAtomicSafetyHandle<int>(this.m_Buffer));
        }

        public NativeArray<int> GetBuffer() => 
            this.m_Buffer;

        public void Dispose()
        {
            this.m_Buffer.Dispose();
        }
        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct AssignSharedValues : IJob
        {
            public NativeArray<int> buffer;
            [ReadOnly]
            public NativeArray<T> source;
            public int sortedBuffer;
            public unsafe void Execute()
            {
                int num = this.sortedBuffer * this.source.Length;
                int num2 = (this.sortedBuffer ^ 1) * this.source.Length;
                int num3 = 2 * this.source.Length;
                int num4 = 3 * this.source.Length;
                int index = 4 * this.source.Length;
                int num6 = 0;
                int num7 = this.buffer[num + num6];
                T other = this.source[num7];
                int num8 = 1;
                this.buffer.set_Item(num2 + num7, 0);
                this.buffer.set_Item(num4 + (num8 - 1), num6);
                this.buffer.set_Item(num3 + (num8 - 1), 1);
                num6++;
                while (true)
                {
                    if (num6 >= this.source.Length)
                    {
                        this.buffer.set_Item(index, num8);
                        return;
                    }
                    num7 = this.buffer[num + num6];
                    T local2 = this.source[num7];
                    if (local2.CompareTo(other) == 0)
                    {
                        int num9 = num3 + (num8 - 1);
                        NativeArray<int>* arrayPtr1 = (NativeArray<int>*) ref this.buffer;
                        arrayPtr1.set_Item(num9, arrayPtr1[num9] + 1);
                        this.buffer.set_Item(num2 + num7, num8 - 1);
                    }
                    else
                    {
                        num8++;
                        other = local2;
                        this.buffer.set_Item(num4 + (num8 - 1), num6);
                        this.buffer.set_Item(num3 + (num8 - 1), 1);
                        this.buffer.set_Item(num2 + num7, num8 - 1);
                    }
                    num6++;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct CopyRemainder : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> buffer;
            [ReadOnly]
            public NativeArray<T> source;
            public int startIndex;
            public int outputBuffer;
            public void Execute(int index)
            {
                int num3 = ((this.outputBuffer * this.source.Length) + this.startIndex) + index;
                int num4 = (((this.outputBuffer ^ 1) * this.source.Length) + this.startIndex) + index;
                this.buffer.set_Item(num3, this.buffer[num4]);
            }
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct InitializeIndices : IJobParallelFor
        {
            public NativeArray<int> buffer;
            public void Execute(int index)
            {
                this.buffer.set_Item(index, index);
            }
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct MergeLeft : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> buffer;
            [ReadOnly]
            public NativeArray<T> source;
            public int leftCount;
            public int rightCount;
            public int startIndex;
            public int outputBuffer;
            private int FindInsertNext(int startOffset, int minNext, int maxNext, T testValue)
            {
                int num4;
                if (minNext == maxNext)
                {
                    int num2 = this.buffer[startOffset + minNext];
                    T other = this.source[num2];
                    if (testValue.CompareTo(other) <= 0)
                    {
                        num4 = minNext;
                    }
                    else
                    {
                        num4 = minNext + 1;
                    }
                }
                else
                {
                    int num = minNext + ((maxNext - minNext) / 2);
                    int num5 = this.buffer[startOffset + num];
                    T other = this.source[num5];
                    if (testValue.CompareTo(other) <= 0)
                    {
                        num4 = this.FindInsertNext(startOffset, minNext, math.max(num - 1, minNext), testValue);
                    }
                    else
                    {
                        num4 = this.FindInsertNext(startOffset, math.min(num + 1, maxNext), maxNext, testValue);
                    }
                }
                return num4;
            }

            public void Execute(int leftNext)
            {
                int num = (this.outputBuffer ^ 1) * this.source.Length;
                int num2 = this.outputBuffer * this.source.Length;
                int num3 = this.buffer[(num + this.startIndex) + leftNext];
                T testValue = this.source[num3];
                int num4 = this.FindInsertNext((num + this.startIndex) + this.leftCount, 0, this.rightCount - 1, testValue);
                int num5 = leftNext + num4;
                this.buffer.set_Item((num2 + this.startIndex) + num5, num3);
            }
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct MergeRight : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> buffer;
            [ReadOnly]
            public NativeArray<T> source;
            public int leftCount;
            public int rightCount;
            public int startIndex;
            public int outputBuffer;
            private int FindInsertNext(int startOffset, int minNext, int maxNext, T testValue)
            {
                int num4;
                if (minNext == maxNext)
                {
                    int num2 = this.buffer[startOffset + minNext];
                    T other = this.source[num2];
                    if (testValue.CompareTo(other) < 0)
                    {
                        num4 = minNext;
                    }
                    else
                    {
                        num4 = minNext + 1;
                    }
                }
                else
                {
                    int num = minNext + ((maxNext - minNext) / 2);
                    int num5 = this.buffer[startOffset + num];
                    T other = this.source[num5];
                    if (testValue.CompareTo(other) < 0)
                    {
                        num4 = this.FindInsertNext(startOffset, minNext, math.max(num - 1, minNext), testValue);
                    }
                    else
                    {
                        num4 = this.FindInsertNext(startOffset, math.min(num + 1, maxNext), maxNext, testValue);
                    }
                }
                return num4;
            }

            public void Execute(int rightNext)
            {
                int num = (this.outputBuffer ^ 1) * this.source.Length;
                int num2 = this.outputBuffer * this.source.Length;
                int num3 = this.buffer[((num + this.startIndex) + this.leftCount) + rightNext];
                T testValue = this.source[num3];
                int num4 = this.FindInsertNext(num + this.startIndex, 0, this.leftCount - 1, testValue);
                int num5 = rightNext + num4;
                this.buffer.set_Item((num2 + this.startIndex) + num5, num3);
            }
        }

        [StructLayout(LayoutKind.Sequential), BurstCompile]
        private struct MergeSortedPairs : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<int> buffer;
            [ReadOnly]
            public NativeArray<T> source;
            public int sortedCount;
            public int outputBuffer;
            public void Execute(int index)
            {
                int num = this.sortedCount * 2;
                int num2 = index * num;
                int num3 = (this.outputBuffer ^ 1) * this.source.Length;
                int num4 = this.outputBuffer * this.source.Length;
                int sortedCount = this.sortedCount;
                int num6 = this.sortedCount;
                int num7 = 0;
                int num8 = 0;
                for (int i = 0; i < num; i++)
                {
                    if ((num7 < sortedCount) && (num8 < num6))
                    {
                        int num10 = this.buffer[(num3 + num2) + num7];
                        int num11 = this.buffer[((num3 + num2) + sortedCount) + num8];
                        if (this.source[num11].CompareTo(this.source[num10]) < 0)
                        {
                            this.buffer.set_Item((num4 + num2) + i, num11);
                            num8++;
                        }
                        else
                        {
                            this.buffer.set_Item((num4 + num2) + i, num10);
                            num7++;
                        }
                    }
                    else if (num7 < sortedCount)
                    {
                        int num12 = this.buffer[(num3 + num2) + num7];
                        this.buffer.set_Item((num4 + num2) + i, num12);
                        num7++;
                    }
                    else
                    {
                        int num13 = this.buffer[((num3 + num2) + sortedCount) + num8];
                        this.buffer.set_Item((num4 + num2) + i, num13);
                        num8++;
                    }
                }
            }
        }
    }
}

