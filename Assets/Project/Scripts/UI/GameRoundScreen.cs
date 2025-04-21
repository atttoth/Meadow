using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

enum ProgressDisplayIndex
{
    SECTION_6,
    SECTION_8,
    DISPLAYS_NUM
}

public class GameRoundScreen : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Image _blackOverlay;
    private TextMeshProUGUI _text;
    private static readonly float PROGRESS_SECTION_WIDTH = 130f;
    private RoundScreenLayout _layout;
    private Transform[] _progressDisplayTransforms;
    private Slider[] _progressSliders;
    private Transform[] _avatarsTransform;
    private ProgressDisplayIndex _activeProgressDisplayIndex;
    private Vector3 _progressDisplayOriginPos;

    public void Init()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _text = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        int numOfDisplays = (int)ProgressDisplayIndex.DISPLAYS_NUM;
        _layout = new RoundScreenLayout(PROGRESS_SECTION_WIDTH);
        _progressDisplayTransforms = new Transform[numOfDisplays];
        _progressSliders = new Slider[numOfDisplays];
        _avatarsTransform = new Transform[numOfDisplays];
        for (int i = 0; i < numOfDisplays; i++)
        {
            Transform display = transform.GetChild(i + 2).transform;
            _progressDisplayTransforms[i] = display;
            _progressSliders[i] = display.GetChild(0).GetComponent<Slider>();
            _avatarsTransform[i] = display.GetChild(1).GetChild(0).transform;
            display.gameObject.SetActive(false);
        }
    }

    public void Setup(GameMode gameMode)
    {
        Color32[] avatarColors = gameMode.CurrentUserColors;
        int numOfRounds = gameMode.UsersOrderMap.Length;
        _activeProgressDisplayIndex = numOfRounds == 6 ? ProgressDisplayIndex.SECTION_6 : ProgressDisplayIndex.SECTION_8;
        for (int roundIndex = 0; roundIndex < numOfRounds; roundIndex++)
        {
            int[] users = gameMode.UsersOrderMap[roundIndex];
            for (int i = 0; i < users.Length; i++)
            {
                int userID = users[i];
                Image avatar = Instantiate(GameResourceManager.Instance.userAvatarPrefab, _avatarsTransform[(int)_activeProgressDisplayIndex]).GetComponent<Image>();
                avatar.color = avatarColors[userID];
                avatar.GetComponent<RectTransform>().anchoredPosition = new(_layout.GetAvatarPositionX(roundIndex, i, numOfRounds, avatarColors.Length), 50f);
            }
        }
        Slider slider = _progressSliders[(int)_activeProgressDisplayIndex];
        slider.maxValue = PROGRESS_SECTION_WIDTH * numOfRounds;
        slider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = avatarColors[0];
        _progressDisplayOriginPos = new(0f, 420f, 0f);
        _progressDisplayTransforms[(int)_activeProgressDisplayIndex].GetComponent<RectTransform>().anchoredPosition = _progressDisplayOriginPos;
    }

    public void ToggleProgressUI(bool value)
    {
        _progressDisplayTransforms[(int)_activeProgressDisplayIndex].gameObject.SetActive(value);
        _canvasGroup.alpha = value ? 1f : 0f;
    }

    public void ShowNextRoundScreenHandler(GameTask task, int nextRound)
    {
        float waitDelay = 1f;
        float fadeDuration = 1f;
        switch (task.State)
        {
            case 0:
                _blackOverlay.enabled = true;
                _text.text = "ROUND " + nextRound;
                ToggleProgressUI(true);
                Fade(true, fadeDuration);
                task.StartDelayMs((int)fadeDuration * 1000);
                break;
            case 1:
                task.StartHandler((Action<GameTask, int>)ShowProgressUI, nextRound);
                break;
            case 2:
                _text.enabled = true;
                task.StartDelayMs((int)waitDelay * 1000);
                break;
            case 3:
                Fade(false, fadeDuration);
                task.StartDelayMs((int)fadeDuration * 1000);
                break;
            default:
                ToggleProgressUI(false);
                _blackOverlay.enabled = false;
                _text.enabled = false;
                task.Complete();
                break;
        }
    }

    public void ShowGameFinishedScreenHandler(GameTask task, int nextRound)
    {
        float fadeDuration = 1f;
        switch (task.State)
        {
            case 0:
                _blackOverlay.enabled = true;
                _text.text = "GAME FINISHED";
                ToggleProgressUI(true);
                Fade(true, fadeDuration);
                task.StartDelayMs((int)fadeDuration * 1000);
                break;
            case 1:
                task.StartHandler((Action<GameTask, int>)ShowProgressUI, nextRound);
                break;
            case 2:
                _text.enabled = true;
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.waitDelay) * 1000));
                break;
            default: // todo: add end game summary
                task.Complete();
                break;
        }
    }

    private void Fade(bool value, float duration)
    {
        float targetValue = value ? 1f : 0f;
        DOTween.Sequence().Append(_canvasGroup.DOFade(targetValue, duration));
    }

    private void ShowProgressUI(GameTask task, int nextRound)
    {
        float waitDelay = GameSettings.Instance.GetDuration(Duration.waitDelay);
        float progressPanelSpeed = 0.3f;
        float sliderDuration = 1f;
        RectTransform rect = _progressDisplayTransforms[(int)_activeProgressDisplayIndex].GetComponent<RectTransform>();
        switch (task.State)
        {
            case 0:
                if (nextRound == 1)
                {
                    task.NextState(6);
                }
                else
                {
                    DOTween.Sequence().Append(rect.DOScale(1.4f, progressPanelSpeed)).Join(rect.DOAnchorPos(Vector3.zero, progressPanelSpeed).SetEase(Ease.Linear));
                    task.StartDelayMs((int)progressPanelSpeed * 1000);
                }
                break;
            case 1:
                task.StartDelayMs((int)waitDelay * 1000);
                break;
            case 2:
                float targetValue = PROGRESS_SECTION_WIDTH * (nextRound - 1);
                Slider slider = _progressSliders[(int)_activeProgressDisplayIndex];
                DOTween.To(() => slider.value, x => slider.value = x, targetValue, sliderDuration);
                task.StartDelayMs((int)sliderDuration * 1000);
                break;
            case 3:
                task.StartDelayMs((int)waitDelay * 1000);
                break;
            case 4:
                DOTween.Sequence().Append(rect.DOScale(1f, progressPanelSpeed)).Join(rect.DOAnchorPos(_progressDisplayOriginPos, progressPanelSpeed).SetEase(Ease.Linear));
                task.StartDelayMs((int)progressPanelSpeed * 1000);
                break;
            case 5:
                task.StartDelayMs((int)waitDelay * 1000);
                break;
            default:
                task.Complete();
                break;
        }
    }
}
