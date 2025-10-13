using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ObjectPoolSupport : MonoBehaviour
{
    public int LoadedCount => loadedObjects != null ? loadedObjects.Count : 0;
    [SerializeField] private string guid;
    [SerializeField] private Transform parent;

    private List<GameObject> loadedObjects = null;
    private GameObject prefab = null;

    public async UniTask<GameObject> LoadAsync(System.Action<GameObject> onEventSpawn = null)
    {
        if (string.IsNullOrEmpty(guid))
            return null;

        if (LoadedCount == 0)
        {
            loadedObjects = new List<GameObject>();
        }
        else
        {
            for (int i = 0; i < loadedObjects.Count; i++)
            {
                var obj = loadedObjects[i];

                if (obj == null)
                {
                    loadedObjects.RemoveAt(i--);
                    continue;
                }

                if (!obj.activeSelf)
                {
                    obj.SetActive(true);

                    onEventSpawn?.Invoke(obj);

                    return obj;
                }
            }
        }

        // 프리팹을 가져옴
        await LoadPrefab();

        if (prefab == null)
        {
            Debug.LogError($"Not found prefab (GUID: {guid}");
            return null;
        }

        // 새 오브젝트 생성
        loadedObjects.Add(Instantiate(prefab, parent != null ? parent : transform));

        loadedObjects[^1].transform.localPosition = Vector3.zero;

        loadedObjects[^1].SetActive(true);

        onEventSpawn?.Invoke(loadedObjects[^1]);

        return loadedObjects[^1];
    }

    private async UniTask LoadPrefab()
    {
        if (prefab != null)
            return;

        prefab = await ResourcesTypeRegistry.Get().LoadAsync<GameObject>(guid);
    }

    [ContextMenu("Test")]
    public void Test()
    {
        LoadAsync().Forget();
    }
}
