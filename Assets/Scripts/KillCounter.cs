using UnityEngine;
using TMPro;

public class KillCounter : MonoBehaviour
{
    public static KillCounter Instance;

    public TMP_Text killText;
    private int killCount = 0;

    void Awake()
    {
        Instance = this;
    }

    public void AddKill()
    {
        killCount++;
        killText.text = "Kills: " + killCount;
        Debug.Log("KILL COUNT UPDATED → " + killCount);
    }
}
