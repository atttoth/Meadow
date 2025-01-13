using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ScoreCollectionScreen : MonoBehaviour
{
    private List<Transform> _scoreTextPool;
    private Transform _poolTransform;

    public void Init()
    {
        _scoreTextPool = new();
        _poolTransform = transform.GetChild(0).GetComponent<Transform>();
    }

    public Transform GetScoreTextObject()
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

    public void DisposeScoreTextObject(Transform scoreTextPrefab)
    {
        _scoreTextPool.Add(scoreTextPrefab);
        scoreTextPrefab.gameObject.SetActive(false);
    }
}
