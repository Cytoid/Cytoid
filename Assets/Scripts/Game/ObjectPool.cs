using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPool
{
    
    private static readonly Dictionary<Type, int> InitialNoteObjectCount = new Dictionary<Type, int>
    {
        {typeof(ClickNote), 24},
        {typeof(HoldNote), 12},
        {typeof(LongHoldNote), 6},
        {typeof(FlickNote), 12},
        {typeof(DragHeadNote), 12},
        {typeof(DragChildNote), 48},
    };
    private const int InitialDragLineObjectCount = 48;

    public readonly SortedDictionary<int, Note> SpawnedNotes = new SortedDictionary<int, Note>(); // Currently on-screen
    private readonly Dictionary<Type, Queue<Note>> pooledNotes = new Dictionary<Type, Queue<Note>>(); // All note objects ever allocated
    private readonly Queue<DragLineElement> pooledDragLines = new Queue<DragLineElement>();
    
    public Game Game { get; }

    private readonly int contentLayer = LayerMask.NameToLayer("Content");
    
    public ObjectPool(Game game)
    {
        Game = game;
        foreach (var type in (NoteType[]) Enum.GetValues(typeof(NoteType))) pooledNotes[ToGameType(type)] = new Queue<Note>();
    }

    public void Initialize()
    {
        var timer = new BenchmarkTimer("Game ObjectPool");
        foreach (var type in InitialNoteObjectCount.Keys)
        {
            for (var i = 0; i < InitialNoteObjectCount[type]; i++)
            {
                var note = InstantiateNoteObject(type);
                note.Initialize(Game);
                note.gameObject.SetActive(false);
                pooledNotes[type].Enqueue(note);
            }
        }
        timer.Time("Notes");
        for (var i = 0; i < InitialDragLineObjectCount; i++)
        {
            var dragLine = InstantiateDragLine();
            dragLine.Initialize(Game);
            dragLine.gameObject.SetActive(false);
            pooledDragLines.Enqueue(dragLine);
        }
        timer.Time("DragLines");
        timer.Time();
    }

    public void Dispose()
    {
        SpawnedNotes.Values.ForEach(it => it.Dispose());
        pooledNotes.Values.ForEach(it => it.ForEach(x => x.Dispose()));
        pooledDragLines.ForEach(it => it.Dispose());
    }
    
    public Note SpawnNote(ChartModel.Note model)
    {
        var note = GetPooledNoteOrInstantiate((NoteType) model.type);
        note.gameObject.SetActive(true);
        note.Initialize(Game);
        note.SetData(model.id);
        note.gameObject.SetLayerRecursively(contentLayer);
        SpawnedNotes[model.id] = note;
        return note;
    }

    public void CollectNote(Note note)
    {
        if (!SpawnedNotes.ContainsKey(note.Model.id)) throw new ArgumentOutOfRangeException();
        Game.inputController.OnNoteCollected(note);
        note.gameObject.SetActive(false);
        SpawnedNotes.Remove(note.Model.id);
        pooledNotes[note.GetType()].Enqueue(note);
    }

    public Note GetPooledNoteOrInstantiate(NoteType type)
    {
        var gameType = ToGameType(type);
        var queue = pooledNotes[gameType];
        return queue.Count == 0 ? InstantiateNoteObject(gameType) : pooledNotes[gameType].Dequeue();
    }

    private Note InstantiateNoteObject(Type gameType)
    {
        var provider = GameObjectProvider.Instance;
        Note note;
        if (gameType == typeof(ClickNote)) note = Object.Instantiate(provider.clickNotePrefab, Game.contentParent.transform).GetComponent<Note>();
        else if (gameType == typeof(HoldNote)) note = Object.Instantiate(provider.holdNotePrefab, Game.contentParent.transform).GetComponent<Note>();
        else if (gameType == typeof(LongHoldNote)) note = Object.Instantiate(provider.longHoldNotePrefab, Game.contentParent.transform).GetComponent<Note>();
        else if (gameType == typeof(FlickNote)) note = Object.Instantiate(provider.flickNotePrefab, Game.contentParent.transform).GetComponent<Note>();
        else if (gameType == typeof(DragHeadNote))  note = Object.Instantiate(provider.dragHeadNotePrefab, Game.contentParent.transform).GetComponent<Note>();
        else if (gameType == typeof(DragChildNote)) note = Object.Instantiate(provider.dragChildNotePrefab, Game.contentParent.transform).GetComponent<Note>();
        else throw new ArgumentOutOfRangeException();
        return note;
    }

    public DragLineElement SpawnDragLine(ChartModel.Note from, ChartModel.Note to)
    {
        var dragLine = GetPooledDragLineOrInstantiate();
        dragLine.gameObject.SetActive(true);
        dragLine.SetData(from, to);
        return dragLine;
    }

    public void CollectDragLine(DragLineElement dragLine)
    {
        dragLine.gameObject.SetActive(false);
        pooledDragLines.Enqueue(dragLine);
    }

    public DragLineElement GetPooledDragLineOrInstantiate()
    {
        return pooledDragLines.Count == 0 ? InstantiateDragLine() : pooledDragLines.Dequeue();
    }

    public DragLineElement InstantiateDragLine()
    {
        var dragLine = Object.Instantiate(GameObjectProvider.Instance.dragLinePrefab, Game.contentParent.transform)
            .GetComponent<DragLineElement>();
        dragLine.gameObject.SetLayerRecursively(contentLayer);
        return dragLine;
    }
    
    private static NoteType ToNoteType<T>() where T : Note
    {
        if (typeof(T) == typeof(ClickNote)) return NoteType.Click;
        if (typeof(T) == typeof(DragHeadNote)) return NoteType.DragHead;
        if (typeof(T) == typeof(DragChildNote)) return NoteType.DragChild;
        if (typeof(T) == typeof(FlickNote)) return NoteType.Flick;
        if (typeof(T) == typeof(HoldNote)) return NoteType.Hold;
        if (typeof(T) == typeof(LongHoldNote)) return NoteType.LongHold;
        throw new ArgumentOutOfRangeException();
    }
    
    private static Type ToGameType(NoteType type)
    {
        switch (type)
        {
            case NoteType.Click:
                return typeof(ClickNote);
            case NoteType.Hold:
                return typeof(HoldNote);
            case NoteType.LongHold:
                return typeof(LongHoldNote);
            case NoteType.DragHead:
                return typeof(DragHeadNote);
            case NoteType.DragChild:
                return typeof(DragChildNote);
            case NoteType.Flick:
                return typeof(FlickNote);
            case NoteType.CDragHead:
                return typeof(DragHeadNote);
            case NoteType.CDragChild:
                return typeof(DragChildNote);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
}