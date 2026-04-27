using UnityEngine;

public class PlayerRangedAttack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float attackRangeRadius = 5f;
    public float firingCooldown = 1.25f;
    private float nextFiringTime = 0f;

    private GameObject attackArea;

    private void Start()
    {
        attackArea = new GameObject("RangedAttackArea");
        attackArea.transform.SetParent(transform);
        attackArea.transform.localPosition = Vector3.zero;

        CircleCollider2D col = attackArea.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = attackRangeRadius;

        var areaLogic = attackArea.AddComponent<PlayerRangedAttackArea>();
        areaLogic.Setup(this); 

        attackArea.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && Time.time >= nextFiringTime)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private System.Collections.IEnumerator PerformAttack()
    {
        nextFiringTime = Time.time + firingCooldown;
        attackArea.SetActive(true);
        
        // Wait a tiny bit so the OnEnable has time to process
        yield return new WaitForSeconds(0.05f);
        
        attackArea.SetActive(false);
    }
}