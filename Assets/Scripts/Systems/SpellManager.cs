using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class SpellManager : MonoBehaviour
{
    [Header("References")]
    public ProjectileShooter projectileShooter;
    public Transform playerCamera;
    public SpeechSpellcaster speechSpellcaster;

    [Header("UI to Hide During Popup")]
    public GameObject[] uiToHide;

    [Header("UI")]
    public GameObject spellPanel;
    public GameObject spellCardPrefab;
    public GameObject unlockPopup;
    public TextMeshProUGUI unlockText;

    [Header("Dissolve Settings")]
    public Material dissolveMaterial;
    public string dissolvePropertyName = "_DissolveAmount";
    public float dissolveDuration = 1.0f;

    [Header("Spawn Settings")]
    [Tooltip("Jarak kartu turun ke bawah sebelum naik")]
    public float slideUpDistance = 100f;
    [Tooltip("Durasi animasi kartu naik dan fade in")]
    public float slideUpDuration = 0.5f;

    private bool _isDissolving = false;

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "lette", "uwai", "sau" };
    private List<string> currentHand = new List<string>();

    [Header("Settings")]
    public bool requireVoiceMatch = true;
    public float healAmount = 30f; // Configurable heal amount

    private int maxHandSize = 3;
    private int _pendingCardIndex = -1;
    private EdgeFlash edgeFlash;

    [Header("Spell Weights")]
    public List<SpellWeight> spellWeights = new List<SpellWeight>
    {
        new SpellWeight { spellName = "lette", weight = 3 },
        new SpellWeight { spellName = "uwai", weight = 3 },
        new SpellWeight { spellName = "sau", weight = 1 } // Less frequent!
    };

    [System.Serializable]
    public class SpellWeight
    {
        public string spellName;
        public int weight = 1; // Higher = more frequent
    }


    void Start()
    {
        RefillHand();
        UpdateSpellUI();
        HideUnlockPopup();
        edgeFlash = FindObjectOfType<EdgeFlash>();

        if (projectileShooter != null)
            projectileShooter.onSpellCast += OnSpellCastSuccess;
    }

    public void UnlockSpell(string spellName)
    {
        if (!unlockedSpells.Contains(spellName))
        {
            unlockedSpells.Add(spellName);
            ShowUnlockPopup(spellName);
        }
    }

    void ShowUnlockPopup(string spellName)
    {
        unlockText.text = $"New Spell Unlocked!\n{spellName}";
        foreach (var ui in uiToHide) if (ui != null) ui.SetActive(false);
        Time.timeScale = 0f;
        unlockPopup.SetActive(true);
    }

    public void OnCloseUnlockPopup()
    {
        unlockPopup.SetActive(false);
        foreach (var ui in uiToHide) if (ui != null) ui.SetActive(true);
        Time.timeScale = 1f;
    }

    void RefillHand()
    {
        currentHand.Clear();
        for (int i = 0; i < maxHandSize; i++)
        {
            currentHand.Add(GetRandomUnlockedSpell());
        }
    }

    string GetRandomUnlockedSpell()
    {
        if (unlockedSpells.Count == 0) return "Lette";
        return unlockedSpells[Random.Range(0, unlockedSpells.Count)];
    }

    Sprite GetSpellSprite(string spellName)
    {
        if (string.IsNullOrEmpty(spellName)) return null;

        string path = $"Spells/{spellName}";
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        return null;
    }

    void UpdateSpellUI(int indexToAnimate = -1)
    {
        foreach (Transform child in spellPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentHand.Count; i++)
        {
            string spellName = currentHand[i];
            Sprite sprite = GetSpellSprite(spellName);

            GameObject card = Instantiate(spellCardPrefab, spellPanel.transform);

            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg == null) cg = card.AddComponent<CanvasGroup>();

            // Logic Anti-Jitter: Sembunyikan kartu baru yang akan dianimasikan
            if (i == indexToAnimate)
                cg.alpha = 0f;
            else
                cg.alpha = 1f;

            Image imageComponent = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = sprite;
                imageComponent.preserveAspect = true;
                imageComponent.material = null;
            }

            TextMeshProUGUI textComponent = card.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null) textComponent.gameObject.SetActive(false);

            Button btn = card.GetComponent<Button>();
            if (btn != null)
            {
                int cardIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSpellClicked(spellName, cardIndex));
            }
        }
    }

    // --- BAGIAN YANG DIPERBAIKI ---
    void OnSpellClicked(string spellName, int cardIndex)
    {
        if (_isDissolving) return;

        // 🔥 LOGIKA HEAL ("sau")
        if (spellName == "sau")
        {
            Debug.Log("[SpellManager] 🟢 'sau' detected — triggering heal!");

            if (PlayerHealth.Instance != null)
            {
                // 1. Eksekusi efek Heal
                PlayerHealth.Instance.Heal(healAmount);
                edgeFlash?.Trigger(Color.green, 0.4f);
                Debug.Log("[SpellManager] ✅ Heal applied");

                // 2. Set Pending Index (PENTING: Agar DissolveRoutine tahu kartu mana yg dihapus)
                _pendingCardIndex = cardIndex;

                // 3. Jalankan Dissolve (ini akan handle animasi dissolve -> ganti kartu -> slide up)
                StartCoroutine(DissolveRoutine(spellName));
            }
            else
            {
                Debug.LogError("[SpellManager] ❌ PlayerHealth.Instance is NULL — cannot heal!");
            }
            return;
        }

        // 🔥 LOGIKA ATTACK
        _pendingCardIndex = cardIndex;

        if (requireVoiceMatch)
        {
            speechSpellcaster?.SetPendingSpell(spellName);
        }
        else
        {
            projectileShooter?.TryShoot(spellName);
        }
    }

    void OnSpellCastSuccess(string spellName)
    {
        if (_pendingCardIndex >= 0 && _pendingCardIndex < currentHand.Count)
        {
            if (currentHand[_pendingCardIndex] == spellName)
            {
                StartCoroutine(DissolveRoutine(spellName));
            }
            else
            {
                Debug.LogWarning($"Spell cast mismatch.");
            }
        }
    }

    // Coroutine ini sekarang digunakan oleh "sau" (langsung) maupun Attack (via callback)
    IEnumerator DissolveRoutine(string spellName)
    {
        _isDissolving = true;
        int targetIndex = _pendingCardIndex;
        Material instanceMat = null;

        if (targetIndex < spellPanel.transform.childCount)
        {
            Transform cardTransform = spellPanel.transform.GetChild(targetIndex);
            Image cardImage = cardTransform.GetComponent<Image>() ?? cardTransform.GetComponentInChildren<Image>();

            if (cardImage != null && dissolveMaterial != null)
            {
                instanceMat = new Material(dissolveMaterial);
                if (cardImage.sprite != null) instanceMat.SetTexture("_MainTex", cardImage.sprite.texture);
                cardImage.material = instanceMat;

                float timer = 0f;
                // Animasi Dissolve (Opacity Material 0 -> 1.1)
                while (timer < dissolveDuration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / dissolveDuration;
                    instanceMat.SetFloat(dissolvePropertyName, Mathf.Lerp(0f, 1.1f, progress));
                    yield return null;
                }
                instanceMat.SetFloat(dissolvePropertyName, 1.1f);
            }
        }

        // Ganti data kartu di tangan
        currentHand[targetIndex] = GetRandomUnlockedSpell();

        // Refresh UI (Kartu target dibuat invisible dulu lewat index parameter)
        UpdateSpellUI(targetIndex);

        if (instanceMat != null) Destroy(instanceMat);

        // Animasi muncul kembali
        StartCoroutine(SlideUpRoutine(targetIndex));
    }

    IEnumerator SlideUpRoutine(int cardIndex)
    {
        yield return new WaitForEndOfFrame();

        if (cardIndex < spellPanel.transform.childCount)
        {
            Transform cardTransform = spellPanel.transform.GetChild(cardIndex);
            RectTransform rect = cardTransform.GetComponent<RectTransform>();
            CanvasGroup cg = cardTransform.GetComponent<CanvasGroup>();

            if (rect != null && cg != null)
            {
                Vector2 targetPos = rect.anchoredPosition;
                Vector2 startPos = targetPos - new Vector2(0, slideUpDistance);

                rect.anchoredPosition = startPos;
                // Alpha sudah 0 dari UpdateSpellUI

                float timer = 0f;
                while (timer < slideUpDuration)
                {
                    timer += Time.deltaTime;
                    float rawT = Mathf.Clamp01(timer / slideUpDuration);
                    float smoothT = rawT * rawT * (3f - 2f * rawT);

                    rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                    cg.alpha = Mathf.Lerp(0f, 1f, rawT);

                    yield return null;
                }

                rect.anchoredPosition = targetPos;
                cg.alpha = 1f;
            }
        }

        _pendingCardIndex = -1;
        _isDissolving = false;
    }

    void HideUnlockPopup()
    {
        if (unlockPopup != null)
            unlockPopup.SetActive(false);
    }
}