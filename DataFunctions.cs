using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

public class DataFunctions
{
    static readonly string encryptionKey = "11NNES78RENE0".PadRight(32);

    public static GameData GetGameDataFromFile(string path, bool decrypt = true)
    {
        // Read the file
        string json = File.ReadAllText(path);

        // Decrypt the file (It is encrypted when saved)
        if (decrypt)
        {
            json = DecryptData(json);
        }

        // Convert to json
        return JsonUtility.FromJson<GameData>(json);
    }

    public static string CheckLastSave()
    {
        // Save Files Path
        string dataPath = Application.persistentDataPath + "/Saves/";

        // If Save Files Path is valid
        if (!Directory.Exists(dataPath))
        {
            return "";
        }

        // Get all the files in an array
        string[] files = Directory.GetFiles(dataPath);

        if (files.Length == 0)
        {
            return "";
        }

        // Get the last played save file
        string recentlyUpdatedSaveFile = files.OrderByDescending(f => File.GetLastWriteTime(f)).ToList()[0];

        // Get the file path
        string filePath = dataPath + Path.GetFileName(recentlyUpdatedSaveFile);

        GameData data = DataFunctions.GetGameDataFromFile(filePath);

        if (data.VERSION != Application.version)
        {
            return "";
        }

        SaveLoadManager loader;

        if (SaveLoadManager.instance == null)
        {
            GameObject loaderObject = new() { name = "SaveLoadManager" };
            SaveLoadManager loaderHandler = loaderObject.AddComponent<SaveLoadManager>();
            loader = loaderHandler;
        }

        loader = SaveLoadManager.instance;

        loader.SAVE_FILE_PATH = filePath;

        return data.COMPANY_NAME;
    }

    public static string EncryptData(string data)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
            aesAlg.IV = new byte[16]; // Initialization Vector

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(data);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    public static void CreateEncryptedDataFile()
    {
        // This function is dev-only! Creates an encrypted data from a json.

        // Get the raw data path
        string path = Application.streamingAssetsPath + "/data_raw.json";

        // Get the GameData from the file
        GameData data = GetGameDataFromFile(path, false);

        // Convert to string to encrypt
        string stringData = JsonUtility.ToJson(data, true);

        // Encrypt the string data
        string encrypedStringData = EncryptData(stringData);

        // Save the data
        File.WriteAllText(Application.streamingAssetsPath + "/data.json", encrypedStringData);
    }

    public static string DecryptData(string encryptedData)
    {
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
        aesAlg.IV = new byte[16]; // Initialization Vector

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msDecrypt = new(Convert.FromBase64String(encryptedData));
        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}