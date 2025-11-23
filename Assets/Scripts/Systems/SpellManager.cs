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
    public SpeechSpellcaster speechSpellcaster;

    [Header("UI to Hide During Popup")]
    public GameObject[] uiToHide;

    [Header("UI")]
    public GameObject spellPanel;
    public GameObject spellCardPrefab;
    public GameObject unlockPopup;
    public TextMeshProUGUI unlockText;

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "Lette", "Uwae" };
    private List<string> currentHand = new List<string>();

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

    void UpdateSpellUI()
    {
        foreach (Transform child in spellPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentHand.Count; i++)
        {
            int cardIndex = i;
            string spellName = currentHand[cardIndex];

            GameObject card = Instantiate(spellCardPrefab, spellPanel.transform);
            card.GetComponentInChildren<TextMeshProUGUI>().text = spellName;
            Button btn = card.GetComponent<Button>();

            btn.onClick.AddListener(() => OnSpellClicked(spellName, cardIndex));
        }
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
            projectileShooter?.TryShoot(spellName);
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