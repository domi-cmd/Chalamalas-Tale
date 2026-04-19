using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
public class AudioManager : MonoBehaviour
{
    [Header("Music Sources")]
    [SerializeField] AudioSource calmSource;
    [SerializeField] AudioSource battleSource;
    [SerializeField] AudioLowPassFilter calmFilter;
    [SerializeField] AudioLowPassFilter battleFilter;

    [Header("Clips")]
    public AudioClip backgroundCalm;
    public AudioClip backgroundBattle;
    public static AudioManager instance;
    [Header("Settings")]
    public float transitionDuration = 2f;
    private Coroutine currentTransition;
    private int enemyCount = 0;  
    private void Awake()
    {
        calmSource.volume = 1f;
        battleSource.volume = 0f;
    }
    void Start()
    {
        calmSource.clip = backgroundCalm;
        battleSource.clip = backgroundBattle;
        
        //loop
        calmSource.loop = true;
        battleSource.loop = true;

        //play musics


        calmSource.Play();
        battleSource.Play();
    }
    public void Update()
    {
        if(PauseMenu.IsPaused == true)
        {
            calmFilter.cutoffFrequency = 500f;
            battleFilter.cutoffFrequency = 500f;
        }else
        {
            calmFilter.cutoffFrequency = 22000f;
            battleFilter.cutoffFrequency = 22000f;
        }
    }

    public void SwitchToBattle()
    {
        StartCrossfade(0f, 1f);
    }

    public void SwitchToCalm()
    {
        StartCrossfade(1f, 0f);
    }

    void StartCrossfade(float calmTarget, float battleTarget)
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(Crossfade(calmTarget, battleTarget));
    }

    IEnumerator Crossfade(float calmTarget, float battleTarget)
    {
        float time = 0f;

        float calmStart = calmSource.volume;
        float battleStart = battleSource.volume;

        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            float t = time / transitionDuration;

            calmSource.volume = Mathf.Lerp(calmStart, calmTarget, t);
            battleSource.volume = Mathf.Lerp(battleStart, battleTarget, t);

            yield return null;
        }

        calmSource.volume = calmTarget;
        battleSource.volume = battleTarget;
    }
    public void RegisterEnemy()
    {
        enemyCount++;

        if (enemyCount == 1)
            SwitchToBattle();
    }

    public void UnregisterEnemy()
    {
        enemyCount--;

        if (enemyCount <= 0)
        {
            enemyCount = 0;
            SwitchToCalm();
        }
    }
    void OnEnable()
{
    SceneManager.sceneLoaded += OnSceneLoaded;
}

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    // Reset
    enemyCount = 0;

    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

    enemyCount = enemies.Length;

    if (enemyCount > 0)
        SwitchToBattle();
    else
        SwitchToCalm();
}
}
