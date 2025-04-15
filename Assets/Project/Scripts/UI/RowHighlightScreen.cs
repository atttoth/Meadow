using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class RowHighlightScreen : MonoBehaviour
{
    private ScreenDisplayItem _highlightFrame;

    public Button Init()
    {
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        _highlightFrame = transform.GetChild(0).GetComponent<ScreenDisplayItem>();
        _highlightFrame.Init();
        _highlightFrame.mainImage.sprite = atlas.GetSprite("highlightFrame");
        _highlightFrame.gameObject.SetActive(false);
        return _highlightFrame.button;
    }

    public void Toggle(float posY)
    {
        if(posY == 0f)
        {
            _highlightFrame.gameObject.SetActive(false);
        }
        else
        {
            RectTransform rect = _highlightFrame.GetComponent<RectTransform>();
            Vector2 prevPos = rect.position;
            prevPos.y = posY;
            rect.position = prevPos;
            _highlightFrame.gameObject.SetActive(true);
        }
    }
}
