using NUnit.Framework;
using UIScroller.Editor;
using UnityEngine;

namespace UIScroller.Tests.Editor
{
    /// <summary>The bento layout editor writes JSON the list reads back, starting from a valid layout.</summary>
    public class BentoLayoutWindowTests
    {
        [Test]
        public void TheStartingLayout_IsValid_AndRoundTripsThroughJson()
        {
            string json = JsonUtility.ToJson(BentoLayoutWindow.StartingLayout(), true);

            BentoLayout layout = BentoLayout.FromJson(json);

            Assert.IsTrue(layout.IsValid, string.Join("\n", layout.Errors));
            Assert.AreEqual(4, layout.Columns);
            Assert.AreEqual(2, layout.MaxRowSpan);
        }

        [Test]
        public void OpeningTheWindow_WithALayout_DoesNotThrow()
        {
            TextAsset json = new(JsonUtility.ToJson(BentoLayoutWindow.StartingLayout()));

            BentoLayoutWindow.Open(json);
            BentoLayoutWindow window = EditorWindowFor();
            Assert.IsNotNull(window);
            window.Close();
        }

        private static BentoLayoutWindow EditorWindowFor()
            => UnityEditor.EditorWindow.GetWindow<BentoLayoutWindow>();
    }
}
