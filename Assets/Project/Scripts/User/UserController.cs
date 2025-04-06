using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class UserController : GameLogicEvent
{
    [HideInInspector]public int userID;
    protected UserControllerLayout _userControllerLayout;
    protected TableView _tableView;
    protected HandView _handView;
    protected MarkerView _markerView;
    protected InfoView _infoView;
    protected IconDisplayView _iconDisplayView;
    protected List<int> _campScoreTokens;
    protected CanvasGroup _canvasGroup;
    protected Dictionary<int, CardIcon[][]> _allIconsOfPrimaryHoldersInOrder; //as cards are stacked in order
    protected Dictionary<int, CardIcon[][]> _allIconsOfSecondaryHoldersInOrder;

    public virtual void CreateUser(GameMode gameMode)
    {
        _userControllerLayout = new UserControllerLayout();
        if(userID > 0) // ignore player layout for now
        {
            Vector2[] screenPosition = _userControllerLayout.GetNpcControllerScreenPositionByUserID(userID);
            GetComponent<RectTransform>().anchoredPosition = screenPosition[0];
            _infoView.GetComponent<RectTransform>().anchoredPosition = screenPosition[1];
        }
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public InfoView InfoView { get { return _infoView; } }
    public MarkerView MarkerView { get { return _markerView; } }
    public IconDisplayView IconDisplayView { get { return _iconDisplayView; } }

    public Delegate GetAddCardToHandHandler()
    {
        return (Action<GameTask, Card>)_handView.AddCardHandler;
    }

    public void StartTurn()
    {
        StartEventHandler(GameLogicEventType.TURN_STARTED, new object[0]);
    }

    public void EndTurn()
    {
        StartEventHandler(GameLogicEventType.TURN_ENDED, new object[0]);
    }

    public void EndRound()
    {
        StartEventHandler(GameLogicEventType.ROUND_ENDED, new object[0]);
    }

    public void EndGame()
    {
        StartEventHandler(GameLogicEventType.GAME_ENDED, new object[0]);
    }

    public void Fade(bool value)
    {
        if(userID > 0)
        {
            float fadeDuration = ReferenceManager.Instance.gameLogicController.GameSettings.gameUIFadeDuration;
            float targetValue = value ? 1f : 0f;
            DOTween.Sequence().Append(_canvasGroup.DOFade(targetValue, fadeDuration));
        }
    }

    public bool TryPlaceCard(CardHolder holder, Card card)
    {
        if(!_infoView.HasEnoughCardPlacements())
        {
            return false;
        }

        List<CardIcon> primaryTableIcons = GetAllCurrentIcons(HolderSubType.PRIMARY);
        List<CardIcon> mainRequirements = card.Data.requirements.ToList();

        if (holder.holderSubType == HolderSubType.PRIMARY)
        {
            if (card.Data.cardType == CardType.Ground)
            {
                if (holder.IsEmpty())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            List<CardIcon> holderIcons = holder.GetAllIconsOfHolder();
            if (holderIcons.Contains(CardIcon.Deer))
            {
                return false;
            }

            if (mainRequirements.Contains(CardIcon.AllDifferent)) // only occurs at card ID 195
            {
                List<CardIcon> allTableIcons = new(primaryTableIcons);
                allTableIcons.AddRange(GetAllCurrentIcons(HolderSubType.SECONDARY));
                return GetDistinctTableIcons(allTableIcons) >= mainRequirements.Where(icon => icon == CardIcon.AllDifferent).ToList().Count;
            }

            List<CardIcon> optionalRequirements = card.Data.optionalRequirements.ToList();
            List<CardIcon> adjacentRequirements = card.Data.adjacentRequirements.ToList();
            bool mainGlobalCondition = PassedGlobalRequirements(primaryTableIcons, mainRequirements);
            bool optionalGlobalCondition = PassedOptionalGlobalRequirements(primaryTableIcons, optionalRequirements);
            bool adjacentGlobalCondition = PassedSingleRequirement(primaryTableIcons, adjacentRequirements);

            if (mainGlobalCondition && optionalGlobalCondition && adjacentGlobalCondition) // check for combined requirement types
            {
                List<List<CardIcon>> adjacentHolderIcons = _tableView.GetAdjacentHolderIcons(holder);
                if (optionalRequirements.Count > 0 && adjacentRequirements.Count > 0)
                {
                    List<CardIcon[]> pairs = CreateIconPairsFromRequirements(optionalRequirements);
                    for (int i = adjacentRequirements.Count - 1; i >= 0; i--) // add optional icon to main requirements that is not present in both adjacent and optional requirements
                    {
                        CardIcon adjacentIcon = adjacentRequirements[i];
                        for (int j = pairs.Count - 1; j >= 0; j--)
                        {
                            CardIcon[] pair = pairs[j];
                            if (!pair.Contains(adjacentIcon))
                            {
                                mainRequirements.AddRange(pair);
                                pairs.RemoveAt(j);
                            }
                        }
                    }
                    return PassedAdjacentIconRequirements(adjacentHolderIcons, adjacentRequirements) || PassedSingleRequirement(holderIcons, mainRequirements);
                }
                else if (optionalRequirements.Count > 0)
                {
                    mainRequirements.AddRange(optionalRequirements);
                    return PassedSingleRequirement(holderIcons, mainRequirements);
                }
                else if (adjacentRequirements.Count > 0 && mainRequirements.Count == 0)
                {
                    return PassedAdjacentIconRequirements(adjacentHolderIcons, adjacentRequirements);
                }
                else if (adjacentRequirements.Count > 0)
                {
                    return PassedAdjacentIconRequirements(adjacentHolderIcons, adjacentRequirements) || PassedSingleRequirement(holderIcons, mainRequirements);
                }
                else
                {
                    return PassedSingleRequirement(holderIcons, mainRequirements);
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            int numOfIcons = mainRequirements.Count;
            mainRequirements = mainRequirements.Where(icon => icon != CardIcon.RoadToken).ToList();
            if (!_infoView.HasEnoughRoadTokens(numOfIcons - mainRequirements.Count) || !Array.Exists(new CardType[] { CardType.Landscape, CardType.Discovery }, type => type == card.Data.cardType))
            {
                return false;
            }

            primaryTableIcons.AddRange(GetAllCurrentIcons(HolderSubType.SECONDARY)); // expand primary icons with secondary icons
            if (card.Data.cardType == CardType.Landscape)
            {
                if(holder.IsEmpty())
                {
                    if (mainRequirements.Contains(CardIcon.AllMatching)) // only occurs at card ID 172
                    {
                        return GetMostCommonTableIconsCount(primaryTableIcons) >= mainRequirements.Where(icon => icon == CardIcon.AllMatching).ToList().Count;
                    }
                    else if (card.Data.requirements.Length == 1) // card has only road token requirement
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (PassedGlobalRequirements(primaryTableIcons, mainRequirements))
            {
                if (card.Data.cardType == CardType.Landscape)
                {
                    return true;
                }
                else
                {
                    return PassedSingleRequirement(holder.GetAllIconsOfHolder(), mainRequirements);
                }
            }
            else
            {
                return false;
            }
        }
    }

    private List<CardIcon> GetAllCurrentIcons(HolderSubType holderSubType)
    {
        Dictionary<int, CardIcon[][]> collection = holderSubType == HolderSubType.PRIMARY ? _allIconsOfPrimaryHoldersInOrder : _allIconsOfSecondaryHoldersInOrder;
        List<CardIcon> allCurrentIcons = new();
        foreach (CardIcon[][] items in collection.Values)
        {
            allCurrentIcons.AddRange(items[items.Length - 1]);
            if (items.Length > 1)
            {
                List<CardIcon> groundIcons = items[0].Where(icon => (int)icon < 5).ToList();
                allCurrentIcons.AddRange(groundIcons);
            }
        }
        return allCurrentIcons;
    }

    private int GetMostCommonTableIconsCount(List<CardIcon> allTableIcons)
    {
        if (allTableIcons.Count == 0)
        {
            return 0;
        }
        else
        {
            return allTableIcons.GroupBy(icon => icon).Select(g => new { Icon = g.Key, Count = g.Count() }).ToList().Max(g => g.Count);
        }
    }

    private int GetDistinctTableIcons(List<CardIcon> allTableIcons)
    {
        return allTableIcons.Distinct().ToList().Count;
    }

    private List<CardIcon[]> CreateIconPairsFromRequirements(List<CardIcon> requirements)
    {
        List<CardIcon[]> pairs = new();
        for (int i = 0; i < requirements.Count; i++)
        {
            if (i % 2 == 0)
            {
                CardIcon icon1 = requirements[i];
                CardIcon icon2 = requirements[i + 1];
                CardIcon[] pair = new CardIcon[] { icon1, icon2 };
                pairs.Add(pair);
            }
        }
        return pairs;
    }

    private bool PassedGlobalRequirements(List<CardIcon> allTableIcons, List<CardIcon> requirements)
    {
        if (requirements.Count < 1)
        {
            return true;
        }

        List<CardIcon> allIcons = new(allTableIcons);
        List<CardIcon> remainingRequirements = new(requirements);
        for (int i = remainingRequirements.Count - 1; i >= 0; i--)
        {
            CardIcon requirement = remainingRequirements[i];
            for (int j = allIcons.Count - 1; j >= 0; j--)
            {
                if (requirement == allIcons[j])
                {
                    remainingRequirements.RemoveAt(i);
                    allIcons.RemoveAt(j);
                    break;
                }
            }
            if (remainingRequirements.Count == 0)
            {
                return true;
            }
        }
        return false;
    }

    private bool PassedOptionalGlobalRequirements(List<CardIcon> allTableIcons, List<CardIcon> requirements)
    {
        if (requirements.Count < 2)
        {
            return true;
        }

        List<CardIcon> allIcons = new(allTableIcons);
        List<CardIcon[]> pairs = CreateIconPairsFromRequirements(requirements);
        for (int i = allIcons.Count - 1; i >= 0; i--)
        {
            CardIcon cardIcon = allIcons[i];
            for (int j = pairs.Count - 1; j >= 0; j--)
            {
                CardIcon[] pair = pairs[j];
                if (Array.Exists(pair, icon => icon == cardIcon))
                {
                    pairs.Remove(pair);
                    allIcons.RemoveAt(i);
                    break;
                }
            }
            if (pairs.Count < 1)
            {
                return true;
            }
        }
        return false;
    }

    private bool PassedAdjacentIconRequirements(List<List<CardIcon>> adjacentHolderIcons, List<CardIcon> requirements)
    {
        for (int i = 0; i < adjacentHolderIcons.Count; i++)
        {
            List<CardIcon> icons = adjacentHolderIcons[i];
            for (int j = 0; j < icons.Count; j++)
            {
                if (Array.Exists(requirements.ToArray(), icon => icon == icons[j]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool PassedSingleRequirement(List<CardIcon> holderIcons, List<CardIcon> requirements)
    {
        if (requirements.Count < 1)
        {
            return true;
        }

        foreach (CardIcon requirement in requirements)
        {
            foreach (CardIcon icon in holderIcons)
            {
                if (icon == requirement)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void SetMarkerUsed()
    {
        _markerView.SetPlacedMarkerToUsed();
        _markerView.IsMarkerConsumed = true;
    }

    public void AddRoadTokensHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _infoView.AddRoadTokens(2);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void AddExtraCardPlacementHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _infoView.SetMaxCardPlacement(2);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void UpdateScore(int score)
    {
        _infoView.IncrementScore(score);
    }

    public void UpdateCampScoreTokens()
    {
        _campScoreTokens.RemoveAt(0);
    }
}
