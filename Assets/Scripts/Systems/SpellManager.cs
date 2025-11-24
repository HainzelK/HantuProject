// SpellManager.cs
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

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "Lette", "Uwai" };
    private List<string> currentHand = new List<string>();
    private Dictionary<string, Sprite> spellSpriteCache = new Dictionary<string, Sprite>();

    [Header("Settings")]
    public bool requireVoiceMatch = true;

    private int maxHandSize = 3;

    private int _pendingCardIndex = -1;
    public float aksaraDisplayDuration = 1.0f; // durasi tampil sebelum shoot
    private GameObject _currentAksaraInstance = null;

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

        // Gunakan spellName langsung (opsional: normalisasi case)
        string path = $"Spells/{spellName}";

        // Coba load sebagai Sprite
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        // Debug: coba cek apakah file ada sebagai Texture2D (tapi seharusnya tidak perlu kalau import benar)
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
        // Bersihkan kartu lama
        foreach (Transform child in spellPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentHand.Count; i++)
        {
            string spellName = currentHand[i];
            Sprite sprite = GetSpellSprite(spellName); // helper yang sudah dibuat

            GameObject card = Instantiate(spellCardPrefab, spellPanel.transform);

            // Cari Image di prefab (bisa di root atau child)
            Image imageComponent = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = sprite;
                imageComponent.preserveAspect = true;

                // 🔍 LOG UNTUK KONFIRMASI
                if (sprite != null)
                {
                    Debug.Log($"[SpellManager] Assigned sprite '{spellName}' to card at index {i}.");
                }
                else
                {
                    Debug.LogWarning($"[SpellManager] Assigned NULL sprite to card at index {i}.");
                }
            }
            // Opsional: sembunyikan teks jika tidak dipakai
            TextMeshProUGUI textComponent = card.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.gameObject.SetActive(false); // atau hapus saja dari prefab
            }

            // Tambahkan listener klik
            Button btn = card.GetComponent<Button>();
            if (btn != null)
            {
                int cardIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSpellClicked(spellName, cardIndex));
            }
        }
    }

    IEnumerator ShowAksaraAndShoot(string spellName)
    {
        // Sembunyikan model lama jika ada
        if (_currentAksaraInstance != null)
        {
            Destroy(_currentAksaraInstance);
        }

        string cleanName = spellName.ToLower(); // "Uwai" → "uwai"
        string path = $"Aksara/aksara_{cleanName}";
        GameObject aksaraPrefab = Resources.Load<GameObject>(path);

        if (aksaraPrefab == null)
        {
            Debug.LogError($"[SpellManager] Aksara prefab not found at 'Resources/{path}'");
            // Tetap lanjutkan ke TryShoot meski model tidak ada
            projectileShooter?.TryShoot(spellName);
            yield break;
        }

        // Spawn di depan kamera
        float displayDistance = 1.5f; // meter di depan
        Vector3 spawnPos = playerCamera.position + playerCamera.forward * displayDistance;
        Quaternion spawnRot = playerCamera.rotation;

        _currentAksaraInstance = Instantiate(aksaraPrefab, spawnPos, spawnRot);

        // Opsional: tambahkan scaling animasi sederhana
        Transform model = _currentAksaraInstance.transform;
        model.localScale = Vector3.zero;

        // Simple pop-in animation (tanpa Animator)
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * 0.5f; // sesuaikan skala akhir
        while (elapsed < 0.3f)
        {
            model.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        model.localScale = targetScale;

        // Tunggu durasi tampil
        yield return new WaitForSeconds(aksaraDisplayDuration);

        // Tembak projectile
        projectileShooter?.TryShoot(spellName);

        // Hancurkan model
        if (_currentAksaraInstance != null)
        {
            Destroy(_currentAksaraInstance);
            _currentAksaraInstance = null;
        }
    }

    public void CastSpellWithAksara(string spellName)
    {
        StartCoroutine(ShowAksaraAndShoot(spellName));
    }

    void OnSpellClicked(string spellName, int cardIndex)
    {
        _pendingCardIndex = cardIndex;

        if (requireVoiceMatch)
        {
            speechSpellcaster?.SetPendingSpell(spellName);
        }
        else
        {
            StartCoroutine(ShowAksaraAndShoot(spellName));
        }
    }
    void OnSpellCastSuccess(string spellName)
    {
        if (_pendingCardIndex >= 0 && _pendingCardIndex < currentHand.Count)
        {
            if (currentHand[_pendingCardIndex] == spellName)
            {
                Debug.Log($"Replacing card at index {_pendingCardIndex} ({spellName}) with a new spell.");
                currentHand[_pendingCardIndex] = GetRandomUnlockedSpell();
                UpdateSpellUI();
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

        _pendingCardIndex = -1;
    }

    void HideUnlockPopup()
    {
        if (unlockPopup != null)
            unlockPopup.SetActive(false);
    }
}