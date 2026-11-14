using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SlotList.Tests.PlayMode
{
    /// <summary>
    /// The Viewport is a nested Canvas (P18): moving slots rebuilds only the list's batches, not the whole screen's.
    /// A list set up without one gets it when it starts.
    /// </summary>
    public class ViewportCanvasTests
    {
        private SlotListRig rig;

        [TearDown]
        public void TearDown()
        {
            rig?.Dispose();
            rig = null;
        }

        [UnityTest]
        public IEnumerator Initialize_MakesTheViewportANestedCanvas_WithARaycaster()
        {
            rig = SlotListRig.Create();
            Assert.IsTrue(rig.Viewport.GetComponent<Canvas>() == null, "The rig starts without one.");

            yield return rig.Initialize(20);

            Canvas nested = rig.Viewport.GetComponent<Canvas>();
            Assert.IsTrue(nested != null);
            Assert.IsFalse(nested.isRootCanvas);
            Assert.IsTrue(rig.Viewport.GetComponent<GraphicRaycaster>() != null);
        }

        [UnityTest]
        public IEnumerator TheNestedCanvas_CopiesTheShaderChannelsOfTheCanvasAboveIt()
        {
            rig = SlotListRig.Create();
            Canvas root = rig.ScrollerRect.GetComponentInParent<Canvas>();
            root.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            yield return rig.Initialize(20);

            Assert.AreEqual(root.additionalShaderChannels, rig.Viewport.GetComponent<Canvas>().additionalShaderChannels);
        }

        [UnityTest]
        public IEnumerator TheRectMask2D_StillCullsWhatIsOutsideTheViewport()
        {
            rig = SlotListRig.Create();
            yield return rig.Initialize(20);

            Image inside = NewImage("Inside", new Vector2(100f, -100f));
            Image outside = NewImage("Outside", new Vector2(100f, -5000f));
            Canvas.ForceUpdateCanvases();
            yield return null;

            Assert.IsFalse(inside.canvasRenderer.cull, "A graphic inside the viewport must draw.");
            Assert.IsTrue(outside.canvasRenderer.cull, "A graphic outside the viewport must be culled by the RectMask2D.");
        }

        private Image NewImage(string name, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(rig.Content, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(50f, 50f);
            rect.anchoredPosition = position;
            return go.GetComponent<Image>();
        }
    }
}
