using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class UserController : MonoBehaviour
{
    [HideInInspector]public int userID;
    protected LogicEventDispatcher _dispatcher;
    protected UserControllerLayout _userControllerLayout;
    protected TableView _tableView;
    protected HandView _handView;
    protected MarkerView _markerView;
    protected InfoView _infoView;
    protected IconDisplayView _iconDisplayView;
    protected List<int> _campScoreTokens;
    protected CanvasGroup _canvasGroup;

    public virtual void CreateUser(GameMode gameMode)
    {
        _dispatcher = new();
        _userControllerLayout = new UserControllerLayout();
        if(userID > 0) // ignore player layout for now
        {
            Vector2[] screenPosition = _userControllerLayout.GetNpcControllerScreenPositionByUserID(userID);
            GetComponent<RectTransform>().anchoredPosition = screenPosition[0];
            _infoView.GetComponent<RectTransform>().anchoredPosition = screenPosition[1];
        }
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }

    public abstract void PlaceInitialGroundCardOnTable(GameTask task, Card card);
    public abstract void UpdateCardHolders(HolderSubType subType, string hitAreaTag);
    public abstract void ExecuteCardPlacement(object[] args);

    public InfoView InfoView { get { return _infoView; } }
    public MarkerView MarkerView { get { return _markerView; } }
    public IconDisplayView IconDisplayView { get { return _iconDisplayView; } }

    public Delegate GetAddCardToHandHandler()
    {
        return (Action<GameTask, List<Card>>)_handView.AddCardHandler;
    }

    public void StartTurn()
    {
        _dispatcher.InvokeEventHandler(GameLogicEventType.TURN_STARTED, new object[0]);
    }

    public void EndTurn()
    {
        _dispatcher.InvokeEventHandler(GameLogicEventType.TURN_ENDED, new object[0]);
    }

    public void EndRound()
    {
        _dispatcher.InvokeEventHandler(GameLogicEventType.ROUND_ENDED, new object[0]);
    }

    public void EndGame()
    {
        _dispatcher.InvokeEventHandler(GameLogicEventType.GAME_ENDED, new object[0]);
    }

    public void Fade(bool value)
    {
        float fadeDuration = GameSettings.Instance.GetDuration(Duration.gameUIFadeDuration);
        float targetValue = value ? 1f : 0f;
        DOTween.Sequence().Append(_canvasGroup.DOFade(targetValue, fadeDuration));
    }

    public bool PassedBasicRequirements(Card card)
    {
        if (!_infoView.HasEnoughCardPlacements())
        {
            return false;
        }

        if(card.Data.cardType == CardType.Landscape)
        {
            return _infoView.HasEnoughRoadTokens(card.Data.requirements.Where(icon => icon == CardIcon.RoadToken).Count());
        }
        return true;
    }

    public bool TryPlaceCard(HolderData holderData, CardData cardData)
    {
        List<CardIcon> primaryTableIcons = _tableView.GetAllRelevantIcons(HolderSubType.PRIMARY);
        List<CardIcon> mainRequirements = cardData.requirements.ToList();

        if (holderData.holderSubType == HolderSubType.PRIMARY)
        {
            if (cardData.cardType == CardType.Ground)
            {
                if (holderData.IsEmpty())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            List<CardIcon> holderIcons = holderData.GetAllIconsOfHolder();
            if (holderIcons.Contains(CardIcon.Deer))
            {
                return false;
            }

            List<CardIcon> optionalRequirements = cardData.optionalRequirements.ToList();
            List<CardIcon> adjacentRequirements = cardData.adjacentRequirements.ToList();
            bool mainGlobalCondition = PassedGlobalRequirements(primaryTableIcons, mainRequirements);
            bool optionalGlobalCondition = PassedOptionalGlobalRequirements(primaryTableIcons, optionalRequirements);
            bool adjacentGlobalCondition = PassedSingleRequirement(primaryTableIcons, adjacentRequirements);

            if (mainGlobalCondition && optionalGlobalCondition && adjacentGlobalCondition) // check for combined requirement types
            {
                List<List<CardIcon>> adjacentHolderIcons = _tableView.GetAdjacentPrimaryHolderIcons(holderData);
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
            if (!Array.Exists(new CardType[] { CardType.Landscape, CardType.Discovery }, type => type == cardData.cardType))
            {
                return false;
            }

            primaryTableIcons.AddRange(_tableView.GetAllRelevantIcons(HolderSubType.SECONDARY)); // expand primary icons with secondary icons
            if (cardData.cardType == CardType.Landscape)
            {
                if(holderData.IsEmpty()) 
                {
                    if (cardData.requirements.Length == 1) // landscape card has only 1 road token requirement (1 requirement is always a road token)
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
                if (cardData.cardType == CardType.Landscape)
                {
                    return true;
                }
                else
                {
                    return PassedSingleRequirement(holderData.GetAllIconsOfHolder(), mainRequirements);
                }
            }
            else
            {
                return false;
            }
        }
    }

    protected List<CardIcon[]> CreateIconPairsFromRequirements(List<CardIcon> requirements)
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

    protected bool PassedGlobalRequirements(List<CardIcon> allTableIcons, List<CardIcon> requirements)
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

    protected bool PassedOptionalGlobalRequirements(List<CardIcon> allTableIcons, List<CardIcon> requirements)
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

    protected bool PassedSingleRequirement(List<CardIcon> holderIcons, List<CardIcon> requirements)
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

    protected Delegate GetUpdateDisplayIconsHandler()
    {
        return (Action<GameTask, List<Card>, List<HolderData>>)_iconDisplayView.UpdateIconsHandler;
    }

    protected List<List<CardIcon>> CreateAdjacentIconPairs(List<CardIcon[]> topIcons)
    {
        List<List<CardIcon>> pairs = new();
        if (topIcons.Count < 2)
        {
            return pairs;
        }

        for (int i = 0; i < topIcons.Count - 1; i++) // create pairs for every posible adjacent icon combinations
        {
            CardIcon[] icons1 = topIcons[i];
            CardIcon[] icons2 = topIcons[i + 1];
            int length = icons1.Length * icons2.Length;
            CardIcon[][] adjacentIcons = new CardIcon[][] { icons1, icons2 };
            adjacentIcons.OrderBy(icons => icons.Length).Reverse();
            int index1 = 0;
            int index2 = 0;
            for (int j = 0; j < length; j++)
            {
                CardIcon icon1 = adjacentIcons[0][index1];
                CardIcon icon2 = adjacentIcons[1][index2];
                if (icon1 != icon2) // ignore same icon pairs
                {
                    List<CardIcon> pair = new() { icon1, icon2 };
                    pairs.Add(pair);
                }
                index1++;
                if (index1 > adjacentIcons[0].Length - 1)
                {
                    index1 = 0;
                    index2++;
                }
            }
        }
        return pairs;
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

    public void ResetCampScoreTokens()
    {
        _campScoreTokens = new() { 2, 3, 4 };
    }

    public int GetNextCampScoreToken()
    {
        return _campScoreTokens.Count > 0 ? _campScoreTokens.First() : 0;
    }

    public void UpdateCampScoreTokens()
    {
        _campScoreTokens.RemoveAt(0);
    }
}
