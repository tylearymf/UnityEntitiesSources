namespace Unity.Assertions
{
    using System;
    using System.Diagnostics;
    using UnityEngine.Assertions;

    [DebuggerStepThrough]
    internal static class Assert
    {
        [Conditional("UNITY_ASSERTIONS")]
        public static void AreApproximatelyEqual(float expected, float actual)
        {
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected, actual);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreApproximatelyEqual(float expected, float actual, float tolerance)
        {
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected, actual, tolerance);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreApproximatelyEqual(float expected, float actual, string message)
        {
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected, actual, message);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreEqual(bool expected, bool actual)
        {
            if (expected != actual)
            {
                UnityEngine.Assertions.Assert.AreEqual<bool>(expected, actual);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreEqual(int expected, int actual)
        {
            if (expected != actual)
            {
                UnityEngine.Assertions.Assert.AreEqual(expected, actual);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreEqual<T>(T expected, T actual)
        {
            UnityEngine.Assertions.Assert.AreEqual<T>(expected, actual);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreNotEqual(bool expected, bool actual)
        {
            if (expected == actual)
            {
                UnityEngine.Assertions.Assert.AreNotEqual<bool>(expected, actual);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreNotEqual(int expected, int actual)
        {
            if (expected == actual)
            {
                UnityEngine.Assertions.Assert.AreNotEqual(expected, actual);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void AreNotEqual<T>(T expected, T actual)
        {
            UnityEngine.Assertions.Assert.AreNotEqual<T>(expected, actual);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsFalse(bool condition)
        {
            if (condition)
            {
                UnityEngine.Assertions.Assert.IsFalse(condition);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsFalse(bool condition, string message)
        {
            if (condition)
            {
                UnityEngine.Assertions.Assert.IsFalse(condition, message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsTrue(bool condition)
        {
            if (!condition)
            {
                UnityEngine.Assertions.Assert.IsTrue(condition);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                UnityEngine.Assertions.Assert.IsTrue(condition, message);
            }
        }
    }
}

