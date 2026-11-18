using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Samples
{
    /// <summary>
    /// A card in a shelf. It listens for its position in the carousel (IViewportPositionListener) and shrinks as it moves
    /// away from the centre.
    /// </summary>
    public class CardSlot : MonoBehaviour, ISlot<ExampleItem>, IViewportPositionListener
    {
        public const float WIDTH = 180f;
        private const float SMALLEST_SCALE = 0.8f;
        private const float EDGE = 0.5f;
        private const int NUMBER_FONT_SIZE = 48;

        [SerializeField] private Text number;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform face;

        private ExampleItem data;

        public float DefaultExtent => WIDTH;

        public ExampleItem Data
        {
            get => data;
            set
            {
                data = value;
                number.text = value.Id.ToString();
                background.color = value.Color;
            }
        }

        public void OnViewportPosition(float position)
        {
            float distance = Mathf.Clamp01(Mathf.Abs(position) / EDGE);
            float scale = Mathf.Lerp(1f, SMALLEST_SCALE, distance);
            face.localScale = new Vector3(scale, scale, 1f);
        }

        public static CardSlot CreateTemplate(Transform templateRoot)
        {
            // The list sizes the slot's root; the face inside it is what scales.
            RectTransform root = ExampleUI.CreateRect(nameof(CardSlot), templateRoot);
            CardSlot slot = root.gameObject.AddComponent<CardSlot>();
            slot.face = ExampleUI.CreateRect("Face", root);
            ExampleUI.Stretch(slot.face);
            slot.background = slot.face.gameObject.AddComponent<Image>();
            slot.number = ExampleUI.CreateText("Number", slot.face, NUMBER_FONT_SIZE, TextAnchor.MiddleCenter, ExampleUI.TextColor);
            return slot;
        }
    }
}
