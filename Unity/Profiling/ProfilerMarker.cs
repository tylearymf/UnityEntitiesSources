namespace Unity.Profiling
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using UnityEngine;

    [StructLayout(LayoutKind.Sequential, Size=1)]
    public struct ProfilerMarker
    {
        public ProfilerMarker(string name)
        {
        }

        [Conditional("ENABLE_PROFILER")]
        public void Begin()
        {
        }

        [Conditional("ENABLE_PROFILER")]
        public void Begin(UnityEngine.Object contextUnityObject)
        {
        }

        [Conditional("ENABLE_PROFILER")]
        public void End()
        {
        }

        public AutoScope Auto() => 
            new AutoScope();
        [StructLayout(LayoutKind.Sequential, Size=1)]
        public struct AutoScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}

