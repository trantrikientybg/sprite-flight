using UnityEngine;

public class SceneChange : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
