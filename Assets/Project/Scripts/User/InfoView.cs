using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class InfoView : MonoBehaviour
{
    [HideInInspector]public Transform scoreTransform;
    protected TextMeshProUGUI _roadTokensText;
    protected TextMeshProUGUI _remainingCardPlacementsText;
    protected TextMeshProUGUI _totalScoreText;
    protected int _roadTokens;
    protected int _maxCardPlacements;
    protected int _cardPlacements;
    protected int _totalScore;

    public bool HasEnoughRoadTokens(int required)
    {
        return required <= _roadTokens;
    }

    public bool HasEnoughCardPlacements()
    {
        return _cardPlacements < _maxCardPlacements;
    }

    public void Init()
    {
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        Transform roadTokeItem = transform.GetChild(0);
        roadTokeItem.GetChild(0).GetComponent<Image>().sprite = atlas.GetSprite("action_1");
        _roadTokensText = roadTokeItem.GetChild(1).GetComponent<TextMeshProUGUI>();

        Transform cardPlacementItem = transform.GetChild(1);
        cardPlacementItem.GetChild(0).GetComponent<Image>().sprite = atlas.GetSprite("action_3");
        _remainingCardPlacementsText = cardPlacementItem.GetChild(1).GetComponent<TextMeshProUGUI>();

        Transform scoreItem = transform.GetChild(2);
        _totalScoreText = scoreItem.GetChild(1).GetComponent<TextMeshProUGUI>();
        scoreTransform = scoreItem.GetChild(0);
        scoreTransform.GetComponent<Image>().sprite = atlas.GetSprite("score");

        SetRoadTokens(2);
        SetMaxCardPlacement(1);
        SetCardPlacement(0);
        SetScore(0);
    }

    private void SetScore(int score)
    {
        _totalScore = score;
        _totalScoreText.text = _totalScore.ToString();
    }

    public void IncrementScore(int score)
    {
        _totalScore += score;
        _totalScoreText.text = _totalScore.ToString();
    }

    private void IncrementNumberOfCardPlacements()
    {
        _cardPlacements++;
        UpdateCardPlacementsText();
    }

    private void DecrementNumberOfCardPlacements()
    {
        _cardPlacements--;
        UpdateCardPlacementsText();
    }

    public void UpdateNumberOfCardPlacementsAction(object[] args)
    {
        bool isActionCancelled = (bool)args[1];
        if(isActionCancelled)
        {
            DecrementNumberOfCardPlacements();
        }
        else
        {
            IncrementNumberOfCardPlacements();
        }
    }

    public void SetCardPlacement(int value)
    {
        _cardPlacements = value;
        UpdateCardPlacementsText();
    }

    public void SetMaxCardPlacement(int value)
    {
        _maxCardPlacements = value;
        UpdateCardPlacementsText();
    }

    private void UpdateCardPlacementsText()
    {
        _remainingCardPlacementsText.text = $"{_cardPlacements} / {_maxCardPlacements}";
    }

    private void SetRoadTokens(int value)
    {
        _roadTokens = value;
        UpdateRoadTokensText();
    }

    public void AddRoadTokens(int value)
    {
        _roadTokens += value;
        UpdateRoadTokensText();
    }

    private void RemoveRoadTokens(int value)
    {
        _roadTokens -= value;
        UpdateRoadTokensText();
    }

    public void UpdateRoadTokensAction(object[] args)
    {
        bool isActionCancelled = (bool)args[1];
        Card card = (Card)args[3];
        if (card.Data.cardType == CardType.Landscape)
        {
            int numOfRoadIcons = card.Data.requirements.ToList().Where(icon => CardIcon.RoadToken == icon).Count();
            if (isActionCancelled)
            {
                AddRoadTokens(numOfRoadIcons);
            }
            else
            {
                RemoveRoadTokens(numOfRoadIcons);
            }
        }
    }

    private void UpdateRoadTokensText()
    {
        _roadTokensText.text = _roadTokens.ToString();
    }
}
