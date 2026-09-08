using UnityEngine;

namespace CodeBase.Infrastructure.AssetManagement
{
    public class AssetProvider : IAssetProvider
    {

        public GameObject LoadAsset(string assetPath)
        {
            GameObject asset = Resources.Load<GameObject>(assetPath);
            if (asset != null)
                Debug.Log($"[AssetProvider] Loaded {assetPath}");
            else
                Debug.LogError($"[AssetProvider] Failed to load {assetPath}");
            return asset;
        }

        public GameObject SpawnAsset(GameObject asset)
        {
            Debug.Log($"[AssetProvider] Spawning {asset.name}");
            GameObject spawnAsset = Object.Instantiate(asset);
            return spawnAsset;
        }

        public GameObject SpawnAsset(GameObject asset, Vector3 position, Quaternion rotation)
        {
            Debug.Log($"[AssetProvider] Spawning {asset.name} with position {position} and rotation {rotation}");
            GameObject spawnAsset = Object.Instantiate(asset,  position, rotation);
            return spawnAsset;
        }

    }
}