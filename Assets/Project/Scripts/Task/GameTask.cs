using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/**
 * execute handler task - ExecHandler()
 * insert new handler - StartHandler()
 * jump to next task state - StartDelayMs()
 * jump to any task state - NextState()
 * complete handler task - Complete()
 **/
public class GameTask
{
    private List<ArrayList> _taskItems;
    private ArrayList _currentTaskItem;
    private int _duration;

    public GameTask()
    {
        _duration = 0;
        _taskItems = new List<ArrayList>() { };
        _currentTaskItem = new ArrayList() { };
    }

    public int State
    {
        get { return (int)_currentTaskItem[2]; }
        set { _currentTaskItem[2] = value; }
    }

    public void StartHandler(Delegate f, params object[] args)
    {
        State++;
        _duration = 0;
        CreateTaskItem(f, args);
    }

    public async void ExecHandler(Delegate f, params object[] args)
    {
        CreateTaskItem(f, args);
        await Task.WhenAll(Execute());
    }

    private void CreateTaskItem(Delegate f, object[] args)
    {
        ArrayList item = new() { f, args, 0 };
        _taskItems.Add(item);
        _currentTaskItem = item;
    }

    private async Task Execute()
    {
        if (State == -1) // check if current handler has finished
        {
            _taskItems.RemoveAt(_taskItems.Count - 1);
            if (_taskItems.Count > 0) // check if any handlers left
            {
                _currentTaskItem = _taskItems[_taskItems.Count - 1];
            }
        }

        if (State > -1)
        {
            await Task.Delay(_duration);
            Delegate f = (Delegate)_currentTaskItem[0];
            object[] args = (object[])_currentTaskItem[1];
            object[] updatedArgs = new object[args.Length + 1];
            updatedArgs[0] = this;
            for (int i = 0; i < args.Length; i++)
            {
                updatedArgs[i + 1] = args[i];
            }
            f.DynamicInvoke(updatedArgs);
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
        _duration = duration;
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
