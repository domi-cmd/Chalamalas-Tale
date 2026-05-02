using UnityEngine;
using System.Collections;  // for different coroutines
using UnityEngine.UI; // for healthbar
/*
class for the dragon ennemy:
- set up: put maximal health
- take damage: only when there are no flames around the dragon
- attack: ATM the dragon doesn't attack directly but from their fire (different patterns generated throught the battle)

*/
public class EnemyDragon : MonoBehaviour, IDamageable
{


    // public SpriteRenderer spriteImage;  // to check if needed

    //define max health
    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBar;

    // flames spawned
    public GameObject flamePrefab; 


    // values for different patterns
    public Transform centerPoint;
    public float initialRadius = 0.5f;
    public float radiusStep = 0.1f;
    public float angleStep = 10f;
    public int flameCount = 70;
    public float spiralDelay = 0.01f;

    // to have the flames appear each 7 seconds
    private float flameTimer = 0f;
    public float flameCooldown = 4f;

    void Start()
    {
        // set health to max at the beginning
        currentHealth = maxHealth;
        healthBar.value = 1f;   // as the UI values are [0,1], we work with fractions of health to be shown

    }

    void Update()
    {
        flameTimer += Time.deltaTime;

        if (flameTimer >= flameCooldown)
        {
            SpawnCloseFlames();
            flameTimer = 0f;
        }
        
    }


    // damage handled via IDamageable
    public void TakeDamage(float damageAmount){
        if (damageAmount <= 0f)
            return;

        currentHealth -= damageAmount;
        Debug.Log("current healt" + currentHealth + "damage taken:" + damageAmount);

        // Update health bar
        healthBar.value = currentHealth / maxHealth;

        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
            Destroy(healthBar);
        }
    }

    public void SpawnCloseFlames()
    {
        StartCoroutine(SpawnCloseFlamesCoroutine());
    }

    private IEnumerator SpawnCloseFlamesCoroutine(){
        Vector3 center = centerPoint.position ;
        // Dragon size to define the borders of the rectangle
        SpriteRenderer sr = centerPoint.GetComponent<SpriteRenderer>();

        float halfWidth = sr.bounds.extents.x;
        float halfHeight = sr.bounds.extents.y;


        // Small margin so flames are "around" the dragon, not inside it
        float margin = 1f;
        float flameSize = 1f;

        float minX = center.x - halfWidth - margin;
        float maxX = center.x + halfWidth + margin;
        float minY = center.y - halfHeight - margin;
        float maxY = center.y + halfHeight + margin;

        int stepsX = Mathf.CeilToInt((maxX - minX) / flameSize);
        int stepsY = Mathf.CeilToInt((maxY - minY) / flameSize);

        for (int ix = 0; ix <= stepsX; ix++)
        {
            for (int iy = 0; iy <= stepsY; iy++)
            {
                /* uncomment for empty rectangle
                bool isBorder =
                    ix == 0 || ix == stepsX ||
                    iy == 0 || iy == stepsY;

                if (!isBorder)
                    continue;
                */

                float x = minX + ix * flameSize;
                float y = minY + iy * flameSize;

                Vector3 spawnPos = new Vector3(x, y, 0f);

                GameObject flame = Instantiate(flamePrefab, spawnPos, Quaternion.identity);
                Destroy(flame, 4f);
            }
        }
        yield return null;
    }

    IEnumerator SpawnSpiral()
    {
        float currentRadius = initialRadius;
        float currentAngle = 0f;

        for (int i = 0; i < flameCount; i++)
        {
            Debug.Log($"flame:{i}");
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 pos = centerPoint.position + new Vector3(
                Mathf.Cos(rad) * currentRadius,
                Mathf.Sin(rad) * currentRadius,
                0f
            );

            // Instantiate a true prefab clone
            Instantiate(flamePrefab, pos, Quaternion.identity);

            currentAngle -= angleStep;
            currentRadius += radiusStep;
            //radiusStep = radiusStep - 0.01;

            yield return new WaitForSecondsRealtime(spiralDelay);
        }
    }



 // sound change
   void OnEnable(){
    AudioManager am = FindAnyObjectByType<AudioManager>();
    if (am != null)
        am.RegisterEnemy();
    }

    void OnDisable()
    {
        AudioManager am = FindAnyObjectByType<AudioManager>();
        if (am != null)
            am.UnregisterEnemy();
    }
}