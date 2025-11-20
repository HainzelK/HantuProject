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

    [Header("UI to Hide During Popup")]
    public GameObject[] uiToHide;

    [Header("UI")]
    public GameObject spellPanel;
    public GameObject spellCardPrefab;
    public GameObject unlockPopup;
    public TextMeshProUGUI unlockText;

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "Spell 1", "Spell 2" };
    private List<string> currentHand = new List<string>(); // ✅ DECLARED HERE

    private int maxHandSize = 3; // ← changed to 3

    void Start()
    {
        RefillHand();
        UpdateSpellUI();
        HideUnlockPopup();
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
        unlockPopup.SetActive(true);
    }

    public void OnCloseUnlockPopup()
    {
        unlockPopup.SetActive(false);
        foreach (var ui in uiToHide)
        {
            if (ui != null) ui.SetActive(true);
        }
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
        if (unlockedSpells.Count == 0) return "Spell 1";
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
        Debug.Log($"Player selected: {spellName}");
        if (projectileShooter != null)
        {
            projectileShooter.TryShoot(spellName);
        }

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