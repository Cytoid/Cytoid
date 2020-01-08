using System;
using DG.Tweening;
using Proyecto26;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public abstract class ContainerEntry<T> : MonoBehaviour
{

    public abstract void SetModel(T entry);

    public abstract T GetModel();

}