using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class InfoView : MonoBehaviour
{
    public Transform scoreTransform;
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

    public void Init()
    {
        _roadTokens = 2;
        _maxCardPlacements = 1;
        _cardPlacements = 0;
        _totalScore = 0;
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        Transform roadTokeItem = transform.GetChild(0);
        _roadTokensText = roadTokeItem.GetChild(0).GetComponent<TextMeshProUGUI>();
        roadTokeItem.GetChild(1).GetComponent<Image>().sprite = atlas.GetSprite("1");

        Transform cardPlacementItem = transform.GetChild(1);
        _remainingCardPlacementsText = cardPlacementItem.GetChild(0).GetComponent<TextMeshProUGUI>();
        cardPlacementItem.GetChild(1).GetComponent<Image>().sprite = atlas.GetSprite("3");

        Transform scoreItem = transform.GetChild(2);
        _totalScoreText = scoreItem.GetChild(0).GetComponent<TextMeshProUGUI>();
        scoreTransform = scoreItem.GetChild(1);
        scoreTransform.GetComponent<Image>().sprite = atlas.GetSprite("21");

        UpdateRoadTokensText();
        UpdateCardPlacementsText();
        RegisterScore(0);
    }

    public void RegisterScore(int score)
    {
        _totalScore += score;
        _totalScoreText.text = _totalScore.ToString();
    }

    public void IncrementNumberOfCardPlacements(GameTaskItemData data)
    {
        _cardPlacements++;
        UpdateCardPlacementsText();
    }

    public void DecrementNumberOfCardPlacements(GameTaskItemData data)
    {
        _cardPlacements--;
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

    public void AddRoadTokens(int value)
    {
        _roadTokens += value;
        UpdateRoadTokensText();
    }

    private void UpdateRoadTokensText()
    {
        _roadTokensText.text = _roadTokens.ToString();
    }
}
