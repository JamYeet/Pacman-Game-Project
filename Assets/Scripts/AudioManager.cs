using System.Collections;
using System.Collections.Generic;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Start is called before the first frame update

    public AudioClip introMusic;
    public AudioClip ghostNormalMusic;

    private AudioSource audioSource;
    private AudioClip currentlyPlaying;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (introMusic != null)
        {
            audioSource.clip = introMusic;
            audioSource.loop = false;
            audioSource.Play();

            float waitTime = Mathf.Min(introMusic.length, 3f);
            Invoke(nameof(PlayGhostNormalStateMusic), waitTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PlayGhostNormalStateMusic()
    {
        audioSource.clip = ghostNormalMusic;
        audioSource.loop = true;
        audioSource.Play();
    }
}
