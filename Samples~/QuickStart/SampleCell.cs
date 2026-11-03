using RecyclableScroller.Runtime;
using TMPro;
using UnityEngine;

public class SampleCell : MonoBehaviour, IRecyclableCell<SampleItem>
{
    [SerializeField] private TMP_Text label;

    private SampleItem data;

    public float DefaultCellSize => 80f;

    public SampleItem Data
    {
        get => data;
        set
        {
            data = value;
            label.text = value.Name;
        }
    }
}
