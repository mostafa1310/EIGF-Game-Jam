using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAnime : MonoBehaviour
{
    public GameObject Obj;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 28);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
