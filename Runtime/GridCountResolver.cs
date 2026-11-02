using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class GridCountResolver
    {
        private readonly uint configuredGridCount;
        private readonly bool useDynamic;
        private readonly float dynamicCellSize;
        private readonly float gridCellSpacing;
        private readonly uint minDynamicCount;
        private readonly uint maxDynamicCount;

        private int currentGridCount = 1;

        public int Current => Mathf.Max(1, currentGridCount);
        public bool IsGridMode => configuredGridCount > 1;
        public bool IsDynamic => IsGridMode && useDynamic;

        public GridCountResolver(uint configuredGridCount, bool useDynamic, float dynamicCellSize, float gridCellSpacing, uint minDynamicCount, uint maxDynamicCount)
        {
            this.configuredGridCount = configuredGridCount;
            this.useDynamic = useDynamic;
            this.dynamicCellSize = dynamicCellSize;
            this.gridCellSpacing = gridCellSpacing;
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

            if (dynamicCellSize <= 0f)
                return fixedCount;

            if (crossAxisViewportSize <= 0f)
                return fixedCount;

            float denominator = dynamicCellSize + gridCellSpacing;
            if (denominator <= 0f)
                return fixedCount;

            int minCount = Mathf.Max(1, (int)minDynamicCount);
            int maxCount = Mathf.Max(minCount, (int)maxDynamicCount);

            int calculated = Mathf.FloorToInt((crossAxisViewportSize + gridCellSpacing) / denominator);
            calculated = Mathf.Clamp(calculated, minCount, maxCount);

            return Mathf.Max(1, calculated);
        }
    }
}

