using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // BẮT BUỘC THÊM DÒNG NÀY ĐỂ DÙNG SLIDER

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;

    [Header("Audio Settings")]
    public Slider volumeSlider; // Biến để tham chiếu tới thanh Slider

    private void Start()
    {
        // Khi bắt đầu game, đặt thanh trượt khớp với âm lượng hiện tại
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("SampleScene"); 
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // HÀM MỚI: Xử lý khi kéo thanh trượt
    // Chú ý: Hàm này phải nhận vào 1 biến float
    public void SetGlobalVolume(float volume)
    {
        AudioListener.volume = volume; // Điều chỉnh âm lượng tổng (từ 0 đến 1)
    }
}