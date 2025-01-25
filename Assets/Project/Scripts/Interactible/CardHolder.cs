using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public enum HolderSubType
{
    NONE,
    PRIMARY,
    SECONDARY
}

public class CardHolder : Holder
{
    public HolderSubType holderSubType;
    private Image _blackOverlay;

    public override void Init(int id, HolderType type)
    {
        base.Init(id, type);
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
    }

    public List<CardIcon> GetAllIconsOfHolder()
    {
        List<CardIcon> allIcons = ((Card)GetItemFromContentListByIndex(_contentList.Count - 1)).Data.icons.ToList();
        if (_contentList.Count > 1 && holderSubType == HolderSubType.PRIMARY)
        {
            CardIcon[] groundIcons = ((Card)GetItemFromContentListByIndex(0)).Data.icons;

            foreach (CardIcon cardIcon in groundIcons)
            {
                if ((int)cardIcon < 5)
                {
                    allIcons.Add(cardIcon);
                }
            }
        }

        return allIcons;
    }

    public void EnableOverlay(bool value)
    {
        _blackOverlay.transform.SetAsLastSibling();
        if (_blackOverlay.enabled != value)
        {
            _blackOverlay.enabled = value;
        }
    }
}
