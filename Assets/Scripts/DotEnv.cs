using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public static class DotEnv
{
    private static readonly Dictionary<string, string> envVars = new Dictionary<string, string>();

    public static void Load()
    {
        // Path ke file .env di root direktori proyek
        string filePath = Path.Combine(Application.dataPath, "..", ".env");

        if (!File.Exists(filePath))
        {
            Debug.LogError(".env file not found. Please create it in the project root directory.");
            return;
        }

        envVars.Clear();

        foreach (var line in File.ReadAllLines(filePath))
        {
            // Abaikan komentar dan baris kosong
            if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
            {
                continue;
            }

            var parts = line.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                continue;
            }

            string key = parts[0].Trim();
            string value = parts[1].Trim();

            // Hapus tanda kutip jika ada
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }

            envVars[key] = value;
        }
    }

    public static string Get(string key, string defaultValue = null)
    {
        return envVars.TryGetValue(key, out var value) ? value : defaultValue;
    }
}