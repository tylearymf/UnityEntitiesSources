namespace Unity.Entities
{
    using System;

    public static class ChangeVersionUtility
    {
        public const int InitialGlobalSystemVersion = 1;

        public static bool DidAddOrChange(uint changeVersion, uint requiredVersion)
        {
            bool flag2;
            if (changeVersion == 0)
            {
                flag2 = true;
            }
            else if (requiredVersion == 0)
            {
                flag2 = true;
            }
            else
            {
                flag2 = (changeVersion - requiredVersion) > 0;
            }
            return flag2;
        }

        public static bool DidChange(uint changeVersion, uint requiredVersion)
        {
            bool flag2;
            if (changeVersion == 0)
            {
                flag2 = false;
            }
            else if (requiredVersion == 0)
            {
                flag2 = true;
            }
            else
            {
                flag2 = (changeVersion - requiredVersion) > 0;
            }
            return flag2;
        }

        public static void IncrementGlobalSystemVersion(ref uint globalSystemVersion)
        {
            globalSystemVersion++;
            if (globalSystemVersion == 0)
            {
                globalSystemVersion++;
            }
        }
    }
}

