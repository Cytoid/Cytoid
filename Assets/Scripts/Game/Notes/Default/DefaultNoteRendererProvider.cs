using System.Collections.Generic;
using System.Linq;
using DragonBones;
using UnityEngine;
using UnityEngine.Serialization;

public class DefaultNoteRendererProvider : SingletonMonoBehavior<DefaultNoteRendererProvider>
{

    public UnityDragonBonesData ClickDragonBonesData;
    public UnityDragonBonesData ClickAltDragonBonesData;
    public UnityDragonBonesData DragDragonBonesData;
    public UnityDragonBonesData HoldDragonBonesData;
    public UnityDragonBonesData LongHoldDragonBonesData;
    public UnityDragonBonesData FlickDragonBonesData;
    
    protected void Start()
    {
        UnityFactory.factory.LoadData(ClickDragonBonesData);
        UnityFactory.factory.LoadData(ClickAltDragonBonesData);
        UnityFactory.factory.LoadData(DragDragonBonesData);
        UnityFactory.factory.LoadData(HoldDragonBonesData);
        UnityFactory.factory.LoadData(LongHoldDragonBonesData);
        UnityFactory.factory.LoadData(FlickDragonBonesData);
    }

}