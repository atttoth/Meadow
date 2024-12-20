using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static GameTask;

/**
 * execute handler task - ExecHandler()
 * insert new handler - StartHandler()
 * jump to next task state - StartDelayMs()
 * jump to any task state - NextState()
 * complete handler task - Complete()
 **/
public class GameTask
{
    public delegate void GameTaskHandler(GameTask task);
    private List<ArrayList> _taskItems;
    private ArrayList _currentTaskItem;
    private int _duration;

    public GameTask()
    {
        _duration = 0;
        _taskItems = new List<ArrayList>() { };
        _currentTaskItem = new ArrayList() { };
    }

    public GameTaskItemData Data
    {
        get { return (GameTaskItemData)_currentTaskItem[1]; }
        set { _currentTaskItem[1] = value; }
    }

    public int State
    {
        get { return (int)_currentTaskItem[2]; }
        set { _currentTaskItem[2] = value; }
    }

    public void StartHandler(GameTaskHandler handler, GameTaskItemData data = null)
    {
        State++;
        _duration = 0;
        CreateTaskItem(handler, data);
    }

    public async void ExecHandler(GameTaskHandler handler, GameTaskItemData data = null)
    {
        CreateTaskItem(handler, data);
        await Task.WhenAll(Execute());
        Debug.Log("task finished");
    }

    private void CreateTaskItem(GameTaskHandler handler, GameTaskItemData data)
    {
        GameTaskItemData itemData = data is null ? new GameTaskItemData() : data;
        ArrayList item = new() { handler, itemData, 0 };
        _taskItems.Add(item);
        _currentTaskItem = item;
    }

    private async Task Execute()
    {
        if(State == -1) // check if current handler has finished
        {
            _taskItems.RemoveAt(_taskItems.Count - 1);
            if(_taskItems.Count > 0) // check if any handlers left
            {
                _currentTaskItem = _taskItems[_taskItems.Count - 1];
            }
        }

        if(State > -1)
        {
            await Task.Delay(_duration);
            GameTaskHandler handler = (GameTaskHandler)_currentTaskItem[0];
            handler(this);
            await Execute();
        }
        else
        {
            await Task.Yield();
        }
    }

    public void StartDelayMs(int duration)
    {
        State++;
        _duration =+ duration;
    }

    public void NextState(int state, int duration = 0)
    {
        State = state;
        _duration = duration;
    }

    public void Complete()
    {
        State = -1;
        _duration = 0;
    }
}
