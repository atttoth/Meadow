using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class PlayerInfoView : ViewBase
{
    private TextMeshProUGUI _roadTokensText;
    private TextMeshProUGUI _remainingCardPlacementsText;
    private TextMeshProUGUI _totalScoreText;
    private List<Transform> _scoreTextPool;
    private Transform _poolTransform;
    public int roadTokens;
    public int maxCardPlacements;
    public int cardPlacements;
    public int totalScore;

    public override void Init()
    {
        roadTokens = 1;
        maxCardPlacements = 1;
        cardPlacements = 0;
        totalScore = 0;
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        Transform roadTokeItem = transform.GetChild(0);
        _roadTokensText = roadTokeItem.GetChild(0).GetComponent<TextMeshProUGUI>();
        roadTokeItem.GetChild(1).GetComponent<Image>().sprite = atlas.GetSprite("1");

        Transform cardPlacementItem = transform.GetChild(1);
        _remainingCardPlacementsText = cardPlacementItem.GetChild(0).GetComponent<TextMeshProUGUI>();
        cardPlacementItem.GetChild(1).GetComponent<Image>().sprite = atlas.GetSprite("3");

        _scoreTextPool = new();
        Transform scoreItem = transform.GetChild(2);
        //scoreItem.GetChild(0).GetComponent<Image>().sprite = atlas.GetSprite("");
        _totalScoreText = scoreItem.GetChild(1).GetComponent<TextMeshProUGUI>();
        _poolTransform = scoreItem.GetChild(2).GetComponent<Transform>();

        UpdateRoadTokensText();
        UpdateCardPlacementText();
        RegisterScore(0);
    }

    private Transform GetOrCreateScoreText()
    {
        Transform scoreTextPrefab;
        if (_scoreTextPool.Count > 0)
        {
            scoreTextPrefab = _scoreTextPool.First();
            _scoreTextPool.RemoveAt(0);
            scoreTextPrefab.transform.SetPositionAndRotation(_poolTransform.position, Quaternion.identity);
            scoreTextPrefab.gameObject.SetActive(true);
        }
        else
        {
            scoreTextPrefab = Object.Instantiate(GameAssets.Instance.cardScoreTextPrefab, _poolTransform);
        }
        return scoreTextPrefab;
    }

    private void DisposeScoreText(Transform scoreTextPrefab)
    {
        _scoreTextPool.Add(scoreTextPrefab);
        scoreTextPrefab.gameObject.SetActive(false);
    }

    private void RegisterScore(int score)
    {
        totalScore += score;
        _totalScoreText.text = totalScore.ToString();
    }

    public void CollectScoreOfCard(float delay, Card card)
    {
        Transform scoreTextPrefab = GetOrCreateScoreText();
        Transform startingPoint = card.GetComponent<Transform>();
        scoreTextPrefab.SetPositionAndRotation(startingPoint.position, Quaternion.identity);
        int score = card.Data.score;
        scoreTextPrefab.GetChild(1).GetComponent<TextMeshProUGUI>().text = score.ToString();
        float cardScoreCollectingSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreCollectingSpeed;

        Sequence scoreCollecting = DOTween.Sequence();
        scoreCollecting.Append(scoreTextPrefab.DOMove(_poolTransform.position, cardScoreCollectingSpeed).SetEase(Ease.InOutQuart).SetDelay(delay));
        scoreCollecting.OnComplete(() =>
        {
            DisposeScoreText(scoreTextPrefab);
            RegisterScore(score);
        });
    }

    public void UpdateCardPlacementText()
    {
        _remainingCardPlacementsText.text = $"{cardPlacements} / {maxCardPlacements}";
    }

    public void UpdateRoadTokensText()
    {
        _roadTokensText.text = roadTokens.ToString();
    }
}
