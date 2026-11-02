using TMPro;
using UnityEngine;

namespace Tools.RecyclableScroller.Scripts
{
    public class EmptyStateDisplay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup emptyContainer;
        [SerializeField] private TextMeshProUGUI emptyLabel;

        private IRecyclableDataSource dataSource;
        private bool isCurrentlyEmpty;

        private const string DEFAULT_EMPTY_MESSAGE = "No items found";

        private void OnEnable()
        {
            RefreshDisplay();
        }

        public void SetDataSource<T>(IRecyclableDataSource<T> source) where T : class
        {
            if (source is null)
            {
                Debug.LogWarning("[EmptyStateDisplay] Attempted to set null data source");
                return;
            }

            dataSource = (IRecyclableDataSource)source;
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

            bool isEmpty = dataSource.ItemCount == 0;

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
    }
}

