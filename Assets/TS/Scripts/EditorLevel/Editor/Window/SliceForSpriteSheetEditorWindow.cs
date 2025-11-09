#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SliceForSpriteSheetWindow : EditorWindow
{
    [MenuItem("TS/Resources Manage/Slice Sprite", false, 0)]
    public static void ShowWindow()
    {
        SliceForSpriteSheetWindow window = (SliceForSpriteSheetWindow) GetWindow(typeof(SliceForSpriteSheetWindow));
        window.titleContent.text = "SliceForSpriteSheetWindow";
    }

    private int count = 1;
    private Texture2D[] textures = null;
    private int[] columns = null;
    private int[] rows = null;

    private void OnGUI()
    {
        count = EditorGUILayout.IntField("Texture Count", count);

        EditorGUILayout.Space(20);

        for (int i = 0; i < count; i++)
        {
            SetArrayCount(ref textures, count);
            SetArrayCount(ref columns, count);
            SetArrayCount(ref rows, count);

            textures[i] = (Texture2D) EditorGUILayout.ObjectField("Texture", textures[i], typeof(Texture2D), false);
            columns[i] = EditorGUILayout.IntField("Columns", columns[i]);
            rows[i] = EditorGUILayout.IntField("Rows", rows[i]);

            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Process Texture"))
        {
            for (int i = 0; i < count; i++)
            {
                ProcessTexture(textures[i], columns[i], rows[i]);
            }
        }
    }

    private void SetArrayCount<T>(ref T[] array, int count)
    {
        if (array == null)
        {
            array = new T[count];
            return;
        }

        if (array.Length != count)
        {
            T[] newArray = new T[count];

            int minCount = Mathf.Min(array.Length, count);

            System.Array.Copy(array, newArray, minCount);

            array = newArray;
        }
    }

    private void ProcessTexture(Texture2D texture, int column, int row)
    {
        if (texture == null)
        {
            Debug.LogError("No texture selected!");
            return;
        }

        column = Mathf.Max(column, 1);
        row = Mathf.Max(row, 1);

        string assetPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError("Failed to get TextureImporter!");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;

        TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
        platformSettings.format = TextureImporterFormat.Automatic;
        importer.SetPlatformTextureSettings(platformSettings);

        int width = texture.width / column;
        int height = texture.height / row;
        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();

        for (int y = 0; y < row; y++)
        {
            for (int x = 0; x < column; x++)
            {
                SpriteMetaData metaData = new SpriteMetaData
                {
                    name = texture.name + "_part" + (y * column + x),
                    rect = new Rect(x * width, texture.height - (y + 1) * height, width, height),
                    alignment = (int) SpriteAlignment.Center
                };
                metaDataList.Add(metaData);
            }
        }

        SerializedObject serializedObject = new SerializedObject(importer);
        SerializedProperty spriteSheetProperty = serializedObject.FindProperty("m_SpriteSheet.m_Sprites");

        spriteSheetProperty.arraySize = metaDataList.Count;
        for (int i = 0; i < metaDataList.Count; i++)
        {
            SerializedProperty element = spriteSheetProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("m_Rect").rectValue = metaDataList[i].rect;
            element.FindPropertyRelative("m_Name").stringValue = metaDataList[i].name;
            element.FindPropertyRelative("m_Alignment").intValue = metaDataList[i].alignment;
        }

        serializedObject.ApplyModifiedProperties();

        importer.SaveAndReimport();

        Debug.Log("Texture processed and sliced successfully!");
    }
}

public static class TextureImporterExtensions
{
    public static void SetSerializedObjectSprites(this TextureImporter importer, List<SpriteMetaData> spriteMetaData)
    {
        SerializedObject serializedObject = new SerializedObject(importer);
        SerializedProperty spriteSheetProperty = serializedObject.FindProperty("m_SpriteSheet.m_Sprites");

        spriteSheetProperty.arraySize = spriteMetaData.Count;
        for (int i = 0; i < spriteMetaData.Count; i++)
        {
            SerializedProperty element = spriteSheetProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("m_Rect").rectValue = spriteMetaData[i].rect;
            element.FindPropertyRelative("m_Name").stringValue = spriteMetaData[i].name;
            element.FindPropertyRelative("m_Alignment").intValue = spriteMetaData[i].alignment;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif