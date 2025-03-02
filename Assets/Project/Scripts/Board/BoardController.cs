using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.WSA;
using static UnityEditor.Progress;

public class BoardController : MonoBehaviour
{
    private static readonly int _GRID_SIZE = 4;
    private Dictionary<int, List<CardHolder>> _cardHolders;
    private Dictionary<int, List<MarkerHolder>> _markerHolders;
    private DeckController _deckController;
    private Transform _cardDrawContainer;

    // drawing sequence
    private List<CardHolder> _emptyHolders; // saved empty card holders to fill
    private List<Card> _cardsToDraw; // saved cards to draw

    private T GetNextItem<T>(List<T> list)
    {
        T item = list[0];
        list.RemoveAt(0);
        return item;
    }

    public void CreateBoard()
    {
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        GetComponent<Image>().sprite = atlas.GetSprite("board_frame");
        _cardHolders = new();
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            List<CardHolder> list = new();
            Transform col = transform.GetChild(0).GetChild(i);
            for (int j = 0; j < _GRID_SIZE; j++)
            {
                CardHolder boardCardHolder = Instantiate(GameAssets.Instance.boardCardHolderPrefab, col).GetComponent<CardHolder>();
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
                    float cardDrawDelay = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawDelayFromDeck;
                    float cardDrawSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawSpeedFromDeck;
                    float cardRotationSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard;
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
                cards.ForEach(card => card.ToggleIcons(true));
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
        int length = numberOnMarker <= 4 ? 1 : _GRID_SIZE;
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
            holder.AddToContentList(marker);
            marker.transform.position = holder.transform.position;
            marker.Rotate(holder.Direction);
            marker.AdjustAlpha(false);
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

    public List<Card> GetUnselectedTopCardsOfDeck(int cardID)
    {
        List<Card> topCards = _deckController.TopCards;
        topCards.ForEach(card => card.ToggleSelection(false));
        List<Card> unselectedTopCards = topCards.Where(card => card.Data.ID != cardID).ToList();
        _deckController.TopCards = unselectedTopCards;
        return unselectedTopCards;
    }

    public List<Card> GetTopCardsOfDeck(DeckType deckType)
    {
        int num = 3;
        _deckController.TopCards = new();
        for (int i = 0; i < num; i++)
        {
            _deckController.TopCards.Add(_deckController.GetCardFromDeck(deckType));
        }
        return _deckController.TopCards;
    }

    public void DisposeTopCards()
    {
        _deckController.ClearTopCards();
    }

    public void Fade(bool value)
    {
        float fadeDuration = ReferenceManager.Instance.gameLogicManager.GameSettings.gameUIFadeDuration;
        float targetValue = value ? 1f : 0f;
        DOTween.Sequence().Append(GetComponent<Image>().DOFade(targetValue, fadeDuration));
        _cardHolders.Select(e => e.Value).ToList().ForEach(holders =>
        {
            holders.ForEach(holder =>
            {
                if (!holder.IsEmpty())
                {
                    Card card = (Card)holder.GetItemFromContentListByIndex(0);
                    card.ToggleIcons(value);
                    DOTween.Sequence().Append(card.GetComponent<Image>().DOFade(targetValue, fadeDuration));
                }
            });
        });
    }
}
