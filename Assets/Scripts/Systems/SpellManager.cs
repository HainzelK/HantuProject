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

    void Start()
    {
        RefillHand();
        UpdateSpellUI(); // Default tanpa animasi
        HideUnlockPopup();

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
        if (string.IsNullOrEmpty(spellName))
        {
            Debug.LogWarning("[SpellManager] Attempted to load sprite with null or empty spell name.");
            return null;
        }

        string path = $"Spells/{spellName}";
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            Debug.LogError($"[SpellManager] Found texture at '{path}' but NOT as Sprite! Please set import type to 'Sprite (2D and UI)' for '{spellName}.png'");
            return null;
        }

        Debug.LogError($"[SpellManager] Sprite not found for spell '{spellName}'. Expected path: 'Assets/Resources/{path}.png'");
        return null;
    }

    void UpdateSpellUI()
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

            // Pastikan ada CanvasGroup untuk kontrol Opacity
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg == null) cg = card.AddComponent<CanvasGroup>();

            // --- KUNCI PERBAIKAN JITTER ---
            // Jika ini adalah kartu yang akan dianimasikan, buat Invisible (Alpha 0) SEKARANG JUGA.
            if (i == indexToAnimate)
            {
                cg.alpha = 0f;
            }
            else
            {
                cg.alpha = 1f;
            }
            // ------------------------------

            Image imageComponent = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = sprite;
                imageComponent.preserveAspect = true;

                if (sprite != null)
                {
                    Debug.Log($"[SpellManager] Assigned sprite '{spellName}' to card at index {i}.");
                }
                else
                {
                    Debug.LogWarning($"[SpellManager] Assigned NULL sprite to card at index {i}.");
                }
            }

            TextMeshProUGUI textComponent = card.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            
            {
                textComponent.gameObject.SetActive(false);
            }

            Button btn = card.GetComponent<Button>();
            if (btn != null)
            {
                int cardIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSpellClicked(spellName, cardIndex));
            }
        }
    }

void OnSpellClicked(string spellName, int cardIndex)
{
    // 🔒 Prevent interaction during dissolve animation
    if (_isDissolving) return;

    // 🔥 HANDLE HEALING SPELL FIRST — BEFORE VOICE CHECK
    if (spellName == "sau")
    {
        Debug.Log("[SpellManager] 🟢 'sau' detected — triggering heal!");

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.Heal(healAmount);
            Debug.Log("[SpellManager] ✅ Heal applied via PlayerHealth.Heal()");
            
            // Replace card after heal with animation
            currentHand[cardIndex] = GetRandomUnlockedSpell();
            UpdateSpellUI(cardIndex); // Pass index to animate new card
            StartCoroutine(SlideUpRoutine(cardIndex));
            Debug.Log("[SpellManager] 🃏 'sau' card replaced with new spell");
        }
        else
        {
            Debug.LogError("[SpellManager] ❌ PlayerHealth.Instance is NULL — cannot heal!");
        }
        return;
    }
    
    // 🔥 ONLY FOR ATTACK SPELLS:
    _pendingCardIndex = cardIndex;

    if (requireVoiceMatch)
    {
        Debug.Log($"[SpellManager] 🎤 Voice mode: setting pending spell to '{spellName}'");
        speechSpellcaster?.SetPendingSpell(spellName);
    }
    else
    {
        Debug.Log($"[SpellManager] ⚡ Direct cast: shooting '{spellName}'");
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

        currentHand[targetIndex] = GetRandomUnlockedSpell();

        // MODIFIKASI 2: Panggil UpdateSpellUI dengan memberitahu index mana yang harus disembunyikan
        UpdateSpellUI(targetIndex);

        if (instanceMat != null) Destroy(instanceMat);

        StartCoroutine(SlideUpRoutine(targetIndex));
    }

    IEnumerator SlideUpRoutine(int cardIndex)
    {
        // Kita tetap butuh ini agar Layout Group selesai menghitung posisi X/Y yang benar
        // Tapi sekarang user tidak melihat prosesnya karena Alpha sudah 0 dari awal.
        yield return new WaitForEndOfFrame();

        if (cardIndex < spellPanel.transform.childCount)
        {
            Transform cardTransform = spellPanel.transform.GetChild(cardIndex);
            RectTransform rect = cardTransform.GetComponent<RectTransform>();
            CanvasGroup cg = cardTransform.GetComponent<CanvasGroup>();
            // cg pasti ada karena sudah ditambahkan di UpdateSpellUI

            if (rect != null && cg != null)
            {
                Vector2 targetPos = rect.anchoredPosition; // Posisi final (dihitung otomatis oleh Layout Group)
                Vector2 startPos = targetPos - new Vector2(0, slideUpDistance);

                // Set posisi awal
                rect.anchoredPosition = startPos;
                // Alpha sudah 0 dari UpdateSpellUI, jadi aman

                float timer = 0f;
                while (timer < slideUpDuration)
                {
                    timer += Time.deltaTime;
                    float rawT = Mathf.Clamp01(timer / slideUpDuration);
                    float smoothT = rawT * rawT * (3f - 2f * rawT);

                    rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                    cg.alpha = Mathf.Lerp(0f, 1f, rawT); // Fade In pelan-pelan

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