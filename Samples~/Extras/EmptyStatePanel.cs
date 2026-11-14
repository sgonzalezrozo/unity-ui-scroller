using SlotList;
using TMPro;
using UnityEngine;

namespace SlotList.Samples.Extras
{
    /// <summary>Shows a message when a data source is empty. Call OnDataSourceChanged after you change the source.</summary>
    public class EmptyStatePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup emptyContainer;
        [SerializeField] private TextMeshProUGUI emptyLabel;

        private IItemCounter dataSource;
        private bool isCurrentlyEmpty;

        private const string DEFAULT_EMPTY_MESSAGE = "No items found";

        private void OnEnable()
        {
            RefreshDisplay();
        }

        public void SetSource<T>(IItemSource<T> source) where T : class
        {
            if (source is null)
            {
                Debug.LogWarning("[EmptyStatePanel] Attempted to set null data source");
                return;
            }

            dataSource = new ItemCounter<T>(source);
            RefreshDisplay();
        }

        public void SetEmptyMessage(string message)
        {
            if (emptyLabel is null)
                return;

            if (string.IsNullOrEmpty(message))
                message = DEFAULT_EMPTY_MESSAGE;

            emptyLabel.text = message;
        }

        public void OnDataSourceChanged()
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (emptyContainer is null || dataSource is null)
                return;

            bool isEmpty = dataSource.Count == 0;

            if (isEmpty == isCurrentlyEmpty)
                return;

            isCurrentlyEmpty = isEmpty;
            emptyContainer.alpha = isEmpty ? 1f : 0f;
            emptyContainer.blocksRaycasts = isEmpty;

            if (isEmpty && emptyLabel is not null && string.IsNullOrEmpty(emptyLabel.text))
                emptyLabel.text = DEFAULT_EMPTY_MESSAGE;
        }

        private void OnDestroy()
        {
            dataSource = null;
        }

        private interface IItemCounter
        {
            int Count { get; }
        }

        private sealed class ItemCounter<T> : IItemCounter where T : class
        {
            private readonly IItemSource<T> source;

            public ItemCounter(IItemSource<T> source)
            {
                this.source = source;
            }

            public int Count => source.ItemCount;
        }
    }
}

