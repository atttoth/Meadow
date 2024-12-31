using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerScoreView : ViewBase
{
    private List<Transform> _scoreTextPool;
    private Transform _poolTransform;
    private TextMeshProUGUI _totalScoreText;
    private int _totalScore;

    public override void Init()
    {
        _scoreTextPool = new();
        _totalScore = 0;
        _poolTransform = transform.GetChild(0).GetComponent<Transform>();
        _totalScoreText = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
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
        _totalScore += score;
        _totalScoreText.text = _totalScore.ToString();
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
}
