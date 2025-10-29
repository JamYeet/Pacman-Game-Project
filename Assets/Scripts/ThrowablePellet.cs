using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowablePellet : MonoBehaviour
{
    public float speed = 10f;
    public float maxLifetime = 5f;
    public float spinSpeed = 720f;
    private Vector2 direction;

    public AudioSource audioSource;
    public AudioClip wallHitClip;
    public ParticleSystem wallHitEffect;


    public void Launch(Vector2 dir)
    {
        direction = dir.normalized;
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            HandleAudioAndParticles();
            GameManager.Instance.ForceGhostsIntoRecovery();
            Destroy(gameObject, 0.05f);
        }

        else if (other.CompareTag("Ghost"))
        {
            GhostController ghost = other.GetComponent<GhostController>();
            if (ghost != null)
            {
                if (ghost.CurrentState == GhostState.Scared || ghost.CurrentState == GhostState.Recovering)
                {
                    HandleAudioAndParticles();
                    ghost.SetState(GhostState.Dead);
                    GameManager.Instance.AddScore(300);
                    GameManager.Instance.PlayGhostDeadMusic();
                }
            }
        }
    }

    private void HandleAudioAndParticles()
    {
        if (audioSource != null && wallHitClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(wallHitClip);
        }

        if (wallHitEffect != null)
        {
            ParticleSystem effect = Instantiate(wallHitEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration);
        }
    }

}
