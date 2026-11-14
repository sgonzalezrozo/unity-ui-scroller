using NUnit.Framework;
using ToolsCore;
using UnityEngine;

namespace SlotList.Tests.PlayMode
{
    /// <summary>Design tests for SlotPool and SlotHandle (Phase 5, step 4): one queue per slot kind, components cached once.</summary>
    public class SlotPoolTests
    {
        private GameObject templates;
        private GameObject itemTemplate;
        private GameObject headerTemplate;
        private GameObject expandableTemplate;
        private RectTransform content;
        private SlotPool pool;

        [SetUp]
        public void SetUp()
        {
            // Most of these pin the switched-off way of waiting; the parked way has its own tests below.
            SlotPool.KeepPooledSlotsActive = false;
            ToolInjectionServices.PrefabInstantiator = null;
            templates = new GameObject("PoolTestTemplates");
            templates.SetActive(false);

            itemTemplate = NewTemplate("Item");
            itemTemplate.AddComponent<RecordingSlot>();
            itemTemplate.AddComponent<TestAppearAnimation>();

            headerTemplate = NewTemplate("Header");
            headerTemplate.AddComponent<RecordingSlot>();

            expandableTemplate = NewTemplate("Expandable");
            expandableTemplate.AddComponent<ExpandableRecordingSlot>();

            content = (RectTransform)new GameObject("PoolTestContent", typeof(RectTransform)).transform;
        }

        [TearDown]
        public void TearDown()
        {
            SlotPool.KeepPooledSlotsActive = true;
            pool?.DestroyAllSlots();
            pool = null;
            Object.DestroyImmediate(templates);
            Object.DestroyImmediate(content.gameObject);
        }

        private GameObject NewTemplate(string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(templates.transform, false);
            return go;
        }

        private SlotPool NewPool(GameObject item, int size = 0, GameObject header = null, int headerSize = 0)
            => new(item, content, size, new NullPrefabInstantiator(), header, headerSize);

        [Test]
        public void GetSlot_Item_UsesTheItemPrefab_UnderContent()
        {
            pool = NewPool(itemTemplate, header: headerTemplate);

            SlotHandle handle = pool.GetSlot(SlotKind.Item);

            Assert.AreEqual(SlotKind.Item, handle.Kind);
            Assert.AreEqual("Item(Clone)", handle.Rect.name);
            Assert.AreEqual(content, handle.Rect.parent);
            Assert.AreEqual(1, pool.ActiveCount);
        }

        [Test]
        public void GetSlot_Header_UsesTheHeaderPrefab()
        {
            pool = NewPool(itemTemplate, header: headerTemplate);

            SlotHandle handle = pool.GetSlot(SlotKind.Header);

            Assert.AreEqual(SlotKind.Header, handle.Kind);
            Assert.AreEqual("Header(Clone)", handle.Rect.name);
        }

        [Test]
        public void ReturnedItem_IsReused()
        {
            pool = NewPool(itemTemplate);
            SlotHandle first = pool.GetSlot(SlotKind.Item);

            pool.ReturnSlot(first);
            SlotHandle second = pool.GetSlot(SlotKind.Item);

            Assert.AreSame(first, second);
        }

        [Test]
        public void ReturnedHeader_GoesBackToTheHeaderQueue_NotTheItemQueue()
        {
            pool = NewPool(itemTemplate, header: headerTemplate);
            SlotHandle header = pool.GetSlot(SlotKind.Header);

            pool.ReturnSlot(header);
            SlotHandle item = pool.GetSlot(SlotKind.Item);
            SlotHandle headerAgain = pool.GetSlot(SlotKind.Header);

            Assert.AreNotSame(header, item);
            Assert.AreEqual("Item(Clone)", item.Rect.name);
            Assert.AreSame(header, headerAgain);
        }

        [Test]
        public void Handle_CachesTheSlotComponents()
        {
            pool = NewPool(itemTemplate);

            SlotHandle handle = pool.GetSlot(SlotKind.Item);

            Assert.IsInstanceOf<RecordingSlot>(handle.Slot);
            Assert.IsInstanceOf<TestAppearAnimation>(handle.Appear);
            Assert.IsNull(handle.Expandable);
        }

        [Test]
        public void Handle_ForAnExpandablePrefab_HasTheExpandableComponent()
        {
            pool = NewPool(expandableTemplate);

            SlotHandle handle = pool.GetSlot(SlotKind.Item);

            Assert.IsInstanceOf<ExpandableRecordingSlot>(handle.Expandable);
            Assert.AreSame(handle.Slot, handle.Expandable);
            Assert.IsNull(handle.Appear);
        }

        [Test]
        public void Prewarm_FillsEachKindsQueue()
        {
            pool = NewPool(itemTemplate, 3, headerTemplate, 2);

            pool.PrewarmPoolDeferred();

            Assert.IsTrue(pool.IsPrewarmed);
            Assert.AreEqual(5, pool.AvailableCount);
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual("Header(Clone)", pool.GetSlot(SlotKind.Header).Rect.name);
        }

        [Test]
        public void ReturnAllSlots_ReturnsEveryActiveSlot()
        {
            pool = NewPool(itemTemplate, header: headerTemplate);
            pool.GetSlot(SlotKind.Item);
            pool.GetSlot(SlotKind.Header);

            pool.ReturnAllSlots();

            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(2, pool.AvailableCount);
        }

        [Test]
        public void ReturnSlot_DeactivatesTheSlot_AndKeepsItUnderContent()
        {
            pool = NewPool(itemTemplate);
            SlotHandle handle = pool.GetSlot(SlotKind.Item);

            pool.ReturnSlot(handle);

            Assert.IsFalse(handle.Rect.gameObject.activeSelf);
            Assert.AreEqual(content, handle.Rect.parent);
        }

        [Test]
        public void PrewarmedSlots_WaitUnderContent()
        {
            pool = NewPool(itemTemplate, 3);

            pool.PrewarmPoolDeferred();

            Assert.AreEqual(3, content.childCount);
            foreach (Transform child in content)
                Assert.IsFalse(child.gameObject.activeSelf);
        }

        [Test]
        public void GetSlot_BringsBackASlotSomeoneMovedElsewhere()
        {
            pool = NewPool(itemTemplate);
            SlotHandle handle = pool.GetSlot(SlotKind.Item);
            pool.ReturnSlot(handle);
            handle.Rect.SetParent(templates.transform, false);

            SlotHandle again = pool.GetSlot(SlotKind.Item);

            Assert.AreSame(handle, again);
            Assert.AreEqual(content, again.Rect.parent);
        }

        [Test]
        public void DestroyAllSlots_DestroysEverySlot()
        {
            pool = NewPool(itemTemplate, 2);
            pool.PrewarmPoolDeferred();
            SlotHandle active = pool.GetSlot(SlotKind.Item);
            RectTransform activeRect = active.Rect;

            pool.DestroyAllSlots();
            pool = null;

            Assert.IsTrue(activeRect == null);
            Assert.AreEqual(0, content.childCount);
        }

        [Test]
        public void KeptActive_AReturnedSlotStaysActive_UnderContent_ParkedOutOfView()
        {
            SlotPool.KeepPooledSlotsActive = true;
            pool = NewPool(itemTemplate);
            SlotHandle handle = pool.GetSlot(SlotKind.Item);

            pool.ReturnSlot(handle);

            Assert.IsTrue(handle.Rect.gameObject.activeSelf);
            Assert.AreEqual(content, handle.Rect.parent);
            Assert.IsTrue(SlotPool.IsParked(handle.Rect));
        }

        [Test]
        public void KeptActive_AReusedSlotIsTheSameOne()
        {
            SlotPool.KeepPooledSlotsActive = true;
            pool = NewPool(itemTemplate);
            SlotHandle handle = pool.GetSlot(SlotKind.Item);
            pool.ReturnSlot(handle);

            Assert.AreSame(handle, pool.GetSlot(SlotKind.Item));
        }
    }
}
