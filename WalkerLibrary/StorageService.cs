﻿using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace WalkerLibrary
{
    public class StorageService
    {
        public void SaveSetting(string key, string value)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
        }

        public void DeleteSetting(string key)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Remove(key);
        }

        public string LoadSetting(string key)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var value = localSettings.Values[key];

            if (value == null)
                return null;

            return value.ToString();
        }

        public async Task<bool> PersistObjectAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key) || value == null)
                throw new ArgumentNullException();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            string json = JsonConvert.SerializeObject(value, Formatting.Indented);

            var file = await localFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, json);

            return true;
        }

        public async Task<T> RetrieveObjectAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException();

            var localFolder = ApplicationData.Current.LocalFolder;

            try
            {
                var file = await localFolder.GetFileAsync(key);
                string json = await FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception exp)
            {
                return default(T);
            }
        }

        public async Task<bool> DeleteObjectAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            try
            {
                StorageFile file = await localFolder.GetFileAsync(key);
                await file.DeleteAsync();
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }
    }
}