using UnityEngine;

namespace Icarus.Core
{
    public static class PlayerProgressState
    {
        private static int _featherCount;

        public static int FeatherCount => _featherCount;

        public static void SetFeatherCount(int featherCount)
        {
            _featherCount = Mathf.Max(0, featherCount);
        }

        public static void AddFeather()
        {
            _featherCount += 1;
        }
    }
}
