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

    // [BARU] Pengaturan Warna Seleksi
    [Header("Card Selection Visuals")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Lebih gelap (abu-abu)

    [Header("Dissolve Settings")]
    public Material dissolveMaterial;
    public string dissolvePropertyName = "_DissolveAmount";
    public float dissolveDuration = 1.0f;

    [Header("Spawn Settings")]
    public float slideUpDistance = 100f;
    public float slideUpDuration = 0.5f;

    [Header("Aksara Animation Settings")]
    public float aksaraDisplayDuration = 1.0f;
    public float aksaraFadeDuration = 0.5f;

    private GameObject _currentAksaraInstance = null;
    private bool _isDissolving = false;
    private bool _isAnimatingAksara = false;

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "lette", "uwai", "sau" };
    private List<string> currentHand = new List<string>();

    [Header("Settings")]
    public bool requireVoiceMatch = true;
    public float healAmount = 30f;

    private int maxHandSize = 3;
    private int _pendingCardIndex = -1;

    // [BARU] Melacak kartu mana yang sedang aktif dipilih secara visual
    private int _selectedCardIndex = -1;

    private EdgeFlash edgeFlash;


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
        return Resources.Load<Sprite>(path);
    }

    void UpdateSpellUI(int indexToAnimate = -1)
    {
        // Reset selection index karena UI di-build ulang
        _selectedCardIndex = -1;

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
                imageComponent.color = normalColor; // Set warna awal normal
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

    // --- BAGIAN UTAMA (MODIFIED) ---

    void OnSpellClicked(string spellName, int cardIndex)
    {
        if (_isDissolving || _isAnimatingAksara) return;

        // [BARU] Logika Swap Selection
        // 1. Jika ada kartu lain yang sebelumnya dipilih, kembalikan warnanya ke normal
        if (_selectedCardIndex != -1 && _selectedCardIndex != cardIndex)
        {
            SetCardColor(_selectedCardIndex, normalColor);
        }

        // 2. Set kartu yang baru diklik menjadi gelap (Selected)
        _selectedCardIndex = cardIndex;
        SetCardColor(cardIndex, selectedColor);

        // Simpan index kartu yang sedang diproses
        _pendingCardIndex = cardIndex;

        if (requireVoiceMatch)
        {
            speechSpellcaster?.SetPendingSpell(spellName);
        }
        else
        {
            CastSpellWithAksara(spellName);
        }
    }

    // [BARU] Helper untuk mengubah warna kartu berdasarkan index
    void SetCardColor(int index, Color color)
    {
        if (index >= 0 && index < spellPanel.transform.childCount)
        {
            Transform card = spellPanel.transform.GetChild(index);
            Image img = card.GetComponent<Image>() ?? card.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.color = color;
            }
        }
    }

    // [BARU] Fungsi Public untuk mereset seleksi (Dipanggil SpeechSpellcaster saat gagal)
    public void ResetSelection()
    {
        if (_selectedCardIndex != -1)
        {
            SetCardColor(_selectedCardIndex, normalColor);
            _selectedCardIndex = -1;
            _pendingCardIndex = -1;
        }
    }

    public void CastSpellWithAksara(string spellName)
    {
        // [BARU] Jika berhasil cast, kita reset visual seleksi agar tidak stuck gelap
        // (Opsional: biarkan gelap sampai dissolve, tapi lebih aman di-reset atau biarkan logic dissolve menanganinya)
        // Di sini saya biarkan logic dissolve yang akan me-refresh UI.

        StartCoroutine(ShowAksaraAndCast(spellName));
    }

    IEnumerator ShowAksaraAndCast(string spellName)
    {
        _isAnimatingAksara = true;
        ExecuteSpellAction(spellName);

        if (_currentAksaraInstance != null) Destroy(_currentAksaraInstance);

        string cleanName = spellName.ToLower();
        string path = $"Aksara/aksara_{cleanName}";
        GameObject aksaraPrefab = Resources.Load<GameObject>(path);

        if (aksaraPrefab != null && playerCamera != null)
        {
            float displayDistance = 1.5f;
            Vector3 spawnPos = playerCamera.position + playerCamera.forward * displayDistance;
            Quaternion spawnRot = playerCamera.rotation * Quaternion.Euler(0, 180f, 0);

            _currentAksaraInstance = Instantiate(aksaraPrefab, spawnPos, spawnRot);
            _currentAksaraInstance.transform.SetParent(playerCamera);
            Transform model = _currentAksaraInstance.transform;

            model.localScale = Vector3.zero;
            SetAksaraAlpha(_currentAksaraInstance, 0f);

            float elapsed = 0f;
            Vector3 targetScale = Vector3.one * 0.5f;

            while (elapsed < aksaraFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / aksaraFadeDuration;
                float scaleCurve = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.1f;
                if (t >= 1f) scaleCurve = 1f;
                model.localScale = Vector3.Lerp(Vector3.zero, targetScale, scaleCurve);
                SetAksaraAlpha(_currentAksaraInstance, Mathf.Lerp(0f, 1f, t));
                yield return null;
            }
            model.localScale = targetScale;
            SetAksaraAlpha(_currentAksaraInstance, 1f);
        }

        yield return new WaitForSeconds(aksaraDisplayDuration);

        if (_currentAksaraInstance != null)
        {
            float fadeOutElapsed = 0f;
            float startAlpha = 1f;
            Vector3 startScale = _currentAksaraInstance.transform.localScale;
            Vector3 endScale = startScale * 1.2f;

            while (fadeOutElapsed < aksaraFadeDuration)
            {
                fadeOutElapsed += Time.deltaTime;
                float t = fadeOutElapsed / aksaraFadeDuration;
                SetAksaraAlpha(_currentAksaraInstance, Mathf.Lerp(startAlpha, 0f, t));
                _currentAksaraInstance.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
        }

        if (_currentAksaraInstance != null)
        {
            Destroy(_currentAksaraInstance);
            _currentAksaraInstance = null;
        }

        _isAnimatingAksara = false;
    }

    void SetAksaraAlpha(GameObject obj, float alpha)
    {
        if (obj == null) return;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    c.a = alpha;
                    m.SetColor("_BaseColor", c);
                }
                else if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
            }
        }
    }

    void ExecuteSpellAction(string spellName)
    {
        if (spellName == "sau")
        {
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.Heal(healAmount);
                edgeFlash?.Trigger(Color.green, 0.4f);
                StartCoroutine(DissolveRoutine(spellName));
            }
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

                // Pastikan warna putih saat dissolve agar shader terlihat benar
                cardImage.color = Color.white;

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
        UpdateSpellUI(targetIndex); // Ini akan me-reset _selectedCardIndex ke -1

        if (instanceMat != null) Destroy(instanceMat);

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