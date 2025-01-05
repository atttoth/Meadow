using UnityEngine;
using UnityEngine.U2D;

public class GameAssets : MonoBehaviour
{
    private static GameAssets _i;

    public static GameAssets Instance
    {
        get
        {
            if (_i == null)
            {
                _i = Instantiate(Resources.Load<GameAssets>("GameAssets"));
            }
            return _i;
        }
    }

    public T GetAssetByName<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.Log("Could not find asset");
            return default;
        }
        return (T)typeof(GameAssets).GetField(name).GetValue(Instance);
    }

    public TextAsset cardDataJson;

    public Transform deckPrefab;
    public Transform cardPrefab;
    public Transform boardCardHolderPrefab;
    public Transform tableCardHolderPrefab;
    public Transform markerPrefab;
    public Transform displayIconPrefab;
    public Transform dummyDisplayIconPrefab;
    public Transform cardScoreTextPrefab;
    public Transform screenDisplayItemPrefab;

    public SpriteAtlas baseAtlas;
    public SpriteAtlas West;
    public SpriteAtlas South;
    public SpriteAtlas East;
    public SpriteAtlas North;
}
