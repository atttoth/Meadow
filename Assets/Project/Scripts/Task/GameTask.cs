using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GameTask
{
    private List<ArrayList> _taskItems;
    private ArrayList _currentTaskItem;
    private int _duration;
    private bool _isWaiting;

    public GameTask()
    {
        _taskItems = new List<ArrayList>() { };
        _currentTaskItem = new ArrayList() { };
        _duration = 0;
        _isWaiting = false;
    }

    public int State
    {
        get { return (int)_currentTaskItem[2]; }
        set { _currentTaskItem[2] = value; }
    }

    /**
     * start task handler execution
     **/
    public void ExecHandler(CancellationToken cancellationToken, Delegate f, params object[] args)
    {
        CreateTaskItem(f, args);
        Execute(cancellationToken).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                //Debug.Log("task canceled.");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("task error: " + task.Exception);
            }  
            else
            {
                //Debug.Log("task completed.");
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    /**
     * insert new task handler into existing task handler, nested handlers execute in reverse call order (newest first)
     **/
    public void StartHandler(Delegate f, params object[] args)
    {
        State++;
        _duration = 0;
        CreateTaskItem(f, args);
    }

    private void CreateTaskItem(Delegate f, object[] args)
    {
        ArrayList item = new() { f, args, 0 };
        _taskItems.Add(item);
        _currentTaskItem = item;
    }

    /**
     * task handler waits at current state
     **/
    public void Wait()
    {
        if(_isWaiting)
        {
            NextState(State, 25);
        }
        else
        {
            StartDelayMs(0);
        }
    }

    /**
     * stop task handler waiting and continue task handler
     **/
    public void CancelWait()
    {
        _isWaiting = false;
    }

    /**
     * increment task handler state, start next state after delay, set wait flag to wait at next handler state
     **/
    public void StartDelayMs(int duration, bool wait = false)
    {
        if (wait)
        {
            _isWaiting = true;
        }
        State++;
        _duration = duration;
    }

    /**
     * set next task handler state, start next state after delay, set wait flag to wait at next handler state
     **/
    public void NextState(int state, int duration = 0, bool wait = false)
    {
        if (wait)
        {
            _isWaiting = true;
        }
        State = state;
        _duration = duration;
    }

    /**
     * set task handler state to -1 to finish handler execution
     **/
    public void Complete()
    {
        State = -1;
        _duration = 0;
    }

    private async Task Execute(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (State == -1) // check if current Delegate f has finished
            {
                _taskItems.RemoveAt(_taskItems.Count - 1);
                if (_taskItems.Count > 0) // check if any Delegate f left
                {
                    _currentTaskItem = _taskItems[_taskItems.Count - 1];
                }
                else
                {
                    break;
                }
            }

            Delegate f = (Delegate)_currentTaskItem[0];
            object[] args = (object[])_currentTaskItem[1];
            object[] updatedArgs = new object[args.Length + 1];
            updatedArgs[0] = this;
            for (int j = 0; j < args.Length; j++)
            {
                updatedArgs[j + 1] = args[j];
            }
            f.DynamicInvoke(updatedArgs);
            await Task.Delay(_duration, cancellationToken);
        }
    }
}
