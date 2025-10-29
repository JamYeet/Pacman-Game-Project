using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{

    public Transform[] waypoints;
    public float speed = 3f;

    private int currentWaypointIndex = 0;
    public Animator anim;

    // Update is called once per frame
    void Update()
    {
        if (waypoints.Length == 0) return;

        Vector3 target = waypoints[currentWaypointIndex].position;
        Vector3 moveDir = (target - transform.position).normalized;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        UpdateAnimation(moveDir);

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    void UpdateAnimation(Vector3 dir)
    {
        if (anim == null) return;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x > 0)
            {
                anim.Play("Player_Walk_Right");
            }
            else if (dir.x < 0)
            {
                anim.Play("Player_Walk_Left");
            }
        }
        else
        {
            if (dir.y > 0)
            {
                anim.Play("Player_Walk_Up");
            }
            else if (dir.y < 0)
            {
                anim.Play("Player_Walk_Down");
            }
        }
    }
}
