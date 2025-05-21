using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

struct EvaluationData
{
    private readonly int _points;
    private readonly int[] _indices;

    public int Points {  get { return _points; } }
    public int[] Indices { get { return _indices; } }

    public EvaluationData(int points, int[] indices)
    {
        _points = points;
        _indices = indices;
    }
}

public class NpcController : UserController
{
    private MarkerHolder _selectedMarkerHolder;
    private Marker _selectedMarker;
    private Card _selectedCard;
    private int[] _probabilityValues;

    // values used at evaluation
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

    public void SelectRandomMarkerAndMarkerHolder(List<MarkerHolder> holders)
    {
        List<Marker> remainingMarkers = _markerView.GetRemainingMarkers();
        System.Random random = new();
        _selectedMarkerHolder = holders[random.Next(0, holders.Count - 1)];
        _selectedMarker = _markerView.GetCurrentMarker(random.Next(0, remainingMarkers.Count - 1));
        remainingMarkers.ForEach(marker => marker.gameObject.SetActive(marker == _selectedMarker));
    }

    public void SelectRow(List<MarkerHolder> holders, List<Card>[] boardCards, List<List<CardIcon>> campIconPairs)
    {
        _campIconPairs = campIconPairs;
        int[] evaluationPoints = EvaluateRowPick(boardCards);
        int markerHolderIndex = evaluationPoints.ToList().IndexOf(GetSelectedPoints(evaluationPoints));
        int markerIndex = 0;
        _selectedMarkerHolder = holders[markerHolderIndex];
        _selectedMarker = _markerView.GetCurrentMarker(markerIndex);
        evaluationPoints.ToList().ForEach(p =>
        {
            Debug.Log("row value: " + p);
        });
    }

    public void SelectInitialGroundCard(List<Card> boardCards, List<Card> groundCards, List<List<CardIcon>> campIconPairs)
    {
        _campIconPairs = campIconPairs;
        int[] evaluationPoints = EvaluateCardSelection(boardCards, groundCards);
        int cardIndex = evaluationPoints.ToList().IndexOf(GetSelectedPoints(evaluationPoints));
        _selectedCard = groundCards[cardIndex];
        evaluationPoints.ToList().ForEach(p =>
        {
            Debug.Log("groundCard value: " + p);
        });
    }

    private int[] EvaluateRowPick(List<Card>[] boardCards)
    {
        string tag = "RectLeft";
        NpcTableView tableView = _tableView as NpcTableView;
        tableView.DisposeStates();
        tableView.SaveState();
        int startingStateIndex = tableView.GetLastTableStateIndex();
        _evaluationDatacollection = new();
        for (int row = 0; row < boardCards.Length; row++)
        {
            tableView.LoadState(startingStateIndex);
            int[] indices = new int[] { -1, row };
            List<CardIcon[]> rowTopIcons = new();
            List<Card> rowCards = boardCards[row];
            Card landscapeCard = rowCards[0].Data.cardType == CardType.Landscape ? rowCards[0] : null;
            List<Card> scoringCards = rowCards.Where(card => card.Data.cardType != CardType.Ground).ToList();
            Card groundCard = rowCards[rowCards.Count - 1];
            rowTopIcons.Add(groundCard.Data.icons);
            tableView.AddNewPrimaryHolder(tag);
            HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
            ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, groundCard });
            int groundCardStateIndex = tableView.GetLastTableStateIndex();
            for (int col = 0; col < scoringCards.Count; col++) // stack every combination of cards on ground card
            {
                tableView.LoadState(groundCardStateIndex);
                Card firstCard = scoringCards[col];
                if (TryPlaceCard(primaryHolderData, firstCard.Data))
                {
                    ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, firstCard });
                    int firstCardStateIndex = tableView.GetLastTableStateIndex();
                    _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(firstCard), indices));
                    List<Card> remainingCards = scoringCards.Where(card => card != firstCard).ToList();
                    List<List<Card>> cardCombinations = new() { new List<Card>() { remainingCards[0], remainingCards[1] }, new List<Card>() { remainingCards[1], remainingCards[0] } };
                    for (int j = 0; j < cardCombinations.Count; j++)
                    {
                        tableView.LoadState(firstCardStateIndex);
                        List<Card> combination = cardCombinations[j];
                        if (TryPlaceCard(primaryHolderData, combination[0].Data))
                        {
                            ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, combination[0] });
                            _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(combination[0]), indices));
                            if(TryPlaceCard(primaryHolderData, combination[1].Data))
                            {
                                ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, combination[1] });
                                _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(combination[1]), indices));
                            }
                        }
                    }
                }

                if (landscapeCard && (landscapeCard.Data.requirements.Where(icon => icon == CardIcon.RoadToken).Count() < landscapeCard.Data.requirements.Length || col == 0)) // check if landscape card placement is possible
                {
                    tableView.AddNewSecondaryHolder();
                    if (TryPlaceCard(tableView.GetLastSecondaryHolderData(), landscapeCard.Data))
                    {
                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(landscapeCard), indices));
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
                _evaluationDatacollection.Add(new EvaluationData(houseIconPoints + icons.Length, indices));
            }

            CompareTopIconsWithCampIcons(rowTopIcons, indices);
        }
        tableView.LoadState(startingStateIndex);
        return GetSumOfEvaluationPoints(4);
    }

    private int[] EvaluateCardSelection(List<Card> boardCards, List<Card> selectableCards)
    {
        List<Card> cardsInHand = (_handView as NpcHandView).Cards;
        NpcTableView tableView = _tableView as NpcTableView;
        tableView.DisposeStates();
        tableView.SaveState();
        int startingStateIndex = tableView.GetLastTableStateIndex();
        _evaluationDatacollection = new(); // EvaluationData: the row value represents index of selectableCards
        if (selectableCards.First().Data.cardType == CardType.Ground)
        {
            List<Card>[] unfulfilledCardsInHand = GetUnfulfilledCardsByCardType(startingStateIndex, cardsInHand); // filter cards in hand that can't be placed immediately
            List<Card>[] unfulfilledBoardCards = GetUnfulfilledCardsByCardType(startingStateIndex, boardCards); // filter board cards that can't be placed immediately
            string[] tags = new string[] { "RectLeft", "RectRight" };
            for(int tagIndex = 0; tagIndex < tags.Length; tagIndex++) // run evaluation with ground card placed on both sides
            {
                tableView.LoadState(startingStateIndex);
                string tag = tags[tagIndex];
                for (int selectableCardIndex = 0; selectableCardIndex < selectableCards.Count; selectableCardIndex++)
                {
                    tableView.LoadState(startingStateIndex);
                    Card groundCard = selectableCards[selectableCardIndex];
                    tableView.AddNewPrimaryHolder(tag);
                    HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
                    ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, groundCard });
                    int groundCardStateIndex = tableView.GetLastTableStateIndex();
                    CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), new int[] { -1, selectableCardIndex });
                    PlaceUnfulfilledCards(groundCardStateIndex, selectableCardIndex, groundCard, unfulfilledCardsInHand);
                    PlaceUnfulfilledCards(groundCardStateIndex, selectableCardIndex, groundCard, unfulfilledBoardCards);
                }
            }
        }
        else
        {
            // todo: landscape/discovery/observation cards
        }

        tableView.LoadState(startingStateIndex);
        return GetSumOfEvaluationPoints(selectableCards.Count);
    }

    private List<Card>[] GetUnfulfilledCardsByCardType(int stateIndex, List<Card> cards)
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

    private void PlaceUnfulfilledCards(int stateIndex, int selectableCardIndex, Card selectableCard, List<Card>[] unfulfilledCards)
    {
        NpcTableView tableView = _tableView as NpcTableView;
        int[] indices = new int[] { -1, selectableCardIndex };
        if (unfulfilledCards[0].Count > 0)
        {
            for (int i = 0; i < unfulfilledCards[0].Count; i++) // primary holder cards
            {
                Card card = unfulfilledCards[0][i];
                bool canBePlacedAnywhere = false;
                for (int j = 0; j < tableView.ActiveState.PrimaryCardHolderDataCollection.Count; j++) // place cards in every primary holder
                {
                    tableView.LoadState(stateIndex);
                    HolderData holderData = tableView.ActiveState.PrimaryCardHolderDataCollection[j];
                    if (TryPlaceCard(holderData, card.Data))
                    {
                        ExecuteEvaluationCardPlacement(new object[] { -1, false, holderData, card });
                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(card), indices));
                        CompareTopIconsWithCampIcons(tableView.GetTopPrimaryIcons(), indices);
                        canBePlacedAnywhere = true;
                    }
                }

                if (!canBePlacedAnywhere)
                {
                    int numOfFulfilledRequirements = GetNumberOfRequirementsFulfilledBySelectableCard(
                        new List<CardIcon>(selectableCard.Data.icons),
                        new List<CardIcon>(card.Data.requirements),
                        new List<CardIcon>(card.Data.optionalRequirements),
                        new List<CardIcon>(card.Data.adjacentRequirements)
                        );
                    _evaluationDatacollection.Add(new EvaluationData(numOfFulfilledRequirements, indices));
                }
            }
        }

        if (unfulfilledCards[1].Count > 0)
        {
            for (int i = 0; i < unfulfilledCards[1].Count; i++) // secondary holder cards
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
                            _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(card), indices));
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
                        _evaluationDatacollection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(card), indices));
                        canBePlacedAnywhere = true;
                    }
                }

                if (!canBePlacedAnywhere)
                {
                    int numOfFulfilledRequirements = GetNumberOfRequirementsFulfilledBySelectableCard(
                        new List<CardIcon>(selectableCard.Data.icons),
                        new List<CardIcon>(card.Data.requirements),
                        new List<CardIcon>(card.Data.optionalRequirements),
                        new List<CardIcon>(card.Data.adjacentRequirements)
                        );
                    _evaluationDatacollection.Add(new EvaluationData(numOfFulfilledRequirements, indices));
                }
            }
        }
    }

    private void CompareTopIconsWithCampIcons(List<CardIcon[]> topIcons, int[] indices)
    {
        List<List<CardIcon>> pairs = CreateAdjacentIconPairs(topIcons);
        for (int i = 0; i < pairs.Count; i++)
        {
            List<CardIcon> pair = pairs[i];
            for (int j = 0; j < _campIconPairs.Count; j++)
            {
                if (pair.OrderBy(x => x).SequenceEqual(_campIconPairs[j].OrderBy(x => x)))
                {
                    _evaluationDatacollection.Add(new EvaluationData(GetNextCampScoreToken(), indices));
                }
            }
        }
    }

    private int GetNumberOfRequirementsFulfilledBySelectableCard(List<CardIcon> selectableCardIcons, List<CardIcon> cardRequirements, List<CardIcon> cardOptionalRequirements, List<CardIcon> cardAdjacentRequirements)
    {
        List<CardIcon> allRelevantIcons = (_tableView as NpcTableView).GetAllRelevantIcons(HolderSubType.PRIMARY).Except(selectableCardIcons).ToList();
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
        return (remainingRequirementIcons - cardRequirements.Count) + (remainingOptionalRequirementPairs - optionalRequirementIconPairs.Count) + (remainingAdjacentRequirementIcons - cardAdjacentRequirements.Count);
    }

    private int GetCardPlacementEvaluationPoints(Card card)
    {
        int totalPoints = card.Data.score;
        switch (card.Data.cardType)
        {
            case CardType.Observation:
                totalPoints += 8;
                break;
            case CardType.Landscape:
                totalPoints += 7;
                break;
            case CardType.Discovery:
                totalPoints += 6;
                break;
            default:
                totalPoints += 5;
                break;
        }
        return totalPoints;
    }

    private int[] GetSumOfEvaluationPoints(int numOfResults)
    {
        int[] evaluationPoints = new int[numOfResults];
        evaluationPoints = evaluationPoints.Select(i => 0).ToArray();
        for (int i = 0; i < _evaluationDatacollection.Count; i++)
        {
            EvaluationData data = _evaluationDatacollection[i];
            int index = data.Indices[1];
            evaluationPoints[index] += data.Points;
        }
        return evaluationPoints;
    }

    private int GetSelectedPoints(int[] evaluationPoints)
    {
        List<int> pointsByOrder = evaluationPoints.ToList().OrderBy(n => n).ToList();
        List<int> values = new();
        List<int> probabilityValues = _probabilityValues.ToList();
        if(pointsByOrder.Count < probabilityValues.Count) // check if there's less options than difficulty levels (e.g. card/deck selection)
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

        for(int i = 0; i < probabilityValues.Count; i++)
        {
            int probability = probabilityValues[i];
            for(int j = 0; j < probability; j++)
            {
                values.Add(pointsByOrder[i]);
            }
        }
        System.Random random = new();
        return values[random.Next(0, values.Count - 1)];
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

    private void ExecuteEvaluationCardPlacement(object[] args)
    {
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
