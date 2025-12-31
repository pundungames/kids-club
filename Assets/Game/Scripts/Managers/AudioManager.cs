using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [System.Serializable]
    public class SoundCategory
    {
        public string categoryName; // Örnek: "Cowboy", "Archer", "Ambient", "Music"
        public List<Sound> sounds;
    }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        public bool loop;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
    }

    [SerializeField] private List<SoundCategory> soundCategories;
    private Dictionary<string, AudioSource> soundDictionary;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // Sahne deðiþse bile devam etsin istiyorsan aç
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        soundDictionary = new Dictionary<string, AudioSource>();

        foreach (var category in soundCategories)
        {
            foreach (var s in category.sounds)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = s.mixerGroup;
                source.clip = s.clip;
                source.loop = s.loop;
                source.volume = s.volume;
                source.pitch = s.pitch;
                soundDictionary[s.name] = source;
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        AllStop();
    }
    public void AllStop()
    {
        foreach (var source in soundDictionary.Values)
        {
            source.Stop();
        }
    }

    private bool IsSoundInCategory(string soundName, string categoryName)
    {
        var category = soundCategories.Find(c => c.categoryName == categoryName);
        if (category != null)
        {
            return category.sounds.Exists(s => s.name == soundName);
        }
        return false;
    }
    public bool IsPlaying(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioSource source))
        {
            return source.isPlaying;
        }
        else
        {
            Debug.LogWarning("AudioManager: Sound not found: " + soundName);
            return false;
        }
    }
    public void Play(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioSource source))
        {
            source.Play();
        }
        else
        {
            Debug.LogWarning("AudioManager: Sound not found: " + soundName);
        }
    }

    public void Stop(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioSource source))
        {
            source.Stop();
        }
        else
        {
            Debug.LogWarning("AudioManager: Sound not found: " + soundName);
        }
    }
    string playedMusic;
    public void PlayMusic(string musicName)
    {
        if (musicName == string.Empty) musicName = playedMusic;
        StopMusics();
        if (soundDictionary.TryGetValue(musicName, out AudioSource target))
        {
            playedMusic = musicName;
            target.Play();
        }
    }

    internal void StopMusics()
    {
        var musicCategory = soundCategories.Find(c => c.categoryName == "Music");

        foreach (var music in musicCategory.sounds)
        {
            if (soundDictionary.TryGetValue(music.name, out AudioSource musicSource))
            {
                musicSource.Stop();
            }
        }
    }
}
