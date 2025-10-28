using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Vector2 levelCenter = Vector2.zero;
    public Vector2 levelSize = new Vector2(25f, 26f);

    private Vector3 startPos;
    private Vector3 endPos;
    private float lerpProgress = 0f;
    private bool isMoving = false;


    void Start()
    {
        SpawnCherry();
        StartCoroutine(MoveAcrossLevel());
    }

    void SpawnCherry()
    {
        int side = Random.Range(0, 4);
        float offset = 2f;

        switch (side)
        {
            case 0: // Left to Right
                startPos = new Vector3(-levelSize.x / 2 - offset, levelCenter.y + 0.5f, 0);
                endPos = new Vector3(levelSize.x / 2 + offset, levelCenter.y + 0.5f, 0);
                break;
            case 1: // Right to Left
                startPos = new Vector3(levelSize.x / 2 + offset, levelCenter.y + 0.5f, 0);
                endPos = new Vector3(-levelSize.x / 2 - offset, levelCenter.y + 0.5f, 0);
                break;
            case 2: // Top to Bottom
                startPos = new Vector3(levelCenter.x, levelSize.y / 2 + offset, 0);
                endPos = new Vector3(levelCenter.x, -levelSize.y / 2 - offset, 0);
                break;
            case 3: // Bottom to Top
                startPos = new Vector3(levelCenter.x, -levelSize.y / 2 - offset, 0);
                endPos = new Vector3(levelCenter.x, levelSize.y / 2 + offset, 0);
                break;
        }
        //
        transform.position = startPos;
        lerpProgress = 0f;
        isMoving = true;
    }

    IEnumerator MoveAcrossLevel()
    {
        while (isMoving)
        {
            lerpProgress += Time.deltaTime * moveSpeed / Vector3.Distance(startPos, endPos);
            transform.position = Vector3.Lerp(startPos, endPos, lerpProgress);

            if (lerpProgress >= 1f)
            {
                isMoving = false;
                Destroy(gameObject);
            }

            yield return null;
        }
    }

    // Future: handle PacStudent collisions
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) // PacStudent tagged "Player"
        {
            Debug.Log("PacStudent collected cherry!");
            Destroy(gameObject);
        }
    }
}
