// EventKit.cs is a moderatley modified version of the excellent Advanced C# Messenger by Ilya Suzdalnitski,
// which is itself based on Rod Hyde's "CSharpMessenger" and Magnus Wolffelt's "CSharpMessenger Extended".
//
// It's been updated to allow for more parameters in events, and its name and the names of some of its functions
// have been changed to better reflect the way I use it personally. Other than that, the script is mostly unchanged.

using System.Collections.Generic;
using System;
using UnityEngine;

static class EventKit
{

#pragma warning disable 0414
	//Ensures that the EventKitHelper will be created automatically upon start of the game.
	static private EventKitHelper messengerHelper = (new GameObject("EventKitHelper")).AddComponent< EventKitHelper >();

	static public Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

	//Message handlers that should never be removed, regardless of calling Cleanup
	static public List< string > permanentMessages = new List< string > ();

	//Marks a certain message as permanent.
	static public void MarkAsPermanent(string eventType)
	{
		permanentMessages.Add(eventType);
	}

	static public void OnListenerAdding(string eventType, Delegate listenerBeingAdded)
	{
		if (!eventTable.ContainsKey(eventType)) {
			eventTable.Add(eventType, null);
		}

		Delegate d = eventTable[eventType];

		if (d != null && d.GetType() != listenerBeingAdded.GetType()) {
			throw new ListenerException(string.Format("Attempting to add listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being added has type {2}", eventType, d.GetType().Name, listenerBeingAdded.GetType().Name));
		}
	}

	static public void OnListenerRemoving(string eventType, Delegate listenerBeingRemoved) { }

	static public void OnListenerRemoved(string eventType)
	{
		if (eventTable[eventType] == null) {
			eventTable.Remove(eventType);
		}
	}

	static public BroadcastException CreateBroadcastSignatureException(string eventType)
	{
		return new BroadcastException(string.Format("Broadcasting message \"{0}\" but listeners have a different signature than the broadcaster.", eventType));
	}

	public class BroadcastException : Exception
	{
		public BroadcastException(string msg)
			: base(msg)
		{
		}
	}

	public class ListenerException : Exception
	{
		public ListenerException(string msg)
			: base(msg)
		{
		}
	}

	static public void Subscribe(string eventType, Callback handler)
	{
		OnListenerAdding(eventType, handler);
		eventTable[eventType] = (Callback)eventTable[eventType] + handler;
	}

	static public void Subscribe<T>(string eventType, Callback<T> handler)
	{
		OnListenerAdding(eventType, handler);
		eventTable[eventType] = (Callback<T>)eventTable[eventType] + handler;
	}

	static public void Subscribe<T, U>(string eventType, Callback<T, U> handler)
	{
		OnListenerAdding(eventType, handler);
		eventTable[eventType] = (Callback<T, U>)eventTable[eventType] + handler;
	}

	static public void Subscribe<T, U, V>(string eventType, Callback<T, U, V> handler)
	{
		OnListenerAdding(eventType, handler);
		eventTable[eventType] = (Callback<T, U, V>)eventTable[eventType] + handler;
	}

	static public void Subscribe<T, U, V, W>(string eventType, Callback<T, U, V, W> handler)
	{
		OnListenerAdding(eventType, handler);
		eventTable[eventType] = (Callback<T, U, V, W>)eventTable[eventType] + handler;
	}

	static public void Subscribe<T, U, V, W, X>(string eventType, Callback<T, U, V, W, X> handler)
	{
		OnListenerAdding(eventType, handler);
		eventTable[eventType] = (Callback<T, U, V, W, X>)eventTable[eventType] + handler;
	}

	static public void Unsubscribe(string eventType, Callback handler)
	{
		OnListenerRemoving(eventType, handler);
		eventTable[eventType] = (Callback)eventTable[eventType] - handler;
		OnListenerRemoved(eventType);
	}

	static public void Unsubscribe<T>(string eventType, Callback<T> handler)
	{
		OnListenerRemoving(eventType, handler);
		eventTable[eventType] = (Callback<T>)eventTable[eventType] - handler;
		OnListenerRemoved(eventType);
	}

	static public void Unsubscribe<T, U>(string eventType, Callback<T, U> handler)
	{
		OnListenerRemoving(eventType, handler);
		eventTable[eventType] = (Callback<T, U>)eventTable[eventType] - handler;
		OnListenerRemoved(eventType);
	}

	static public void Unsubscribe<T, U, V>(string eventType, Callback<T, U, V> handler)
	{
		OnListenerRemoving(eventType, handler);
		eventTable[eventType] = (Callback<T, U, V>)eventTable[eventType] - handler;
		OnListenerRemoved(eventType);
	}

	static public void Unsubscribe<T, U, V, W>(string eventType, Callback<T, U, V, W> handler)
	{
		OnListenerRemoving(eventType, handler);
		eventTable[eventType] = (Callback<T, U, V, W>)eventTable[eventType] - handler;
		OnListenerRemoved(eventType);
	}

	static public void Unsubscribe<T, U, V, W, X>(string eventType, Callback<T, U, V, W, X> handler)
	{
		OnListenerRemoving(eventType, handler);
		eventTable[eventType] = (Callback<T, U, V, W, X>)eventTable[eventType] - handler;
		OnListenerRemoved(eventType);
	}
	
	static public void Broadcast(string eventType)
	{
		Delegate d;

		if (eventTable.TryGetValue(eventType, out d)) {
			var callback = d as Callback;

			if (callback != null) {
				callback();
			}
			else
			{
				throw CreateBroadcastSignatureException(eventType);
			}
		}
	}

	static public void Broadcast<T>(string eventType, T arg1)
	{
		Delegate d;

		if (eventTable.TryGetValue(eventType, out d)) {
			var callback = d as Callback<T>;

			if (callback != null) {
				callback(arg1);
			}
			else
			{
				throw CreateBroadcastSignatureException(eventType);
			}
		}
	}

	static public void Broadcast<T, U>(string eventType, T arg1, U arg2)
	{
		Delegate d;

		if (eventTable.TryGetValue(eventType, out d)) {
			var callback = d as Callback<T, U>;

			if (callback != null) {
				callback(arg1, arg2);
			}
			else
			{
				throw CreateBroadcastSignatureException(eventType);
			}
		}
	}

	static public void Broadcast<T, U, V>(string eventType, T arg1, U arg2, V arg3)
	{
		Delegate d;

		if (eventTable.TryGetValue(eventType, out d)) {
			var callback = d as Callback<T, U, V>;

			if (callback != null) {
				callback(arg1, arg2, arg3);
			}
			else
			{
				throw CreateBroadcastSignatureException(eventType);
			}
		}
	}

	static public void Broadcast<T, U, V, W>(string eventType, T arg1, U arg2, V arg3, W arg4)
	{
		Delegate d;

		if (eventTable.TryGetValue(eventType, out d)) {
			var callback = d as Callback<T, U, V, W>;

			if (callback != null) {
				callback(arg1, arg2, arg3, arg4);
			}
			else
			{
				throw CreateBroadcastSignatureException(eventType);
			}
		}
	}

	static public void Broadcast<T, U, V, W, X>(string eventType, T arg1, U arg2, V arg3, W arg4, X arg5)
	{
		Delegate d;

		if (eventTable.TryGetValue(eventType, out d)) {
			var callback = d as Callback<T, U, V, W, X>;

			if (callback != null) {
				callback(arg1, arg2, arg3, arg4, arg5);
			}
			else
			{
				throw CreateBroadcastSignatureException(eventType);
			}
		}
	}
	
	static public void Cleanup()
	{
		var messagesToRemove = new List<string>();

		foreach (KeyValuePair<string, Delegate> pair in eventTable)
		{
			bool wasFound = false;

			foreach (string message in permanentMessages)
			{
				if (pair.Key == message)
				{
					wasFound = true;
					break;
				}
			}

			if (!wasFound)
			{
				messagesToRemove.Add(pair.Key);
			}
		}
	}
}


//This manager will ensure that the messenger's eventTable will be cleaned up upon loading of a new level.
public sealed class EventKitHelper : MonoBehaviour
{
	void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	//Clean up eventTable every time a new level loads.
	public void SceneLoaded(int unused)
	{
		EventKit.Cleanup();
	}
}

// This version of Callback.cs was modified by Cary Miller.

public delegate void Callback();
public delegate void Callback<T>(T arg1);
public delegate void Callback<T, U>(T arg1, U arg2);
public delegate void Callback<T, U, V>(T arg1, U arg2, V arg3);
public delegate void Callback<T, U, V, W>(T arg1, U arg2, V arg3, W arg4);
public delegate void Callback<T, U, V, W, X>(T arg1, U arg2, V arg3, W arg4, X arg5);