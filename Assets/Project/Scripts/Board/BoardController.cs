using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class BoardController : MonoBehaviour
{
    private static readonly int _GRID_SIZE = 4;
    private Dictionary<int, List<CardHolder>> _cardHolders;
    private Dictionary<int, List<MarkerHolder>> _markerHolders;
    private DeckController _deckController;
    private Transform _cardDrawContainer;
    private CanvasGroup _canvasGroup;
    private BoardLayout _boardLayout;
    private List<Card> _cardsForSelection; // cards saved for selection screen

    // used for board fill
    private List<CardHolder> _emptyHolders; // saved empty card holders to fill
    private List<Card> _cardsToDraw; // saved cards to fill card holders

    private T GetNextItem<T>(List<T> list)
    {
        T item = list[0];
        list.RemoveAt(0);
        return item;
    }

    public void Create()
    {
        SpriteAtlas atlas = GameResourceManager.Instance.Base;
        GetComponent<Image>().sprite = atlas.GetSprite("board_frame");
        _cardHolders = new();
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            List<CardHolder> list = new();
            Transform col = transform.GetChild(0).GetChild(i);
            for (int j = 0; j < _GRID_SIZE; j++)
            {
                CardHolder boardCardHolder = Instantiate(GameResourceManager.Instance.boardCardHolderPrefab, col).GetComponent<CardHolder>();
                boardCardHolder.Init(j, HolderType.BoardCard);
                boardCardHolder.holderSubType = HolderSubType.NONE;
                boardCardHolder.EnableOverlay(false);
                list.Add(boardCardHolder);
            }
            _cardHolders.Add(i, list);
        }

        _markerHolders = new();
        for (int i = 0; i < 3; i++)
        {
            List<MarkerHolder> list = new();
            Transform holderGroup = transform.GetChild(1).GetChild(i);
            for (int j = 0; j < _GRID_SIZE; j++)
            {
                MarkerHolder boardMarkerHolder = holderGroup.GetChild(j).GetComponent<MarkerHolder>();
                boardMarkerHolder.Init(j, HolderType.BoardMarker);
                list.Add(boardMarkerHolder);
            }
            _markerHolders.Add(i, list);
        }

        _deckController = transform.GetChild(2).GetComponent<DeckController>();
        _deckController.Init();
        _cardDrawContainer = transform.GetChild(3).transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _boardLayout = new BoardLayout(GetComponent<RectTransform>());
        ToggleRayCastOfMarkerHolders(false);
    }

    public void EnableRightSideMarkerHoldersForRowPick()
    {
        foreach (List<MarkerHolder> holders in _markerHolders.Values)
        {
            holders.ForEach(holder =>
            {
                if(holder.transform.parent.CompareTag("RightHolder"))
                {
                    holder.ToggleRayCast(true);
                }
            });
        }
    }

    private void SaveTargetHoldersAndCards(DeckType activeDeckType)
    {
        List<CardHolder> holders = new() { };
        List<Card> cards = new() { };
        for (int colIndex = 0; colIndex < (int)DeckType.NUM_OF_DECKS; colIndex++)
        {
            DeckType deckType = colIndex switch
            {
                0 => DeckType.West,
                1 or 2 => activeDeckType,
                _ => DeckType.East,
            };
            List<CardHolder> emptyHolders = GetEmptyCardHoldersByColumn(colIndex);
            if (emptyHolders.Count > 0)
            {
                emptyHolders.Reverse();
                List<Card> selectedCards = _deckController.GetCardsReadyToDraw(emptyHolders.Count, deckType, colIndex);
                for (int i = 0; i < emptyHolders.Count; i++)
                {
                    CardHolder emptyHolder = emptyHolders[i];
                    Card card = selectedCards[i];
                    holders.Add(emptyHolder);
                    cards.Add(card);
                }
            }
        }
        _emptyHolders = holders;
        _cardsToDraw = cards;
    }

    public void BoardFillHandler(GameTask task, DeckType deckType, List<Card> cards)
    {
        switch (task.State)
        {
            case 0:
                SaveTargetHoldersAndCards(deckType);
                cards.AddRange(_cardsToDraw);
                task.StartDelayMs(0);
                break;
            case 1:
                int duration = 0;
                if (_emptyHolders.Count > 0)
                {
                    float cardDrawDelay = GameSettings.Instance.GetDuration(Duration.cardDrawDelayFromDeck);
                    float cardDrawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromDeck);
                    float cardRotationSpeed = GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard);
                    duration = (int)(((_emptyHolders.Count - 1) * cardDrawDelay + cardDrawSpeed + cardRotationSpeed) * 1000);
                    int i = 0;
                    while (_emptyHolders.Count > 0)
                    {
                        float delay = i * cardDrawDelay;
                        CardHolder holder = GetNextItem(_emptyHolders);
                        Card card = GetNextItem(_cardsToDraw);
                        card.PlayDrawingAnimation(delay, holder, _cardDrawContainer);
                        i++;
                    }
                }
                task.StartDelayMs(duration);
                break;
            case 2:
                cards.ForEach(card => card.CardIconItemsView.Toggle(true));
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void BoardClearHandler(GameTask task) // todo: put back unused cards to deck with tween
    {
        switch(task.State)
        {
            case 0:
                for (int col = 0; col < _cardHolders.Count; col++)
                {
                    for (int row = 0; row < _cardHolders[col].Count; row++)
                    {
                        CardHolder holder = _cardHolders[col][row];
                        Card card = (Card)holder.GetItemFromContentListByIndex(0);
                        holder.RemoveItemFromContentList(card);
                        card.gameObject.SetActive(false);
                    }
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private List<CardHolder> GetEmptyCardHoldersByColumn(int colIndex)
    {
        List<CardHolder> list = _cardHolders[colIndex];
        List<CardHolder> emptyHolders = new();
        foreach (CardHolder holder in list)
        {
            if (holder.IsEmpty())
            {
                emptyHolders.Add(holder);
            }
        }
        return emptyHolders;
    }

    private void SelectCardsFromBoard(int[][] indices)
    {
        for (int i = 0; i < indices.Length; i++)
        {
            int[] pair = indices[i];
            GetCardFromCardHolder(pair[0], pair[1]).ToggleSelection(true);
        }
    }

    private Card GetCardFromCardHolder(int col, int row)
    {
        CardHolder holder = _cardHolders[col][row];
        return (Card)holder.GetItemFromContentListByIndex(0);
    }

    private int[][] GetColAndRowIndicesOfCardHolder(int holderID, int holderListKey, int numberOnMarker)
    {
        int length = numberOnMarker < MarkerView.BLANK_MARKER_ID ? 1 : _GRID_SIZE;
        int[][] indices = new int[length][];
        if (length == 1) // 1-2-3-4 markers point to a single position
        {
            switch (holderListKey)
            {
                case 2:
                    indices[0] = new int[] { holderID, _GRID_SIZE - numberOnMarker };
                    break;
                case 1:
                    indices[0] = new int[] { numberOnMarker - 1, holderID };
                    break;
                default:
                    indices[0] = new int[] { _GRID_SIZE - numberOnMarker, holderID };
                    break;
            };
            return indices;
        }
        else
        {
            int index1 = holderListKey == 2 ? 1 : 0;
            int index2 = holderListKey == 2 ? 0 : 1;
            for (int i = 0; i < length; i++)
            {
                int[] arr = new int[2];
                arr[index1] = i;
                arr[index2] = holderID;
                indices[i] = arr;
            }
            return indices;
        }
    }

    public List<Card> GetRowCards()
    {
        List<Card> cards = new();
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            for (int j = 0; j < _GRID_SIZE; j++)
            {
                Card card = GetCardFromCardHolder(i, j);
                if (card.isSelected)
                {
                    cards.Add(card);
                    card.OnPick();
                }
            }
        }
        return cards;
    }

    public void ToggleRayCastOfCards(bool value)
    {
        for (int col = 0; col < _cardHolders.Count; col++)
        {
            for (int row = 0; row < _cardHolders[col].Count; row++)
            {
                CardHolder holder = _cardHolders[col][row];
                if (holder.GetContentListSize() > 0)
                {
                    holder.GetAllContent().ForEach(card => card.ToggleRayCast(value));
                }
            }
        }
    }

    public void ShowMarkersAtBoard(MarkerHolder holder, List<Marker> markers)
    {
        markers.ForEach(marker =>
        {
            MarkerHolder prevHolder = marker.transform.parent.GetComponent<MarkerHolder>();
            if (prevHolder != null && !prevHolder.IsEmpty())
            {
                prevHolder.RemoveItemFromContentList(marker);
            }
            holder.AddToHolder(marker);
            marker.transform.position = holder.transform.position;
            marker.Rotate(holder.Direction);
            marker.SetAlpha(false);
        });
    }

    public void HideMarkersAtBoard(List<Marker> markers)
    {
        markers.ForEach(marker => marker.gameObject.SetActive(false));
    }

    public void ToggleRayCastOfMarkerHolders(bool value)
    {
        foreach (List<MarkerHolder> holders in _markerHolders.Values)
        {
            holders.ForEach(holder => holder.ToggleRayCast(value));
        }
    }

    public void SelectCard(Marker marker, MarkerHolder holder)
    {
        int[][] indices = GetColAndRowIndicesOfCardHolder(holder.ID, (int)holder.Direction, marker.numberOnMarker);
        ToggleBlackOverlayOfCardHolders(true, indices);
        SelectCardsFromBoard(indices);
    }

    public void ToggleCardsSelection(bool value)
    {
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            for (int j = 0; j < _GRID_SIZE; j++)
            {
                GetCardFromCardHolder(i, j).ToggleSelection(value);
            }
        }
    }

    public void ToggleBlackOverlayOfCardHolders(bool value, int[][] indices)
    {
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            List<CardHolder> holders = _cardHolders[i];
            holders.ForEach(holder => holder.EnableOverlay(value));
        }
        if (value)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                int[] pair = indices[i];
                _cardHolders[pair[0]][pair[1]].EnableOverlay(!value);
            }
        }
    }

    public void EnableAnyCardSelectionHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                ToggleCardsSelection(true);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public List<Card> GetUnselectedCards(int cardID)
    {
        _cardsForSelection.ForEach(card => card.ToggleSelection(false));
        List<Card> unselectedTopCards = _cardsForSelection.Where(card => card.Data.ID != cardID).ToList();
        _cardsForSelection = unselectedTopCards;
        return unselectedTopCards;
    }

    public List<Card> GetTopCardsOfDeck(DeckType deckType)
    {
        int num = 3;
        for (int i = 0; i < num; i++)
        {
            _cardsForSelection.Add(_deckController.GetCardFromDeck(deckType));
        }
        return _cardsForSelection;
    }

    public List<Card> CreateInitialGroundCards()
    {
        SpriteAtlas atlas = GameResourceManager.Instance.GetAssetByName<SpriteAtlas>(DeckType.East.ToString());
        _cardsForSelection = new();
        _deckController.InitialGroundCardData.ForEach(data =>
        {
            Card card = Instantiate(GameResourceManager.Instance.cardPrefab, _cardDrawContainer).GetComponent<Card>();
            card.Init(data, atlas.GetSprite(data.ID.ToString()), atlas.GetSprite("back"));
            _cardsForSelection.Add(card);
        });
        return _cardsForSelection;
    }

    public void DisposeUnselectedCards(bool isHandSetup)
    {
        if(isHandSetup)
        {
            Object.Destroy(_cardsForSelection.First().gameObject);
        }
        else
        {
            DeckType deckType = _cardsForSelection.First().Data.deckType;
            Deck deck = _deckController.GetDeckByDeckType(deckType);
            _cardsForSelection.ForEach(card =>
            {
                card.transform.SetParent(deck.transform);
                card.GetComponent<RectTransform>().anchoredPosition = new(0f, 0f);
                deck.AddCard(card);
            });
        }
        _cardsForSelection.Clear();
    }

    public void ToggleCanInspectFlagOfCards(bool value)
    {
        _cardHolders.Select(e => e.Value).ToList().ForEach(holders =>
        {
            holders.ForEach(holder =>
            {
                if(!holder.IsEmpty())
                {
                    Card card = (Card)holder.GetItemFromContentListByIndex(0);
                    card.ToggleCanInspectFlag(value);
                }
            });
        });
    }

    public List<MarkerHolder> GetAvailableMarkerHolders(int holderParentID = -1)
    {
        if(holderParentID > -1)
        {
            return _markerHolders[holderParentID];
        }
        else
        {
            List<MarkerHolder> availableHolders = new();
            foreach (List<MarkerHolder> holders in _markerHolders.Values)
            {
                holders.ForEach(holder =>
                {
                    if (holder.IsEmpty())
                    {
                        availableHolders.Add(holder);
                    }
                });
            }
            return availableHolders;
        }
    }

    public Card GetSingleSelectedCard()
    {
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            for (int j = 0; j < _GRID_SIZE; j++)
            {
                Card card = GetCardFromCardHolder(i, j);
                if(card.isSelected)
                {
                    return card;
                }
            }
        }
        return null;
    }

    public void Fade(bool value)
    {
        float fadeDuration = GameSettings.Instance.GetDuration(Duration.gameUIFadeDuration);
        float targetValue = value ? 1f : 0f;
        DOTween.Sequence().Append(_canvasGroup.DOFade(targetValue, fadeDuration));
    }
}
