using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A dispatcher to run actions on the main Unity thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{

    #region === RUNTIME DATA ===
    private static readonly Queue<Action> _queue = new();
    #endregion

    #region === UNITY LIFECYCLE ===
    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
                _queue.Dequeue()?.Invoke();
        }
    }
    #endregion

    #region === PUBLIC API ===
    public static void Enqueue(Action action)
    {
        lock (_queue) _queue.Enqueue(action);
    }
    #endregion
}
