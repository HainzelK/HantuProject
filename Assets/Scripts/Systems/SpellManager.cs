// SpellManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SpellManager : MonoBehaviour
{
    [Header("References")]
    public ProjectileShooter projectileShooter;
    public Transform playerCamera;
    public SpeechSpellcaster speechSpellcaster; // drag di inspector

    [Header("UI to Hide During Popup")]
    public GameObject[] uiToHide;

    [Header("UI")]
    public GameObject spellPanel;
    public GameObject spellCardPrefab;
    public GameObject unlockPopup;
    public TextMeshProUGUI unlockText;

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "Lette", "Uwae" };
    private List<string> currentHand = new List<string>(); // ✅ DECLARED HERE

    [Header("Settings")]
    public bool requireVoiceMatch = true;   // kalau true → harus ngomong, kalau false → langsung tembak


    private int maxHandSize = 3; // ← changed to 3
    private string _pendingSpellForReplacement = null; // spell yang nanti perlu diganti kartunya

    void Start()
    {
        RefillHand();
        UpdateSpellUI();
        HideUnlockPopup();

        // Subscribe ke event
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

        // Hide other UI
        foreach (var ui in uiToHide)
        {
            if (ui != null) ui.SetActive(false);
        }

        // ✅ PAUSE THE GAME
        Time.timeScale = 0f;
        unlockPopup.SetActive(true);
    }

    public void OnCloseUnlockPopup()
    {
        unlockPopup.SetActive(false);

        // Re-show other UI
        foreach (var ui in uiToHide)
        {
            if (ui != null) ui.SetActive(true);
        }

        // ✅ RESUME THE GAME
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

    void UpdateSpellUI()
    {
        foreach (Transform child in spellPanel.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (string spell in currentHand)
        {
            string spellName = spell;
            GameObject card = Instantiate(spellCardPrefab, spellPanel.transform);
            card.GetComponentInChildren<TextMeshProUGUI>().text = spellName;
            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => OnSpellClicked(spellName));
        }
    }

    void OnSpellClicked(string spellName)
    {
        if (requireVoiceMatch)
        {
            speechSpellcaster?.SetPendingSpell(spellName);
        }
        else
        {
            projectileShooter?.TryShoot(spellName);
            // Ganti kartu via event onSpellCast (lihat langkah opsional di bawah)
        }
    }

    void OnSpellCastSuccess(string spellName)
    {
        int index = currentHand.IndexOf(spellName);
        if (index >= 0)
        {
            currentHand[index] = GetRandomUnlockedSpell();
            UpdateSpellUI();
        }
    }

    void HideUnlockPopup()
    {
        if (unlockPopup != null)
            unlockPopup.SetActive(false);
    }
}