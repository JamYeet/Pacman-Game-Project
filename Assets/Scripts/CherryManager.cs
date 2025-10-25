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

    void Start()
    {
        StartCoroutine(CherrySpawnLoop());
    }

    IEnumerator CherrySpawnLoop()
    {
        // Wait 5 s before first cherry
        yield return new WaitForSeconds(spawnDelay);

        while (true)
        {
            // Spawn and track the instance
            var cherryInstance = Instantiate(cherryPrefab);
            currentCherry = cherryInstance;

            CherryController ctrl = cherryInstance.GetComponent<CherryController>();
            ctrl.levelCenter = levelCenter;
            ctrl.levelSize = levelSize;

            // Wait until the instance is gone
            yield return new WaitUntil(() => currentCherry == null);

            // Wait 5 s after destruction before next spawn
            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
