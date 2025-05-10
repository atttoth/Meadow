using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.WSA;

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
                _probabilityValues = new int[] { 0, 0, 3, 7 };
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

    public void SelectRow(List<MarkerHolder> holders, List<Card>[] cards, List<List<CardIcon>> campIconPairs)
    {
        int[] evaluationPoints = EvaluateRowPick(cards, campIconPairs);
        int markerHolderIndex = evaluationPoints.ToList().IndexOf(GetSelectedPoints(evaluationPoints));
        int markerIndex = 0;
        _selectedMarkerHolder = holders[markerHolderIndex];
        _selectedMarker = _markerView.GetCurrentMarker(markerIndex);
        evaluationPoints.ToList().ForEach(p =>
        {
            Debug.Log(p);
        });
    }

    public void SelectInitialGroundCard(List<Card>[] cards, List<Card> groundCards, List<List<CardIcon>> campIconPairs) // todo
    {
        int cardIndex = 0;
        _selectedCard = groundCards[cardIndex];
    }

    private int[] EvaluateRowPick(List<Card>[] cards, List<List<CardIcon>> campIconPairs)
    {
        string tag = "RectLeft";
        NpcTableView tableView = _tableView as NpcTableView;
        tableView.SaveState();
        int startingStateIndex = tableView.GetLastTableStateIndex();
        List<EvaluationData> collection = new();
        for (int row = 0; row < cards.Length; row++)
        {
            tableView.LoadState(startingStateIndex);
            int[] indices = new int[] { -1, row };
            List<CardIcon[]> rowTopIcons = new();
            List<Card> rowCards = cards[row];
            Card landscapeCard = rowCards[0].Data.cardType == CardType.Landscape ? rowCards[0] : null;
            List<Card> scoringCards = rowCards.Where(card => card.Data.cardType != CardType.Ground).ToList();
            Card groundCard = rowCards[rowCards.Count - 1];
            rowTopIcons.Add(groundCard.Data.icons);
            UpdateCardHolders(HolderSubType.PRIMARY, tag);
            HolderData primaryHolderData = tableView.GetPrimaryHolderDataByTag(tag);
            ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, groundCard });
            int groundCardStateIndex = tableView.GetLastTableStateIndex();
            for (int col = 0; col < scoringCards.Count; col++) // STEP 1: stack every combination of cards on ground card
            {
                tableView.LoadState(groundCardStateIndex);
                Card firstCard = scoringCards[col];
                if (TryPlaceCard(primaryHolderData, firstCard.Data))
                {
                    ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, firstCard });
                    int firstCardStateIndex = tableView.GetLastTableStateIndex();
                    collection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(firstCard), indices));
                    List<Card> remainingCards = scoringCards.Where(card => card != firstCard).ToList();
                    List<List<Card>> cardCombinations = new() { new List<Card>() { remainingCards[0], remainingCards[1] }, new List<Card>() { remainingCards[1], remainingCards[0] } };
                    for (int j = 0; j < cardCombinations.Count; j++)
                    {
                        tableView.LoadState(firstCardStateIndex);
                        List<Card> combination = cardCombinations[j];
                        if (TryPlaceCard(primaryHolderData, combination[0].Data))
                        {
                            ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, combination[0] });
                            collection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(combination[0]), indices));
                            if(TryPlaceCard(primaryHolderData, combination[1].Data))
                            {
                                ExecuteEvaluationCardPlacement(new object[] { -1, false, primaryHolderData, combination[1] });
                                collection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(combination[1]), indices));
                            }
                        }
                    }
                }

                // STEP 2: check if landscape card placement is possible
                if (landscapeCard && (landscapeCard.Data.requirements.Where(icon => icon == CardIcon.RoadToken).Count() < landscapeCard.Data.requirements.Length || col == 0))
                {
                    UpdateCardHolders(HolderSubType.SECONDARY, tag);
                    if (TryPlaceCard(tableView.GetLastSecondaryHolderData(), landscapeCard.Data))
                    {
                        collection.Add(new EvaluationData(GetCardPlacementEvaluationPoints(landscapeCard), indices));
                    }
                }

                if (firstCard.Data.cardType == CardType.Observation)
                {
                    rowTopIcons.Add(firstCard.Data.icons);
                }
            }

            for (int i = 0; i < rowTopIcons.Count; i++) // STEP 3: add evaluation points by number of top icons of row
            {
                CardIcon[] icons = rowTopIcons[i];
                int houseIconPoints = Array.Exists(icons, icon => icon == CardIcon.House) ? 2 : 0;
                collection.Add(new EvaluationData(houseIconPoints + icons.Length, indices));
            }

            List<List<CardIcon>> pairs = CreateAdjacentIconPairs(rowTopIcons);
            for (int i = 0; i < pairs.Count; i++) // STEP 4: find matching top icons - camp icon pairs
            {
                List<CardIcon> pair = pairs[i];
                for (int j = 0; j < campIconPairs.Count; j++)
                {
                    if (pair.OrderBy(x => x).SequenceEqual(campIconPairs[j].OrderBy(x => x)))
                    {
                        collection.Add(new EvaluationData(GetNextCampScoreToken(), indices));
                    }
                }
            }
        }

        // STEP 5: sum up evaluation points of rows
        int[] evaluationPointsByRow = new int[] { 0, 0, 0, 0 };
        for(int i = 0; i < collection.Count; i++)
        {
            EvaluationData data = collection[i];
            int rowIdx = data.Indices[1];
            evaluationPointsByRow[rowIdx] += data.Points;
        }
        tableView.LoadState(0);
        return evaluationPointsByRow;
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

    private int GetSelectedPoints(int[] evaluationPoints)
    {
        List<int> pointsByOrder = evaluationPoints.ToList().OrderBy(n => n).Reverse().ToList();
        List<int> values = new();
        for(int i = 0; i < _probabilityValues.Length; i++)
        {
            int probability = _probabilityValues[i];
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
        UpdateCurrentIconsEntryAction(args);
        (_tableView as NpcTableView).RegisterCardPlacementAction(args);
        UpdateCurrentIconsOfHolderAction(args);
        (_tableView as NpcTableView).SaveState();
    }

    public override void ExecuteCardPlacement(object[] args)
    {
        Card card = (Card)args[3];
        _infoView.UpdateNumberOfCardPlacementsAction(args);
        _infoView.UpdateRoadTokensAction(args);
        UpdateCurrentIconsEntryAction(args);
        (_handView as NpcHandView).PlaceCardFromHandAction(args);
        (_tableView as NpcTableView).RegisterCardPlacementAction(args);
        card.transform.SetParent((_tableView as NpcTableView).PlacedCardsContainer);
        UpdateCurrentIconsOfHolderAction(args);
    }

    public void RegisterScoreHandler(GameTask task)
    {
        UpdateScore((_handView as NpcHandView).GetLastCardInHand().Data.score);
        task.Complete();
    }
}
