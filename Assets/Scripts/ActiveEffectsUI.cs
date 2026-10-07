using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class ActiveEffectsUI : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField, Min(0f)] private float edgePadding = 28f;
    [SerializeField, Min(200f)] private float panelWidth = 380f;
    [SerializeField, Min(0f)] private float panelOpacity = 0.62f;
    [SerializeField, Min(1f)] private float fontSize = 26f;
    [SerializeField, Min(1f)] private float rowHeight = 34f;

    private readonly List<PlayerControl.TimedEffectStatus> activeEffects =
        new List<PlayerControl.TimedEffectStatus>(4);
    private readonly StringBuilder textBuilder = new StringBuilder(160);
    private PlayerControl player;
    private RectTransform panelRect;
    private Image panelImage;
    private TextMeshProUGUI effectsText;

    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (fontAsset == null)
        {
            Debug.LogError("Assign the Science Gothic SDF font asset to ActiveEffectsUI.", this);
            enabled = false;
            return;
        }

        CreatePanel(canvas.transform);
        player = FindAnyObjectByType<PlayerControl>();
    }

    void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerControl>();
        }

        if (player != null)
        {
            player.GetActiveTimedEffects(activeEffects);
        }
        panelRect.gameObject.SetActive(true);
        panelRect.sizeDelta = new Vector2(
            panelWidth,
            28f + rowHeight * (activeEffects.Count + 2));
        UpdateEffectsText();
    }

    void CreatePanel(Transform canvasTransform)
    {
        GameObject panelObject = new GameObject(
            "Active Effects Panel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.layer = gameObject.layer;
        panelObject.transform.SetParent(canvasTransform, false);

        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.anchoredPosition = new Vector2(-edgePadding, -edgePadding);
        panelRect.sizeDelta = new Vector2(panelWidth, 28f + rowHeight);

        panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.025f, 0.04f, 0.08f, panelOpacity);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Effect Descriptions",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(panelRect, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 10f);
        textRect.offsetMax = new Vector2(-18f, -10f);

        effectsText = textObject.GetComponent<TextMeshProUGUI>();
        effectsText.font = fontAsset;
        effectsText.fontSize = fontSize;
        effectsText.enableAutoSizing = true;
        effectsText.fontSizeMin = fontSize * 0.75f;
        effectsText.fontSizeMax = fontSize;
        effectsText.color = Color.white;
        effectsText.alignment = TextAlignmentOptions.TopLeft;
        effectsText.textWrappingMode = TextWrappingModes.NoWrap;
        effectsText.raycastTarget = false;
    }

    void UpdateEffectsText()
    {
        textBuilder.Clear();
        textBuilder.Append("<b>ACTIVE EFFECTS</b>");
        if (activeEffects.Count == 0)
        {
            textBuilder.Append("\nNo active effects");
        }

        for (int i = 0; i < activeEffects.Count; i++)
        {
            PlayerControl.TimedEffectStatus effect = activeEffects[i];
            textBuilder.Append('\n');
            textBuilder.Append(effect.displayName);
            textBuilder.Append("  ");
            textBuilder.Append(Mathf.Max(0f, effect.secondsRemaining).ToString("F1"));
            textBuilder.Append('s');
        }

        effectsText.text = textBuilder.ToString();
    }
}
