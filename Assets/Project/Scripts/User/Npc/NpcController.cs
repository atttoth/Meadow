using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

struct EvaluationData
{
    private readonly int _points;
    private readonly object _content; // stores any type of data associated with an evaluated action (MarkerHolder, Card)

    public int Points {  get { return _points; } }
    public object Content { get { return _content; } }

    public EvaluationData(int points, object content)
    {
        _points = points;
        _content = content;
    }
}

public class NpcController : UserController
{
    private MarkerHolder _selectedMarkerHolder;
    private Marker _selectedMarker;
    private Card _selectedCard;
    private int[] _probabilityValues;

    // only used at evaluation
    private List<EvaluationData> _evaluationDatacollection;
    private List<List<CardIcon>> _campIconPairs;

    public MarkerHolder SelectedMarkerHolder {  get { return _selectedMarkerHolder; } }
    public Marker SelectedMarker { get { return _selectedMarker; } }
    public Card SelectedCard { get { return _selectedCard; } }

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

    public void SelectAction(List<object[]> boardContent, List<List<CardIcon>> campIconPairs) // this is only handling default action for now (TODO: regular/special action needs to be decided)
    {
        // regular action
        // boardContent: index0: Card (card), index1: List<MarkerHolder> (markerHolders), index2: List<int> (markerDistances)
        _campIconPairs = campIconPairs;
        _evaluationDatacollection = new();
        List<Marker> remainingMarkers = _markerView.GetRemainingMarkers();
        List<Card> availableCards = GetAvailableBoardContent(boardContent, remainingMarkers);
        List<Card>[] sortedCards = SortCardsByDeckType(availableCards);
        EvaluateCardSelection(sortedCards[0], new());
        EvaluateCardSelection(sortedCards[1], new());
        Dictionary<Card, int> evaluationResult = GetResultOfEvaluationPoints(availableCards);
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

        remainingMarkers.ForEach(marker => marker.gameObject.SetActive(marker == _selectedMarker));
        Debug.Log("distance " + currentMarkerDistance);
    }

    public void SelectRow(List<MarkerHolder> holders, List<Card>[] boardCards, List<List<CardIcon>> campIconPairs)
    {
        _campIconPairs = campIconPairs;
        _evaluationDatacollection = new();
        EvaluateRowPick(holders, boardCards);
        Dictionary<MarkerHolder, int> evaluationResult = GetResultOfEvaluationPoints(holders);
        _selectedMarkerHolder = GetSelectedEvaluatedItem(evaluationResult);
        _selectedMarker = _markerView.GetCurrentMarker(0);
        evaluationResult.ToList().ForEach(p =>
        {
            Debug.Log("row value: " + p.Value);
        });
    }

    public void SelectInitialGroundCard(List<Card> boardCards, List<Card> groundCards, List<List<CardIcon>> campIconPairs)
    {
        _campIconPairs = campIconPairs;
        _evaluationDatacollection = new();
        EvaluateCardSelection(groundCards, boardCards);
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
            MarkerHolder holder = holders[row];
            List<CardIcon[]> rowTopIcons = new();
            List<Card> rowCards = boardCards[row];
            Card landscapeCard = rowCards[0].Data.cardType == CardType.Landscape ? rowCards[0] : null;
            List<Card> scoringCards = rowCards.Where(card => card.Data.cardType != CardType.Ground).ToList();
            Card groundCard = rowCards[rowCards.Count - 1];
            rowTopIcons.Add(groundCard.Data.icons);
            tableView.AddNewPrimaryHolder(tag);
            HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
            ExecuteEvaluationCardPlacement(primaryHolderData, groundCard);
            int groundCardStateIndex = tableView.GetLastTableStateIndex();
            for (int col = 0; col < scoringCards.Count; col++) // stack every combination of cards on ground card
            {
                tableView.LoadState(groundCardStateIndex);
                Card firstCard = scoringCards[col];
                if (TryPlaceCard(primaryHolderData, firstCard.Data))
                {
                    ExecuteEvaluationCardPlacement(primaryHolderData, firstCard, holder);
                    int firstCardStateIndex = tableView.GetLastTableStateIndex();
                    List<Card> remainingCards = scoringCards.Where(card => card != firstCard).ToList();
                    List<List<Card>> cardCombinations = new() { new List<Card>() { remainingCards[0], remainingCards[1] }, new List<Card>() { remainingCards[1], remainingCards[0] } };
                    for (int j = 0; j < cardCombinations.Count; j++)
                    {
                        tableView.LoadState(firstCardStateIndex);
                        List<Card> combination = cardCombinations[j];
                        if (TryPlaceCard(primaryHolderData, combination[0].Data))
                        {
                            ExecuteEvaluationCardPlacement(primaryHolderData, combination[0], holder);
                            if(TryPlaceCard(primaryHolderData, combination[1].Data))
                            {
                                ExecuteEvaluationCardPlacement(primaryHolderData, combination[1], holder);
                            }
                        }
                    }
                }

                if (landscapeCard && (landscapeCard.Data.requirements.Where(icon => icon == CardIcon.RoadToken).Count() < landscapeCard.Data.requirements.Length || col == 0)) // check if landscape card placement is possible
                {
                    tableView.AddNewSecondaryHolder();
                    if (TryPlaceCard(tableView.GetLastSecondaryHolderData(), landscapeCard.Data))
                    {
                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(landscapeCard), holder));
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
                _evaluationDatacollection.Add(new EvaluationData(houseIconPoints + icons.Length, holder));
            }

            CompareTopIconsWithCampIcons(rowTopIcons, holder);
        }
        tableView.LoadState(startingStateIndex);
    }

    private void EvaluateCardSelection(List<Card> selectableCards, List<Card> otherCards)
    {
        if(selectableCards.Count == 0)
        {
            Debug.Log("NO CARDS");
            return;
        }

        List<Card> cardsInHand = (_handView as NpcHandView).Cards;
        NpcTableView tableView = _tableView as NpcTableView;
        tableView.DisposeStates();
        tableView.SaveState();
        int startingStateIndex = tableView.GetLastTableStateIndex();
        List<Card>[] unfulfilledCardsInHand = GetUnfulfilledCardsByHolderSubType(startingStateIndex, cardsInHand); // cards in hand that can't be placed immediately
        List<Card>[] unfulfilledOtherCards = GetUnfulfilledCardsByHolderSubType(startingStateIndex, otherCards); // other cards that can't be placed immediately
        if (selectableCards.First().Data.deckType == DeckType.East) // ground cards
        {
            string[] tags = new string[] { "RectLeft", "RectRight" };
            for(int tagIndex = 0; tagIndex < tags.Length; tagIndex++) // run evaluation with ground card placed on left/right side
            {
                tableView.LoadState(startingStateIndex);
                string tag = tags[tagIndex];
                for (int selectableCardIndex = 0; selectableCardIndex < selectableCards.Count; selectableCardIndex++)
                {
                    tableView.LoadState(startingStateIndex);
                    Card selectableCard = selectableCards[selectableCardIndex];
                    tableView.AddNewPrimaryHolder(tag);
                    HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
                    ExecuteEvaluationCardPlacement(primaryHolderData, selectableCard);
                    int groundCardStateIndex = tableView.GetLastTableStateIndex();
                    CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), selectableCard);
                    PlaceUnfulfilledCards(groundCardStateIndex, selectableCard, unfulfilledCardsInHand);
                    PlaceUnfulfilledCards(groundCardStateIndex, selectableCard, unfulfilledOtherCards);
                }
            }
        }
        else
        {
            List<Card>[] separatedSelectableCards = SeparateCardsByHolderSubType(selectableCards);
            for(int groupIndex = 0; groupIndex < separatedSelectableCards.Length; groupIndex++)
            {
                List<Card> selectableCardsGroup = separatedSelectableCards[groupIndex];
                if(groupIndex == 0)
                {
                    for (int i = 0; i < selectableCardsGroup.Count; i++) // primary cards
                    {
                        Card selectableCard = selectableCardsGroup[i];
                        for (int j = 0; j < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; j++)
                        {
                            tableView.LoadState(startingStateIndex);
                            HolderData holderData = tableView.ActiveState.PrimaryCardHolderDataCollection[j];
                            if(TryPlaceCard(holderData, selectableCard.Data))
                            {
                                ExecuteEvaluationCardPlacement(holderData, selectableCard, selectableCard);
                                int observationCardStateIndex = tableView.GetLastTableStateIndex();
                                CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), selectableCard);
                                PlaceUnfulfilledCards(observationCardStateIndex, selectableCard, new List<Card>[] { unfulfilledCardsInHand[0] , new() });
                                PlaceUnfulfilledCards(observationCardStateIndex, selectableCard, new List<Card>[] { unfulfilledOtherCards[0], new() });
                            }

                            for(int k = 0; k < unfulfilledCardsInHand[0].Count; k++) // place selectableCard on hand cards
                            {
                                tableView.LoadState(startingStateIndex);
                                Card card = unfulfilledCardsInHand[0][k];
                                if (TryPlaceCard(holderData, card.Data))
                                {
                                    ExecuteEvaluationCardPlacement(holderData, card);
                                    int observationCardStateIndex = tableView.GetLastTableStateIndex();
                                    for(int l = 0; l < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; l++)
                                    {
                                        tableView.LoadState(observationCardStateIndex);
                                        HolderData data = tableView.ActiveState.PrimaryCardHolderDataCollection[l];
                                        if (TryPlaceCard(data, selectableCard.Data))
                                        {
                                            ExecuteEvaluationCardPlacement(data, selectableCard, selectableCard);
                                            CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), selectableCard);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < selectableCardsGroup.Count; i++) // secondary cards
                    {
                        tableView.LoadState(startingStateIndex);
                        Card selectableCard = selectableCardsGroup[i];
                        if(selectableCard.Data.cardType == CardType.Landscape)
                        {
                            tableView.AddNewSecondaryHolder();
                            HolderData holderData = tableView.GetLastSecondaryHolderData();
                            if (TryPlaceCard(holderData, selectableCard.Data))
                            {
                                ExecuteEvaluationCardPlacement(holderData, selectableCard, selectableCard);
                                int landscapeCardStateIndex = tableView.GetLastTableStateIndex();
                                for(int j = 0; j < unfulfilledCardsInHand[1].Count; j++) // place hand cards on selectable (landscape) card
                                {
                                    tableView.LoadState(landscapeCardStateIndex);
                                    Card card = unfulfilledCardsInHand[1][j];
                                    if (TryPlaceCard(holderData, card.Data))
                                    {
                                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), selectableCard));
                                    }
                                }

                                for(int j = 0; j < unfulfilledOtherCards[1].Count; j++) // place other cards on selectable (landscape) card
                                {
                                    tableView.LoadState(landscapeCardStateIndex);
                                    Card card = unfulfilledOtherCards[1][j];
                                    if (TryPlaceCard(holderData, card.Data))
                                    {
                                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), selectableCard));
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int j = 0; j < tableView.ActiveState.SecondaryCardHolderDataCollection.Count; j++) // place selectable (discovery) card
                            {
                                HolderData holderData = tableView.ActiveState.SecondaryCardHolderDataCollection[j];
                                if(TryPlaceCard(holderData, selectableCard.Data))
                                {
                                    _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(selectableCard), selectableCard));
                                }
                            }
                        }
                    }
                }
            }
        }
        tableView.LoadState(startingStateIndex);
    }

    private List<Card> GetAvailableBoardContent(List<object[]> boardContent, List<Marker> remainingMarkers)
    {
        int[] markerDistances = remainingMarkers.Select(marker => marker.numberOnMarker).ToArray();
        List<Card> availableCards = new();
        for (int i = 0; i < boardContent.Count; i++) // filter cards with no available holders to reach/available marker to reach
        {
            object[] content = boardContent[i];
            List<int> holderDistances = (List<int>)content[2];
            if (holderDistances.Count > 0)
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

    private List<Card>[] SortCardsByDeckType(List<Card> cards) // index0: deck-East cards, index1: deck West/South/North cards
    {
        List<Card> groundCards = new();
        List<Card> otherCards = new();
        cards.ForEach(card =>
        {
            if(card.Data.deckType == DeckType.East)
            {
                groundCards.Add(card);
            }
            else
            {
                otherCards.Add(card);
            }
        });
        return new List<Card>[] { groundCards, otherCards };
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

    private void PlaceUnfulfilledCards(int stateIndex, object evaluationContent, List<Card>[] unfulfilledCards)
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
                Card evaluatedCard = (Card)evaluationContent;
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
                Card evaluatedCard = (Card)evaluationContent;
                CheckNumberOfRequirementsFulfilledBySelectableCard(tableView.GetAllRelevantIcons(HolderSubType.PRIMARY).Except(evaluatedCard.Data.icons).ToList(), new List<CardIcon>(evaluatedCard.Data.icons), card.Data, evaluationContent);
            }
        }
    }

    private void CompareTopIconsWithCampIcons(List<CardIcon[]> topIcons, object evaluationContent)
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

    private void CheckNumberOfRequirementsFulfilledBySelectableCard(List<CardIcon> allRelevantIcons, List<CardIcon> selectableCardIcons, CardData cardData, object content) // global icon check only!
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
        _evaluationDatacollection.Add(new EvaluationData(points, content));
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
        items.ForEach(item => evaluationResults[item] = 0);
        for (int i = 0; i < _evaluationDatacollection.Count; i++)
        {
            EvaluationData data = _evaluationDatacollection[i];
            T item = (T)data.Content;
            evaluationResults[item] += data.Points;
        }
        return evaluationResults;
    }

    private T GetSelectedEvaluatedItem<T>(Dictionary<T, int> evaluationResult)
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
        else if(pointsByOrder.Count > probabilityValues.Count) // keep best options
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

    public override void UpdateCardHolders(HolderSubType subType, string hitAreaTag)
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

    public override void PlaceInitialGroundCardOnTable(GameTask task, Card card)
    {
        switch (task.State)
        {
            case 0:
                string tag = "RectLeft";
                UpdateCardHolders(HolderSubType.PRIMARY, tag);
                HolderData holderData = (_tableView as NpcTableView).GetPrimaryHolderDataByTag(tag);
                ExecuteCardPlacement(new object[] { card.Data.ID, false, holderData, card });
                task.StartDelayMs(0);
                break;
            case 1:
                task.StartHandler(GetUpdateDisplayIconsHandler(), new List<Card>() { card }, _tableView.ActiveState.PrimaryCardHolderDataCollection);
                break;
            case 2:
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.waitDelay) * 1000));
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void ExecuteEvaluationCardPlacement(HolderData holderData, Card cardToPlace, object evaluationContent = null, Card cardToEvaluate = null)
    {
        if(evaluationContent != null)
        {
            Card card = cardToEvaluate ? cardToEvaluate : cardToPlace;
            _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(card), evaluationContent));
        }
        object[] args = new object[] { -1, false, holderData, cardToPlace };
        (_tableView as NpcTableView).RegisterCardPlacementAction(args);
        (_tableView as NpcTableView).UpdateHolderIconsAction(args);
        (_tableView as NpcTableView).SaveState();
    }

    public override void ExecuteCardPlacement(object[] args)
    {
        Card card = (Card)args[3];
        _infoView.UpdateNumberOfCardPlacementsAction(args);
        _infoView.UpdateRoadTokensAction(args);
        (_handView as NpcHandView).PlaceCardFromHandAction(args);
        (_tableView as NpcTableView).RegisterCardPlacementAction(args);
        (_tableView as NpcTableView).UpdateHolderIconsAction(args);
        card.transform.SetParent((_tableView as NpcTableView).PlacedCardsContainer);
    }

    public void RegisterScoreHandler(GameTask task)
    {
        UpdateScore((_handView as NpcHandView).GetLastCardInHand().Data.score);
        task.Complete();
    }
}
