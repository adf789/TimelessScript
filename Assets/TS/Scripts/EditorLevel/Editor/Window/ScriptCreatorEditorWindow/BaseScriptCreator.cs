
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class BaseScriptCreator : Editor
{

    public abstract void Create(string addPath, string assetName);
    public virtual void DrawCustomOptions() { }
    public virtual void OnAfterReload() { }
    
    // 경로 미리보기 관련 메서드들
    public virtual List<string> GetFinalPaths(string addPath, string assetName) { return new List<string>(); }
    public virtual void DrawPathPreview(string addPath, string assetName) { }

    public virtual void DrawScriptDeletor()
    {
        EditorGUILayout.HelpBox("지원하지 않는 타입입니다.", MessageType.Info);
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

        // 모든 경로 구분자를 표준화하여 처리
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
            Debug.LogWarning("파일 이름이 공백 상태입니다.");
            return false;
        }

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("폴더 이름이 공백 상태입니다.");
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

        Debug.LogWarning($"'{deleteFileName}' 파일을 해당 폴더 경로에서 찾을 수 없습니다.");
        return false;
    }

    private void DeleteIfEmptyFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        // 폴더가 비어있고, 다른 파일들이 없는지 확인
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
    
    // Ping 기능을 위한 유틸리티 메서드
    protected virtual void PingFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("경로가 비어있습니다.");
            return;
        }

        // Assets 폴더 기준으로 상대 경로 만들기
        string assetsRelativePath = folderPath;
        if (folderPath.StartsWith(Application.dataPath))
        {
            assetsRelativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);
        }
        else if (!folderPath.StartsWith("Assets/"))
        {
            // StringDefine.PATH_SCRIPT 등이 이미 Assets/로 시작하므로 중복 방지
            assetsRelativePath = folderPath.TrimStart('/');
        }

        // 경로를 표준화
        assetsRelativePath = assetsRelativePath.Replace("\\", "/");

        // 폴더가 존재하지 않으면 생성 여부 확인
        if (!Directory.Exists(assetsRelativePath) && !AssetDatabase.IsValidFolder(assetsRelativePath))
        {
            if (EditorUtility.DisplayDialog("폴더 생성", 
                $"폴더가 존재하지 않습니다.\n'{assetsRelativePath}'\n\n폴더를 생성하시겠습니까?", 
                "생성", "취소"))
            {
                // 폴더 생성
                string[] pathParts = assetsRelativePath.Split('/');
                string currentPath = pathParts[0]; // "Assets"
                
                for (int i = 1; i < pathParts.Length; i++)
                {
                    string newPath = currentPath + "/" + pathParts[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                    }
                    currentPath = newPath;
                }
                
                AssetDatabase.Refresh();
                Debug.Log($"폴더 생성 완료: {assetsRelativePath}");
            }
            else
            {
                Debug.Log("폴더 생성이 취소되었습니다.");
                return;
            }
        }

        // 폴더를 Project 창에서 하이라이트
        UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetsRelativePath);
        if (folderObj != null)
        {
            EditorGUIUtility.PingObject(folderObj);
            Selection.activeObject = folderObj;
            Debug.Log($"폴더로 이동: {assetsRelativePath}");
        }
        else
        {
            Debug.LogWarning($"폴더 객체를 찾을 수 없습니다: {assetsRelativePath}");
        }
    }
}