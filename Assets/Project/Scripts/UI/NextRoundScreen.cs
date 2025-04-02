using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NextRoundScreen : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Image _blackOverlay;
    private TextMeshProUGUI _text;

    public void Init()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _text = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
    }

    public void StartShowHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                float fadeInDuration = ReferenceManager.Instance.gameLogicController.GameSettings.overlayScreenFadeDuration;
                _blackOverlay.enabled = true;
                _text.enabled = true;
                Fade(true, fadeInDuration);
                task.StartDelayMs((int)fadeInDuration * 1000);
                break;
            case 1:
                task.StartDelayMs(500);
                break;
            case 2:
                float fadeOutDuration = ReferenceManager.Instance.gameLogicController.GameSettings.overlayScreenFadeDuration;
                Fade(false, fadeOutDuration);
                task.StartDelayMs((int)fadeOutDuration * 1000);
                break;
            default:
                _blackOverlay.enabled = false;
                _text.enabled = false;
                task.Complete();
                break;
        }
    }

    private void Fade(bool value, float duration)
    {
        float targetValue = value ? 1f : 0f;
        DOTween.Sequence().Append(_canvasGroup.DOFade(targetValue, duration));
    }
}
