
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
    private List<bool> startAnimFolders = new List<bool>();
    private List<bool> endAnimFolders = new List<bool>();
    private int count = 0;
    private int playingSpriteSheetIndex = -1;
    private int playingAnimationType = 0; // 0: Main, 1: Start, 2: End, 3: Sequence
    private bool isPlayingSequence = false;
    private int sequencePhase = 0; // 0: Start, 1: Loop, 2: End
    private int sequenceLoopCount = 0;
    private const int MAX_SEQUENCE_LOOPS = 3; // 루프 애니메이션을 몇 번 반복할지

    private SerializedProperty defaultState = null;
    private SerializedProperty spriteRenderer = null;
    private SerializedProperty spriteImage = null;
    #endregion Value

    #region Function
    private void ResetValues()
    {
        dataFolders.Clear();
        defaultValues.Clear();
        startAnimFolders.Clear();
        endAnimFolders.Clear();

        bool isExistDefault = false;
        for (int i = 0; i < inspectorTarget.spriteSheets.Count; ++i)
        {
            dataFolders.Add(true);
            defaultValues.Add(inspectorTarget.spriteSheets[i].IsDefault);
            startAnimFolders.Add(false);
            endAnimFolders.Add(false);

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

        defaultState = serializedObject.FindProperty("defaultState");
        spriteRenderer = serializedObject.FindProperty("spriteRenderer");
        spriteImage = serializedObject.FindProperty("spriteImage");

        ResetValues();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private void UpdateAnimation(int spriteIndex, Sprite[] sprites, ref int frame, ref int index, int animationType = 0)
    {
        if (!IsPlayingTestAnimation)
        {
            return;
        }

        if (sprites == null)
                return;

        int frameDelay = 0;
        switch (animationType)
        {
            case 0: // Main Animation
                frameDelay = inspectorTarget.GetFrameDelay(spriteIndex, index);
                break;
            case 1: // Start Animation
                frameDelay = inspectorTarget.GetStartFrameDelay(spriteIndex, index);
                break;
            case 2: // End Animation
                frameDelay = inspectorTarget.GetEndFrameDelay(spriteIndex, index);
                break;
        }

        if (frame < frameDelay)
        {
            frame++;
            return;
        }
        else
        {
            frame = 0;
        }

        index++;

        if (index >= sprites.Length)
            index = 0;

        inspectorTarget.SetSprite(sprites[index]);
    }

    private async UniTask PlayAnimation(int index, int animationType = 0)
    {
        if (IsPlayingTestAnimation)
            StopAnimation();
        else
            inspectorTarget.LoadAnimations(true);

        playingSpriteSheetIndex = index;
        playingAnimationType = animationType;
        var spriteSheet = inspectorTarget.spriteSheets[index];

        var spriteResourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();
        Sprite[] sprites = null;
        string guid = "";

        // 애니메이션 타입에 따라 다른 스프라이트 로드
        switch (animationType)
        {
            case 0: // Main Animation
                sprites = spriteResourcesPath.LoadAll<Sprite>(spriteSheet.Guid);
                guid = spriteSheet.Guid;
                break;
            case 1: // Start Animation
                sprites = spriteResourcesPath.LoadAll<Sprite>(spriteSheet.StartAnimationGuid);
                guid = spriteSheet.StartAnimationGuid;
                break;
            case 2: // End Animation
                sprites = spriteResourcesPath.LoadAll<Sprite>(spriteSheet.EndAnimationGuid);
                guid = spriteSheet.EndAnimationGuid;
                break;
        }

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"No sprites found for animation type {animationType} with guid {guid}");
            StopAnimation();
            return;
        }

        int frame = 0;
        int animationIndex = 0;

        while (true)
        {
            UpdateAnimation(index, sprites, ref frame, ref animationIndex, animationType);

            await UniTask.Delay(13, cancellationToken: TokenPool.Get(GetHashCode()));
        }
    }

    private void StopAnimation()
    {
        playingSpriteSheetIndex = -1;
        playingAnimationType = 0;
        isPlayingSequence = false;
        sequencePhase = 0;
        sequenceLoopCount = 0;

        TokenPool.Cancel(GetHashCode());
    }

    private async UniTask PlaySequenceAnimation(int index)
    {
        if (IsPlayingTestAnimation)
            StopAnimation();
        else
            inspectorTarget.LoadAnimations(true);

        playingSpriteSheetIndex = index;
        playingAnimationType = 3; // Sequence
        isPlayingSequence = true;
        sequencePhase = 0;
        sequenceLoopCount = 0;

        var spriteSheet = inspectorTarget.spriteSheets[index];
        var spriteResourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();

        // 각 단계별 스프라이트 로드
        Sprite[] startSprites = null;
        Sprite[] mainSprites = null;
        Sprite[] endSprites = null;

        if (!string.IsNullOrEmpty(spriteSheet.StartAnimationGuid))
            startSprites = spriteResourcesPath.LoadAll<Sprite>(spriteSheet.StartAnimationGuid);

        if (!string.IsNullOrEmpty(spriteSheet.Guid))
            mainSprites = spriteResourcesPath.LoadAll<Sprite>(spriteSheet.Guid);

        if (!string.IsNullOrEmpty(spriteSheet.EndAnimationGuid))
            endSprites = spriteResourcesPath.LoadAll<Sprite>(spriteSheet.EndAnimationGuid);

        // 시퀀스 시작: Start → Loop → End
        try
        {
            // Phase 0: Start Animation
            if (startSprites != null && startSprites.Length > 0)
            {
                sequencePhase = 0;
                await PlayAnimationPhase(index, startSprites, 1); // Start animation type
            }

            // Phase 1: Loop Animation (여러 번 반복)
            if (mainSprites != null && mainSprites.Length > 0)
            {
                sequencePhase = 1;
                for (sequenceLoopCount = 0; sequenceLoopCount < MAX_SEQUENCE_LOOPS; sequenceLoopCount++)
                {
                    if (!isPlayingSequence) break;
                    await PlayAnimationPhase(index, mainSprites, 0); // Main animation type
                }
            }

            // Phase 2: End Animation
            if (endSprites != null && endSprites.Length > 0)
            {
                sequencePhase = 2;
                await PlayAnimationPhase(index, endSprites, 2); // End animation type
            }

            // 시퀀스 완료
            StopAnimation();
        }
        catch (System.OperationCanceledException)
        {
            // 정상적인 취소
        }
    }

    private async UniTask PlayAnimationPhase(int index, Sprite[] sprites, int animationType)
    {
        int frame = 0;
        int animationIndex = 0;

        while (animationIndex < sprites.Length && isPlayingSequence)
        {
            int frameDelay = 0;
            switch (animationType)
            {
                case 0: // Main Animation
                    frameDelay = inspectorTarget.GetFrameDelay(index, animationIndex);
                    break;
                case 1: // Start Animation
                    frameDelay = inspectorTarget.GetStartFrameDelay(index, animationIndex);
                    break;
                case 2: // End Animation
                    frameDelay = inspectorTarget.GetEndFrameDelay(index, animationIndex);
                    break;
            }

            if (frame < frameDelay)
            {
                frame++;
            }
            else
            {
                frame = 0;
                inspectorTarget.SetSprite(sprites[animationIndex]);
                animationIndex++;
            }

            await UniTask.Delay(13, cancellationToken: TokenPool.Get(GetHashCode()));
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            EditorGUILayout.PropertyField(defaultState);
            EditorGUILayout.PropertyField(spriteRenderer);
            EditorGUILayout.PropertyField(spriteImage);
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
                        startAnimFolders.RemoveAt(removeIndex);
                        endAnimFolders.RemoveAt(removeIndex);
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
                        startAnimFolders.Add(false);
                        endAnimFolders.Add(false);

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

                                GUILayout.BeginHorizontal();
                                {
                                    dataFolders[i] = EditorGUILayout.Foldout(dataFolders[i], $"Element {i} ({inspectorTarget.spriteSheets[i].State})");

                                    GUILayout.FlexibleSpace();

                                    // 삭제 버튼 (노드가 1개 이상일 때만 활성화)
                                    bool canDelete = inspectorTarget.spriteSheets.Count > 1;
                                    EditorGUI.BeginDisabledGroup(!canDelete);

                                    GUI.backgroundColor = canDelete ? Color.red : Color.gray;
                                    string deleteTooltip = canDelete ? "이 노드를 삭제합니다" : "최소 1개의 노드는 필요합니다";

                                    if (GUILayout.Button(new GUIContent("✖", deleteTooltip), GUILayout.Width(25), GUILayout.Height(18)))
                                    {
                                        string nodeName = inspectorTarget.spriteSheets[i].State.ToString();
                                        string message = $"'{nodeName}' 애니메이션 노드를 삭제하시겠습니까?\n\n" +
                                                       $"연결된 Start/End 애니메이션도 함께 제거됩니다.";

                                        if (EditorUtility.DisplayDialog("노드 삭제", message, "삭제", "취소"))
                                        {
                                            DeleteNode(i);
                                            return; // 삭제 후 즉시 리턴하여 인덱스 오류 방지
                                        }
                                    }
                                    GUI.backgroundColor = Color.white;
                                    EditorGUI.EndDisabledGroup();
                                }
                                GUILayout.EndHorizontal();

                                EditorGUI.indentLevel = 2;

                                if (dataFolders[i])
                                {
                                    EditorGUI.BeginDisabledGroup(IsPlayingTestAnimation);
                                    DrawKey(i);
                                    DrawToggleValue(i);
                                    DrawMain(i);
                                    DrawFrameDelay(i);
                                    DrawStartEndAnimations(i);
                                    EditorGUI.EndDisabledGroup();
                                    DrawPlayButtons(i);
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
                                startAnimFolders.Add(false);
                                endAnimFolders.Add(false);
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
                                startAnimFolders.RemoveAt(removeIndex);
                                endAnimFolders.RemoveAt(removeIndex);
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
            data.State = (AnimationState)EditorGUILayout.EnumPopup($"State", data.State);
        }
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();
    }

    private void DrawToggleValue(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUI.BeginChangeCheck();
        {
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

            GUILayout.BeginHorizontal();
            {
                data.IsPlayOnetime = EditorGUILayout.Toggle($"Play Onetime", data.IsPlayOnetime);
            }
            GUILayout.EndHorizontal();
        }
        if (EditorGUI.EndChangeCheck())
                SetReadyDirty();
    }

    private void DrawFrameDelay(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUILayout.Space(5);

        GUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField("⏱️ Main Animation Timing", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            {
                data.IsCustomDelay = EditorGUILayout.Toggle("Custom Frame Delays", data.IsCustomDelay);
            }
            GUILayout.EndHorizontal();

            if (data.IsCustomDelay)
            {
                DrawCustomFrameDelayArray(ref data.CustomFrameDelay, "Main Custom Delays");
            }
            else if (data.CustomFrameDelay != null && data.CustomFrameDelay.Length > 0)
            {
                data.CustomFrameDelay = new int[0];
            }

            GUILayout.BeginHorizontal();
            {
                int frameDelay = EditorGUILayout.IntField("Main Frame Delay", data.FrameDelay);
                if (frameDelay >= 0)
                    data.FrameDelay = frameDelay;
            }
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                SetReadyDirty();
        }
        GUILayout.EndVertical();
    }

    private void DrawMain(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUILayout.Space(5);

        GUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField("🔄 Main Loop Animation", EditorStyles.boldLabel);

            string guidToPath = AssetDatabase.GUIDToAssetPath(data.Guid);
            Sprite[] loadedSprites = null;

            if (!string.IsNullOrEmpty(guidToPath))
                loadedSprites = AssetDatabase.LoadAllAssetsAtPath(guidToPath).OfType<Sprite>().ToArray();

            if (loadedSprites != null && loadedSprites.Length > 0)
                data.SourceImage = loadedSprites[0];

            EditorGUI.BeginChangeCheck();
            {
                data.SourceImage = (Sprite) EditorGUILayout.ObjectField("Main Sprite Sheet", data.SourceImage, typeof(Sprite), false);

                if (data.SourceImage != null)
                {
                    EditorGUILayout.LabelField($"Sprites Count: {loadedSprites?.Length ?? 0}");
                }
            }
            if (EditorGUI.EndChangeCheck())
                SetReadyDirty();

            if (data.SourceImage == null)
            {
                data.Guid = string.Empty;
                data.SpriteCount = 0;
                EditorGUILayout.HelpBox("메인 루핑 애니메이션을 위한 스프라이트 시트를 선택해주세요.", MessageType.Info);
            }
            else
            {
                // GUID 삽입
                string assetPath = AssetDatabase.GetAssetPath(data.SourceImage);
                data.Guid = AssetDatabase.AssetPathToGUID(assetPath);
                data.SpriteCount = loadedSprites.Length;

                // 리소스 추가
                var spriteResoursPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();
                spriteResoursPath.AddResourceFromObject(data.SourceImage);
            }
        }
        GUILayout.EndVertical();
    }

    private void DrawStartEndAnimations(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUILayout.Space(10);

        GUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField("Start/End Animations", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Start Animation
            startAnimFolders[i] = EditorGUILayout.Foldout(startAnimFolders[i], "🎬 Start Animation (시작 단발성)", true);
            if (startAnimFolders[i])
            {
                EditorGUI.indentLevel++;
                DrawStartAnimation(i);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // End Animation
            endAnimFolders[i] = EditorGUILayout.Foldout(endAnimFolders[i], "🏁 End Animation (종료 단발성)", true);
            if (endAnimFolders[i])
            {
                EditorGUI.indentLevel++;
                DrawEndAnimation(i);
                EditorGUI.indentLevel--;
            }
        }
        GUILayout.EndVertical();
    }

    private void DrawStartAnimation(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        string guidToPath = AssetDatabase.GUIDToAssetPath(data.StartAnimationGuid);
        Sprite[] loadedSprites = null;
        Sprite sourceSprite = null;

        if (!string.IsNullOrEmpty(guidToPath))
        {
            loadedSprites = AssetDatabase.LoadAllAssetsAtPath(guidToPath).OfType<Sprite>().ToArray();
            if (loadedSprites != null && loadedSprites.Length > 0)
                sourceSprite = loadedSprites[0];
        }

        EditorGUI.BeginChangeCheck();
        {
            sourceSprite = (Sprite)EditorGUILayout.ObjectField("Start Sprite Sheet", sourceSprite, typeof(Sprite), false);

            if (sourceSprite != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(sourceSprite);
                data.StartAnimationGuid = AssetDatabase.AssetPathToGUID(assetPath);

                var spriteResoursPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();
                spriteResoursPath.AddResourceFromObject(sourceSprite);

                EditorGUILayout.LabelField($"Sprites Count: {loadedSprites?.Length ?? 0}");
            }
            else
            {
                data.StartAnimationGuid = string.Empty;
            }
        }
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();

        if (!string.IsNullOrEmpty(data.StartAnimationGuid))
            DrawStartFrameDelay(i);
    }

    private void DrawEndAnimation(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        string guidToPath = AssetDatabase.GUIDToAssetPath(data.EndAnimationGuid);
        Sprite[] loadedSprites = null;
        Sprite sourceSprite = null;

        if (!string.IsNullOrEmpty(guidToPath))
        {
            loadedSprites = AssetDatabase.LoadAllAssetsAtPath(guidToPath).OfType<Sprite>().ToArray();
            if (loadedSprites != null && loadedSprites.Length > 0)
                sourceSprite = loadedSprites[0];
        }

        EditorGUI.BeginChangeCheck();
        {
            sourceSprite = (Sprite)EditorGUILayout.ObjectField("End Sprite Sheet", sourceSprite, typeof(Sprite), false);

            if (sourceSprite != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(sourceSprite);
                data.EndAnimationGuid = AssetDatabase.AssetPathToGUID(assetPath);

                var spriteResoursPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();
                spriteResoursPath.AddResourceFromObject(sourceSprite);

                EditorGUILayout.LabelField($"Sprites Count: {loadedSprites?.Length ?? 0}");
            }
            else
            {
                data.EndAnimationGuid = string.Empty;
            }
        }
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();

        if (!string.IsNullOrEmpty(data.EndAnimationGuid))
            DrawEndFrameDelay(i);
    }

    private void DrawStartFrameDelay(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        {
            data.IsStartCustomDelay = EditorGUILayout.Toggle("Custom Delay", data.IsStartCustomDelay);
        }
        GUILayout.EndHorizontal();

        if (data.IsStartCustomDelay)
        {
            DrawCustomFrameDelayArray(ref data.StartCustomFrameDelay, "Start Custom Delays");
        }
        else if (data.StartCustomFrameDelay != null && data.StartCustomFrameDelay.Length > 0)
        {
            data.StartCustomFrameDelay = new int[0];
        }

        GUILayout.BeginHorizontal();
        {
            int frameDelay = EditorGUILayout.IntField("Start Frame Delay", data.StartFrameDelay);
            if (frameDelay >= 0)
                data.StartFrameDelay = frameDelay;
        }
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();
    }

    private void DrawEndFrameDelay(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();
        {
            data.IsEndCustomDelay = EditorGUILayout.Toggle("Custom Delay", data.IsEndCustomDelay);
        }
        GUILayout.EndHorizontal();

        if (data.IsEndCustomDelay)
        {
            DrawCustomFrameDelayArray(ref data.EndCustomFrameDelay, "End Custom Delays");
        }
        else if (data.EndCustomFrameDelay != null && data.EndCustomFrameDelay.Length > 0)
        {
            data.EndCustomFrameDelay = new int[0];
        }

        GUILayout.BeginHorizontal();
        {
            int frameDelay = EditorGUILayout.IntField("End Frame Delay", data.EndFrameDelay);
            if (frameDelay >= 0)
                data.EndFrameDelay = frameDelay;
        }
        GUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            SetReadyDirty();
    }

    private void DrawCustomFrameDelayArray(ref int[] customDelays, string label)
    {
        int count = customDelays == null ? 0 : customDelays.Length;

        for (int index = 0; index < count; index++)
        {
            if (index == 0)
                EditorGUILayout.PrefixLabel(label);

            customDelays[index] = EditorGUILayout.IntField($"Frame {index}", customDelays[index]);
        }

        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                int[] array = new int[count + 1];
                if (customDelays != null)
                    System.Array.Copy(customDelays, array, count);
                customDelays = array;
            }

            if (GUILayout.Button("-", GUILayout.Width(22)) && count > 0)
            {
                int[] array = new int[count - 1];
                System.Array.Copy(customDelays, array, array.Length);
                customDelays = array;
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawPlayButtons(int i)
    {
        SpriteSheetAnimationAuthoring.Node data = inspectorTarget.spriteSheets[i];

        EditorGUILayout.Space(5);

        GUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField("🎮 Animation Preview", EditorStyles.boldLabel);

            // 시퀀스 재생 버튼 (강조)
            bool hasSequence = !string.IsNullOrEmpty(data.Guid) &&
                               (!string.IsNullOrEmpty(data.StartAnimationGuid) || !string.IsNullOrEmpty(data.EndAnimationGuid));

            if (hasSequence)
            {
                GUILayout.BeginHorizontal();
                {
                    if (playingSpriteSheetIndex == i && playingAnimationType == 3)
                    {
                        // 현재 재생 중인 시퀀스 단계 표시
                        string phaseText = sequencePhase switch
                        {
                            0 => "🎬 Start",
                            1 => $"🔄 Loop ({sequenceLoopCount + 1}/{MAX_SEQUENCE_LOOPS})",
                            2 => "🏁 End",
                            _ => "🎞️ Sequence"
                        };

                        if (GUILayout.Button($"⏹ Stop {phaseText}", GUILayout.Height(30)))
                            StopAnimation();
                    }
                    else
                    {
                        // Sequence 재생 버튼 (전체 시퀀스)
                        GUI.backgroundColor = Color.cyan;
                        if (GUILayout.Button("🎞️ Play Full Sequence (Start→Loop→End)", GUILayout.Height(30)))
                            PlaySequenceAnimation(i).Forget();
                        GUI.backgroundColor = Color.white;
                    }
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
            }

            // 개별 애니메이션 재생 버튼들
            GUILayout.BeginHorizontal();
            {
                // Main Animation
                if (playingSpriteSheetIndex == i && playingAnimationType == 0)
                {
                    if (GUILayout.Button("⏹ Stop Main", GUILayout.Height(25)))
                        StopAnimation();
                }
                else
                {
                    if (GUILayout.Button("▶️ Play Main", GUILayout.Height(25)))
                        PlayAnimation(i, 0).Forget();
                }

                // Start Animation
                bool hasStartAnim = !string.IsNullOrEmpty(data.StartAnimationGuid);
                EditorGUI.BeginDisabledGroup(!hasStartAnim);
                if (playingSpriteSheetIndex == i && playingAnimationType == 1)
                {
                    if (GUILayout.Button("⏹ Stop Start", GUILayout.Height(25)))
                        StopAnimation();
                }
                else
                {
                    if (GUILayout.Button("🎬 Play Start", GUILayout.Height(25)))
                        PlayAnimation(i, 1).Forget();
                }
                EditorGUI.EndDisabledGroup();

                // End Animation
                bool hasEndAnim = !string.IsNullOrEmpty(data.EndAnimationGuid);
                EditorGUI.BeginDisabledGroup(!hasEndAnim);
                if (playingSpriteSheetIndex == i && playingAnimationType == 2)
                {
                    if (GUILayout.Button("⏹ Stop End", GUILayout.Height(25)))
                        StopAnimation();
                }
                else
                {
                    if (GUILayout.Button("🏁 Play End", GUILayout.Height(25)))
                        PlayAnimation(i, 2).Forget();
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();

            // 시퀀스 정보 표시
            if (hasSequence)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"Sequence: {(!string.IsNullOrEmpty(data.StartAnimationGuid) ? "Start" : "")} → Loop{(!string.IsNullOrEmpty(data.EndAnimationGuid) ? " → End" : "")}", EditorStyles.miniLabel);
            }
        }
        GUILayout.EndVertical();
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

    private void DeleteNode(int index)
    {
        if (index < 0 || index >= inspectorTarget.spriteSheets.Count)
            return;

        // 현재 재생 중인 애니메이션이 삭제될 노드라면 정지
        if (IsPlayingTestAnimation && playingSpriteSheetIndex == index)
        {
            StopAnimation();
        }

        // 삭제될 노드가 기본 노드인지 확인
        bool wasDefault = inspectorTarget.spriteSheets[index].IsDefault;

        // 노드와 관련 데이터 제거
        inspectorTarget.spriteSheets.RemoveAt(index);
        dataFolders.RemoveAt(index);
        defaultValues.RemoveAt(index);
        startAnimFolders.RemoveAt(index);
        endAnimFolders.RemoveAt(index);

        // count 업데이트
        count = inspectorTarget.spriteSheets.Count;

        // 재생 중인 애니메이션 인덱스 조정
        if (IsPlayingTestAnimation && playingSpriteSheetIndex > index)
        {
            playingSpriteSheetIndex--;
        }

        // 기본 노드가 삭제되었다면 새로운 기본 노드 설정
        if (wasDefault && inspectorTarget.spriteSheets.Count > 0)
        {
            defaultValues[0] = true;
            inspectorTarget.spriteSheets[0].IsDefault = true;
        }

        // 모든 노드가 삭제되었다면 기본값 초기화
        if (inspectorTarget.spriteSheets.Count == 0)
        {
            defaultValues.Clear();
            dataFolders.Clear();
            startAnimFolders.Clear();
            endAnimFolders.Clear();
        }

        // Dirty 플래그 설정
        SetReadyDirty();

        Debug.Log($"[SpriteSheetAnimationAuthoringInspector] 노드 {index} 삭제됨. 남은 노드 수: {inspectorTarget.spriteSheets.Count}");
    }

    #endregion Function
}