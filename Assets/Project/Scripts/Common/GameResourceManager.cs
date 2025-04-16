using UnityEngine;
using UnityEngine.U2D;

public class GameResourceManager : MonoBehaviour
{
    private static GameResourceManager _instance;

    public static GameResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Instantiate(Resources.Load<GameResourceManager>("GameResourceManager"));
            }
            return _instance;
        }
    }

    public T GetAssetByName<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.Log("Could not find asset");
            return default;
        }
        return (T)typeof(GameResourceManager).GetField(name).GetValue(Instance);
    }

    public TextAsset cardDataJson;

    public Transform npcControllerPrefab;
    public Transform deckPrefab;
    public Transform cardPrefab;
    public Transform topIconsHolderPrefab;
    public Transform requiredIconsHolderPrefab;
    public Transform cardIconItemPrefab;
    public Transform boardCardHolderPrefab;
    public Transform tablePrimaryCardHolderPrefab;
    public Transform tableSecondaryCardHolderPrefab;
    public Transform markerPrefab;
    public Transform displayIconPrefab;
    public Transform dummyDisplayIconPrefab;
    public Transform cardScoreTextPrefab;
    public Transform screenDisplayItemPrefab;

    public SpriteAtlas Base;
    public SpriteAtlas West;
    public SpriteAtlas South;
    public SpriteAtlas East;
    public SpriteAtlas North;
}
