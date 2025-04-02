using System;
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
        if (Input.GetKeyDown(KeyCode.E)) // for testing
        {
            (_gameLogicController.GetActiveUserController() as PlayerController).ResetCampScoreTokens();
            (_gameLogicController.GetActiveUserController() as PlayerController).EnableTableView(false);
            //new GameTask().ExecHandler((Action<GameTask>)_campController.ShowViewSetupHandler);
            new GameTask().ExecHandler((Action<GameTask>)_gameLogicController.TestHandler);
        }

        if (Input.GetKeyDown(KeyCode.R)) // for testing
        {
            (_gameLogicController.GetActiveUserController() as PlayerController).ResetMarkers(); // make markers disappear in a pattern?
            (_gameLogicController.GetActiveUserController() as PlayerController).EnableTableView(true);
            _gameLogicController.TestFunction1();
        }

        if (Input.GetKeyDown(KeyCode.S)) // for testing
        {
            _gameLogicController.TestFunction2();
        }

        if (Input.GetKeyDown(KeyCode.P)) // for testing
        {
            GameMode gameMode = new GameMode(GameModeType.SINGLE_PLAYER_RANDOM, 1, 1);
            _gameLogicController.SetupSession(gameMode, CreateUserControllersForRound(gameMode));
        }
    }

    private UserController[] CreateUserControllersForRound(GameMode gameMode)
    {
        List<UserController> userControllers = new() { ReferenceManager.Instance.playerController };
        Transform userControllerContainer = GameObject.Find("GameCanvas").transform.GetChild(1);
        if (gameMode.numOfNpcControllers > 0)
        {
            for (int i = 0; i < gameMode.numOfNpcControllers; i++)
            {
                NpcController npcController = Instantiate(GameAssets.Instance.npcControllerPrefab, userControllerContainer).GetComponent<NpcController>();
                npcController.userID = userControllers.Count;
                npcController.transform.SetSiblingIndex(userControllers.Count);
                userControllers.Add(npcController);
            }
        }
        return userControllers.ToArray();
    }
}
