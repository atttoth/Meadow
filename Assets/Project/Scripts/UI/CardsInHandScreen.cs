using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CardsInHandScreen : MonoBehaviour
{
    private Image _blackOverlay;
    private List<SpriteAtlas> _atlasCollection;
    private List<Card> _activeFakeCards;
    private Transform _activeFakeCardsTransform;
    private List<Card> _fakeCardPool;
    private Transform _poolTransform;
    private List<Sequence> _showSequences;

    public void Init()
    {
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _atlasCollection = new();
        List<DeckType> deckTypes = new() { DeckType.West, DeckType.South, DeckType.East, DeckType.North };
        deckTypes.ForEach(deckType =>
        {
            SpriteAtlas atlas = GameResourceManager.Instance.GetAssetByName<SpriteAtlas>(deckType.ToString());
            _atlasCollection.Add(atlas);
        });
        _activeFakeCards = new();
        _activeFakeCardsTransform = transform.GetChild(2).GetComponent<Transform>();
        _fakeCardPool = new();
        _poolTransform = transform.GetChild(1).GetComponent<Transform>();
        _showSequences = new();
    }

    private Card GetFakeCard()
    {
        Card fakeCard;
        if (_fakeCardPool.Count > 0)
        {
            fakeCard = _fakeCardPool.First();
            fakeCard.transform.SetParent(_activeFakeCardsTransform);
            _fakeCardPool.RemoveAt(0);
        }
        else
        {
            fakeCard = Instantiate(GameResourceManager.Instance.cardPrefab, _activeFakeCardsTransform).GetComponent<Card>();
        }
        return fakeCard;
    }

    private void DisposeFakeCards()
    {
        for(int i = _activeFakeCards.Count - 1; i >= 0; i--)
        {
            Card fakeCard = _activeFakeCards[i];
            _activeFakeCards.RemoveAt(i);
            fakeCard.gameObject.SetActive(false);
            fakeCard.transform.SetParent(_poolTransform);
            _fakeCardPool.Add(fakeCard);
        }
    }

    private void SetupFakeCards(List<CardData> dataCollection)
    {
        dataCollection.ForEach(data =>
        {
            SpriteAtlas atlas = _atlasCollection[(int)data.deckType];
            Sprite cardFront = atlas.GetSprite(data.ID.ToString());
            Card fakeCard = GetFakeCard();
            fakeCard.Create(data, cardFront, null);
            fakeCard.MainImage.sprite = cardFront;
            Color color = fakeCard.MainImage.color;
            color.a = 0f;
            fakeCard.MainImage.color = color;
            fakeCard.gameObject.SetActive(true);
            _activeFakeCards.Add(fakeCard);
        });
    }

    public void ShowScreenHandler(GameTask task, List<CardData> dataCollection)
    {
        switch(task.State)
        {
            case 0:
                if (_showSequences.Count > 0)
                {
                    _showSequences.ForEach(sequence => sequence.Kill());
                    _showSequences.Clear();
                }
                SetupFakeCards(dataCollection);
                float screenFadeSpeed = GameSettings.Instance.GetDuration(Duration.cardsInHandScreenFadeSpeed);
                Color color = _blackOverlay.color;
                color.a = 0f;
                _blackOverlay.color = color;
                _blackOverlay.enabled = true;
                DOTween.Sequence().Append(_blackOverlay.DOFade(0.65f, screenFadeSpeed).SetEase(Ease.InOutQuart));
                task.StartDelayMs((int)(screenFadeSpeed * 1000));
                break;
            case 1:
                float fadeDelay = GameSettings.Instance.GetDuration(Duration.fakeCardFadeDelay);
                float speed = GameSettings.Instance.GetDuration(Duration.fakeCardFadeSpeed);
                int i = 0;
                while (_activeFakeCards.Count > i)
                {
                    float delay = i * fadeDelay;
                    Card fakeCard = _activeFakeCards[i];
                    Sequence sequence = DOTween.Sequence();
                    _showSequences.Add(sequence);
                    sequence.Append(fakeCard.MainImage.DOFade(1f, speed).SetEase(Ease.InOutQuart).SetDelay(delay));
                    i++;
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void HideScreenHandler(GameTask task, List<CardData> cards)
    {
        DisposeFakeCards();
        _blackOverlay.enabled = false;
        task.Complete();
    }
}
