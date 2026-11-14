using System.Collections.Generic;
using ToolsCore;
using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// Creates and reuses slots, with one queue per slot kind. A returned slot always goes back to its own kind's queue.
    /// Unused slots wait under Content: moving a UI element to another parent rebuilds its graphics, so a slot never
    /// leaves Content. They stay active, parked far to the side of the list where the Viewport's RectMask2D culls them
    /// (not drawn, not clickable), so reusing one doesn't wake its graphics and scripts again (Phase 10, P6 option B:
    /// on a low-end phone it cut the list's own cost by about three quarters). A slot's OnEnable therefore doesn't run
    /// when it's reused: slots refresh when Data is set.
    /// </summary>
    public class SlotPool
    {
        /// <summary>
        /// Internal switch between the two ways of waiting, so the benchmark can compare them on a device: true (the
        /// default) parks returned slots active, false switches them off. Read when a slot is returned.
        /// </summary>
        internal static bool KeepPooledSlotsActive = true;

        // Across the scroll axis, far enough that no scrolling brings a parked slot into the viewport. An odd value that
        // an item's position never lands on exactly, so IsParked can tell a parked slot from an item far down the list.
        private const float PARK_POSITION = -1_000_000.5f;

        private readonly RectTransform contentTransform;
        private readonly IPrefabInstantiator instantiator;
        private readonly Dictionary<SlotKind, GameObject> prefabs = new();
        private readonly Dictionary<SlotKind, int> initialSizes = new();
        private readonly Dictionary<SlotKind, int> warmedUp = new();
        private readonly Dictionary<SlotKind, Queue<SlotHandle>> available = new();
        private readonly List<SlotHandle> active = new();
        private bool isPrewarmed;
        private readonly bool isVertical;

        public int ActiveCount => active.Count;
        public bool IsPrewarmed => isPrewarmed;

        public int AvailableCount
        {
            get
            {
                int count = 0;
                foreach (Queue<SlotHandle> queue in available.Values)
                    count += queue.Count;
                return count;
            }
        }

        public int TotalCount => ActiveCount + AvailableCount;

        public SlotPool(GameObject itemPrefab, RectTransform content, int poolSize, IPrefabInstantiator instantiator, GameObject headerPrefab = null, int headerPoolSize = 0, bool isVertical = true)
        {
            this.isVertical = isVertical;
            contentTransform = content;
            this.instantiator = instantiator ?? ToolInjectionServices.PrefabInstantiator;

            AddKind(SlotKind.Item, itemPrefab, poolSize);
            if (headerPrefab != null)
                AddKind(SlotKind.Header, headerPrefab, headerPoolSize);
        }

        /// <summary>Creates every remaining initial slot of every kind at once.</summary>
        public void PrewarmPoolDeferred()
            => PrewarmSome(int.MaxValue);

        /// <summary>Creates up to <paramref name="maxSlots"/> of the remaining initial slots. The list calls it a few per frame.</summary>
        public void PrewarmSome(int maxSlots)
        {
            if (isPrewarmed)
                return;

            int created = 0;
            bool remaining = false;
            foreach (KeyValuePair<SlotKind, int> entry in initialSizes)
            {
                warmedUp.TryGetValue(entry.Key, out int done);
                while (done < entry.Value && created < maxSlots)
                {
                    SlotHandle handle = Create(entry.Key);
                    done++;
                    created++;
                    if (handle != null)
                        Park(handle);
                }

                warmedUp[entry.Key] = done;
                remaining |= done < entry.Value;
            }

            isPrewarmed = !remaining;
        }

        /// <summary>Takes a slot of this kind from its queue, or creates one. Returns null if the kind has no prefab.</summary>
        public SlotHandle GetSlot(SlotKind kind = SlotKind.Item)
        {
            SlotHandle handle = TakeAvailable(kind) ?? Create(kind);
            if (handle == null)
                return null;

            // Only a slot someone else moved needs bringing back: re-parenting is expensive.
            if (handle.Rect.parent != contentTransform)
                handle.Rect.SetParent(contentTransform, worldPositionStays: false);
            active.Add(handle);
            return handle;
        }

        public void ReturnSlot(SlotHandle handle)
        {
            if (handle == null || !active.Remove(handle))
                return;

            if (handle.Rect == null)
                return;

            Park(handle);
        }

        public void ReturnAllSlots()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                ReturnSlot(active[i]);
        }

        public void DestroyAllSlots()
        {
            ReturnAllSlots();

            foreach (Queue<SlotHandle> queue in available.Values)
            {
                while (queue.Count > 0)
                {
                    SlotHandle handle = queue.Dequeue();
                    if (handle.Rect != null)
                        Object.DestroyImmediate(handle.GameObject);
                }
            }

            active.Clear();
        }

        private void AddKind(SlotKind kind, GameObject prefab, int initialSize)
        {
            prefabs[kind] = prefab;
            initialSizes[kind] = initialSize;
            available[kind] = new Queue<SlotHandle>();
        }

        private void Park(SlotHandle handle)
        {
            if (KeepPooledSlotsActive && handle.GameObject.activeSelf)
            {
                // To the side of the list (across the scroll axis), so scrolling never brings it back into view.
                Vector2 position = handle.Rect.anchoredPosition;
                if (isVertical)
                    position.x = PARK_POSITION;
                else
                    position.y = PARK_POSITION;
                handle.Rect.anchoredPosition = position;
            }
            else
            {
                handle.GameObject.SetActive(false);
            }

            available[handle.Kind].Enqueue(handle);
        }

        /// <summary>True for a slot parked by <see cref="KeepPooledSlotsActive"/>: active but out of view, waiting in the pool.</summary>
        internal static bool IsParked(RectTransform rect)
        {
            Vector2 position = rect.anchoredPosition;
            return position.x == PARK_POSITION || position.y == PARK_POSITION;
        }

        private SlotHandle TakeAvailable(SlotKind kind)
        {
            if (!available.TryGetValue(kind, out Queue<SlotHandle> queue))
                return null;

            int destroyed = 0;
            while (queue.Count > 0)
            {
                SlotHandle handle = queue.Dequeue();
                if (handle.Rect != null)
                {
                    if (destroyed > 0)
                        Debug.LogWarning($"[SlotPool] Skipped {destroyed} pooled {kind} slots that were destroyed outside the pool.");
                    return handle;
                }

                destroyed++;
            }

            if (destroyed > 0)
                Debug.LogWarning($"[SlotPool] Skipped {destroyed} pooled {kind} slots that were destroyed outside the pool.");
            return null;
        }

        private SlotHandle Create(SlotKind kind)
        {
            if (!prefabs.TryGetValue(kind, out GameObject prefab) || prefab == null || instantiator == null)
                return null;

            GameObject instance = instantiator.Instantiate(prefab, contentTransform);
            if (instance == null)
                return null;

            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
                return new SlotHandle(rect, kind);

            Debug.LogError("[SlotPool] The slot prefab has no RectTransform on its root.");
            Object.Destroy(instance);
            return null;
        }
    }
}
