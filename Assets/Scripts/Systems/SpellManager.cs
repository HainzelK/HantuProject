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
    public List<string> unlockedSpells = new List<string> { "lette", "uwai", "sau" };
    private List<string> currentHand = new List<string>();
    private Dictionary<string, Sprite> spellSpriteCache = new Dictionary<string, Sprite>();

    [Header("Settings")]
    public bool requireVoiceMatch = true;
    public float healAmount = 30f; // Configurable heal amount

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
    Debug.Log($"[SpellManager] Card clicked: '{spellName}' at index {cardIndex}");

    // 🔥 HANDLE HEALING SPELL FIRST — BEFORE VOICE CHECK
    if (spellName == "sau")
    {
        Debug.Log("[SpellManager] 🟢 'Sau' detected — triggering heal!");

        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.Heal(healAmount);
            Debug.Log("[SpellManager] ✅ Heal applied via PlayerHealth.Heal()");
            
            // Replace card after heal
            currentHand[cardIndex] = GetRandomUnlockedSpell();
            UpdateSpellUI();
            Debug.Log("[SpellManager] 🃏 'Sau' card replaced with new spell");
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