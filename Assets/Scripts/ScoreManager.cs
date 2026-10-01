using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Hiển thị")]
    public TextMeshProUGUI scoreText;

    [Header("Cài đặt điểm")]
    public float scorePerSecond = 10f;

    private float currentScore = 0f;
    private bool isGameActive = true;

    void Update()
    {
        if (isGameActive)
        {
            currentScore += scorePerSecond * Time.deltaTime;

            if (scoreText != null)
            {
                scoreText.text = "Score: " + Mathf.FloorToInt(currentScore).ToString();
            }
        }
    }

    public void GameOver()
    {
        if (!isGameActive)
        {
            return;
        }

        isGameActive = false;
        MusicController musicController = FindAnyObjectByType<MusicController>();
        if (musicController != null)
        {
            musicController.StopMusic();
        }

        ScoreManager1 scoreManager1 = FindAnyObjectByType<ScoreManager1>();
        if (scoreManager1 != null)
        {
            scoreManager1.GameOver(Mathf.FloorToInt(currentScore));
        }
        else
        {
            Debug.LogWarning("No ScoreManager1 was found to update and save the high score.", this);
        }
    }
}