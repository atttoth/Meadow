using System.Collections.Generic;
using UnityEngine;

public class GameLogicManager : MonoBehaviour
{
    private GameLogicController _gameLogicController;

    private void Start()
    {
        _gameLogicController = ReferenceManager.Instance.gameLogicController;
        _gameLogicController.Init();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) //todo: create game menu
        {
            GameMode gameMode = new GameMode(GameModeType.SINGLE_PLAYER_RANDOM, 1);
            _gameLogicController.StartSession(gameMode, CreateUserControllersForSession(gameMode));
        }
    }

    private UserController[] CreateUserControllersForSession(GameMode gameMode)
    {
        List<UserController> userControllers = new() { ReferenceManager.Instance.playerController };
        Transform userControllerContainer = GameObject.Find("GameCanvas").transform.GetChild(1);
        if (gameMode.ModeType == GameModeType.SINGLE_PLAYER_RANDOM)
        {
            for (int i = 0; i < gameMode.NumOfNpcControllers; i++)
            {
                NpcController npcController = Instantiate(GameAssets.Instance.npcControllerPrefab, userControllerContainer).GetComponent<NpcController>();
                npcController.userID = userControllers.Count;
                npcController.transform.SetSiblingIndex(userControllers.Count);
                userControllers.Add(npcController);
            }
        }
        userControllers.ForEach(controller => controller.CreateUser(gameMode));
        return userControllers.ToArray();
    }
}
