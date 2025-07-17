using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class BoardController : MonoBehaviour
{
    private static readonly int GRID_SIZE = 4;
    private Dictionary<int, List<CardHolder>> _cardHolders;
    private Dictionary<int, List<MarkerHolder>> _markerHolders;
    private DeckController _deckController;
    private Transform _cardDrawContainer;
    private CanvasGroup _canvasGroup;
    private BoardLayout _boardLayout;
    private List<CardHolder> _preparedHolders; // saved empty card holders to fill
    private List<Card> _preparedCards; // saved cards for selection screen, fill card holders, half-time board change

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
        for (int i = 0; i < GRID_SIZE; i++)
        {
            List<CardHolder> list = new();
            Transform col = transform.GetChild(0).GetChild(i);
            for (int j = 0; j < GRID_SIZE; j++)
            {
                CardHolder boardCardHolder = Instantiate(GameResourceManager.Instance.boardCardHolderPrefab, col).GetComponent<CardHolder>();
                boardCardHolder.Init(j, HolderType.BoardCard);
                boardCardHolder.Data.holderSubType = HolderSubType.NONE;
                boardCardHolder.EnableOverlay(false);
                list.Add(boardCardHolder);
            }
            _cardHolders.Add(i, list);
        }

        _markerHolders = new();
        int boardSides = 3;
        for (int groupIndex = 0; groupIndex < boardSides; groupIndex++)
        {
            List<MarkerHolder> list = new();
            Transform holderGroup = transform.GetChild(1).GetChild(groupIndex);
            for (int j = 0; j < GRID_SIZE; j++)
            {
                MarkerHolder boardMarkerHolder = holderGroup.GetChild(j).GetComponent<MarkerHolder>();
                boardMarkerHolder.Init(j, HolderType.BoardMarker);
                list.Add(boardMarkerHolder);
            }
            _markerHolders.Add(groupIndex, list);
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

    public void BoardFillHandler(GameTask task, DeckType activeDeckType)
    {
        switch (task.State)
        {
            case 0:
                _preparedCards = new();
                _preparedHolders = new();
                DeckType[] deckTypes = new DeckType[] { DeckType.West, activeDeckType, activeDeckType, DeckType.East };
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    Transform display = _deckController.GetDisplayDeckTransform(col);
                    DeckType deckType = deckTypes[col];
                    for (int row = GRID_SIZE - 1; row >= 0; row--)
                    {
                        CardHolder holder = _cardHolders[col][row];
                        if (holder.Data.IsEmpty())
                        {
                            Card card = _deckController.GetCardFromDeck(deckType);
                            card.transform.SetParent(display);
                            card.transform.position = display.position;
                            card.ToggleRayCast(false);
                            _preparedHolders.Add(holder);
                            _preparedCards.Add(card);
                        }
                    }
                }
                task.StartDelayMs(0);
                break;
            case 1:
                int duration = 0;
                if (_preparedHolders.Count > 0)
                {
                    float cardDrawDelay = GameSettings.Instance.GetDuration(Duration.cardDrawDelayFromDeck);
                    float cardDrawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromDeck);
                    float cardRotationSpeed = GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard);
                    duration = (int)(((_preparedHolders.Count - 1) * cardDrawDelay + cardDrawSpeed + cardRotationSpeed) * 1000);
                    int i = 0;
                    while (_preparedHolders.Count > 0)
                    {
                        float delay = i * cardDrawDelay;
                        CardHolder holder = GetNextItem(_preparedHolders);
                        Card card = GetNextItem(_preparedCards);
                        card.FillBoardTween(delay, holder, _cardDrawContainer);
                        i++;
                    }
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void BoardChangeHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                _preparedCards = new();
                for (int col = 0; col < GRID_SIZE; col++)
                {
                    for (int row = 0; row < GRID_SIZE; row++)
                    {
                        CardHolder holder = _cardHolders[col][row];
                        Card card = GetCardFromCardHolder(col, row);
                        holder.Data.RemoveItemFromContentList(card);
                        _preparedCards.Add(card);
                    }
                }
                task.StartDelayMs(0);
                break;
            case 1:
                int duration = 0;
                float cardDrawDelay = GameSettings.Instance.GetDuration(Duration.cardDrawDelayFromDeck);
                float cardDrawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromDeck);
                float cardRotationSpeed = GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard);
                duration = (int)(((_preparedCards.Count - 1) * cardDrawDelay + cardDrawSpeed + cardRotationSpeed) * 1000);
                int i = 0;
                int colIndex = 0;
                while (_preparedCards.Count > 0)
                {
                    float delay = i * cardDrawDelay;
                    Card card = GetNextItem(_preparedCards);
                    card.ClearBoardTween(_deckController.GetDisplayDeckTransform(colIndex), delay);
                    i++;
                    if(i % 4 == 0)
                    {
                        colIndex++;
                    }
                }
                task.StartDelayMs(duration);
                break;
            case 2:
                DisposePreparedCards(false);
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.waitDelay) * 1000));
                break;
            case 3:
                task.StartHandler((Action<GameTask, DeckType>)BoardFillHandler, DeckType.North);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void DrawRandomNorthCardFromDeckHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                _preparedCards = new();
                Card card = _deckController.GetCardFromDeck(DeckType.North);
                Transform display = _deckController.GetDisplayDeckTransform(-1);
                card.transform.SetParent(display);
                card.transform.position = display.position;
                card.ToggleRayCast(false);
                _preparedCards.Add(card);
                task.StartDelayMs(0);
                break;
            case 1:
                float cardDrawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromDeck);
                float cardRotationSpeed = GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard);
                _preparedCards.First().DrawFromDeckTween(GetSingleSelectedCard().GetComponent<RectTransform>().position.y);
                task.StartDelayMs((int)((cardDrawSpeed + cardRotationSpeed) * 1000));
                break;
            case 2:
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.waitDelay) * 1000));
                break;
            default:
                task.Complete();
                break;
        }
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
        return (Card)holder.Data.GetItemFromContentListByIndex(0);
    }

    private int[][] GetColAndRowIndicesOfCardHolder(int holderID, int holderListKey, int numberOnMarker)
    {
        int length = numberOnMarker < MarkerView.BLANK_MARKER_ID ? 1 : GRID_SIZE;
        int[][] indices = new int[length][];
        if (length == 1) // 1-2-3-4 markers point to a single position
        {
            switch (holderListKey)
            {
                case 2:
                    indices[0] = new int[] { holderID, GRID_SIZE - numberOnMarker };
                    break;
                case 1:
                    indices[0] = new int[] { numberOnMarker - 1, holderID };
                    break;
                default:
                    indices[0] = new int[] { GRID_SIZE - numberOnMarker, holderID };
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

    public List<Card> GetRowOfSelectedCards()
    {
        List<Card> cards = new();
        for (int col = 0; col < GRID_SIZE; col++)
        {
            for (int row = 0; row < GRID_SIZE; row++)
            {
                Card card = GetCardFromCardHolder(col, row);
                if (card.isSelected)
                {
                    cards.Add(card);
                    card.OnPick();
                }
            }
        }
        return cards;
    }

    public List<object[]> GetAllCardsWithAvailableMarkerHolders()
    {
        List<object[]> boardContent = new();
        for (int col = 0; col < GRID_SIZE; col++)
        {
            for (int row = 0; row < GRID_SIZE; row++)
            {
                Card card = GetCardFromCardHolder(col, row);
                List<MarkerHolder> markerHolders = new();
                List<int> distances = new();
                for (int groupIndex = 0; groupIndex < _markerHolders.Count; groupIndex++)
                {
                    int orderIndex = groupIndex < _markerHolders.Count - 1 ? row : col;
                    MarkerHolder holder = _markerHolders[groupIndex][orderIndex];
                    if(holder.Data.IsEmpty())
                    {
                        int distance = groupIndex == 0 ? col + 1 : groupIndex == 1 ? GRID_SIZE - col : GRID_SIZE - row;
                        markerHolders.Add(holder);
                        distances.Add(distance);
                    }
                }
                boardContent.Add(new object[] { card, markerHolders, distances });
            }
        }
        return boardContent;
    }

    public void ToggleRayCastOfCards(bool value)
    {
        for (int col = 0; col < _cardHolders.Count; col++)
        {
            for (int row = 0; row < _cardHolders[col].Count; row++)
            {
                CardHolder holder = _cardHolders[col][row];
                if (holder.Data.ContentList.Count > 0)
                {
                    holder.Data.ContentList.ForEach(card => card.ToggleRayCast(value));
                }
            }
        }
    }

    public void ShowMarkersAtBoard(MarkerHolder holder, List<Marker> markers)
    {
        markers.ForEach(marker =>
        {
            MarkerHolder prevHolder = marker.transform.parent.GetComponent<MarkerHolder>();
            if (prevHolder != null && !prevHolder.Data.IsEmpty())
            {
                prevHolder.Data.RemoveItemFromContentList(marker);
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
        int[][] indices = GetColAndRowIndicesOfCardHolder(holder.Data.ID, (int)holder.Direction, marker.numberOnMarker);
        ToggleBlackOverlayOfCardHolders(true, indices);
        SelectCardsFromBoard(indices);
    }

    public void ToggleCardsSelection(bool value)
    {
        for (int i = 0; i < GRID_SIZE; i++)
        {
            for (int j = 0; j < GRID_SIZE; j++)
            {
                GetCardFromCardHolder(i, j).ToggleSelection(value);
            }
        }
    }

    public void ToggleBlackOverlayOfCardHolders(bool value, int[][] indices)
    {
        for (int i = 0; i < GRID_SIZE; i++)
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

    public Card GetPreparedCardByID(int cardID)
    {
        return cardID > -1 ? _preparedCards.Find(card => card.Data.ID == cardID) : _preparedCards.First();
    }

    public List<Card>[] GetRandomCardOfAllDecks(int amount)
    {
        int length = (int)DeckType.NUM_OF_DECKS;
        List<Card>[] cardsByDeck = new List<Card>[length];
        for(int i = 0; i < length; i++)
        {
            List<Card> cards = new();
            for(int j = 0; j < amount; j++)
            {
                Card card = _deckController.GetCardFromDeck((DeckType)i);
                _preparedCards.Add(card);
                cards.Add(card);
            }
            cardsByDeck[i] = cards;
        }
        return cardsByDeck;
    }

    public List<Card> CreateInitialGroundCards()
    {
        SpriteAtlas atlas = GameResourceManager.Instance.GetAssetByName<SpriteAtlas>(DeckType.East.ToString());
        _preparedCards = new();
        _deckController.InitialGroundCardData.ForEach(data =>
        {
            Card card = Instantiate(GameResourceManager.Instance.cardPrefab, _cardDrawContainer).GetComponent<Card>();
            card.Create(data, atlas.GetSprite(data.ID.ToString()), atlas.GetSprite("back"));
            _preparedCards.Add(card);
        });
        return _preparedCards;
    }

    public void DisposePreparedCards(bool isHandSetup, Card card = null)
    {
        if (card) // prevent disposing selected card
        {
            _preparedCards.Remove(card);
        }

        if (isHandSetup) // destroy unselected ground card copy
        {
            _preparedCards.ForEach(card => Destroy(card.gameObject));
        }
        else // put cards back to deck
        {
            _preparedCards.ForEach(card =>
            {
                DeckType deckType = card.Data.deckType;
                Deck deck = _deckController.GetDeckByDeckType(deckType);
                card.transform.SetParent(deck.transform);
                card.transform.position = deck.transform.position;
                deck.AddCard(card);
            });
        }
        _preparedCards = new();
    }

    public void ToggleCanInspectFlagOfCards(bool value)
    {
        for (int col = 0; col < _cardHolders.Count; col++)
        {
            for (int row = 0; row < _cardHolders[col].Count; row++)
            {
                CardHolder holder = _cardHolders[col][row];
                if (!holder.Data.IsEmpty())
                {
                    GetCardFromCardHolder(col, row).ToggleCanInspectFlag(value);
                }
            }
        }
    }

    public Card GetSingleSelectedCard()
    {
        for (int col = 0; col < _cardHolders.Count; col++)
        {
            for (int row = 0; row < _cardHolders[col].Count; row++)
            {
                Card card = GetCardFromCardHolder(col, row);
                if (card.isSelected)
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
