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
        {NoteType.CDragChild, 48}
    };
    private int initialDragLineObjectCount = 48;

    public readonly SortedDictionary<int, Note> SpawnedNotes = new SortedDictionary<int, Note>(); // Currently on-screen
    
    private readonly Dictionary<NoteType, NotePoolItem> notePoolItems = new Dictionary<NoteType, NotePoolItem>();
    private readonly DragLinePoolItem dragLinePoolItem = new DragLinePoolItem();
    private readonly Dictionary<EffectController.Effect, PrefabPoolItem> effectPoolItems = new Dictionary<EffectController.Effect, PrefabPoolItem>();
    
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
        initialNoteObjectCount[type] = count;
    }

    public void Initialize()
    {
        initialDragLineObjectCount = initialNoteObjectCount[NoteType.DragHead] 
                                     + initialNoteObjectCount[NoteType.DragChild]
                                     + initialNoteObjectCount[NoteType.CDragHead] 
                                     + initialNoteObjectCount[NoteType.CDragChild];
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
        var chart = Game.Chart;
        var map = new Dictionary<EffectController.Effect, int>
        {
            {
                EffectController.Effect.Clear,
                chart.MaxSamePageNonDragTypeNoteCount * 2
            },
            {
                EffectController.Effect.ClearDrag, 
                chart.MaxSamePageDragTypeNoteCount * 2
            },
            {
                EffectController.Effect.Miss,
                chart.MaxSamePageNoteCount * 2
            },
            {
                EffectController.Effect.Hold, 
                chart.MaxSamePageHoldTypeNoteCount * 16 * 2
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
        SpawnedNotes.Values.ForEach(it => it.Dispose());
        notePoolItems.Values.ForEach(it => it.Dispose());
        dragLinePoolItem.Dispose();
    }
    
    public Note SpawnNote(ChartModel.Note model)
    {
        var note = Spawn(notePoolItems[(NoteType) model.type], new NoteInstantiateProvider{Type = (NoteType) model.type}, new NoteSpawnProvider{Model = model});
        SpawnedNotes[model.id] = note;
        return note;
    }

    public void CollectNote(Note note)
    {
        if (!SpawnedNotes.ContainsKey(note.Model.id)) throw new ArgumentOutOfRangeException();
        Game.inputController.OnNoteCollected(note);
        Collect(notePoolItems[note.Type], note);
        SpawnedNotes.Remove(note.Model.id);
    }

    public DragLineElement SpawnDragLine(ChartModel.Note from, ChartModel.Note to)
    {
        return Spawn(dragLinePoolItem, new PoolItemInstantiateProvider(),
            new DragLineSpawnProvider {From = from, To = to});
    }

    public void CollectDragLine(DragLineElement element)
    {
        Collect(dragLinePoolItem, element);
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
        public override ParticleSystem OnInstantiate(Game game, ParticleSystemInstantiateProvider arguments)
        {
            return Object.Instantiate(arguments.Prefab, arguments.Parent, true);
        }

        public override void OnSpawn(Game game, ParticleSystem particle, ParticleSystemSpawnProvider arguments)
        {
            particle.gameObject.SetActive(true);
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
            particle.Stop();
            particle.gameObject.SetActive(false);
        }

        public override void Dispose()
        {
            PooledItems.ForEach(Object.Destroy);
        }
    }

}

