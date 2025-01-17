using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DummyType
{
    CARD,
    ACTION_ICON
}

public class OverlayController : GameLogicEvent
{
    private GameObject _dummy;
    private Image _blackOverlay;
    private Vector3 _defaultPosition;
    Sequence examineSequence;
    private MarkerActionScreen _markerActionScreen;
    private DeckSelectionScreen _deckSelectionScreen;
    private ScoreCollectionScreen _scoreCollectionScreen;

    public void CreateOverlay()
    {
        _dummy = transform.GetChild(1).gameObject;
        _defaultPosition = _dummy.transform.position;
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        EnableDummy(false);
        _markerActionScreen = transform.GetChild(2).GetComponent<MarkerActionScreen>();
        List<Button> actionIconButtons = _markerActionScreen.Init();
        actionIconButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = true;
            StartEventHandler(GameLogicEventType.MARKER_ACTION_SELECTED, new GameTaskItemData() { markerAction = (MarkerAction)button.GetComponent<ScreenDisplayItem>().type });
        }));

        _deckSelectionScreen = transform.GetChild(3).GetComponent<DeckSelectionScreen>();
        List<Button> deckButtons = _deckSelectionScreen.Init();
        deckButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = true;
            StartEventHandler(GameLogicEventType.DECK_SELECTED, new GameTaskItemData() { deckType = (DeckType)button.GetComponent<ScreenDisplayItem>().type });
        }));

        _scoreCollectionScreen = transform.GetChild(4).GetComponent<ScoreCollectionScreen>();
        _scoreCollectionScreen.Init();
    }

    public void SetDummy(Sprite sprite, bool needToRotate, DummyType type)
    {
        int[] size = GetDummySize(type);
        _dummy.GetComponent<RectTransform>().sizeDelta = new(size[0], size[1]);
        _dummy.GetComponent<Image>().sprite = sprite;
        if (needToRotate)
        {
            _dummy.transform.eulerAngles = new Vector3(_dummy.transform.rotation.eulerAngles.x, _dummy.transform.rotation.eulerAngles.y, _dummy.transform.rotation.eulerAngles.z + 90f);
            _dummy.transform.position = new Vector3(_dummy.transform.position.x, _dummy.transform.position.y + 60f, _dummy.transform.position.z);
        }
    }

    private int[] GetDummySize(DummyType type)
    {
        return type switch
        {
            DummyType.CARD => new int[] { 160, 232 },
            DummyType.ACTION_ICON => new int[] { 300, 300 },
            _ => null
        };
    }

    public void StartCardShowSequence()
    {
        examineSequence.Kill();
        examineSequence = DOTween.Sequence();
        examineSequence.Append(_dummy.transform.DOScale(3.5f, 0.5f).SetEase(Ease.InOutSine));
        examineSequence.Play();
    }

    public void EnableDummy(bool value)
    {
        _dummy.SetActive(value);
        if (_blackOverlay.enabled != value)
        {
            _blackOverlay.enabled = value;
        }
    }

    public void ToggleMarkerActionScreen(Marker marker)
    {
        _markerActionScreen.ToggleScreen(marker);
    }

    public void ShowDeckSelectionScreenHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _deckSelectionScreen.ToggleDeckSelectionScreen(true, task.Data.deckType);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void HideDeckSelectionScreenHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _deckSelectionScreen.ToggleDeckSelectionScreen(false, task.Data.deckType);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ShowCardSelectionHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_deckSelectionScreen.ShowCardSelectionHandler, task.Data);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void HideCardSelectionHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_deckSelectionScreen.HideCardSelectionHandler, task.Data);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void CollectCardScoreHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                List<Card> cards = task.Data.cards;
                float cardScoreDelay = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreDelay;
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreCollectingSpeed;
                int duration = (int)(((cards.Count - 1) * cardScoreDelay + speed) * 1000);
                int i = 0;
                while (cards.Count > 0)
                {
                    float delay = i * cardScoreDelay;
                    Card card = cards.First();
                    cards.RemoveAt(0);
                    Transform scoreTextPrefab = _scoreCollectionScreen.GetScoreTextObject();
                    scoreTextPrefab.SetPositionAndRotation(card.GetComponent<Transform>().position, Quaternion.identity);
                    scoreTextPrefab.GetChild(1).GetComponent<TextMeshProUGUI>().text = card.Data.score.ToString();
                    Sequence scoreCollecting = DOTween.Sequence();
                    scoreCollecting.Append(scoreTextPrefab.DOMove(task.Data.targetTransform.position, speed).SetEase(Ease.InOutQuart).SetDelay(delay));
                    scoreCollecting.OnComplete(() =>
                    {
                        StartEventHandler(GameLogicEventType.SCORE_COLLECTED, new GameTaskItemData() { score = card.Data.score });
                        _scoreCollectionScreen.DisposeScoreTextObject(scoreTextPrefab);
                    });
                    i++;
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void CollectCampScoreHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreCollectingSpeed;
                int duration = (int)(speed * 1000);
                Transform scoreTextPrefab = _scoreCollectionScreen.GetScoreTextObject();
                scoreTextPrefab.SetPositionAndRotation(task.Data.originTransform.position, Quaternion.identity);
                scoreTextPrefab.GetChild(1).GetComponent<TextMeshProUGUI>().text = task.Data.score.ToString();
                Sequence scoreCollecting = DOTween.Sequence();
                scoreCollecting.Append(scoreTextPrefab.DOMove(task.Data.targetTransform.position, speed).SetEase(Ease.InOutQuart));
                scoreCollecting.OnComplete(() =>
                {
                    StartEventHandler(GameLogicEventType.SCORE_COLLECTED, task.Data);
                    _scoreCollectionScreen.DisposeScoreTextObject(scoreTextPrefab);
                });
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }
}
