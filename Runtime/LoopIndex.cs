using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// Index and distance maths for a looping list. The engine lays out a few copies of the items one after another
    /// (virtual indices) and shows <c>virtual % count</c>. When the scroll nears either end of the copies, it jumps by
    /// whole copies, which looks the same, so it never reaches an end and never scrolls far enough to lose float precision.
    /// </summary>
    internal static class LoopIndex
    {
        public const int MIN_COPIES = 4;

        /// <summary>The real item a virtual index shows, including negative indices.</summary>
        public static int Real(int virtualIndex, int count)
        {
            if (count <= 0)
                return -1;

            int real = virtualIndex % count;
            return real < 0 ? real + count : real;
        }

        /// <summary>The copy of <paramref name="real"/> closest to <paramref name="around"/>: the shortest way round.</summary>
        public static int NearestCopy(int real, int count, int around)
        {
            if (count <= 0)
                return real;

            int offset = around - real;
            int copies = Mathf.FloorToInt((offset + count * 0.5f) / count);
            return real + copies * count;
        }

        /// <summary>
        /// How many copies to lay out: enough that after a jump of one copy the viewport is still inside the copies.
        /// <paramref name="period"/> is the size of one copy (all items with their spacing).
        /// </summary>
        public static int Copies(float viewportSize, float period)
        {
            if (period <= 0f)
                return MIN_COPIES;

            return Mathf.Max(MIN_COPIES, Mathf.CeilToInt(viewportSize / period) + 3);
        }

        /// <summary>
        /// How far to jump (a whole number of copies) so the viewport stays between the end of the first copy and the
        /// start of the last. 0 when it's already there. <paramref name="start"/> is where the first copy starts.
        /// </summary>
        public static float Recentre(float distance, float viewportSize, float start, float period, int copies)
        {
            if (period <= 0f || copies < MIN_COPIES)
                return 0f;

            float low = start + period;
            float high = start + (copies - 1) * period - viewportSize;
            if (high < low)
                return 0f;

            float shift = 0f;
            if (distance < low)
                shift = Mathf.Ceil((low - distance) / period) * period;
            else if (distance > high)
                shift = -Mathf.Ceil((distance - high) / period) * period;

            return shift;
        }
    }
}
