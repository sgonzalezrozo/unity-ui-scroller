using UnityEngine;

namespace SlotList
{
    public class SlotsPerLineResolver
    {
        private readonly uint configuredGridCount;
        private readonly bool useDynamic;
        private readonly float dynamicItemSize;
        private readonly float slotSpacing;
        private readonly uint minDynamicCount;
        private readonly uint maxDynamicCount;

        private int currentGridCount = 1;

        public int Current => Mathf.Max(1, currentGridCount);
        public bool IsGridMode => configuredGridCount > 1;
        public bool IsDynamic => IsGridMode && useDynamic;

        public SlotsPerLineResolver(uint configuredGridCount, bool useDynamic, float dynamicItemSize, float slotSpacing, uint minDynamicCount, uint maxDynamicCount)
        {
            this.configuredGridCount = configuredGridCount;
            this.useDynamic = useDynamic;
            this.dynamicItemSize = dynamicItemSize;
            this.slotSpacing = slotSpacing;
            this.minDynamicCount = minDynamicCount;
            this.maxDynamicCount = maxDynamicCount;
        }

        public int Resolve(float crossAxisViewportSize)
        {
            currentGridCount = Calculate(crossAxisViewportSize);
            return currentGridCount;
        }

        public int Calculate(float crossAxisViewportSize)
        {
            int fixedCount = Mathf.Max(1, (int)configuredGridCount);
            if (!IsDynamic)
                return fixedCount;

            if (dynamicItemSize <= 0f)
                return fixedCount;

            if (crossAxisViewportSize <= 0f)
                return fixedCount;

            float denominator = dynamicItemSize + slotSpacing;
            if (denominator <= 0f)
                return fixedCount;

            int minCount = Mathf.Max(1, (int)minDynamicCount);
            int maxCount = Mathf.Max(minCount, (int)maxDynamicCount);

            int calculated = Mathf.FloorToInt((crossAxisViewportSize + slotSpacing) / denominator);
            calculated = Mathf.Clamp(calculated, minCount, maxCount);

            return Mathf.Max(1, calculated);
        }
    }
}

