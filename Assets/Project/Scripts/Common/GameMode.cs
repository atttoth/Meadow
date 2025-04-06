using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameModeType
{
    SINGLE_PLAYER_RANDOM,
    SINGLE_PLAYER_NORMAL // todo
}

public class GameMode
{
    private GameModeType _modeType;
    private int _numOfPlayerControllers;
    private int _numOfNpcControllers;
    private Color32[] _markerColors = new Color32[] { 
        new Color32(33, 85, 229, 255),
        new Color32(255, 18, 0, 255),
        new Color32(255, 18, 0, 255),
        new Color32(255, 18, 0, 255)
    };

    // gameplay
    private int[][] _usersOrderMap;
    private int _userOrderIndex;
    private int _activeUserIndex;
    private int _currentRoundIndex;

    public GameMode(GameModeType modeType, int numOfPlayerControllers, int numOfNpcControllers)
    {
        _modeType = modeType;
        _numOfPlayerControllers = numOfPlayerControllers;
        _numOfNpcControllers = numOfNpcControllers;
        CreateOrderOfUsers();
    }

    public GameModeType ModeType { get { return _modeType; } }
    public int NumOfPlayerControllers { get { return _numOfPlayerControllers; } }
    public int NumOfNpcControllers {  get { return _numOfNpcControllers; } }
    public int ActiveUserIndex { get { return _activeUserIndex; } }
    public int CurrentRoundIndex { get { return _currentRoundIndex; } }
    public int[][] UsersOrderMap {  get { return _usersOrderMap; } }

    public bool IsGameEnded => _currentRoundIndex == _usersOrderMap.Length - 1;

    private void CreateOrderOfUsers()
    {
        int totalUsers = _numOfPlayerControllers + _numOfNpcControllers;
        int rounds = GetNumORounds(totalUsers);
        _usersOrderMap = new int[rounds][];
        _userOrderIndex = 0;
        _activeUserIndex = 0;
        _currentRoundIndex = 0;
        int startingUserID = 0;
        for (int i = 0; i < rounds; i++)
        {
            int[] orderOfIDs = new int[totalUsers];
            int userID = startingUserID;
            for (int j = 0; j < totalUsers; j++)
            {
                orderOfIDs[j] = userID;
                userID = userID == totalUsers - 1 ? 0 : userID + 1;
            }
            startingUserID = startingUserID == totalUsers - 1 ? 0 : startingUserID + 1;
            _usersOrderMap[i] = orderOfIDs;
        }
    }

    private int GetNumORounds(int totalUsers)
    {
        switch(totalUsers)
        {
            case 2:
            case 3: 
                return 6;
            default: 
                return 8;
        }
    }

    public Color32 GetMarkerColorByUserID(int userID)
    {
        return _markerColors[userID];
    }

    public void SetNextRound()
    {
        _currentRoundIndex++;
        _activeUserIndex = _usersOrderMap[_currentRoundIndex][0];
    }

    public bool IsOverHalfTime(int roundIndex = -1)
    {
        int index = roundIndex > -1 ? roundIndex : _currentRoundIndex;
        return index > (int)(_usersOrderMap.Length * 0.5f) - 1;
    }

    public int PeekNextUserID()
    {
        int userOrderIndex = _userOrderIndex == (_numOfPlayerControllers + _numOfNpcControllers) - 1 ? 0 : _userOrderIndex + 1;
        return _usersOrderMap[_currentRoundIndex][userOrderIndex];
    }

    public void SetNextActiveUserIndex()
    {
        _userOrderIndex = _userOrderIndex == (_numOfPlayerControllers + _numOfNpcControllers) - 1 ? 0 : _userOrderIndex + 1;
        _activeUserIndex = _usersOrderMap[_currentRoundIndex][_userOrderIndex];
    }
}
