using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.U2D;

public class BoardManager : MonoBehaviour
{
    private static int _GRID_SIZE = 4;
    private Dictionary<int, List<CardHolder>> _cardHolders;
    private Dictionary<int, List<MarkerHolder>> _markerHolders;
    private SpriteAtlas _atlas;

    // drawing sequence
    private List<CardHolder> _emptyHolders; // saved empty card holders to fill
    private List<Card> _cardsToDraw; // saved cards to draw

    public List<CardHolder> EmptyHolders
    {
        get { return _emptyHolders; }
        set { _emptyHolders = value; }
    }

    public List<Card> CardsToDraw
    {
        get { return _cardsToDraw; }
        set { _cardsToDraw = value; }
    }

    private T GetNextItem<T>(List<T> list)
    {
        T item = list[0];
        list.RemoveAt(0);
        return item;
    }

    public void CreateBoard()
    {
        _atlas = GameAssets.Instance.baseAtlas;
        GetComponent<Image>().sprite = _atlas.GetSprite("board_frame");
        transform.GetChild(0).GetComponent<Image>().sprite = _atlas.GetSprite("board_inside");
        _cardHolders = new();
        for (int i = 0; i < _GRID_SIZE; i++)
        {
            List<CardHolder> list = new();
            Transform col = transform.GetChild(0).GetChild(i);
            for(int j = 0; j < _GRID_SIZE; j++)
            {
                CardHolder boardCardHolder = Instantiate(GameAssets.Instance.boardCardHolderPrefab, col).GetComponent<CardHolder>();
                boardCardHolder.name = $"CardHolder{j}";
                boardCardHolder.ID = j;
                boardCardHolder.holderType = HolderType.BoardCard;
                boardCardHolder.contentList = new();
                boardCardHolder.backgroundImage = GetComponent<Image>();
                boardCardHolder.blackOverlay = boardCardHolder.transform.GetChild(0).GetComponent<Image>();
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
                boardMarkerHolder.Init();
                boardMarkerHolder.ID = j;
                boardMarkerHolder.holderType = HolderType.BoardMarker;
                boardMarkerHolder.contentList = new();
                list.Add(boardMarkerHolder);
            }
            _markerHolders.Add(i, list);
        }
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

    public Card[] SelectCardsFromBoard(int[][] indices)
    {
        Card[] cards = new Card[4];
        for(int i = 0; i < 4; i++)
        {
            int[] pair = indices[i];
            Card card = GetCardFromCardHolder(pair[0], pair[1]);
            cards[i] = card;
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

    public void FindParentOfCard(int cardID)
    {
        for(int col = 0; col < _cardHolders.Count; col++)
        {
            for(int row = 0; row < _cardHolders[col].Count; row++)
            {
                CardHolder holder = _cardHolders[col][row];
                Card card = (Card)holder.GetItemFromContentListByIndex(0);
                if(card && card.Data.ID == cardID)
                {
                    card.holderColIndex = col;
                    holder.RemoveItemFromContentList(card);
                    break;
                }
            }
        }
    }

    public void ToggleRayTargetOfCardsAndHolders(bool value)
    {
        for (int col = 0; col < _cardHolders.Count; col++)
        {
            for (int row = 0; row < _cardHolders[col].Count; row++)
            {
                CardHolder holder = _cardHolders[col][row];
                holder.backgroundImage.raycastTarget = value;
                foreach (Card card in holder.contentList)
                {
                    card.ToggleRayCast(value);
                }
            }
        }
    }

    public void ShowMarkersAtBoard(MarkerHolder holder, List<Marker> markers)
    {
        markers.ForEach(marker =>
        {
            MarkerHolder prevHolder = marker.parent.GetComponent<MarkerHolder>();
            if(prevHolder != null && !prevHolder.IsEmpty())
            {
                prevHolder.RemoveItemFromContentList(marker);
            }
            holder.AddToContentList(marker);
            marker.transform.position = holder.transform.position;
            marker.Rotate(holder.Direction);
        });
    }

    public void HideMarkersAtBoard(MarkerHolder holder, List<Marker> markers)
    {
        markers.ForEach(marker =>
        {
            marker.gameObject.SetActive(false);
        });
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
            Card card = SelectCardFromBoard(indices);
            EnableOverlayOfCardHolders(true, indices);
            card.Select();
        }
        else
        {
            int[][] indices = GetColAndRowIndicesOfCardHolder(holder.ID, (int)holder.Direction);
            Card[] cards = SelectCardsFromBoard(indices);
            EnableOverlayOfCardHolders(true, indices);
            foreach (Card card in cards)
            {
                card.Select();
            }
        }
    }

    public void DeSelectCards()
    {
        for(int i = 0; i < 4; i++)
        {
            for(int j = 0; j < 4; j++)
            {
                Card card = GetCardFromCardHolder(i, j);
                if(card.isSelected)
                {
                    card.isSelected = false;
                }
            }
        }
    }

    public void EnableOverlayOfCardHolders(bool value, int[] indices = null)
    {
        for(int i = 0; i < _cardHolders.Count; i++)
        {
            List<CardHolder> holders = _cardHolders[i];
            foreach(CardHolder holder in holders)
            {
                holder.blackOverlay.transform.SetAsLastSibling();
                holder.EnableOverlay(value);
            }
        }
        if(value)
        {
            _cardHolders[indices[0]][indices[1]].EnableOverlay(!value);
        }
    }

    public void EnableOverlayOfCardHolders(bool value, int[][] indices = null)
    {
        for (int i = 0; i < _cardHolders.Count; i++)
        {
            List<CardHolder> holders = _cardHolders[i];
            foreach (CardHolder holder in holders)
            {
                holder.blackOverlay.transform.SetAsLastSibling();
                holder.EnableOverlay(value);
            }
        }
        if (value)
        {
            for (int i = 0; i < 4; i++)
            {
                int[] pair = indices[i];
                _cardHolders[pair[0]][pair[1]].EnableOverlay(!value);
            }
        }
    }
}
