using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

enum NewCardHolderID // to indicate a non-existing card holder
{
    LEFT = -999,
    RIGHT = 999,
}

struct EvaluationData
{
    private readonly int _points;
    private readonly object[] _content; // stores any type/number of data associated with an evaluated action (MarkerHolder, Card, holderID)

    public int Points { get { return _points; } }
    public object[] Content { get { return _content; } }

    public EvaluationData(int points, object[] content)
    {
        _points = points;
        _content = content;
    }
}

public class NpcController : UserController
{
    private List<Card> _cardsToPlaceOnTable;
    private List<HolderData> _cardHolderDatasOnTable;
    private MarkerHolder _selectedMarkerHolder;
    private Marker _selectedMarker;
    private Card _selectedCard;
    private int[] _probabilityValues;
    private List<EvaluationData> _evaluationDatacollection;
    private List<List<CardIcon>> _campIconPairs;

    public MarkerHolder SelectedMarkerHolder {  get { return _selectedMarkerHolder; } }
    public Marker SelectedMarker { get { return _selectedMarker; } }
    public Card SelectedCard { get { return _selectedCard; } set { _selectedCard = value; } }

    public override void CreateUser(GameMode gameMode)
    {
        _tableView = transform.GetChild(0).GetComponent<NpcTableView>();
        _iconDisplayView = _tableView.transform.GetChild(1).GetComponent<IconDisplayView>();
        _infoView = _tableView.transform.GetChild(2).GetComponent<InfoView>();
        _handView = transform.GetChild(1).GetComponent<NpcHandView>();
        _markerView = transform.GetChild(2).GetComponent<NpcMarkerView>();
        _tableView.Init();
        _iconDisplayView.Init();
        _infoView.Init();
        _handView.Init();
        _markerView.Init(gameMode.CurrentUserColors[userID]);
        base.CreateUser(gameMode);
        CreateProbabilityValues(gameMode.Difficulty);
    }

    private void CreateProbabilityValues(GameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case GameDifficulty.EASY:
                _probabilityValues = new int[] { 4, 3, 2, 1 };
                break;
            case GameDifficulty.MEDIUM:
                _probabilityValues = new int[] { 1, 3, 3, 3 };
                break;
            case GameDifficulty.HARD:
                _probabilityValues = new int[] { 0, 1, 2, 7 };
                break;
            default:
                _probabilityValues = new int[] { 0, 0, 0, 10 };
                break;
        }
    }

    public void UpdateCampIconPairs(List<List<CardIcon>> campIconPairs)
    {
        _campIconPairs = campIconPairs;
    }

    public void SelectAction(List<object[]> boardContent) // this is only handling default action for now (TODO: regular/special action needs to be decided)
    {
        // regular action
        // boardContent: index0: Card (card), index1: List<MarkerHolder> (markerHolders), index2: List<int> (markerDistances)
        _evaluationDatacollection = new();
        List<Marker> remainingMarkers = _markerView.GetRemainingMarkers();
        List<Card> boardCards = GetAvailableBoardCards(boardContent, remainingMarkers);
        EvaluateCardSelection(boardCards, _handView.Cards, new());
        Dictionary<Card, int> evaluationResult = GetResultOfEvaluationPoints(boardCards);
        _selectedCard = GetSelectedEvaluatedItem(evaluationResult);
        object[] selectedBoardContent = boardContent.Find(content => (Card)content[0] == _selectedCard);
        List<MarkerHolder> holders = (List<MarkerHolder>)selectedBoardContent[1];
        List<int> markerDistances = (List<int>)selectedBoardContent[2];
        List<int> remainingMarkerDistances = remainingMarkers.Select(marker => marker.numberOnMarker).ToList();
        int contentIdx = 0;
        int currentMarkerDistance = 0;
        for (int i = 0; i < markerDistances.Count; i++)
        {
            currentMarkerDistance = markerDistances[i];
            if(remainingMarkerDistances.Contains(currentMarkerDistance)) // find first match for now (revise this later with complex evaluation)
            {
                contentIdx = i;
                break;
            }
        }
        _selectedMarkerHolder = holders[contentIdx];
        int markerIndex = remainingMarkers.FindIndex(marker => marker.numberOnMarker == currentMarkerDistance || marker.numberOnMarker == 5);
        _selectedMarker = _markerView.GetCurrentMarker(markerIndex);
        //TODO: implement these in EvaluateCardSelection():
        // 1. place board cards on each of table cards (check separately landscape cards/ exclude ground cards) - check road tokens for landscape cards -> check for fulfilled camp icons
        // 1/a. if board card can be placed on table, check every hand card if can be put on table (including secondary cards), also keep track of unfulfilled hand cards -> check for fulfilled camp icons
        // 1/b. check if any board card icon can fulifll unfulfilled hand cards' requirements (if yes, score board card)
        // 3. place hand cards on table, check every board card if can be put on table (including secondary cards) -> check for fulfilled camp icons
        // 4. place every primary board card on hand card - considering future synergies
        // 5. place every primary hand card on board card - considering future synergies
        // 6 consider having max 2 of the same ground icon/top icon - otherwise deduct evaluation point

        remainingMarkers.ForEach(marker => marker.gameObject.SetActive(marker == _selectedMarker));
        Debug.Log("distance " + currentMarkerDistance);
    }

    public void SelectCardToPlace(List<object[]> boardContent)
    {
        _evaluationDatacollection = new();
        List<Marker> remainingMarkers = _markerView.GetRemainingMarkers();
        List<Card> cardsInHand = _handView.Cards.Where(card => PassedBasicRequirements(card.Data)).ToList();
        List<Card> boardCards = GetAvailableBoardCards(boardContent, remainingMarkers);
        EvaluateCardPlacement(cardsInHand, boardCards);
        Dictionary<Card, int> evaluationResult = GetResultOfEvaluationPoints(cardsInHand);
        Card selectedCard = GetSelectedEvaluatedItem(evaluationResult);
        _cardsToPlaceOnTable = selectedCard == null ? new List<Card>() : new List<Card>() { selectedCard }; // handle only 1 card for now (revise this later with complex evaluation)
        Dictionary<Card, int> cardHolderIDs = GetHolderIDsByEvaluationPoints(); // num of entries depending on _cardsToPlaceOnTable count
        _cardHolderDatasOnTable = GetSelectedHolderDatas(cardHolderIDs);
    }

    public void SelectRow(List<object[]> boardContent)
    {
        _evaluationDatacollection = new();
        object[] items = PrepareBoardContentForRowSelection(boardContent, MarkerDirection.RIGHT);
        List<Card>[] boardCards = (List<Card>[])items[0];
        List<MarkerHolder> holders = (List<MarkerHolder>)items[1];
        EvaluateRowPick(holders, boardCards);
        Dictionary<MarkerHolder, int> evaluationResult = GetResultOfEvaluationPoints(holders);
        _selectedMarkerHolder = GetSelectedEvaluatedItem(evaluationResult);
        _selectedMarker = _markerView.GetCurrentMarker(0);
        evaluationResult.ToList().ForEach(p =>
        {
            Debug.Log("row value: " + p.Value);
        });
    }

    public void SelectInitialGroundCard(List<object[]> boardContent, List<Card> groundCards)
    {
        _evaluationDatacollection = new();
        EvaluateCardSelection(groundCards, _handView.Cards, GetAvailableBoardCards(boardContent, null));
        Dictionary<Card, int> evaluationResult = GetResultOfEvaluationPoints(groundCards);
        _selectedCard = GetSelectedEvaluatedItem(evaluationResult);
        evaluationResult.ToList().ForEach(p =>
        {
            Debug.Log("groundCard value: " + p.Value);
        });
    }

    private void EvaluateRowPick(List<MarkerHolder> holders, List<Card>[] boardCards)
    {
        string tag = "RectLeft";
        NpcTableView tableView = _tableView as NpcTableView;
        tableView.DisposeStates();
        tableView.SaveState();
        int startingStateIndex = tableView.GetLastTableStateIndex();
        for (int row = 0; row < boardCards.Length; row++)
        {
            tableView.LoadState(startingStateIndex);
            object[] evaluationContent = new object[] { holders[row] };
            List<CardIcon[]> rowTopIcons = new();
            List<Card> rowCards = boardCards[row];
            Card landscapeCard = rowCards[0].Data.cardType == CardType.Landscape ? rowCards[0] : null;
            List<Card> scoringCards = rowCards.Where(card => card.Data.cardType != CardType.Ground).ToList();
            Card groundCard = rowCards[rowCards.Count - 1];
            rowTopIcons.Add(groundCard.Data.icons);
            tableView.AddNewPrimaryHolder(tag);
            HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
            ExecuteEvaluationCardPlacement(primaryHolderData, groundCard, new object[0]);
            int groundCardStateIndex = tableView.GetLastTableStateIndex();
            for (int col = 0; col < scoringCards.Count; col++) // stack every combination of cards on ground card
            {
                tableView.LoadState(groundCardStateIndex);
                Card firstCard = scoringCards[col];
                if (TryPlaceCard(primaryHolderData, firstCard.Data))
                {
                    ExecuteEvaluationCardPlacement(primaryHolderData, firstCard, evaluationContent);
                    int firstCardStateIndex = tableView.GetLastTableStateIndex();
                    List<Card> remainingCards = scoringCards.Where(card => card != firstCard).ToList();
                    List<List<Card>> cardCombinations = new() { new List<Card>() { remainingCards[0], remainingCards[1] }, new List<Card>() { remainingCards[1], remainingCards[0] } };
                    for (int j = 0; j < cardCombinations.Count; j++)
                    {
                        tableView.LoadState(firstCardStateIndex);
                        List<Card> combination = cardCombinations[j];
                        if (TryPlaceCard(primaryHolderData, combination[0].Data))
                        {
                            ExecuteEvaluationCardPlacement(primaryHolderData, combination[0], evaluationContent);
                            if(TryPlaceCard(primaryHolderData, combination[1].Data))
                            {
                                ExecuteEvaluationCardPlacement(primaryHolderData, combination[1], evaluationContent);
                            }
                        }
                    }
                }

                if (landscapeCard && (landscapeCard.Data.requirements.Where(icon => icon == CardIcon.RoadToken).Count() < landscapeCard.Data.requirements.Length || col == 0)) // check if landscape card placement is possible
                {
                    tableView.AddNewSecondaryHolder();
                    if (TryPlaceCard(tableView.GetLastSecondaryHolderData(), landscapeCard.Data))
                    {
                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(landscapeCard), evaluationContent));
                    }
                }

                if (firstCard.Data.cardType == CardType.Observation) // add scoring cards' top icons
                {
                    rowTopIcons.Add(firstCard.Data.icons);
                }
            }

            for (int i = 0; i < rowTopIcons.Count; i++) // add evaluation points by number of top icons of row
            {
                CardIcon[] icons = rowTopIcons[i];
                int houseIconPoints = Array.Exists(icons, icon => icon == CardIcon.House) ? 2 : 0;
                _evaluationDatacollection.Add(new EvaluationData(houseIconPoints + icons.Length, evaluationContent));
            }

            CompareTopIconsWithCampIcons(rowTopIcons, evaluationContent);
        }
        tableView.LoadState(startingStateIndex);
    }

    private void EvaluateCardSelection(List<Card> selectableCards, List<Card> otherCards1, List<Card> otherCards2)
    {
        if(selectableCards.Count == 0)
        {
            Debug.Log("NO CARDS");
            return;
        }

        NpcTableView tableView = _tableView as NpcTableView;
        tableView.DisposeStates();
        tableView.SaveState();
        int startingStateIndex = tableView.GetLastTableStateIndex();
        List<Card>[] unfulfilledOtherCards1 = GetUnfulfilledCardsByHolderSubType(startingStateIndex, otherCards1); // card collection that can't be placed immediately
        List<Card>[] unfulfilledOtherCards2 = GetUnfulfilledCardsByHolderSubType(startingStateIndex, otherCards2); // card collection that can't be placed immediately
        List<Card>[] separatedSelectableCards = SeparateCardsByHolderSubType(selectableCards);
        Card selectableCard;
        object[] evaluationContent;
        for (int groupIndex = 0; groupIndex < separatedSelectableCards.Length; groupIndex++)
        {
            List<Card> selectableCardsGroup = separatedSelectableCards[groupIndex];
            if (groupIndex == 0) // all primary cards
            {
                for (int i = 0; i < selectableCardsGroup.Count; i++)
                {
                    selectableCard = selectableCardsGroup[i];
                    if (selectableCard.Data.cardType == CardType.Ground) // ground card
                    {
                        string[] tags = new string[] { "RectLeft", "RectRight" };
                        _evaluationDatacollection.Add(new EvaluationData(selectableCard.Data.score + 1, new object[] { selectableCard, NewCardHolderID.LEFT }));
                        for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++)
                        {
                            tableView.LoadState(startingStateIndex);
                            string tag = tags[tagIndex];
                            tableView.AddNewPrimaryHolder(tag);
                            HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
                            evaluationContent = new object[] { selectableCard, tagIndex == 0 ? NewCardHolderID.LEFT : NewCardHolderID.RIGHT };
                            ExecuteEvaluationCardPlacement(primaryHolderData, selectableCard, new object[0]);
                            int groundCardStateIndex = tableView.GetLastTableStateIndex();
                            CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), evaluationContent);
                            PlaceUnfulfilledCards(groundCardStateIndex, evaluationContent, unfulfilledOtherCards1);
                            PlaceUnfulfilledCards(groundCardStateIndex, evaluationContent, unfulfilledOtherCards2);
                        }
                    }
                    else // observation card
                    {
                        for (int j = 0; j < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; j++)
                        {
                            tableView.LoadState(startingStateIndex);
                            HolderData holderData = tableView.ActiveState.PrimaryCardHolderDataCollection[j];
                            evaluationContent = new object[] { selectableCard, holderData.ID };
                            if (j == 0) // give a base evaluation point to card
                            {
                                _evaluationDatacollection.Add(new EvaluationData(selectableCard.Data.score + 1, evaluationContent));
                            }
                            if (TryPlaceCard(holderData, selectableCard.Data))
                            {
                                ExecuteEvaluationCardPlacement(holderData, selectableCard, evaluationContent);
                                int observationCardStateIndex = tableView.GetLastTableStateIndex();
                                CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), evaluationContent);
                                PlaceUnfulfilledCards(observationCardStateIndex, evaluationContent, new List<Card>[] { unfulfilledOtherCards1[0], new() });
                                PlaceUnfulfilledCards(observationCardStateIndex, evaluationContent, new List<Card>[] { unfulfilledOtherCards2[0], new() });
                            }

                            for (int k = 0; k < unfulfilledOtherCards1[0].Count; k++)
                            {
                                tableView.LoadState(startingStateIndex);
                                Card card = unfulfilledOtherCards1[0][k];
                                if (TryPlaceCard(holderData, card.Data))
                                {
                                    evaluationContent = new object[0];
                                    ExecuteEvaluationCardPlacement(holderData, card, evaluationContent);
                                    int observationCardStateIndex = tableView.GetLastTableStateIndex();
                                    for (int l = 0; l < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; l++)
                                    {
                                        tableView.LoadState(observationCardStateIndex);
                                        HolderData data = tableView.ActiveState.PrimaryCardHolderDataCollection[l];
                                        if (TryPlaceCard(data, selectableCard.Data))
                                        {
                                            evaluationContent = new object[] { selectableCard, holderData.ID };
                                            ExecuteEvaluationCardPlacement(data, selectableCard, evaluationContent);
                                            CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), evaluationContent);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else // all secondary cards
            {
                for (int i = 0; i < selectableCardsGroup.Count; i++)
                {
                    tableView.LoadState(startingStateIndex);
                    selectableCard = selectableCardsGroup[i];
                    if (selectableCard.Data.cardType == CardType.Landscape)
                    {
                        tableView.AddNewSecondaryHolder();
                        HolderData holderData = tableView.GetLastSecondaryHolderData();
                        evaluationContent = new object[] { selectableCard, NewCardHolderID.RIGHT };
                        _evaluationDatacollection.Add(new EvaluationData(selectableCard.Data.score + 1, evaluationContent)); // give a base evaluation point to card
                        if (TryPlaceCard(holderData, selectableCard.Data))
                        {
                            ExecuteEvaluationCardPlacement(holderData, selectableCard, evaluationContent);
                            int landscapeCardStateIndex = tableView.GetLastTableStateIndex();
                            for (int j = 0; j < unfulfilledOtherCards1[1].Count; j++) // place otherCards1 on selectable (landscape) card
                            {
                                tableView.LoadState(landscapeCardStateIndex);
                                Card card = unfulfilledOtherCards1[1][j];
                                if (TryPlaceCard(holderData, card.Data))
                                {
                                    _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), evaluationContent));
                                }
                            }

                            for (int j = 0; j < unfulfilledOtherCards2[1].Count; j++) // place otherCards2 on selectable (landscape) card
                            {
                                tableView.LoadState(landscapeCardStateIndex);
                                Card card = unfulfilledOtherCards2[1][j];
                                if (TryPlaceCard(holderData, card.Data))
                                {
                                    _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), evaluationContent));
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < tableView.ActiveState.SecondaryCardHolderDataCollection.Count; j++) // place selectable (discovery) card
                        {
                            HolderData holderData = tableView.ActiveState.SecondaryCardHolderDataCollection[j];
                            evaluationContent = new object[] { selectableCard, holderData.ID };
                            if (j == 0) // give a base evaluation point to card
                            {
                                _evaluationDatacollection.Add(new EvaluationData(selectableCard.Data.score + 1, evaluationContent));
                            }
                            if (TryPlaceCard(holderData, selectableCard.Data))
                            {
                                _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), evaluationContent));
                            }
                        }
                    }
                }
            }
        }
        tableView.LoadState(startingStateIndex);
    }

    private void EvaluateCardPlacement(List<Card> selectableCards, List<Card> boardCards)
    {
        if (selectableCards.Count == 0)
        {
            Debug.Log("NO CARDS");
            return;
        }

        NpcTableView tableView = _tableView as NpcTableView;
        tableView.DisposeStates();
        tableView.SaveState();
        int startingStateIndex = tableView.GetLastTableStateIndex();
        List<Card>[] unfulfilledBoardCards = GetUnfulfilledCardsByHolderSubType(startingStateIndex, boardCards); // card collection that can't be placed immediately
        List<Card>[] separatedSelectableCards = SeparateCardsByHolderSubType(selectableCards);
        Card selectableCard;
        object[] evaluationContent; // index0: Card card, index1: int holderID, index2: int pointModifier
        for (int groupIndex = 0; groupIndex < separatedSelectableCards.Length; groupIndex++)
        {
            List<Card> selectableCardsGroup = separatedSelectableCards[groupIndex];
            if (groupIndex == 0) // all primary cards
            {
                for (int i = 0; i < selectableCardsGroup.Count; i++)
                {
                    selectableCard = selectableCardsGroup[i];
                    if (selectableCard.Data.cardType == CardType.Ground) // ground card
                    {
                        string[] tags = new string[] { "RectLeft", "RectRight" };
                        for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++)
                        {
                            tableView.LoadState(startingStateIndex);
                            string tag = tags[tagIndex];
                            tableView.AddNewPrimaryHolder(tag);
                            HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
                            evaluationContent = new object[] { selectableCard, tagIndex == 0 ? NewCardHolderID.LEFT : NewCardHolderID.RIGHT };
                            ExecuteEvaluationCardPlacement(primaryHolderData, selectableCard, evaluationContent);
                            int groundCardStateIndex = tableView.GetLastTableStateIndex();
                            CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), evaluationContent);
                            PlaceUnfulfilledCards(groundCardStateIndex, evaluationContent, unfulfilledBoardCards);
                        }
                    }
                    else // observation card
                    {
                        for (int j = 0; j < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; j++)
                        {
                            tableView.LoadState(startingStateIndex);
                            HolderData holderData = tableView.ActiveState.PrimaryCardHolderDataCollection[j];
                            bool canBePlaced = false;
                            if (TryPlaceCard(holderData, selectableCard.Data))
                            {
                                canBePlaced = true;
                                Card topCard = (Card)holderData.GetItemFromContentListByIndex(holderData.ContentList.Count - 1);
                                int pointModifier = topCard?.Data.icons.Length > 1 ? -4 : 0;
                                evaluationContent = new object[] { selectableCard, holderData.ID, pointModifier };
                                ExecuteEvaluationCardPlacement(holderData, selectableCard, evaluationContent);
                                int observationCardStateIndex = tableView.GetLastTableStateIndex();
                                CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), evaluationContent);
                                PlaceUnfulfilledCards(observationCardStateIndex, evaluationContent, new List<Card>[] { unfulfilledBoardCards[0], new() });
                            }

                            if (canBePlaced)
                            {
                                for (int k = 0; k < unfulfilledBoardCards[0].Count; k++)
                                {
                                    tableView.LoadState(startingStateIndex);
                                    Card card = unfulfilledBoardCards[0][k];
                                    if (TryPlaceCard(holderData, card.Data))
                                    {
                                        evaluationContent = new object[0];
                                        ExecuteEvaluationCardPlacement(holderData, card, evaluationContent);
                                        int observationCardStateIndex = tableView.GetLastTableStateIndex();
                                        for (int l = 0; l < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; l++)
                                        {
                                            tableView.LoadState(observationCardStateIndex);
                                            HolderData data = tableView.ActiveState.PrimaryCardHolderDataCollection[l];
                                            if (TryPlaceCard(data, selectableCard.Data))
                                            {
                                                evaluationContent = new object[] { selectableCard, holderData.ID };
                                                ExecuteEvaluationCardPlacement(data, selectableCard, evaluationContent);
                                                CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), evaluationContent);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else // all secondary cards
            {
                for (int i = 0; i < selectableCardsGroup.Count; i++)
                {
                    tableView.LoadState(startingStateIndex);
                    selectableCard = selectableCardsGroup[i];
                    if (selectableCard.Data.cardType == CardType.Landscape)
                    {
                        tableView.AddNewSecondaryHolder();
                        HolderData holderData = tableView.GetLastSecondaryHolderData();
                        if (TryPlaceCard(holderData, selectableCard.Data))
                        {
                            evaluationContent = new object[] { selectableCard, NewCardHolderID.RIGHT };
                            ExecuteEvaluationCardPlacement(holderData, selectableCard, evaluationContent);
                            int landscapeCardStateIndex = tableView.GetLastTableStateIndex();
                            for (int j = 0; j < unfulfilledBoardCards[1].Count; j++) // place otherCards1 on selectable (landscape) card
                            {
                                tableView.LoadState(landscapeCardStateIndex);
                                Card card = unfulfilledBoardCards[1][j];
                                if (TryPlaceCard(holderData, card.Data))
                                {
                                    _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), evaluationContent));
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < tableView.ActiveState.SecondaryCardHolderDataCollection.Count; j++) // place selectable (discovery) card
                        {
                            HolderData holderData = tableView.ActiveState.SecondaryCardHolderDataCollection[j];
                            if (TryPlaceCard(holderData, selectableCard.Data))
                            {
                                evaluationContent = new object[] { selectableCard, holderData.ID };
                                _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), evaluationContent));
                            }
                        }
                    }
                }
            }
        }
        tableView.LoadState(startingStateIndex);
    }

    private List<Card> GetAvailableBoardCards(List<object[]> boardContent, List<Marker> remainingMarkers)
    {
        int[] markerDistances = remainingMarkers?.Select(marker => marker.numberOnMarker).ToArray();
        List<Card> availableCards = new();
        for (int i = 0; i < boardContent.Count; i++) // filter cards with no available holders to reach/available marker to reach
        {
            object[] content = boardContent[i];
            List<int> holderDistances = (List<int>)content[2];
            if(remainingMarkers == null)
            {
                availableCards.Add((Card)content[0]);
            }
            else if (holderDistances.Count > 0)
            {
                for (int j = 0; j < holderDistances.Count; j++)
                {
                    int holderDistance = holderDistances[j];
                    if (Array.Exists(markerDistances, markerDistance => markerDistance == holderDistance) || markerDistances.Contains(5))
                    {
                        availableCards.Add((Card)content[0]);
                        break;
                    }
                }
            }
        }
        return availableCards;
    }

    private object[] PrepareBoardContentForRowSelection(List<object[]> boardContent, MarkerDirection direction)
    {
        List<Card>[] cardsByRow = new List<Card>[] { new(), new(), new(), new() };
        List<MarkerHolder> holders = new();
        int numOfRows = cardsByRow.Length;
        int rowIndex = 0;
        for(int i = 0; i < boardContent.Count; i++)
        {
            object[] item = boardContent[i];
            Card card = (Card)item[0];
            cardsByRow[rowIndex].Add(card);
            if (holders.Count < numOfRows)
            {
                MarkerHolder holder = ((List<MarkerHolder>)item[1]).Find(holder => holder.Direction == direction);
                holders.Add(holder);
            }
            rowIndex = rowIndex == numOfRows - 1 ? 0 : rowIndex + 1;
        }
        return new object[] { cardsByRow, holders };
    }

    private List<Card>[] GetUnfulfilledCardsByHolderSubType(int stateIndex, List<Card> cards)
    {
        NpcTableView tableView = _tableView as NpcTableView;
        List<Card> unfulfilledPrimaryCards = new();
        List<Card> unfulfilledSecondaryCards = new();
        for (int i = 0; i < cards.Count; i++)
        {
            tableView.LoadState(stateIndex);
            List<HolderData> primaryHolderDataCollection = tableView.ActiveState.PrimaryCardHolderDataCollection;
            List<HolderData> secondaryHolderDataCollection = tableView.ActiveState.SecondaryCardHolderDataCollection;
            Card card = cards[i];
            if (primaryHolderDataCollection.Count > 0 && card.Data.cardType == CardType.Observation)
            {
                bool canBePlaced = false;
                for (int j = 0; j < primaryHolderDataCollection.Count; j++)
                {
                    HolderData holderData = primaryHolderDataCollection[j];
                    if (TryPlaceCard(holderData, card.Data))
                    {
                        canBePlaced = true;
                        break;
                    }
                }
                if(!canBePlaced)
                {
                    unfulfilledPrimaryCards.Add(card);
                }
            }
            else if(card.Data.cardType == CardType.Observation)
            {
                unfulfilledPrimaryCards.Add(card);
            }

            if (secondaryHolderDataCollection.Count > 0 && card.Data.cardType == CardType.Discovery)
            {
                bool canBePlaced = false;
                for (int k = 0; k < secondaryHolderDataCollection.Count; k++)
                {
                    HolderData holderData = secondaryHolderDataCollection[k];
                    if (TryPlaceCard(holderData, card.Data))
                    {
                        canBePlaced = true;
                        break;
                    }
                }
                if (!canBePlaced)
                {
                    unfulfilledSecondaryCards.Add(card);
                }
            }
            else if(card.Data.cardType == CardType.Discovery)
            {
                unfulfilledSecondaryCards.Add(card);
            }

            if (card.Data.cardType == CardType.Landscape)
            {
                tableView.AddNewSecondaryHolder();
                HolderData holderData = tableView.GetLastSecondaryHolderData();
                if (!TryPlaceCard(holderData, card.Data))
                {
                    unfulfilledSecondaryCards.Add(card);
                }
            }
        }
        return new List<Card>[] { unfulfilledPrimaryCards, unfulfilledSecondaryCards };
    }

    private void PlaceUnfulfilledCards(int stateIndex, object[] evaluationContent, List<Card>[] unfulfilledCards)
    {
        NpcTableView tableView = _tableView as NpcTableView;
        for (int i = 0; i < unfulfilledCards[0].Count; i++) // primary cards
        {
            Card card = unfulfilledCards[0][i];
            bool canBePlacedAnywhere = false;
            for (int j = 0; j < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; j++) // place cards in every primary holder
            {
                tableView.LoadState(stateIndex);
                HolderData holderData = tableView.ActiveState.PrimaryCardHolderDataCollection[j];
                if (TryPlaceCard(holderData, card.Data))
                {
                    ExecuteEvaluationCardPlacement(holderData, card, evaluationContent);
                    CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), evaluationContent);
                    canBePlacedAnywhere = true;
                }
            }

            if (!canBePlacedAnywhere)
            {
                Card evaluatedCard = (Card)evaluationContent[0];
                CheckNumberOfRequirementsFulfilledBySelectableCard(tableView.GetAllRelevantIcons(HolderSubType.PRIMARY).Except(evaluatedCard.Data.icons).ToList(), new List<CardIcon>(evaluatedCard.Data.icons), card.Data, evaluationContent);
            }
        }

        for (int i = 0; i < unfulfilledCards[1].Count; i++) // secondary cards
        {
            tableView.LoadState(stateIndex);
            Card card = unfulfilledCards[1][i];
            bool canBePlacedAnywhere = false;
            if (card.Data.cardType == CardType.Discovery)
            {
                for (int j = 0; j < tableView.ActiveState.SecondaryCardHolderDataCollection.Count; j++) // place cards in every secondary holder
                {
                    HolderData holderData = tableView.ActiveState.SecondaryCardHolderDataCollection[j];
                    if (TryPlaceCard(holderData, card.Data))
                    {
                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(card), evaluationContent));
                        canBePlacedAnywhere = true;
                    }
                }
            }
            else
            {
                tableView.AddNewSecondaryHolder();
                HolderData holderData = tableView.GetLastSecondaryHolderData();
                if (TryPlaceCard(holderData, card.Data))
                {
                    _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(card), evaluationContent));
                    canBePlacedAnywhere = true;
                }
            }

            if (!canBePlacedAnywhere)
            {
                Card evaluatedCard = (Card)evaluationContent[0];
                CheckNumberOfRequirementsFulfilledBySelectableCard(tableView.GetAllRelevantIcons(HolderSubType.PRIMARY).Except(evaluatedCard.Data.icons).ToList(), new List<CardIcon>(evaluatedCard.Data.icons), card.Data, evaluationContent);
            }
        }
    }

    private void CompareTopIconsWithCampIcons(List<CardIcon[]> topIcons, object[] evaluationContent)
    {
        int points = GetNextCampScoreToken();
        if(points == 0)
        {
            return;
        }

        List<List<CardIcon>> pairs = CreateAdjacentIconPairs(topIcons);
        for (int i = 0; i < pairs.Count; i++)
        {
            List<CardIcon> pair = pairs[i];
            for (int j = 0; j < _campIconPairs.Count; j++)
            {
                if (pair.OrderBy(x => x).SequenceEqual(_campIconPairs[j].OrderBy(x => x)))
                {
                    _evaluationDatacollection.Add(new EvaluationData(points, evaluationContent));
                }
            }
        }
    }

    private void CheckNumberOfRequirementsFulfilledBySelectableCard(List<CardIcon> allRelevantIcons, List<CardIcon> selectableCardIcons, CardData cardData, object[] evaluationContent) // global icon check only!
    {
        List<CardIcon> cardRequirements = new(cardData.requirements);
        List<CardIcon> cardOptionalRequirements = new(cardData.optionalRequirements);
        List<CardIcon> cardAdjacentRequirements = new(cardData.adjacentRequirements);
        if (cardRequirements.Count > 0)
        {
            for (int i = cardRequirements.Count - 1; i >= 0; i--)
            {
                CardIcon requirementIcon = cardRequirements[i];
                for (int j = allRelevantIcons.Count - 1; j >= 0; j--)
                {
                    CardIcon icon = allRelevantIcons[j];
                    if (requirementIcon == icon)
                    {
                        cardRequirements.RemoveAt(i);
                        allRelevantIcons.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        List<CardIcon> optionalRequirements = cardOptionalRequirements;
        bool isAdjacentAndOptionalRequirement = cardAdjacentRequirements.Count == 2;
        if(isAdjacentAndOptionalRequirement) // examine adjacent requirements only as optional requirements
        {
            optionalRequirements.AddRange(cardAdjacentRequirements);
        }
        List<CardIcon[]> optionalRequirementIconPairs = CreateIconPairsFromRequirements(optionalRequirements);
        if (optionalRequirementIconPairs.Count > 0)
        {
            for (int i = optionalRequirementIconPairs.Count - 1; i >= 0; i--)
            {
                CardIcon[] pair = optionalRequirementIconPairs[i];
                for (int j = allRelevantIcons.Count - 1; j >= 0; j--)
                {
                    CardIcon icon = allRelevantIcons[j];
                    if (pair.Contains(icon))
                    {
                        optionalRequirementIconPairs.RemoveAt(i);
                        allRelevantIcons.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        if(!isAdjacentAndOptionalRequirement)
        {
            for (int i = cardAdjacentRequirements.Count - 1; i >= 0; i--)
            {
                CardIcon requirementIcon = cardAdjacentRequirements[i];
                for (int j = allRelevantIcons.Count - 1; j >= 0; j--)
                {
                    CardIcon icon = allRelevantIcons[j];
                    if (requirementIcon == icon)
                    {
                        cardAdjacentRequirements.RemoveAt(i);
                        allRelevantIcons.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        int remainingRequirementIcons = cardRequirements.Count;
        int remainingOptionalRequirementPairs = optionalRequirementIconPairs.Count;
        int remainingAdjacentRequirementIcons = isAdjacentAndOptionalRequirement ? 0 : cardAdjacentRequirements.Count;
        for (int i = 0; i < selectableCardIcons.Count; i++) // check number of fulfilled icons in total
        {
            CardIcon icon = selectableCardIcons[i];
            if (cardRequirements.Count > 0)
            {
                for (int j = cardRequirements.Count - 1; j >= 0; j--)
                {
                    CardIcon requirementIcon = cardRequirements[j];
                    if (icon == requirementIcon)
                    {
                        cardRequirements.RemoveAt(j);
                        break;
                    }
                }
            }

            if (optionalRequirementIconPairs.Count > 0)
            {
                for (int j = optionalRequirementIconPairs.Count - 1; j >= 0; j--)
                {
                    CardIcon[] pair = optionalRequirementIconPairs[j];
                    if (pair.Contains(icon))
                    {
                        optionalRequirementIconPairs.RemoveAt(j);
                        break;
                    }
                }
            }

            if(!isAdjacentAndOptionalRequirement && cardAdjacentRequirements.Count > 0)
            {
                for (int j = cardAdjacentRequirements.Count - 1; j >= 0; j--)
                {
                    CardIcon requirementIcon = cardAdjacentRequirements[j];
                    if (icon == requirementIcon)
                    {
                        cardAdjacentRequirements.RemoveAt(j);
                        break;
                    }
                }
            }
        }
        int points = (remainingRequirementIcons - cardRequirements.Count) + (remainingOptionalRequirementPairs - optionalRequirementIconPairs.Count) + (remainingAdjacentRequirementIcons - cardAdjacentRequirements.Count);
        _evaluationDatacollection.Add(new EvaluationData(points, evaluationContent));
    }

    private List<Card>[] SeparateCardsByHolderSubType(List<Card> cards)
    {
        List<Card>[] separatedCards = new List<Card>[] { new(), new() };
        cards.ForEach(card =>
        {
            if(Array.Exists(new CardType[] { CardType.Ground, CardType.Observation }, type => type == card.Data.cardType))
            {
                separatedCards[0].Add(card);
            }
            else
            {
                separatedCards[1].Add(card);
            }
        });
        return separatedCards;
    }

    private int GetCardPlacementEvaluationPoints(Card card)
    {
        int totalPoints = card.Data.score;
        switch (card.Data.cardType)
        {
            case CardType.Observation:
            case CardType.Landscape:
            case CardType.Discovery:
                totalPoints += 8;
                break;
            default:
                totalPoints += 4;
                break;
        }
        return totalPoints;
    }

    private Dictionary<T, int> GetResultOfEvaluationPoints<T>(List<T> items)
    {
        Dictionary<T, int> evaluationResults = new();
        if(_evaluationDatacollection.Count > 0)
        {
            items.ForEach(item => evaluationResults[item] = 0);
            for (int i = 0; i < _evaluationDatacollection.Count; i++)
            {
                EvaluationData data = _evaluationDatacollection[i];
                T item = (T)data.Content[0];
                evaluationResults[item] += data.Points;
            }
        }
        return evaluationResults;
    }

    private T GetSelectedEvaluatedItem<T>(Dictionary<T, int> evaluationResult)
    {
        if(evaluationResult.Count > 0)
        {
            Dictionary<T, int> orderedEvaluationResult = evaluationResult.OrderBy(e => e.Value).ToDictionary(x => x.Key, x => x.Value);
            List<int> pointsByOrder = orderedEvaluationResult.Select(e => e.Value).ToList();
            List<int> values = new();
            List<int> probabilityValues = _probabilityValues.ToList();
            if (pointsByOrder.Count < probabilityValues.Count) // check if there's less options than difficulty levels (e.g. card/deck selection)
            {
                probabilityValues = new();
                for (int i = _probabilityValues.Length - 1; i >= 0; i--)
                {
                    probabilityValues.Insert(0, _probabilityValues[i]);
                    if (probabilityValues.Count == pointsByOrder.Count)
                    {
                        break;
                    }
                }
            }
            else if (pointsByOrder.Count > probabilityValues.Count) // keep best options
            {
                List<int> copyOfPointsByOrder = new(pointsByOrder);
                pointsByOrder = new();
                int length = copyOfPointsByOrder.Count - probabilityValues.Count - 1;
                for (int i = copyOfPointsByOrder.Count - 1; i > length; i--)
                {
                    pointsByOrder.Add(copyOfPointsByOrder[i]);
                }
                pointsByOrder.Reverse();
            }

            for (int i = 0; i < probabilityValues.Count; i++)
            {
                int probability = probabilityValues[i];
                for (int j = 0; j < probability; j++)
                {
                    values.Add(pointsByOrder[i]);
                }
            }
            System.Random random = new();
            int randomlySelectedEvaluationPoint = values[random.Next(0, values.Count - 1)];
            return orderedEvaluationResult.FirstOrDefault(x => x.Value == randomlySelectedEvaluationPoint).Key;
        }
        else
        {
            return default;
        }
    }

    private Dictionary<Card, int> GetHolderIDsByEvaluationPoints()
    {
        object[] collectionOfholderIDs = new object[_cardsToPlaceOnTable.Count];
        for (int i = 0; i < _cardsToPlaceOnTable.Count; i++) // collect points by holderIDs
        {
            Card card = _cardsToPlaceOnTable[i];
            Dictionary<int, int> pointsByHolderIDs = new();
            for (int j = 0; j < _evaluationDatacollection.Count; j++)
            {
                EvaluationData data = _evaluationDatacollection[j];
                if ((Card)data.Content[0] == card)
                {
                    int points = data.Points;
                    int holderID = (int)data.Content[1];
                    if (pointsByHolderIDs.ContainsKey(holderID))
                    {
                        pointsByHolderIDs[holderID] += points;
                    }
                    else
                    {
                        pointsByHolderIDs[holderID] = points;
                    }
                    
                }
            }
            collectionOfholderIDs[i] = pointsByHolderIDs;
        }

        Dictionary<Card, int> result = new();
        for (int i = 0; i < collectionOfholderIDs.Length; i++) // select holderID by highest points
        {
            Card card = _cardsToPlaceOnTable[i];
            Dictionary<int, int> pointsByHolderIDs = (Dictionary<int, int>)collectionOfholderIDs[i];
            int holderID = pointsByHolderIDs.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            result[card] = holderID;
        }
        return result;
    }

    private List<HolderData> GetSelectedHolderDatas(Dictionary<Card, int> cardHolderIDs)
    {
        NpcTableView tableView = _tableView as NpcTableView;
        List<HolderData> result = new();
        cardHolderIDs.ToList().ForEach(e =>
        {
            Card card = e.Key;
            int holderID = cardHolderIDs[card];
            if (card.Data.cardType == CardType.Ground)
            {
                string tag = holderID == (int)NewCardHolderID.LEFT ? "RectLeft" : "RectRight";
                tableView.AddNewPrimaryHolder(tag);
                result.Add(tableView.GetPrimaryHolderDataByTag(tag));
            }
            else if (card.Data.cardType == CardType.Landscape)
            {
                tableView.AddNewSecondaryHolder();
                result.Add(tableView.GetLastSecondaryHolderData());
            }
            else if (card.Data.cardType == CardType.Observation)
            {
                result.Add(tableView.ActiveState.PrimaryCardHolderDataCollection.Find(data => data.ID == holderID));
            }
            else if (card.Data.cardType == CardType.Discovery)
            {
                result.Add(tableView.ActiveState.SecondaryCardHolderDataCollection.Find(data => data.ID == holderID));
            }
        });
        return result;
    }

    protected override void UpdateCardHolders(HolderSubType subType, string hitAreaTag)
    {
        if (subType == HolderSubType.PRIMARY)
        {
            (_tableView as NpcTableView).AddNewPrimaryHolder(hitAreaTag);
        }
        else if (subType == HolderSubType.SECONDARY)
        {
            (_tableView as NpcTableView).AddNewSecondaryHolder();
        }
    }

    public void ShowMarkerPlacementHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask, MarkerHolder, Marker>)(_markerView as NpcMarkerView).PlaceMarkerToHolder, _selectedMarkerHolder, _selectedMarker);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void PlaceCardOnTableHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                if (_selectedCard) // initial ground card placement
                {
                    string tag = "RectLeft";
                    UpdateCardHolders(HolderSubType.PRIMARY, tag);
                    HolderData holderData = (_tableView as NpcTableView).GetPrimaryHolderDataByTag(tag);
                    ExecuteCardPlacement(holderData, _selectedCard);
                }
                else if (_cardsToPlaceOnTable.Count > 0) // evaluated card placement
                {
                    Debug.Log("num of cards: " + _cardsToPlaceOnTable.Count);
                    (_tableView as NpcTableView).ActiveState.PrimaryCardHolderDataCollection.ForEach(data => Debug.Log("holderID: " + data.ID));
                    for (int i = 0; i < _cardsToPlaceOnTable.Count; i++)
                    {
                        Card currentCard = _cardsToPlaceOnTable[i];
                        HolderData holderData = _cardHolderDatasOnTable[i];
                        ExecuteCardPlacement(holderData, currentCard);
                        Debug.Log("card's first icon: " + currentCard.Data.icons?[0]);
                        Debug.Log("selected holderID: " + holderData.ID);
                    }
                }
                task.StartDelayMs(0);
                break;
            case 1:
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.waitDelay) * 1000));
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void UpdateDisplayIconsHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                List<Card> cards = _selectedCard ? new() { _selectedCard } : _cardsToPlaceOnTable;
                task.StartHandler(GetUpdateDisplayIconsHandler(), cards, _tableView.ActiveState.PrimaryCardHolderDataCollection);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void RegisterScoreHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                if (_cardsToPlaceOnTable.Count > 0)
                {
                    int totalScore = _cardsToPlaceOnTable.Select(card => card.Data.score).Sum();
                    UpdateScore(totalScore);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void ExecuteEvaluationCardPlacement(HolderData holderData, Card cardToPlace, object[] evaluationContent, Card cardToEvaluate = null)
    {
        if(evaluationContent.Length > 0)
        {
            Card card = cardToEvaluate ? cardToEvaluate : cardToPlace;
            int pointModifier = evaluationContent.Length > 2 ? (int)evaluationContent[2] : 0;
            _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(card) + pointModifier, evaluationContent));
        }
        (_tableView as NpcTableView).RegisterCardPlacementAction(holderData, cardToPlace);
        (_tableView as NpcTableView).UpdateHolderIconsAction(holderData);
        (_tableView as NpcTableView).SaveState();
    }

    protected override void ExecuteCardPlacement(HolderData holderData, Card card)
    {
        _infoView.UpdateNumberOfCardPlacementsAction();
        _infoView.UpdateRoadTokensAction(card);
        (_handView as NpcHandView).RemoveCard(card);
        (_tableView as NpcTableView).RegisterCardPlacementAction(holderData, card);
        (_tableView as NpcTableView).UpdateHolderIconsAction(holderData);
        card.transform.SetParent((_tableView as NpcTableView).PlacedCardsContainer);
    }
}
