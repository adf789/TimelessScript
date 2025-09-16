
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor.SceneManagement;
using System;
using System.Linq;

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
            defaultValues.Add(inspectorTarget.spriteSheets[i].IsDefault);

            isExistDefault |= inspectorTarget.spriteSheets[i].IsDefault;
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
        string key = spriteSheet.Key;
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

            await UniTask.Delay(40, cancellationToken: TokenPool.Get(GetHashCode()));
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
                Debug.LogError(ex.StackTrace);
            }
    }

    private void DrawKey(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        {
            data.Key = EditorGUILayout.TextField($"Key", data.Key);
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

            data.IsDefault = defaultValues[i];
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
            data.IsCustomDelay = EditorGUILayout.Toggle("Custom Delay 사용 유무", data.IsCustomDelay);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            if (data.IsCustomDelay)
            {
                GUILayout.BeginVertical();
                {
                    int count = data.CustomFrameDelay == null ? 0 : data.CustomFrameDelay.Length;

                    for (int index = 0; index < count; index++)
                    {
                        if (index == 0)
                            EditorGUILayout.PrefixLabel("Custom Delay");

                        data.CustomFrameDelay[index] = EditorGUILayout.IntField(data.CustomFrameDelay[index]);
                    }

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("+", GUILayout.Width(22)))
                        {
                            int[] array = new int[count + 1];

                            if (data.CustomFrameDelay != null)
                                System.Array.Copy(data.CustomFrameDelay, array, count);

                            data.CustomFrameDelay = array;
                        }


                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            if (count > 0)
                            {
                                int[] array = new int[count - 1];
                                System.Array.Copy(data.CustomFrameDelay, array, array.Length);
                                data.CustomFrameDelay = array;
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            else if (data.CustomFrameDelay != null && data.CustomFrameDelay.Length > 0)
                data.CustomFrameDelay = new int[0];
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            int frameDelay = EditorGUILayout.IntField("Frame Delay (기본 딜레이)", data.FrameDelay);

            if (frameDelay >= 0)
                data.FrameDelay = frameDelay;
        }
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();
    }

    private void DrawMain(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        string guidToPath = AssetDatabase.GUIDToAssetPath(data.Guid);
        Sprite[] loadedSprites = null;

        if (!string.IsNullOrEmpty(guidToPath))
            loadedSprites = AssetDatabase.LoadAllAssetsAtPath(guidToPath).OfType<Sprite>().ToArray();

        if (loadedSprites != null && loadedSprites.Length > 0)
            data.SourceImage = loadedSprites[0];

        EditorGUI.BeginChangeCheck();
        {
            data.SourceImage = (Sprite) EditorGUILayout.ObjectField("SourceImage", data.SourceImage, typeof(Sprite), false);
        }
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();

        if (data.SourceImage == null)
        {
            data.Guid = string.Empty;
            data.SpriteCount = 0;
            return;
        }

        // GUID 삽입
        string assetPath = AssetDatabase.GetAssetPath(data.SourceImage);
        data.Guid = AssetDatabase.AssetPathToGUID(assetPath);
        data.SpriteCount = loadedSprites.Length;

        // 리소스 추가
        var spriteResoursPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();

        spriteResoursPath.AddResourceFromObject(data.SourceImage);
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