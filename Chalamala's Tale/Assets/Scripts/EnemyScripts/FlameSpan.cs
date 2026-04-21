using UnityEngine;
using System.Collections;

public class FlameSpan : MonoBehaviour
{
    public GameObject flamePrefab; 
    public Transform centerPoint;
    public float initialRadius = 0.5f;
    public float radiusStep = 0.1f;
    public float angleStep = 10f;
    public int flameCount = 70;
    public float spiralDelay = 0.01f;

    void Start()
    {
        StartCoroutine(SpawnSpiral());
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
}