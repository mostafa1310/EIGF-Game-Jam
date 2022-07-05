using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class enemyai : MonoBehaviour
{
    [SerializeField] NavMeshAgent enemy;
    [SerializeField] GameObject player;
    float disteins;
    [SerializeField] Animator enemyanim;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        enemyanim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        disteins = Vector3.Distance(enemy.gameObject.transform.position, player.transform.position);
        if (disteins <= 20)
        {
            enemy.SetDestination(player.transform.position);
            enemyanim.SetBool("run", true);
        }
    }
}
