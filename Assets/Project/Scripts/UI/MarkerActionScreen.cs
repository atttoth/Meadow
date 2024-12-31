using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class MarkerActionScreen : MonoBehaviour
{
    private Image _blackOverlay;
    private TextMeshProUGUI _selectText;
    private List<GameObject> _actionIconObjects;
    private Marker _currentMarker;

    public Marker GetCurrentMarker()
    {
        return _currentMarker;
    }

    public List<Button> Init()
    {
        int numOfActions = 4;
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;

        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _selectText = transform.GetChild(5).GetComponent<TextMeshProUGUI>();
        _actionIconObjects = new();
        for (int i = 0; i < numOfActions; i++)
        {
            GameObject item = transform.GetChild(1 + i).gameObject;
            item.transform.GetChild(0).GetComponent<Image>().sprite = atlas.GetSprite(i.ToString());
            _actionIconObjects.Add(item);
        }
        return _actionIconObjects.Select(item => item.GetComponent<Button>()).ToList();
    }

    public void ToggleScreen(Marker marker)
    {
        if(marker)
        {
            _currentMarker = marker;
            _blackOverlay.enabled = true;
            _selectText.enabled = true;
            if (_currentMarker.action == MarkerAction.DO_ANY)
            {
                List<float> positions = new() { -300f, -100f, 100f, 300f };
                _actionIconObjects.ForEach(item =>
                {
                    item.GetComponent<RectTransform>().anchoredPosition = new(positions.First(), 200f);
                    positions.RemoveAt(0);
                    item.SetActive(true);
                });
            }
            else
            {
                GameObject item = _actionIconObjects.Find(item => item.CompareTag(_currentMarker.action.ToString()));
                item.GetComponent<RectTransform>().anchoredPosition = new(0f, 200f);
                item.SetActive(true);
            }
            _currentMarker.Parent = _currentMarker.transform.parent;
            _currentMarker.transform.SetParent(transform.root);
        }
        else
        {
            _actionIconObjects.ForEach(item => item.SetActive(false));
            _blackOverlay.enabled = false;
            _selectText.enabled = false;
            _currentMarker.transform.SetParent(_currentMarker.Parent);
            _currentMarker.Parent = null;
            _currentMarker = null;
        }
    }
}
