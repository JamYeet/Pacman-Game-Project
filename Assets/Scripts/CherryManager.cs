using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryManager : MonoBehaviour
{
    public GameObject cherryPrefab;
    public float spawnDelay = 5f;
    public Vector2 levelCenter = Vector2.zero;
    public Vector2 levelSize = new Vector2(27f, 29f);

    private GameObject currentCherry;
    private Coroutine spawnRoutine;

    void OnEnable()
    {
        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(CherrySpawnLoop());
        }
    }

    void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    IEnumerator CherrySpawnLoop()
    {
        // Wait
        yield return new WaitForSeconds(spawnDelay);

        while (true)
        {
            var cherryInstance = Instantiate(cherryPrefab);
            currentCherry = cherryInstance;

            CherryController ctrl = cherryInstance.GetComponent<CherryController>();
            ctrl.levelCenter = levelCenter;
            ctrl.levelSize = levelSize;

            yield return new WaitUntil(() => currentCherry == null);

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
