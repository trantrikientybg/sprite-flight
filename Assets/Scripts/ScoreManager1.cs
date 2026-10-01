using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;

public class ScoreManager1 : MonoBehaviour
{
    private const string SaveFileName = "highscore.dat";
    private const string EncryptionKeyMaterial = "SpriteFlight.HighScore.Encryption.v1";
    private const string AuthenticationKeyMaterial = "SpriteFlight.HighScore.Authentication.v1";

    [Header("UI TextMeshPro")]
    public TMP_Text score;
    public TMP_Text highScore;
    public TMP_Text currentScore;
    public GameObject gameOverPanel;

    private int savedHighScore;
    private string savePath;
    private bool gameOverProcessed;

    private void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        savedHighScore = LoadHighScore();
        UpdateHighScoreText();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (score == null)
        {
            Debug.LogWarning("ScoreManager1: Assign the GameOver score TMP text.", this);
        }

        if (highScore == null)
        {
            Debug.LogWarning("ScoreManager1: Assign the high score TMP text.", this);
        }

        if (currentScore == null)
        {
            Debug.LogWarning("ScoreManager1: Assign the current score TMP text.", this);
        }
    }

    public void GameOver(int finalScore)
    {
        if (gameOverProcessed)
        {
            return;
        }

        gameOverProcessed = true;
        int normalizedScore = Mathf.Max(0, finalScore);
        SetScoreText(normalizedScore.ToString(CultureInfo.InvariantCulture));

        if (normalizedScore > savedHighScore)
        {
            savedHighScore = normalizedScore;
            SaveHighScore(savedHighScore);
            UpdateHighScoreText();
        }

        if (currentScore != null)
        {
            currentScore.gameObject.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ScoreManager1: Assign the GameOver panel.", this);
        }
    }

    private void SetScoreText(string value)
    {
        if (score != null)
        {
            score.text = value;
        }
    }

    private void UpdateHighScoreText()
    {
        if (highScore != null)
        {
            highScore.text = savedHighScore.ToString(CultureInfo.InvariantCulture);
        }
    }

    private int LoadHighScore()
    {
        if (!File.Exists(savePath))
        {
            return 0;
        }

        try
        {
            byte[] fileData = File.ReadAllBytes(savePath);
            byte[] plainData = DecryptAndAuthenticate(fileData);
            string value = Encoding.UTF8.GetString(plainData);
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result)
                || result < 0)
            {
                throw new FormatException("The saved high score is invalid.");
            }

            return result;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is CryptographicException
            || exception is FormatException
            || exception is ArgumentException)
        {
            Debug.LogWarning("Could not load the high score; starting at 0. " + exception.Message, this);
            return 0;
        }
    }

    private void SaveHighScore(int value)
    {
        try
        {
            byte[] plainData = Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture));
            byte[] fileData = EncryptAndAuthenticate(plainData);
            File.WriteAllBytes(savePath, fileData);
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is CryptographicException
            || exception is ArgumentException)
        {
            Debug.LogError("Could not save the high score. " + exception.Message, this);
        }
    }

    private static byte[] EncryptAndAuthenticate(byte[] plainData)
    {
        byte[] iv = new byte[16];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(iv);
        }

        byte[] cipherText;
        using (Aes aes = Aes.Create())
        {
            aes.Key = DeriveKey(EncryptionKeyMaterial);
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                cipherText = encryptor.TransformFinalBlock(plainData, 0, plainData.Length);
            }
        }

        byte[] authenticatedData = Combine(iv, cipherText);
        byte[] signature;
        using (HMACSHA256 hmac = new HMACSHA256(DeriveKey(AuthenticationKeyMaterial)))
        {
            signature = hmac.ComputeHash(authenticatedData);
        }

        return Combine(authenticatedData, signature);
    }

    private static byte[] DecryptAndAuthenticate(byte[] fileData)
    {
        const int ivLength = 16;
        const int signatureLength = 32;
        if (fileData.Length <= ivLength + signatureLength)
        {
            throw new FormatException("The saved high score file is incomplete.");
        }

        int cipherLength = fileData.Length - ivLength - signatureLength;
        if (cipherLength % 16 != 0)
        {
            throw new FormatException("The saved high score file has an invalid length.");
        }

        byte[] authenticatedData = new byte[ivLength + cipherLength];
        Buffer.BlockCopy(fileData, 0, authenticatedData, 0, authenticatedData.Length);

        byte[] storedSignature = new byte[signatureLength];
        Buffer.BlockCopy(fileData, authenticatedData.Length, storedSignature, 0, signatureLength);

        byte[] expectedSignature;
        using (HMACSHA256 hmac = new HMACSHA256(DeriveKey(AuthenticationKeyMaterial)))
        {
            expectedSignature = hmac.ComputeHash(authenticatedData);
        }

        if (!SignaturesMatch(storedSignature, expectedSignature))
        {
            throw new CryptographicException("The saved high score failed its integrity check.");
        }

        byte[] iv = new byte[ivLength];
        Buffer.BlockCopy(fileData, 0, iv, 0, ivLength);

        byte[] cipherText = new byte[cipherLength];
        Buffer.BlockCopy(fileData, ivLength, cipherText, 0, cipherLength);

        using (Aes aes = Aes.Create())
        {
            aes.Key = DeriveKey(EncryptionKeyMaterial);
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            }
        }
    }

    private static byte[] DeriveKey(string keyMaterial)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial));
        }
    }

    private static bool SignaturesMatch(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int difference = 0;
        for (int i = 0; i < first.Length; i++)
        {
            difference |= first[i] ^ second[i];
        }

        return difference == 0;
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
