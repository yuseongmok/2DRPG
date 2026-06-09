using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("오디오 소스")]
    private AudioSource bgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();

    [System.Serializable]
    public struct SoundEffect
    {
        public string soundName; // 오디오 이름 (예: "Hit", "Portal", "Click")
        public AudioClip clip;   // 실제 사운드 파일
    }

    [Header("사운드 리스트 등록")]
    public List<SoundEffect> bgmList;
    public List<SoundEffect> sfxList;

    [Header("최대 동시 효과음 개수")]
    public int maxSFXChannels = 10;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 음악이 끊기지 않게 보존!
            InitAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitAudioSources()
    {
        // 배경음 소스 초기화
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;

        // 효과음 소스 채널(풀) 생성
        for (int i = 0; i < maxSFXChannels; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            sfxSources.Add(source);
        }
    }

    //어디서든 배경음을 틀 때 호출하는 함수
    public void PlayBGM(string name, float volume = 0.5f)
    {
        SoundEffect sound = bgmList.Find(s => s.soundName == name);
        if (sound.clip != null)
        {
            if (bgmSource.clip == sound.clip && bgmSource.isPlaying) return; // 이미 재생 중이면 패스

            bgmSource.clip = sound.clip;
            bgmSource.volume = volume;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM을 찾을 수 없습니다: {name}");
        }
    }

    //어디서든 효과음을 터트릴 때 호출하는 함수
    public void PlaySFX(string name, float volume = 1f)
    {
        SoundEffect sound = sfxList.Find(s => s.soundName == name);
        if (sound.clip != null)
        {
            // 쉬고 있는 오디오 소스를 찾아서 재생
            AudioSource freeSource = sfxSources.Find(s => !s.isPlaying);
            
            // 만약 모든 채널이 바쁘다면 첫 번째 채널을 강제로 재사용
            if (freeSource == null) freeSource = sfxSources[0];

            freeSource.clip = sound.clip;
            freeSource.volume = volume;
            freeSource.Play();
        }
        else
        {
            Debug.LogWarning($"SFX를 찾을 수 없습니다: {name}");
        }
    }

    // 배경음 일시정지/정지 등 필요시 확장 가능
    public void StopBGM() => bgmSource.Stop();
}