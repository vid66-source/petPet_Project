using CodeBase.Infrastructure.Services;
using UnityEngine;

namespace CodeBase.Infrastructure.AssetManagement
{
    public interface IAssetProvider : IService
    {
        public GameObject LoadAsset(string assetPath);
        public GameObject SpawnAsset(GameObject asset);
        public GameObject SpawnAsset(GameObject asset, Vector3 position, Quaternion rotation);
    }
}