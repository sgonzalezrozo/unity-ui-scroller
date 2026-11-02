using Tools.RecyclableScroller.Core.Scripts.Pooling;
using UnityEngine;

namespace Tools.RecyclableScroller.Core.Scripts.Algorithms
{
    public class LineContentSizeCalculator
    {
        public float CalculateTotalSize(CellLine[] cellLines, float paddingStart, float paddingEnd, float lineSpacing)
        {
            if (cellLines == null || cellLines.Length == 0)
                return 0f;

            float linesSize = 0f;
            float spacingTotal = 0f;

            for (int i = 0; i < cellLines.Length; i++)
            {
                linesSize += cellLines[i].Size;

                if (i < cellLines.Length - 1)
                    spacingTotal += lineSpacing;
            }

            return paddingStart + linesSize + spacingTotal + paddingEnd;
        }

        public void ApplyToContent(RectTransform contentTransform, float size, bool isVertical)
        {
            if (contentTransform == null)
                return;

            Vector2 sizeDelta = contentTransform.sizeDelta;
            Vector2 newSizeDelta = sizeDelta;

            if (isVertical)
                newSizeDelta.y = size;
            else
                newSizeDelta.x = size;

            if (newSizeDelta != sizeDelta)
                contentTransform.sizeDelta = newSizeDelta;
        }
    }
}

