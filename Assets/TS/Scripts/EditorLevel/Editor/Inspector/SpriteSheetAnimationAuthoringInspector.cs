
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor.SceneManagement;
using System;

[CustomEditor(typeof(SpriteSheetAnimationAuthoring))]
public class SpriteSheetAnimationAuthoringInspector : Editor
{
    private SpriteSheetAnimationAuthoring inspectorTarget;

    #region Property
    public bool IsPlayingTestAnimation { get => playingSpriteSheetIndex != -1; }
    #endregion Property

    #region Value
    private bool folder = true;
    private bool isDirty = false;
    private List<bool> dataFolders = new List<bool>();
    private List<bool> defaultValues = new List<bool>();
    private int count = 0;
    private int playingSpriteSheetIndex = -1;

    private SerializedProperty state = null;
    #endregion Value

    #region Function
    private void ResetValues()
    {
        dataFolders.Clear();
        defaultValues.Clear();

        bool isExistDefault = false;
        for (int i = 0; i < inspectorTarget.spriteSheets.Count; ++i)
        {
            dataFolders.Add(true);
            defaultValues.Add(inspectorTarget.spriteSheets[i].isDefault);

            isExistDefault |= inspectorTarget.spriteSheets[i].isDefault;
        }

        // 첫 번째 인덱스가 기본
        if (!isExistDefault && defaultValues.Count > 0)
            defaultValues[0] = true;

        count = inspectorTarget.spriteSheets.Count;
    }

    private void OnEnable()
    {
        inspectorTarget = (SpriteSheetAnimationAuthoring) target;

        state = serializedObject.FindProperty("state");

        ResetValues();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private void UpdateAnimation(string key, int frame, ref int index)
    {
        if (!IsPlayingTestAnimation)
            return;

        inspectorTarget.OnUpdateAnimation(key, frame, ref index);
    }

    private async UniTask PlayAnimation(int index)
    {
        if (IsPlayingTestAnimation)
            StopAnimation();
        else
            inspectorTarget.LoadAnimations(true);

        playingSpriteSheetIndex = index;
        var spriteSheet = inspectorTarget.spriteSheets[index];

        inspectorTarget.Initialize();
        string key = spriteSheet.key;
        int frame = 0;
        int animationIndex = 0;

        while (true)
        {
            int prevIndex = animationIndex;
            UpdateAnimation(key, frame, ref animationIndex);

            if (prevIndex > animationIndex)
                frame = 0;
            else
                frame++;

            await UniTask.Delay(20, cancellationToken: TokenPool.Get(GetHashCode()));
        }
    }

    private void StopAnimation()
    {
        playingSpriteSheetIndex = -1;

        TokenPool.Cancel(GetHashCode());
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            EditorGUILayout.PropertyField(state);
        }
        if(EditorGUI.EndChangeCheck())
            SetReadyDirty();

        try
            {
                if (count < inspectorTarget.spriteSheets.Count)
                {
                    // 개수가 줄어든 것
                    while (count < inspectorTarget.spriteSheets.Count)
                    {
                        int removeIndex = inspectorTarget.spriteSheets.Count - 1;

                        dataFolders.RemoveAt(removeIndex);
                        defaultValues.RemoveAt(removeIndex);
                        inspectorTarget.spriteSheets.RemoveAt(removeIndex);

                        SetReadyDirty();
                    }
                }
                else if (count > inspectorTarget.spriteSheets.Count)
                {
                    // 개수가 늘어난 것
                    while (count > inspectorTarget.spriteSheets.Count)
                    {
                        inspectorTarget.spriteSheets.Add(new SpriteSheetAnimationAuthoring.Node());
                        dataFolders.Add(false);
                        defaultValues.Add(defaultValues.Count == 0);

                        SetReadyDirty();
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    folder = EditorGUILayout.Foldout(folder, "Sheet");
                    count = EditorGUILayout.IntField(inspectorTarget.spriteSheets.Count, GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                if (!folder)
                    return;

                EditorGUILayout.Space(6);

                GUILayout.BeginVertical("실제 리소스 정보는 ResourcesPathObject에 저장됩니다.\n커밋하실 때 같이 올려주세요.", "window");
                {
                    EditorGUILayout.Space(16);

                    for (int i = 0; i < inspectorTarget.spriteSheets.Count; ++i)
                    {
                        EditorGUI.indentLevel = 1;

                        GUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.Space(10, false);

                            GUILayout.BeginVertical(style: "window");
                            {
                                EditorGUILayout.Space(-20, false);

                                dataFolders[i] = EditorGUILayout.Foldout(dataFolders[i], "Element");

                                EditorGUI.indentLevel = 2;

                                if (dataFolders[i])
                                {
                                    EditorGUI.BeginDisabledGroup(IsPlayingTestAnimation);
                                    DrawKey(i);
                                    DrawDefaultValue(i);
                                    DrawFrameDelay(i);
                                    DrawMain(i);
                                    EditorGUI.EndDisabledGroup();
                                    DrawPlayButton(i);
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel = 1;

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("+", GUILayout.Width(22)))
                        {
                            if (inspectorTarget.spriteSheets.Count < int.MaxValue)
                            {
                                if (inspectorTarget.spriteSheets.Count == 0)
                                    inspectorTarget.spriteSheets.Add(new SpriteSheetAnimationAuthoring.Node());
                                else
                                    inspectorTarget.spriteSheets.Add(new SpriteSheetAnimationAuthoring.Node(inspectorTarget.spriteSheets[^1]));

                                count = inspectorTarget.spriteSheets.Count;
                                dataFolders.Add(true);
                                defaultValues.Add(defaultValues.Count == 0);
                            }
                        }

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            if (inspectorTarget.spriteSheets.Count > 0)
                            {
                                int removeIndex = inspectorTarget.spriteSheets.Count - 1;
                                count = removeIndex;
                                dataFolders.RemoveAt(removeIndex);
                                defaultValues.RemoveAt(removeIndex);
                                inspectorTarget.spriteSheets.RemoveAt(removeIndex);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    EditorGUI.indentLevel = 0;
                }
                GUILayout.EndVertical();

                if (!IsPlayingTestAnimation)
                {
                    if (isDirty)
                    {
                        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                        EditorUtility.SetDirty(target);

                        ResetDirty();
                    }

                    serializedObject.ApplyModifiedProperties();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
    }

    private void DrawKey(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        {
            data.key = EditorGUILayout.TextField($"Key", data.key);
        }
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();
    }

    private void DrawDefaultValue(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        {
            bool isDefault = EditorGUILayout.Toggle($"Default Animation", defaultValues[i]);

            if (defaultValues[i] != isDefault)
            {
                for (int j = 0; j < defaultValues.Count; j++)
                {
                    int enableIndex = isDefault ? i : 0;

                    defaultValues[j] = j == enableIndex;
                }
            }

            data.isDefault = defaultValues[i];
        }
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();
    }

    private void DrawFrameDelay(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        {
            data.isCustomDelay = EditorGUILayout.Toggle("Custom Delay 사용 유무", data.isCustomDelay);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            if (data.isCustomDelay)
            {
                GUILayout.BeginVertical();
                {
                    int count = data.customFrameDelay == null ? 0 : data.customFrameDelay.Length;

                    for (int index = 0; index < count; index++)
                    {
                        if (index == 0)
                            EditorGUILayout.PrefixLabel("Custom Delay");

                        data.customFrameDelay[index] = EditorGUILayout.IntField(data.customFrameDelay[index]);
                    }

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("+", GUILayout.Width(22)))
                        {
                            int[] array = new int[count + 1];

                            if (data.customFrameDelay != null)
                                System.Array.Copy(data.customFrameDelay, array, count);

                            data.customFrameDelay = array;
                        }


                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            if (count > 0)
                            {
                                int[] array = new int[count - 1];
                                System.Array.Copy(data.customFrameDelay, array, array.Length);
                                data.customFrameDelay = array;
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            else if (data.customFrameDelay != null && data.customFrameDelay.Length > 0)
                data.customFrameDelay = new int[0];
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            int frameDelay = EditorGUILayout.IntField("Frame Delay (기본 딜레이)", data.frameDelay);

            if (frameDelay >= 0)
                data.frameDelay = frameDelay;
        }
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();
    }

    private void DrawMain(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        string guidToPath = AssetDatabase.GUIDToAssetPath(data.guid);

        if (!string.IsNullOrEmpty(guidToPath))
            data.sourceImage = AssetDatabase.LoadAssetAtPath<Sprite>(guidToPath);

        EditorGUI.BeginChangeCheck();
        {
            data.sourceImage = (Sprite) EditorGUILayout.ObjectField("SourceImage", data.sourceImage, typeof(Sprite), false);
        }
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();

        if (data.sourceImage == null)
        {
            data.guid = string.Empty;
            return;
        }

        // GUID 삽입
        string assetPath = AssetDatabase.GetAssetPath(data.sourceImage);
        data.guid = AssetDatabase.AssetPathToGUID(assetPath);

        // 리소스 추가
        var spriteResoursPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();

        spriteResoursPath.AddResourceFromObject(data.sourceImage);
    }

    private void DrawPlayButton(int i)
    {
        if (playingSpriteSheetIndex == i)
        {
            if (GUILayout.Button("Stop", GUILayout.Width(150)))
                StopAnimation();
        }
        else
        {
            if (GUILayout.Button("Play Animation Test", GUILayout.Width(150)))
                PlayAnimation(i).Forget();
        }
    }

    private void SetReadyDirty()
    {
        isDirty = true;
    }

    private void ResetDirty()
    {
        isDirty = false;
    }
    #endregion Function
}