using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;

public class CampManager : MonoBehaviour
{
    private static readonly int NUM_OF_SLOTS = 3;
    private bool _isActionEnabled;
    private List<MarkerHolder> _markerHolders;

    public void CreateCamp()
    {
        _markerHolders = new();
        Transform markerDisplayHolders = transform.GetChild(0);
        for(int i = 0; i < NUM_OF_SLOTS; i++)
        {
            MarkerHolder markerHolder = markerDisplayHolders.GetChild(i).GetComponent<MarkerHolder>();
            markerHolder.Init(i, HolderType.CampMarker);
            _markerHolders.Add(markerHolder);
        }
    }

    public void ToggleMarkerHolders(bool value)
    {
        _markerHolders.ForEach(holder => holder.ToggleRayCast(value));
    }

    public void EnableCampAction(bool value)
    {
        _isActionEnabled = value;
    }
}
