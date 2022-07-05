using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject zombie;
    public static int numberofzombie;
    [SerializeField] Transform[] wayitepoint;
    private void Start()
    {
        /*for (int i=0; i <= 10; i++)
        {
            wayitepoint[i]=GameObject.find
        }*/
    }
    private void Update()
    {
        Debug.Log(numberofzombie);
        if (numberofzombie < 6)
        {
            for (int i = 0; i < 6; i++)
            {
                if (numberofzombie >= 5)
                {
                    break;
                }
                Transform transform = wayitepoint[Random.Range(0, wayitepoint.Length)];
                Instantiate(zombie, transform.position, Quaternion.identity);
                numberofzombie++;
                i++;
            }
        }
    }
}
