using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace EditorLevel.Processor
{
    /// <summary>
    /// Graphic 컴포넌트가 추가될 때 RaycastTarget을 자동으로 비활성화하는 프로세서
    /// </summary>
    public class GraphicRaycastTargetProcessor : AssetPostprocessor
    {
        /// <summary>
        /// 컴포넌트가 추가될 때 호출되는 이벤트 핸들러
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // 컴포넌트가 추가될 때마다 체크
            // ObjectFactory.componentWasAdded += OnComponentAdded;
        }

        /// <summary>
        /// 컴포넌트가 추가되었을 때 처리
        /// </summary>
        /// <param name="component">추가된 컴포넌트</param>
        private static void OnComponentAdded(Component component)
        {
            // Graphic 컴포넌트인지 확인 (Image, Text, RawImage 등)
            if (component is Graphic graphic)
            {
                // RaycastTarget을 false로 설정
                graphic.raycastTarget = false;

                // 변경사항을 에디터에 알림
                EditorUtility.SetDirty(graphic);

                Debug.Log($"[GraphicRaycastTargetProcessor] RaycastTarget disabled for {component.GetType().Name} on {graphic.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// 기존 Graphic 컴포넌트들의 RaycastTarget을 일괄 비활성화하는 메뉴 아이템
    /// </summary>
    public static class GraphicRaycastTargetMenu
    {
        [MenuItem("Tools/UI/Disable All RaycastTargets")]
        public static void DisableAllRaycastTargets()
        {
            var graphics = Object.FindObjectsOfType<Graphic>(true);
            int count = 0;

            foreach (var graphic in graphics)
            {
                if (graphic.raycastTarget)
                {
                    Undo.RecordObject(graphic, "Disable RaycastTarget");
                    graphic.raycastTarget = false;
                    EditorUtility.SetDirty(graphic);
                    count++;
                }
            }

            Debug.Log($"[GraphicRaycastTargetMenu] Disabled RaycastTarget on {count} Graphic components");
            EditorUtility.DisplayDialog("Complete", $"Disabled RaycastTarget on {count} Graphic components", "OK");
        }

        [MenuItem("Tools/UI/Disable Selected RaycastTargets")]
        public static void DisableSelectedRaycastTargets()
        {
            var selectedObjects = Selection.gameObjects;
            int count = 0;

            foreach (var obj in selectedObjects)
            {
                var graphics = obj.GetComponentsInChildren<Graphic>(true);
                foreach (var graphic in graphics)
                {
                    if (graphic.raycastTarget)
                    {
                        Undo.RecordObject(graphic, "Disable RaycastTarget");
                        graphic.raycastTarget = false;
                        EditorUtility.SetDirty(graphic);
                        count++;
                    }
                }
            }

            Debug.Log($"[GraphicRaycastTargetMenu] Disabled RaycastTarget on {count} selected Graphic components");
            EditorUtility.DisplayDialog("Complete", $"Disabled RaycastTarget on {count} selected Graphic components", "OK");
        }
    }
}