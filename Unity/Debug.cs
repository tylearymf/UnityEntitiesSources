namespace Unity
{
    using System;
    using UnityEngine;

    internal static class Debug
    {
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }

        public static void LogException(Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }

        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }
    }
}

