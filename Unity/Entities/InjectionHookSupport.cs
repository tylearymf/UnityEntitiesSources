namespace Unity.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class InjectionHookSupport
    {
        private static bool s_HasHooks;
        private static readonly List<InjectionHook> k_Hooks = new List<InjectionHook>();

        internal static InjectionHook HookFor(FieldInfo fieldInfo)
        {
            InjectionHook hook;
            if (s_HasHooks)
            {
                using (List<InjectionHook>.Enumerator enumerator = k_Hooks.GetEnumerator())
                {
                    while (true)
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                        InjectionHook current = enumerator.Current;
                        if (current.IsInterestedInField(fieldInfo))
                        {
                            return current;
                        }
                    }
                }
                hook = null;
            }
            else
            {
                hook = null;
            }
            return hook;
        }

        public static bool IsValidHook(Type type)
        {
            bool flag2;
            if (type.IsAbstract)
            {
                flag2 = false;
            }
            else if (type.ContainsGenericParameters)
            {
                flag2 = false;
            }
            else if (!typeof(InjectionHook).IsAssignableFrom(type))
            {
                flag2 = false;
            }
            else
            {
                flag2 = type.GetCustomAttributes(typeof(CustomInjectionHookAttribute), true).Length != 0;
            }
            return flag2;
        }

        public static void RegisterHook(InjectionHook hook)
        {
            s_HasHooks = true;
            k_Hooks.Add(hook);
        }

        public static void UnregisterHook(InjectionHook hook)
        {
            k_Hooks.Remove(hook);
            s_HasHooks = k_Hooks.Count != 0;
        }

        internal static IReadOnlyCollection<InjectionHook> Hooks =>
            k_Hooks;
    }
}

