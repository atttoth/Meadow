using System.Collections.Generic;
using UnityEngine;

public class GameLogicManager : MonoBehaviour
{
    private GameLogicController _gameLogicController;

    private void Start()
    {
        _gameLogicController = new(GameResourceManager.Instance.boardController, GameResourceManager.Instance.campController, GameResourceManager.Instance.screenController);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) //todo: create game menu
        {
            GameSettings.Instance.SetGameSpeed(GameSpeed.SPEED5);
            GameMode gameMode = new GameMode(GameModeType.SINGLE_PLAYER, GameDifficulty.VERY_HARD);
            _gameLogicController.SetupSession(gameMode, CreateUserControllersForSession(gameMode));
            OnLogicEvent(-1, new object[0]);
        }
    }

    private void OnApplicationQuit()
    {
        _gameLogicController.Kill();
    }

    private UserController[] CreateUserControllersForSession(GameMode gameMode)
    {
        List<UserController> userControllers = new() { GameResourceManager.Instance.playerController };
        Transform userControllerContainer = GameObject.Find("GameCanvas").transform.GetChild(1);
        if (gameMode.ModeType == GameModeType.SINGLE_PLAYER)
        {
            for (int i = 0; i < gameMode.NumOfNpcControllers; i++)
            {
                NpcController npcController = Instantiate(GameResourceManager.Instance.npcControllerPrefab, userControllerContainer).GetComponent<NpcController>();
                npcController.userID = userControllers.Count;
                npcController.transform.SetSiblingIndex(userControllers.Count);
                userControllers.Add(npcController);
            }
        }
        userControllers.ForEach(controller => controller.CreateUser(gameMode));
        return userControllers.ToArray();
    }

    public void OnLogicEvent(object eventType, object[] args)
    {
        _gameLogicController.Execute((int)eventType, args);
    }
}
