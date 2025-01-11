using UnityEngine;

public class PlayAudioForAnimation : StateMachineBehaviour
{
    [Header("属性")]
    [Tooltip("播放音频")] public AudioClip audioClip;
    [Tooltip("音频音量")] public float volume = 1f;
    [Tooltip("播放延迟")] public float delay = 0f;
    // 音频源
    private AudioSource audioSource;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 获取音频源
        audioSource = animator.gameObject.GetComponent<AudioSource>();
        // 设定音频
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        // 播放音频
        if (delay > 0f)
        {
            audioSource.PlayDelayed(delay);
        }
        else
        {
            audioSource.Play();
        }
    }
}
