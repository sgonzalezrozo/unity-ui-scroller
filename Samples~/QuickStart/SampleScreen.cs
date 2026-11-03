using System.Collections.Generic;
using RecyclableScroller.Runtime;
using UnityEngine;

public class SampleScreen : MonoBehaviour
{
    [SerializeField] private SampleScroller scroller;

    private void Start()
    {
        List<SampleItem> items = new();
        for (int i = 0; i < 10000; i++)
            items.Add(new SampleItem { Name = $"Item {i}" });

        scroller.Initialize(new ListDataSource<SampleItem>(items));
    }
}
