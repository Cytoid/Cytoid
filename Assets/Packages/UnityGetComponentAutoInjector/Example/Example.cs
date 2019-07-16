using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
	[SerializeField, GetComponent]
	private Transform cachedTransform = null; 

	[SerializeField, GetComponent]
	private GameObject cachedGameObject = null;

	[SerializeField, GetComponentInParent]
	private Transform parent = null;

	[SerializeField, GetComponentInChildren]
	private Example children = null;

	[SerializeField, GetComponentInChildrenOnly]
	private Transform[] childrens = null;

	[SerializeField, GetComponentInChildrenOnly(false)]
	private Transform[] childrenOnly = null; 

	[SerializeField, FindGameObject("Main Camera")]
    private Camera find = null;

	[SerializeField, FindGameObject("Directional Light")]
	private Light find2 = null;

	private void Awake()
	{
		CDebug.Log("(Example) Cached : ", cachedTransform, cachedGameObject);
	}
}