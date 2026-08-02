using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPool
{

    private readonly Dictionary<NoteType, int> initialNoteObjectCount = new Dictionary<NoteType, int>
    {
        {NoteType.Click, 24},
        {NoteType.Hold, 12},
        {NoteType.LongHold, 6},
        {NoteType.Flick, 12},
        {NoteType.DragHead, 12},
        {NoteType.DragChild, 48},
        {NoteType.CDragHead, 12},
        {NoteType.CDragChild, 48},
        {NoteType.DropClick, 24},
        {NoteType.DropDrag, 24}
    };
    private int initialDragLineObjectCount = 48;

    public readonly SortedDictionary<int, Note> SpawnedNotes = new SortedDictionary<int, Note>(); // Currently on-screen
    public readonly SortedDictionary<int, DragLineElement> SpawnedDragLines = new SortedDictionary<int, DragLineElement>();

    public readonly DragStackManager DragStacks = new DragStackManager();

    /// <summary>Hard caps so MaxSamePage on stress charts cannot preallocate thousands of FX.</summary>
    public const int MaxPooledClearEffects = 64;
    public const int MaxPooledMissEffects = 64;
    public const int MaxPooledHoldEffects = 128;
    public const int MaxPooledNotesPerType = 256;
    public const int MaxPooledDragLines = 256;

    private readonly Dictionary<NoteType, NotePoolItem> notePoolItems = new Dictionary<NoteType, NotePoolItem>();
    private readonly DragLinePoolItem dragLinePoolItem = new DragLinePoolItem();
    private readonly Dictionary<EffectController.Effect, PrefabPoolItem> effectPoolItems = new Dictionary<EffectController.Effect, PrefabPoolItem>();

    /// <summary>
    /// Bumped on <see cref="Dispose"/> so delayed FX collect callbacks can no-op.
    /// </summary>
    public int Generation { get; private set; }

    /// <summary>Geometry key → live shared drag line.</summary>
    private readonly Dictionary<long, DragLineElement> dragLinesByGeometry = new Dictionary<long, DragLineElement>();
    private readonly Dictionary<long, int> dragLineGeometryRefCount = new Dictionary<long, int>();
    private readonly Dictionary<int, long> dragLineFromIdToGeometry = new Dictionary<int, long>();

    public Game Game { get; }

    public ObjectPool(Game game)
    {
        Game = game;
        foreach (var type in (NoteType[]) Enum.GetValues(typeof(NoteType)))
        {
            notePoolItems[type] = new NotePoolItem();
        }
        foreach (var effect in (EffectController.Effect[]) Enum.GetValues(typeof(EffectController.Effect)))
        {
            effectPoolItems[effect] = new PrefabPoolItem();
        }
    }

    public void UpdateNoteObjectCount(NoteType type, int count)
    {
        initialNoteObjectCount[type] = Mathf.Clamp(count, 1, MaxPooledNotesPerType);
    }

    public void Initialize()
    {
        DragStacks.Bind(Game);

        initialDragLineObjectCount = Mathf.Clamp(
            initialNoteObjectCount[NoteType.DragHead]
            + initialNoteObjectCount[NoteType.DragChild]
            + initialNoteObjectCount[NoteType.CDragHead]
            + initialNoteObjectCount[NoteType.CDragChild],
            1,
            MaxPooledDragLines);

        // Prefer host count when stacks exist — far smaller than raw drag note peaks.
        var chart = Game.Chart;
        if (chart.MaxSamePageDragStackHostCount > 0)
        {
            var hostBased = Mathf.Max(
                chart.MaxSamePageDragStackHostCount * 2,
                chart.MaxSamePageNoteCountByType.TryGetValue(NoteType.Click, out var clickCount) ? clickCount : 24);
            initialDragLineObjectCount = Mathf.Clamp(
                Mathf.Max(initialDragLineObjectCount / 4, hostBased),
                1,
                MaxPooledDragLines);
        }

        var timer = new BenchmarkTimer("Game ObjectPool");
        foreach (var type in initialNoteObjectCount.Keys)
        {
            for (var i = 0; i < initialNoteObjectCount[type]; i++)
            {
                Collect(notePoolItems[type], Instantiate(notePoolItems[type], new NoteInstantiateProvider {Type = type}));
            }
        }
        timer.Time("Notes");
        for (var i = 0; i < initialDragLineObjectCount; i++)
        {
            Collect(dragLinePoolItem, Instantiate(dragLinePoolItem, new PoolItemInstantiateProvider()));
        }
        timer.Time("DragLines");
        var map = new Dictionary<EffectController.Effect, int>
        {
            {
                EffectController.Effect.Clear,
                Mathf.Clamp(chart.MaxSamePageNonDragTypeNoteCount * 2, 1, MaxPooledClearEffects)
            },
            {
                EffectController.Effect.ClearDrag,
                Mathf.Clamp(chart.MaxSamePageDragTypeNoteCount * 2, 1, MaxPooledClearEffects)
            },
            {
                EffectController.Effect.Miss,
                Mathf.Clamp(chart.MaxSamePageNoteCount * 2, 1, MaxPooledMissEffects)
            },
            {
                EffectController.Effect.Hold,
                Mathf.Clamp(chart.MaxSamePageHoldTypeNoteCount * 16 * 2, 1, MaxPooledHoldEffects)
            }
        };
        foreach (var pair in map)
        {
            var effect = pair.Key;
            var count = pair.Value;
            Debug.Log($"{effect} => {count}");
            for (var i = 0; i < count; i++)
            {
                Collect(effectPoolItems[effect], Instantiate(
                    effectPoolItems[effect], new ParticleSystemInstantiateProvider
                    {
                        Prefab = Game.effectController.GetPrefab(effect),
                        Parent = Game.effectController.EffectParentTransform
                    }));
            }
        }
        timer.Time("Effects");
        timer.Time();
    }

    public void Dispose()
    {
        Generation++;
        DragStacks.Dispose();
        SpawnedNotes.Values.ForEach(it => it.Dispose());
        SpawnedNotes.Clear();
        notePoolItems.Values.ForEach(it => it.Dispose());

        // Shared geometry may map many from-ids to one GO.
        var uniqueLines = new HashSet<DragLineElement>();
        foreach (var line in SpawnedDragLines.Values) uniqueLines.Add(line);
        foreach (var line in uniqueLines) line.Dispose();
        SpawnedDragLines.Clear();
        dragLinesByGeometry.Clear();
        dragLineGeometryRefCount.Clear();
        dragLineFromIdToGeometry.Clear();
        dragLinePoolItem.Dispose();
        effectPoolItems.Values.ForEach(it => it.Dispose());
    }

    public Note SpawnNote(ChartModel.Note model)
    {
        if (SpawnedNotes.ContainsKey(model.id)) return SpawnedNotes[model.id];
        var note = Spawn(notePoolItems[(NoteType) model.type], new NoteInstantiateProvider{Type = (NoteType) model.type}, new NoteSpawnProvider{Model = model});
        SpawnedNotes[model.id] = note;
        DragStacks.Register(note);
        return note;
    }

    public void CollectNote(Note note)
    {
        if (!SpawnedNotes.ContainsKey(note.Model.id)) return;
        Game.inputController.OnNoteCollected(note);
        Collect(notePoolItems[note.Type], note);
        SpawnedNotes.Remove(note.Model.id);
    }

    public DragLineElement SpawnDragLine(ChartModel.Note from, ChartModel.Note to)
    {
        if (SpawnedDragLines.ContainsKey(from.id)) return SpawnedDragLines[from.id];

        var geometryKey = MakeDragLineGeometryKey(from, to);
        if (dragLinesByGeometry.TryGetValue(geometryKey, out var shared))
        {
            dragLineGeometryRefCount[geometryKey] = dragLineGeometryRefCount[geometryKey] + 1;
            dragLineFromIdToGeometry[from.id] = geometryKey;
            shared.AddGeometryRef(from.id);
            SpawnedDragLines[from.id] = shared;
            return shared;
        }

        var line = Spawn(dragLinePoolItem, new PoolItemInstantiateProvider(),
            new DragLineSpawnProvider {From = from, To = to});
        line.GeometryKey = geometryKey;
        line.AddGeometryRef(from.id);
        dragLinesByGeometry[geometryKey] = line;
        dragLineGeometryRefCount[geometryKey] = 1;
        dragLineFromIdToGeometry[from.id] = geometryKey;
        SpawnedDragLines[from.id] = line;
        return line;
    }

    public void CollectDragLine(DragLineElement element)
    {
        if (element == null) return;

        var key = element.GeometryKey;
        if (key != 0)
        {
            dragLinesByGeometry.Remove(key);
            dragLineGeometryRefCount.Remove(key);
        }

        foreach (var fromId in element.DrainGeometryRefs())
        {
            SpawnedDragLines.Remove(fromId);
            dragLineFromIdToGeometry.Remove(fromId);
        }

        if (element.FromNoteModel != null)
        {
            SpawnedDragLines.Remove(element.FromNoteModel.id);
            dragLineFromIdToGeometry.Remove(element.FromNoteModel.id);
        }

        Collect(dragLinePoolItem, element);
    }

    private static long MakeDragLineGeometryKey(ChartModel.Note from, ChartModel.Note to)
    {
        // Include note type so Drag vs CDrag edges with matching quantized (t,x) do not share.
        unchecked
        {
            long hash = 17;
            hash = hash * 31 + from.type;
            hash = hash * 31 + to.type;
            hash = hash * 31 + (long) Mathf.Round(from.start_time * 1000f);
            hash = hash * 31 + (long) Mathf.Round(to.start_time * 1000f);
            hash = hash * 31 + (long) Mathf.Round((float) from.x * 10000f);
            hash = hash * 31 + (long) Mathf.Round((float) to.x * 10000f);
            return hash;
        }
    }

    public ParticleSystem SpawnEffect(EffectController.Effect effect, Vector3 position, Transform parent = default)
    {
        return Spawn(effectPoolItems[effect],
            new ParticleSystemInstantiateProvider
            {
                Prefab = Game.effectController.GetPrefab(effect), Parent = Game.effectController.EffectParentTransform
            },
            new ParticleSystemSpawnProvider
            {
                Position = position,
                Parent = parent
            });
    }

    public void CollectEffect(EffectController.Effect effect, ParticleSystem particle)
    {
        Collect(effectPoolItems[effect], particle);
    }

    private T Instantiate<T, TI, TS>(PoolItem<T, TI, TS> poolItem, TI instantiateArguments)
        where TI : PoolItemInstantiateProvider
        where TS : PoolItemSpawnProvider
    {
        // Debug.Log("Instantiating " + typeof(T).Name);
        return poolItem.OnInstantiate(Game, instantiateArguments);
    }

    private T Spawn<T, TI, TS>(PoolItem<T, TI, TS> poolItem, TI instantiateArguments, TS spawnArguments)
        where TI : PoolItemInstantiateProvider
        where TS : PoolItemSpawnProvider
    {
        var obj = poolItem.PooledItems.Count == 0 ? Instantiate(poolItem, instantiateArguments) : poolItem.PooledItems.Dequeue();
        poolItem.OnSpawn(Game, obj, spawnArguments);
        return obj;
    }

    private void Collect<T, TI, TS>(PoolItem<T, TI, TS> poolItem, T obj)
        where TI : PoolItemInstantiateProvider
        where TS : PoolItemSpawnProvider
    {
        poolItem.OnCollect(Game, obj);
        poolItem.PooledItems.Enqueue(obj);
    }

    public class PoolItemInstantiateProvider
    {
    }

    public class PoolItemSpawnProvider
    {
    }

    public abstract class PoolItem<T, TI, TS> where TI : PoolItemInstantiateProvider where TS : PoolItemSpawnProvider
    {
        public readonly Queue<T> PooledItems = new Queue<T>();

        public abstract T OnInstantiate(Game game, TI arguments);

        public abstract void OnSpawn(Game game, T item, TS arguments);

        public abstract void OnCollect(Game game, T item);

        public abstract void Dispose();

    }

    public class NoteInstantiateProvider : PoolItemInstantiateProvider
    {
        public NoteType Type;
    }

    public class NoteSpawnProvider : PoolItemSpawnProvider
    {
        public ChartModel.Note Model;
    }

    public class NotePoolItem : PoolItem<Note, NoteInstantiateProvider, NoteSpawnProvider>
    {
        public override Note OnInstantiate(Game game, NoteInstantiateProvider arguments)
        {
            var provider = GameObjectProvider.Instance;
            var type = arguments.Type;
            Note note;
            switch (type)
            {
                case NoteType.Click:
                    note = Object.Instantiate(provider.clickNotePrefab, game.contentParent.transform).GetComponent<Note>();
                    break;
                case NoteType.CDragHead:
                    note = Object.Instantiate(provider.cDragHeadNotePrefab, game.contentParent.transform).GetComponent<Note>();
                    break;
                case NoteType.Hold:
                    note = Object.Instantiate(provider.holdNotePrefab, game.contentParent.transform).GetComponent<Note>();
                    break;
                case NoteType.LongHold:
                    note = Object.Instantiate(provider.longHoldNotePrefab, game.contentParent.transform).GetComponent<Note>();
                    break;
                case NoteType.Flick:
                    note = Object.Instantiate(provider.flickNotePrefab, game.contentParent.transform).GetComponent<Note>();
                    break;
                case NoteType.DragHead:
                    note = Object.Instantiate(provider.dragHeadNotePrefab, game.contentParent.transform).GetComponent<Note>();
                    break;
                case NoteType.DragChild:
                case NoteType.CDragChild:
                    note = Object.Instantiate(provider.dragChildNotePrefab, game.contentParent.transform).GetComponent<Note>();
                    break;
                case NoteType.DropClick:
                    note = Object.Instantiate(provider.dropClickNotePrefab, game.contentParent.transform).GetComponent<DropClickNote>();
                    break;
                case NoteType.DropDrag:
                    note = Object.Instantiate(provider.dropDragNotePrefab, game.contentParent.transform).GetComponent<DropDragNote>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return note;
        }

        public override void OnSpawn(Game game, Note note, NoteSpawnProvider arguments)
        {
            note.gameObject.SetActive(true);
            note.Initialize(game);
            note.SetData(arguments.Model.id);
            note.gameObject.SetLayerRecursively(game.ContentLayer);
        }

        public override void OnCollect(Game game, Note note)
        {
            if (note == null || note.gameObject == null) return;
            note.IsDragStackFollower = false;
            note.DragStack = null;
            note.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            PooledItems.ForEach(it => it.Dispose());
        }
    }

    public class DragLineSpawnProvider : PoolItemSpawnProvider
    {
        public ChartModel.Note From;
        public ChartModel.Note To;
    }

    public class DragLinePoolItem : PoolItem<DragLineElement, PoolItemInstantiateProvider, DragLineSpawnProvider>
    {
        public override DragLineElement OnInstantiate(Game game, PoolItemInstantiateProvider arguments)
        {
            var dragLine = Object.Instantiate(GameObjectProvider.Instance.dragLinePrefab, game.contentParent.transform)
                .GetComponent<DragLineElement>();
            dragLine.gameObject.SetLayerRecursively(game.ContentLayer);
            return dragLine;
        }

        public override void OnSpawn(Game game, DragLineElement dragLine, DragLineSpawnProvider arguments)
        {
            dragLine.gameObject.SetActive(true);
            dragLine.Initialize(game);
            dragLine.SetData(arguments.From, arguments.To);
        }

        public override void OnCollect(Game game, DragLineElement dragLine)
        {
            if (dragLine == null || dragLine.gameObject == null) return;
            dragLine.GeometryKey = 0;
            dragLine.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            PooledItems.ForEach(it => it.Dispose());
        }
    }

    public class ParticleSystemInstantiateProvider : PoolItemInstantiateProvider
    {
        public ParticleSystem Prefab;
        public Transform Parent;
    }

    public class ParticleSystemSpawnProvider : PoolItemSpawnProvider
    {
        public Transform Parent;
        public Vector3 Position;
    }

    public class PrefabPoolItem : PoolItem<ParticleSystem, ParticleSystemInstantiateProvider, ParticleSystemSpawnProvider>
    {
        private readonly List<ParticleSystem> allItems = new List<ParticleSystem>();

        public override ParticleSystem OnInstantiate(Game game, ParticleSystemInstantiateProvider arguments)
        {
            var particle = Object.Instantiate(arguments.Prefab, arguments.Parent, true);
            allItems.Add(particle);
            return particle;
        }

        public override void OnSpawn(Game game, ParticleSystem particle, ParticleSystemSpawnProvider arguments)
        {
            particle.gameObject.SetActive(true);
            particle.transform.localScale = Vector3.one;
            if (arguments.Parent != default)
            {
                var transform = particle.transform;
                transform.SetParent(arguments.Parent);
                transform.localPosition = arguments.Position;
            }
            else
            {
                particle.transform.position = arguments.Position;
            }
            // Play() is controlled by the caller
        }

        public override void OnCollect(Game game, ParticleSystem particle)
        {
            if (particle == null || particle.gameObject == null) return;
            particle.Stop();
            particle.transform.localScale = Vector3.one;
            particle.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            allItems.ForEach(Object.Destroy);
            allItems.Clear();
            PooledItems.Clear();
        }
    }

}

