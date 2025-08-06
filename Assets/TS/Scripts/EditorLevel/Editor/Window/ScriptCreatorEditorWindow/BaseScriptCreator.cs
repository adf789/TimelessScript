
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
        EditorGUILayout.LabelField("�������� �ʴ� Ÿ���Դϴ�.");
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

        // ��� ���� ���丮 �����Ͽ� ����
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
            Debug.LogWarning("���� �̸��� ��� �ֽ��ϴ�.");
            return false;
        }

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("���� �̸��� ��� �ֽ��ϴ�.");
            return false;
        }

        string absolutePath = Path.Combine(Application.dataPath.Replace("Assets", ""), folderPath);

        if (!Directory.Exists(absolutePath))
        {
            Debug.LogWarning("���� ��ΰ� �ùٸ��� �ʽ��ϴ�.");
            return false;
        }

        string[] csFiles = Directory.GetFiles(absolutePath, extension, SearchOption.AllDirectories);

        foreach (string filePath in csFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (fileName == deleteFileName)
            {
                // ����
                File.Delete(filePath);
                string metaFile = filePath + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);

                Debug.Log($"���� '{deleteFileName}' ������: {filePath}");

                // �� ���� �ڵ� ����
                string fileFolder = Path.GetDirectoryName(filePath);
                DeleteIfEmptyFolder(fileFolder);

                AssetDatabase.Refresh();
                return true;
            }
        }

        Debug.LogWarning($"'{deleteFileName}' ������ �ش� ���� ������ ã�� �� �����ϴ�.");
        return false;
    }

    private void DeleteIfEmptyFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        // ������ ����, ���� ������ ������ ����
        bool isEmpty = Directory.GetFiles(folderPath).Length == 0 &&
                       Directory.GetDirectories(folderPath).Length == 0;

        if (isEmpty)
        {
            Directory.Delete(folderPath);
            string metaFile = folderPath + ".meta";
            if (File.Exists(metaFile))
                File.Delete(metaFile);

            Debug.Log($"�� ���� ������: {folderPath}");
        }
    }
}