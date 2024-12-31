using UnityEngine;

public class ReferenceManager : MonoBehaviour
{
    public static ReferenceManager Instance; // A static reference to the GameManager instance
    public GameLogicManager gameLogicManager;
    public BoardManager boardManager;
    public PlayerManager playerManager;
    public CampManager campManager;
    public OverlayManager overlayManager;

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
}
