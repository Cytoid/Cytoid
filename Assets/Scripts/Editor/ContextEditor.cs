using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

[CustomEditor(typeof(Context))]
public class ContextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (Context.ScreenManager != null)
            {
                GUILayout.Label("Screen history:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (var intent in Context.ScreenManager.History)
                {
                    GUILayout.Label(intent.ScreenId);
                }
                GUILayout.Label("");
            }
            
            if (Context.AssetMemory != null)
            {
                GUILayout.Label("Asset memory usage:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (AssetTag tag in Enum.GetValues(typeof(AssetTag)))
                {
                    GUILayout.Label(
                        $"{tag}: {Context.AssetMemory.CountTagUsage(tag)}/{(Context.AssetMemory.GetTagLimit(tag) > 0 ? Context.AssetMemory.GetTagLimit(tag).ToString() : "∞")}");
                }
                GUILayout.Label("");
            }
            
            if (Context.BundleManager != null)
            {
                GUILayout.Label("Loaded bundles:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (var pair in Context.BundleManager.LoadedBundles)
                {
                    GUILayout.Label($"{pair.Key}: {pair.Value.RefCount}");
                }
                GUILayout.Label("");
            }

            if (GUILayout.Button("Unload unused assets"))
            {
                Resources.UnloadUnusedAssets();
            }

            if (GUILayout.Button("Toggle offline mode"))
            {
                Context.SetOffline(!Context.IsOffline());
            }

            if (GUILayout.Button("Make API work/not work"))
            {
                Context.MockApiUrl = Context.MockApiUrl == null ? "https://servicessss.cytoid.io" : null;
            }

            if (GUILayout.Button("Test reward overlay"))
            {
                RewardOverlay.Show(new List<OnlinePlayerStateChange.Reward>
                {
                    JsonConvert.DeserializeObject<OnlinePlayerStateChange.Reward>(@"{""type"":""character"",""value"":{""illustrator"":{""name"":""しがらき"",""url"":""https://www.pixiv.net/en/users/1004274""},""designer"":{""name"":"""",""url"":""""},""name"":""Mafumafu"",""description"":""何でも屋です。"",""_id"":""5e6f90dcdab3462655fb93a4"",""levelId"":4101,""asset"":""Mafu"",""tachieAsset"":""MafuTachie"",""id"":""5e6f90dcdab3462655fb93a4""}}"),
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "level",
                        onlineLevelValue = new Lazy<OnlineLevel>(() => MockData.OnlineLevel)
                    },
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "badge",
                        badgeValue = new Lazy<Badge>(() => JsonConvert.DeserializeObject<Badge>(@"{""_id"":""5f38e922fe1dfb383c7b93fa"",""uid"":""sora-1"",""listed"":false,""metadata"":{""imageUrl"":""http://artifacts.cytoid.io/badges/sora1.jpg""},""type"":""event"",""id"":""5f38e922fe1dfb383c7b93fa""}"))
                    },
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "badge",
                        badgeValue = new Lazy<Badge>(() => JsonConvert.DeserializeObject<Badge>(@"{""_id"":""5f390f2cfe1dfb383c7b93fb"",""uid"":""sora-2"",""listed"":false,""metadata"":{""imageUrl"":""http://artifacts.cytoid.io/badges/sora2.jpg"",""overrides"":[""sora-1""]},""type"":""event"",""id"":""5f390f2cfe1dfb383c7b93fb""}"))
                    },
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "badge",
                        badgeValue = new Lazy<Badge>(() => JsonConvert.DeserializeObject<Badge>(@"{""_id"":""5f390f57fe1dfb383c7b93fc"",""uid"":""sora-3"",""listed"":false,""metadata"":{""imageUrl"":""http://artifacts.cytoid.io/badges/sora3.jpg"",""overrides"":[""sora-1"",""sora-2""]},""type"":""event"",""id"":""5f390f57fe1dfb383c7b93fc""}"))
                    },
                });
            }
            
            if (GUILayout.Button("Update NavigationBackdrop Blur"))
            {
                NavigationBackdrop.Instance.UpdateBlur();
            }

            EditorUtility.SetDirty(target);
        }
    }
}
#endif