using UnityEngine;
using UnityEngine.UI;

namespace SlotList.Samples
{
    /// <summary>
    /// A list that loads 5 pages, one at a time, as you get near the end; the last page has only 3 items.
    /// Pull down past the top and release to start again from page 0.
    /// Add it to an empty GameObject in an empty scene and press Play.
    /// </summary>
    public class PagingExample : MonoBehaviour
    {
        private const float SPACING = 8f;

        private Text status;
        private PagingExampleList list;

        private void Start()
        {
            RectTransform body = ExampleUI.CreateScreen("Paging", out status);

            RowSlot template = RowSlot.CreateTemplate(ExampleUI.CreateTemplateRoot());
            list = ExampleUI.CreateList<PagingExampleList>(body, template.gameObject);
            list.Settings.lineSpacing = SPACING;
            list.PageRequested += OnPageRequested;
            list.PageLoaded += OnPageLoaded;
            list.PageFailed += OnPageFailed;
            list.ReachedEnd += OnReachedEnd;

            list.SetSource(new PagedSource<ExampleItem>(new ExamplePageLoader()));
        }

        private void OnPageRequested()
            => status.text = "Loading...";

        private void OnPageLoaded(int count)
            => status.text = $"{list.GetItemCount()} items";

        private void OnPageFailed(System.Exception exception)
            => status.text = exception.Message;

        private void OnReachedEnd()
            => status.text = $"{list.GetItemCount()} items, no more pages";
    }
}
