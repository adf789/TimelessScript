using Cysharp.Threading.Tasks;
using UnityEngine;

public class TownScene : MonoBehaviour
{
    [SerializeField] private Transform _townSubScene;

    void Start()
    {
        TilemapStreamingManager.Instance.SetMapParent(_townSubScene);
    }
}
