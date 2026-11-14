using SlotList;
using TMPro;
using UnityEngine;

public class SampleSlot : MonoBehaviour, ISlot<SampleItem>
{
    [SerializeField] private TMP_Text label;

    private SampleItem data;

    public float DefaultExtent => 80f;

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
