using System;
using UnityEngine;

namespace Tools.RecyclableScroller.Abstractions.Scripts
{
    /// <summary>
    /// Provides normalized viewport metrics (0-1 range).
    /// Framework-agnostic interface for querying scroll state and dimensions.
    /// All position values are normalized (0 = start, 1 = end).
    /// </summary>
    public interface IViewportProvider
    {
        /// <summary>
        /// Get normalized scroll position in primary axis (0 = start, 1 = end).
        /// For vertical scrolling: 0 = top, 1 = bottom.
        /// For horizontal scrolling: 0 = left, 1 = right.
        /// </summary>
        float GetNormalizedScrollPosition();

        /// <summary>
        /// Get viewport size in primary axis (pixels/points).
        /// For vertical: height of visible area.
        /// For horizontal: width of visible area.
        /// </summary>
        float GetViewportSize();

        /// <summary>
        /// Get viewport size in cross axis (pixels/points).
        /// For vertical: width of visible area.
        /// For horizontal: height of visible area.
        /// </summary>
        float GetCrossAxisViewportSize();

        /// <summary>
        /// Get total content size in primary axis (pixels/points).
        /// Sum of all cell heights (vertical) or widths (horizontal).
        /// </summary>
        float GetContentSize();

        /// <summary>
        /// Absolute position of content container.
        /// May be negative if scrolled past start.
        /// X = horizontal offset, Y = vertical offset.
        /// </summary>
        Vector2 GetContentAnchoredPosition();

        /// <summary>
        /// Fired when viewport dimensions change (resize, orientation change, layout update).
        /// Listeners should invalidate caches.
        /// </summary>
        event Action OnViewportDimensionsChanged;
    }
}

