using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClickSound : MonoBehaviour
{
    public AudioClip clickSound;
    [Range(0f, 1f)] public float volume = 1f;

    public void PlayClick()
    {
        if (clickSound == null)
        {
            Debug.LogError("Assign a click sound clip to ButtonClickSound.", this);
            return;
        }

        Camera mainCamera = Camera.main;
        Vector3 position = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clickSound, position, volume);
    }

    public void PlayClickAndLoadScene(string sceneName)
    {
        PlayClick();
        StartCoroutine(LoadSceneAfterClick(sceneName));
    }

    private IEnumerator LoadSceneAfterClick(string sceneName)
    {
        if (clickSound != null)
        {
            yield return new WaitForSecondsRealtime(clickSound.length);
        }

        SceneManager.LoadScene(sceneName);
    }
}
