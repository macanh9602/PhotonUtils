using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A dispatcher to run actions on the main Unity thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _queue = new();

    public static void Enqueue(Action action)
    {
        lock (_queue) _queue.Enqueue(action);
    }

    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
                _queue.Dequeue()?.Invoke();
        }
    }
}
