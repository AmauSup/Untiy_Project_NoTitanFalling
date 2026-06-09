using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public void ToggleMute()
    {
        AudioListener.pause = !AudioListener.pause;
    }
}
