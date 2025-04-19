using UnityEngine;
using UnityEngine.U2D;

public class GameResourceManager : MonoBehaviour
{
    public static GameResourceManager Instance;

    void Awake()
    {
        if (Instance == null) // If there is no instance already
        {
            DontDestroyOnLoad(gameObject); // Keep the GameObject, this component is attached to, across different scenes
            Instance = this;
        }
        else if (Instance != this) // If there is already an instance and it's not `this` instance
        {
            Destroy(gameObject); // Destroy the GameObject, this component is attached to
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
    public Transform userAvatarPrefab;

    public SpriteAtlas Base;
    public SpriteAtlas West;
    public SpriteAtlas South;
    public SpriteAtlas East;
    public SpriteAtlas North;

    public GameLogicManager gameLogicManager;
    public BoardController boardController;
    public PlayerController playerController;
    public CampController campController;
    public ScreenController screenController;
}
