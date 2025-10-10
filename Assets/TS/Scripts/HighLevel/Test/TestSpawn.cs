using Cysharp.Threading.Tasks;
using UnityEngine;

public class TestSpawn : MonoBehaviour
{
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private int spawnCount = 10;
    [SerializeField] private ObjectPoolSupport objectPool;

    private float passingTime = 0f;

    private void Update()
    {
        if (passingTime >= spawnDelay)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                objectPool.LoadAsync((obj) => obj.transform.localPosition = new Vector2(Random.Range(-1.5f, 1.5f), 0)).Forget();
            }
            passingTime = 0f;
        }
        else
        {
            passingTime += Time.deltaTime;
        }

        GameManager.Instance.AnalysisData.SetSpawnCount(objectPool.transform.childCount);
    }
}
