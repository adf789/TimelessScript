using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ObjectPoolSupport : MonoBehaviour
{
    public int LoadedCount => loadedObjects != null ? loadedObjects.Count : 0;
    [SerializeField] private string guid;

    private List<GameObject> loadedObjects = null;
    private GameObject prefab = null;

    public async UniTask<GameObject> LoadAsync()
    {
        if (string.IsNullOrEmpty(guid))
            return null;

        if (LoadedCount == 0)
        {
            loadedObjects = new List<GameObject>();
        }
        else
        {
            foreach (var obj in loadedObjects)
            {
                if (!obj.activeSelf)
                    return obj;
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
        loadedObjects.Add(Instantiate(prefab, transform));

        loadedObjects[^1].transform.localPosition = Vector3.zero;

        loadedObjects[^1].SetActive(true);

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
