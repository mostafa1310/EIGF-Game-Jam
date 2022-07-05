using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject zombie;
    public float delayTime = 4f;

    IEnumerator Start()
    {
        var obj = Instantiate(zombie, transform.position, transform.rotation) as GameObject;
        yield return new WaitForSeconds(delayTime);
        StartCoroutine(Start());
    }
}
