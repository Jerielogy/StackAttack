using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX Clips")]
    public AudioSource moveSound;   // Used for moving and rotating
    public AudioSource clearSound;  // Used for clearing lines

    [Header("BGM")]
    public AudioSource backgroundMusic;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps music playing during scene changes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMove() { if (moveSound) moveSound.Play(); }
    public void PlayClear() { if (clearSound) clearSound.Play(); }

    public void SetMusicVolume(float volume) { if (backgroundMusic) backgroundMusic.volume = volume; }
}