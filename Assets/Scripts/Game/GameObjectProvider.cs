using UnityEngine;

public class GameObjectProvider : SingletonMonoBehavior<GameObjectProvider>
{
     public GameObject clickNotePrefab;
     public GameObject dragChildNotePrefab;
     public GameObject dragHeadNotePrefab;
     public GameObject dragLinePrefab;
     public GameObject flickNotePrefab;
     public GameObject holdNotePrefab;
     public GameObject longHoldNotePrefab;
     public GameObject boundaryBottom;
     public GameObject boundaryTop;
}