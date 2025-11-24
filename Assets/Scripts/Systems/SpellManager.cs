using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections; // Ditambahkan untuk IEnumerator

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

    // --- BAGIAN BARU: SETTING DISSOLVE ---
    [Header("Dissolve Settings")]
    public Material dissolveMaterial; // Masukkan DissolveMAT di sini
    public string dissolvePropertyName = "_DissolveAmount"; // Sesuaikan nama property di Shader Graph (misal _Dissolve, _Cutoff, dll)
    public float dissolveDuration = 1.0f;
    private bool _isDissolving = false; // Untuk mencegah spam saat animasi berjalan
    // -------------------------------------

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "Lette", "Uwai" };
    private List<string> currentHand = new List<string>();
    private Dictionary<string, Sprite> spellSpriteCache = new Dictionary<string, Sprite>();

    [Header("Settings")]
    public bool requireVoiceMatch = true;

    private int maxHandSize = 3;

    private int _pendingCardIndex = -1;

    void Start()
    {
        RefillHand();
        UpdateSpellUI();
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
        foreach (var ui in uiToHide)
        {
            if (ui != null) ui.SetActive(false);
        }
        Time.timeScale = 0f;
        unlockPopup.SetActive(true);
    }

    public void OnCloseUnlockPopup()
    {
        unlockPopup.SetActive(false);
        foreach (var ui in uiToHide)
        {
            if (ui != null) ui.SetActive(true);
        }
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

            Image imageComponent = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = sprite;
                imageComponent.preserveAspect = true;
                // Pastikan material default (bukan dissolve) saat spawn awal
                imageComponent.material = null;

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
        if (_isDissolving) return; // Cegah klik saat sedang animasi

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

    // --- BAGIAN YANG DIMODIFIKASI ---
    void OnSpellCastSuccess(string spellName)
    {
        if (_pendingCardIndex >= 0 && _pendingCardIndex < currentHand.Count)
        {
            if (currentHand[_pendingCardIndex] == spellName)
            {
                // SEBELUMNYA: Langsung ganti kartu
                // currentHand[_pendingCardIndex] = GetRandomUnlockedSpell();
                // UpdateSpellUI();

                // SEKARANG: Jalankan animasi dulu, baru ganti kartu
                StartCoroutine(DissolveRoutine(spellName));
            }
            else
            {
                Debug.LogWarning($"Spell cast ({spellName}) does not match pending card ({currentHand[_pendingCardIndex]}) at index {_pendingCardIndex}. No card replaced.");
            }
        }
        else
        {
            Debug.LogError($"OnSpellCastSuccess called with no valid pending card index ({_pendingCardIndex}).");
        }

        // Catatan: _pendingCardIndex di-reset di akhir Coroutine sekarang
    }

    // --- FUNGSI BARU UNTUK DISSOLVE ---
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
                // 1. Buat Material Baru
                instanceMat = new Material(dissolveMaterial);

                // 2. Set Texture agar tidak putih
                if (cardImage.sprite != null)
                {
                    instanceMat.SetTexture("_MainTex", cardImage.sprite.texture);
                }

                // 3. Pasang Material ke Kartu
                cardImage.material = instanceMat;

                // 4. ANIMASI DARI 0 -> 1.1 (Sesuai Request)
                float timer = 0f;
                float startValue = 0f;   // Awal: 0 (Muncul)
                float endValue = 1.1f;   // Akhir: 1.1 (Hilang)

                // Pastikan set nilai awal dulu sebelum loop
                instanceMat.SetFloat(dissolvePropertyName, startValue);

                while (timer < dissolveDuration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / dissolveDuration;

                    // Bergerak pelan-pelan dari 0 ke 1.1
                    float currentValue = Mathf.Lerp(startValue, endValue, progress);

                    instanceMat.SetFloat(dissolvePropertyName, currentValue);

                    yield return null;
                }

                // Set nilai akhir (1.1)
                instanceMat.SetFloat(dissolvePropertyName, endValue);
            }
        }

        Debug.Log($"Replacing card at index {targetIndex} ({spellName}) with a new spell.");

        currentHand[targetIndex] = GetRandomUnlockedSpell();
        UpdateSpellUI();

        // Bersihkan Memori
        if (instanceMat != null)
        {
            Destroy(instanceMat);
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