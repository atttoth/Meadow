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
    private Image _blackOverlay;

    public override void Init(int id, HolderType type)
    {
        base.Init(id, type);
        if(_blackOverlay == null)
        {
            _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        }
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
