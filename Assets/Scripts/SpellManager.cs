// SpellManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SpellManager : MonoBehaviour
{
    [Header("References")]
    public ProjectileShooter projectileShooter; // your existing shooter
    public Transform playerCamera; // usually AR Camera

    [Header("UI to Hide During Popup")]
public GameObject[] uiToHide; // drag spell panel, wave text, kill text, etc.

    [Header("UI")]
    public GameObject spellPanel; // panel containing spell cards
    public GameObject spellCardPrefab; // UI button with image & text
    public GameObject unlockPopup; // shown when new spell unlocked
    public TextMeshProUGUI unlockText;

    [Header("Spells")]
    public List<string> unlockedSpells = new List<string> { "Spell 1", "Spell 2" };
    private List<string> currentHand = new List<string>();

    private int maxHandSize = 2;

    void Start()
    {
        // Start with 2 random spells from unlocked pool
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
    
    // Hide other UI
    foreach (var ui in uiToHide)
    {
        if (ui != null) ui.SetActive(false);
    }
    
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
        // Clear existing cards
        foreach (Transform child in spellPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Create new cards
 foreach (string spell in currentHand)
{
    // ✅ Create a local copy of 'spell' for the lambda
    string spellName = spell;
    GameObject card = Instantiate(spellCardPrefab, spellPanel.transform);
    card.GetComponentInChildren<TextMeshProUGUI>().text = spellName;
    Button btn = card.GetComponent<Button>();
    btn.onClick.AddListener(() => OnSpellClicked(spellName, card)); // ✅ NOW CORRECT
}
    }

void OnSpellClicked(string spellName, GameObject card)
{
    Debug.Log($"Player selected: {spellName}");

    // Fire projectile WITH spell name for debug
    if (projectileShooter != null)
    {
        projectileShooter.TryShoot(spellName); // 👈 pass name
    }

    // Replace card
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