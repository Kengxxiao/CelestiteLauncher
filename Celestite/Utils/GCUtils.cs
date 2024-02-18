using System;
using System.Runtime;

namespace Celestite.Utils
{
    public static class GCUtils
    {
        public static void CollectGeneration2()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Aggressive);
        }
    }
}
