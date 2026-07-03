using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string profilePath => Path.Combine(Application.persistentDataPath, "profile.json");
    private static string runPath => Path.Combine(Application.persistentDataPath, "run_temp.json");

    public static void saveProfile(ProfileData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(profilePath, json);
    }

    public static void saveRun(RunData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(runPath, json);
    }

    public static ProfileData loadProfile()
    {
        if (File.Exists(profilePath))
        {
            string json = File.ReadAllText(profilePath);
            return JsonUtility.FromJson<ProfileData>(json);
        }

        return new ProfileData();
    }

    public static RunData loadRun()
    {
        if (File.Exists(runPath))
        {
            string json = File.ReadAllText(runPath);
            return JsonUtility.FromJson<RunData>(json);
        }
        return null;
    }

    public static void deleteRun()
    {
        File.Delete(runPath);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Project Echoes/Reset Current Run Data")]
    public static void ResetRunDataEditor()
    {
        if (File.Exists(runPath))
        {
            File.Delete(runPath);
            Debug.Log("[SaveManager] Run data deleted successfully!");
        }
        else
        {
            Debug.Log("[SaveManager] No run data found to delete.");
        }
    }

    [UnityEditor.MenuItem("Project Echoes/Reset ALL Save Data (Profile + Run)")]
    public static void ResetAllSaveDataEditor()
    {
        if (File.Exists(runPath)) File.Delete(runPath);
        if (File.Exists(profilePath)) File.Delete(profilePath);
        Debug.Log("[SaveManager] All save data has been completely reset!");
    }
#endif
}