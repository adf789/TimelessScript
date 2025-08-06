
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public abstract class BaseScriptCreator : Editor
{

    public abstract void Create(string addPath, string assetName);
    public virtual void DrawCustomOptions() { }
    public virtual void OnAfterReload() { }

    public virtual void DrawScriptDeletor()
    {
        EditorGUILayout.LabelField("지원하지 않는 타입입니다.");
    }

    protected virtual void CreateScript(string path, string fileName, string content)
    {
        if (string.IsNullOrEmpty(path))
            return;

        string filePath = Path.Combine(path, $"{fileName.Replace("/", "")}.cs").Replace("\\", "/");
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, content);
        }
    }

    protected virtual void CreateDirectoryIfNotExist(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        // 모든 하위 디렉토리 포함하여 생성
        string normalizedPath = path.Replace("\\", "/");
        if (!Directory.Exists(normalizedPath))
        {
            Directory.CreateDirectory(normalizedPath);
        }
    }

    protected virtual bool DeleteFileInFolder(string deleteFileName, string extension, string folderPath)
    {
        if (string.IsNullOrEmpty(deleteFileName))
        {
            Debug.LogWarning("파일 이름이 비어 있습니다.");
            return false;
        }

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("폴더 이름이 비어 있습니다.");
            return false;
        }

        string absolutePath = Path.Combine(Application.dataPath.Replace("Assets", ""), folderPath);

        if (!Directory.Exists(absolutePath))
        {
            Debug.LogWarning("폴더 경로가 올바르지 않습니다.");
            return false;
        }

        string[] csFiles = Directory.GetFiles(absolutePath, extension, SearchOption.AllDirectories);

        foreach (string filePath in csFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (fileName == deleteFileName)
            {
                // 삭제
                File.Delete(filePath);
                string metaFile = filePath + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);

                Debug.Log($"파일 '{deleteFileName}' 삭제됨: {filePath}");

                // 빈 폴더 자동 삭제
                string fileFolder = Path.GetDirectoryName(filePath);
                DeleteIfEmptyFolder(fileFolder);

                AssetDatabase.Refresh();
                return true;
            }
        }

        Debug.LogWarning($"'{deleteFileName}' 파일을 해당 폴더 내에서 찾을 수 없습니다.");
        return false;
    }

    private void DeleteIfEmptyFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        // 파일이 없고, 서브 폴더도 없으면 삭제
        bool isEmpty = Directory.GetFiles(folderPath).Length == 0 &&
                       Directory.GetDirectories(folderPath).Length == 0;

        if (isEmpty)
        {
            Directory.Delete(folderPath);
            string metaFile = folderPath + ".meta";
            if (File.Exists(metaFile))
                File.Delete(metaFile);

            Debug.Log($"빈 폴더 삭제됨: {folderPath}");
        }
    }
}