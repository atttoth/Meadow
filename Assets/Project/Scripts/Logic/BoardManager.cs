using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    private static readonly int _GRID_SIZE = 4;
    private Dictionary<int, List<CardHolder>> _cardHolders;
    private Dictionary<int, List<MarkerHolder>> _markerHolders;

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
        transform.GetChild(0).GetComponent<Image>().sprite = atlas.GetSprite("board_inside");
        _cardHolders = new();
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            List<CardHolder> list = new();
            Transform col = transform.GetChild(0).GetChild(i);
            for(int j = 0; j < _GRID_SIZE; j++)
            {
                CardHolder boardCardHolder = Instantiate(GameAssets.Instance.boardCardHolderPrefab, col).GetComponent<CardHolder>();
                boardCardHolder.Init(j, HolderType.BoardCard);
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
    }

    public void PrepareCardDrawing(List<CardHolder> holders, List<Card> cards)
    {
        _emptyHolders = holders;
        _cardsToDraw = cards;
    }

    public void BoardFillHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                float cardDrawDelay = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawDelayFromDeck;
                float cardDrawSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawSpeedFromDeck;
                float cardRotationSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard;
                int duration = (int)(((_emptyHolders.Count - 1) * cardDrawDelay + cardDrawSpeed + cardRotationSpeed) * 1000);
                int i = 0;
                while (_emptyHolders.Count > 0)
                {
                    float delay = i * cardDrawDelay;
                    CardHolder holder = GetNextItem(_emptyHolders);
                    Card card = GetNextItem(_cardsToDraw);
                    card.PlayDrawingAnimation(delay, holder);
                    i++;
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public List<CardHolder> GetEmptyCardHoldersByColumn(int colIndex)
    {
        List<CardHolder> list = _cardHolders[colIndex];
        List<CardHolder> emptyHolders = new();
        foreach(CardHolder holder in list)
        {
            if(holder.IsEmpty())
            {
                emptyHolders.Add(holder);
            }
        }
        return emptyHolders;
    }

    public Card SelectCardFromBoard(int[] indices)
    {
        Card card = GetCardFromCardHolder(indices[0], indices[1]);
        return card;
    }

    public List<Card> SelectCardsFromBoard(int[][] indices)
    {
        List<Card> cards = new();
        for(int i = 0; i < indices.Length; i++)
        {
            int[] pair = indices[i];
            Card card = GetCardFromCardHolder(pair[0], pair[1]);
            cards.Add(card);
        }
        return cards;
    }

    private Card GetCardFromCardHolder(int col, int row)
    {
        CardHolder holder = _cardHolders[col][row];
        return (Card)holder.GetItemFromContentListByIndex(0);
    }

    public int[] GetColAndRowIndicesOfCardHolder(int holderID, int holderListKey, int numberOnMarker)
    {
        return holderListKey switch
        {
            2 => new int[] { holderID, _GRID_SIZE - numberOnMarker },
            1 => new int[] { numberOnMarker - 1, holderID },
            _ => new int[] { _GRID_SIZE - numberOnMarker, holderID }
        };
    }

    public int[][] GetColAndRowIndicesOfCardHolder(int holderID, int holderListKey)
    {
        int index = holderListKey == 2 ? 0 : 1;
        int length = (int)DeckType.NUM_OF_DECKS;
        int[][] indices = new int[length][];
        for (int i = 0; i < length; i++)
        {
            List<int> list = new();
            list.Add(i);
            list.Insert(index, holderID);
            indices[i] = list.ToArray();
        }
        return indices;
    }

    public void ToggleRayTargetOfCardsAndHolders(bool value)
    {
        for (int col = 0; col < _cardHolders.Count; col++)
        {
            for (int row = 0; row < _cardHolders[col].Count; row++)
            {
                CardHolder holder = _cardHolders[col][row];
                if(holder.GetContentListSize() > 0)
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
            if(prevHolder != null && !prevHolder.IsEmpty())
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

    public void ToggleMarkerHolders(bool value)
    {
        foreach (List<MarkerHolder> holders in _markerHolders.Values)
        {
            holders.ForEach(holder => holder.ToggleRayCast(value));
        }
    }

    public void SelectCard(Marker marker, MarkerHolder holder)
    {
        if (marker.ID < 4)
        {
            int[] indices = GetColAndRowIndicesOfCardHolder(holder.ID, (int)holder.Direction, marker.numberOnMarker);
            ToggleBlackOverlayOfCardHolders(true, indices);
            SelectCardFromBoard(indices).Select();
        }
        else
        {
            int[][] indices = GetColAndRowIndicesOfCardHolder(holder.ID, (int)holder.Direction);
            ToggleBlackOverlayOfCardHolders(true, indices);
            SelectCardsFromBoard(indices).ForEach(card => card.Select());
        }
    }

    public void DeSelectCards()
    {
        for(int i = 0; i < _GRID_SIZE; i++)
        {
            for(int j = 0; j < _GRID_SIZE; j++)
            {
                GetCardFromCardHolder(i, j).isSelected = false;
            }
        }
    }

    public void ToggleBlackOverlayOfCardHolders(bool value, int[] indices = null)
    {
        for(int i = 0; i < _GRID_SIZE; i++)
        {
            List<CardHolder> holders = _cardHolders[i];
            holders.ForEach(holder => holder.EnableOverlay(value));
        }
        if(value)
        {
            _cardHolders[indices[0]][indices[1]].EnableOverlay(!value);
        }
    }

    public void ToggleBlackOverlayOfCardHolders(bool value, int[][] indices = null)
    {
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            List<CardHolder> holders = _cardHolders[i];
            holders.ForEach(holder => holder.EnableOverlay(value));
        }
        if (value)
        {
            for (int i = 0; i < _GRID_SIZE; i++)
            {
                int[] pair = indices[i];
                _cardHolders[pair[0]][pair[1]].EnableOverlay(!value);
            }
        }
    }
}
