using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeSwipeAttack : MonoBehaviour
{
    private GameObject attackArea = default;

    private bool attacking = false;
    private float timeToAttack = 0.25f;
    private float timer = 0f;


    void Start()
    {
        attackArea = transform.GetChild(0).gameObject;
    }
    
    void Update()
    {
        // Here we define the input key which triggers the attack (for now set to j)
        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }

        if (attacking)
        {
            timer += Time.deltaTime;

            if(timer > timeToAttack)
            {
                timer = 0;
                attacking = false;
                attackArea.SetActive(attacking);
            }
        }
        
    }

    // Activates the attack area, which then checks whether there are colliders in the trigger area, meaning
    // that an enemy has been hit :)
    void Attack()
    {
        attacking = true;
        attackArea.SetActive(attacking);
    }
}
