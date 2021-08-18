using System.Collections;

namespace CrustMonitorNode
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty(this IEnumerable enumerable)
        {
            if (enumerable != null)
                return !enumerable.GetEnumerator().MoveNext();
            return true;
        }
    }
}