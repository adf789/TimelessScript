using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class RewardEffectSupport : MonoBehaviour
{
    [SerializeField] private TextMeshPro countText;

    public void Show(int count)
    {
        countText.SetText(count.ToString());

        gameObject.SetActive(true);
    }
}
