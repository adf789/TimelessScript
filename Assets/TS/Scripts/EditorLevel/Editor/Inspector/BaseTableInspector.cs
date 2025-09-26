using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

[CustomEditor(typeof(BaseTable), true)]
public class BaseTableInspector : Editor
{
    private BaseTable baseTable;
    private Type dataType;
    private MethodInfo getDataCountMethod;
    private MethodInfo getAllDatasMethod;
    private MethodInfo getNextAutoIDMethod;
    private MethodInfo getIDRangeInfoMethod;
    private MethodInfo isInIDRangeMethod;

    // UI 상태
    private bool mainFoldout = true;
    private List<bool> itemFoldouts = new List<bool>();
    private bool isDirty = false;

    // 검색 및 필터
    private string searchText = "";
    private bool showFilters = false;

    // 검증 결과
    private HashSet<uint> duplicateIDs = new HashSet<uint>();
    private Dictionary<int, string> validationErrors = new Dictionary<int, string>();

    // 정렬
    private SortMode sortMode = SortMode.Index;
    private bool sortAscending = true;

    // 타입별 필터링 (런타임에서 enum 타입 감지)
    private object filterEnumValue = null;
    private Type enumType = null;

    private enum SortMode
    {
        Index,
        ID,
        Name,
        Type
    }

    private void OnEnable()
    {
        baseTable = (BaseTable) target;

        // 제네릭 타입 정보 추출
        Type baseType = baseTable.GetType();
        while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(BaseTable<>)))
        {
            baseType = baseType.BaseType;
        }

        if (baseType != null)
        {
            dataType = baseType.GetGenericArguments()[0];
            getDataCountMethod = baseType.GetMethod("GetDataCount");
            getAllDatasMethod = baseType.GetMethod("GetAllDatas");
            getNextAutoIDMethod = baseType.GetMethod("GetNextAutoID");
            getIDRangeInfoMethod = baseType.GetMethod("GetIDRangeInfo");
            isInIDRangeMethod = baseType.GetMethod("IsInIDRange");

            // Type 필드가 있는지 확인하고 enum 타입 감지
            DetectEnumTypeField();
        }

        RefreshItemFoldouts();
        ValidateAllItems();
    }

    private void DetectEnumTypeField()
    {
        if (dataType == null) return;

        // "Type", "type", "*Type", "*type" 패턴으로 enum 필드 찾기
        var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.FieldType.IsEnum &&
                (field.Name.ToLower().Contains("type") || field.Name.ToLower().EndsWith("type")))
            {
                enumType = field.FieldType;
                filterEnumValue = Enum.GetValues(enumType).GetValue(0); // 첫 번째 enum 값으로 초기화
                break;
            }
        }
    }

    private void RefreshItemFoldouts()
    {
        int itemCount = GetItemCount();

        while (itemFoldouts.Count < itemCount)
            itemFoldouts.Add(false);

        while (itemFoldouts.Count > itemCount)
            itemFoldouts.RemoveAt(itemFoldouts.Count - 1);
    }

    private int GetItemCount()
    {
        if (getDataCountMethod != null)
            return (int) getDataCountMethod.Invoke(baseTable, null);
        return 0;
    }

    private System.Collections.IList GetAllItems()
    {
        if (getAllDatasMethod != null)
            return (System.Collections.IList) getAllDatasMethod.Invoke(baseTable, null);
        return null;
    }

    private uint GetNextAutoID()
    {
        if (getNextAutoIDMethod != null)
            return (uint) getNextAutoIDMethod.Invoke(baseTable, null);
        return 0;
    }

    private string GetIDRangeInfo()
    {
        if (getIDRangeInfoMethod != null)
            return (string) getIDRangeInfoMethod.Invoke(baseTable, null);
        return "No Range Set";
    }

    private bool IsInIDRange(uint id)
    {
        if (isInIDRangeMethod != null)
            return (bool) isInIDRangeMethod.Invoke(baseTable, new object[] { id });
        return true;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        DrawIDSettings();
        DrawHeader();
        DrawSearchAndFilters();
        DrawMainContent();

        if (EditorGUI.EndChangeCheck())
        {
            isDirty = true;
            ValidateAllItems();
        }

        if (isDirty)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
            isDirty = false;
        }
    }

    private void DrawIDSettings()
    {
        var idBandwidthProp = serializedObject.FindProperty("idBandwidth");
        var idRangeSizeProp = serializedObject.FindProperty("idRangeSize");

        if (idBandwidthProp == null || idRangeSizeProp == null)
            return;

        EditorGUI.BeginChangeCheck();

        GUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField("⚙️ ID Range Settings", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(idBandwidthProp, new GUIContent("ID Bandwidth", "대역폭 기준값 (예: 1000000 = 아이템, 2000000 = 퀘스트)"));

                if (GUILayout.Button("?", GUILayout.Width(20)))
                {
                    ShowIDSettingsDialog();
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(idRangeSizeProp, new GUIContent("Range Size", "이 대역폭에서 사용할 ID 개수"));

            // 실시간 계산된 범위 표시
            uint bandwidth = (uint) idBandwidthProp.intValue;
            uint rangeSize = (uint) idRangeSizeProp.intValue;
            uint actualStart = bandwidth + 1;
            uint actualEnd = bandwidth + rangeSize - 1;

            EditorGUILayout.LabelField($"📍 Actual ID Range: {actualStart:N0} ~ {actualEnd:N0}", EditorStyles.miniLabel);

            // 범위 벗어난 데이터 개수 표시
            int outOfRangeCount = CountOutOfRangeItems();
            if (outOfRangeCount > 0)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField($"⚠️ {outOfRangeCount} items are out of range", EditorStyles.miniLabel);
                GUI.color = Color.white;

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("🔧 Auto Fix Out of Range IDs", GUILayout.Height(20)))
                    {
                        AutoFixOutOfRangeIDs();
                    }

                    if (GUILayout.Button("📋 Show Details", GUILayout.Width(80), GUILayout.Height(20)))
                    {
                        ShowOutOfRangeDetails();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            // ID 범위가 변경되었으므로 검증 업데이트
            ValidateAllItems();
        }

        EditorGUILayout.Space(5);
    }

    private void DrawHeader()
    {
        GUILayout.BeginVertical("box");
        {
            string tableName = dataType != null ? $"{dataType.Name} Table" : "Data Table";
            EditorGUILayout.LabelField($"📦 {tableName} Manager", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField($"Total Items: {GetItemCount()}");

                if (duplicateIDs.Count > 0)
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField($"⚠ Duplicates: {duplicateIDs.Count}", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }

                GUILayout.FlexibleSpace();

                // 검증 버튼
                if (GUILayout.Button("🔍 Validate", GUILayout.Width(80)))
                {
                    ValidateAllItems();
                    ShowValidationResults();
                }

                // 새 아이템 생성 버튼
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("+ New Data", GUILayout.Width(80)))
                {
                    CreateNewItem();
                }
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    private void DrawSearchAndFilters()
    {
        GUILayout.BeginVertical("box");
        {
            GUILayout.BeginHorizontal();
            {
                showFilters = EditorGUILayout.Foldout(showFilters, "🔎 Search & Filters");

                GUILayout.FlexibleSpace();

                // 정렬 옵션
                EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
                sortMode = (SortMode) EditorGUILayout.EnumPopup(sortMode, GUILayout.Width(60));

                if (GUILayout.Button(sortAscending ? "⬆" : "⬇", GUILayout.Width(25)))
                {
                    sortAscending = !sortAscending;
                }
            }
            GUILayout.EndHorizontal();

            if (showFilters)
            {
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
                    searchText = EditorGUILayout.TextField(searchText);

                    // 동적 enum 타입 필터 (감지된 경우에만 표시)
                    if (enumType != null)
                    {
                        EditorGUILayout.LabelField("Type:", GUILayout.Width(35));
                        var newFilterValue = EditorGUILayout.EnumPopup((Enum) filterEnumValue, GUILayout.Width(80));
                        if (!newFilterValue.Equals(filterEnumValue))
                        {
                            filterEnumValue = newFilterValue;
                        }
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(50)))
                    {
                        searchText = "";
                        if (enumType != null)
                        {
                            filterEnumValue = Enum.GetValues(enumType).GetValue(0);
                        }
                    }
                }
                GUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
        }
        GUILayout.EndVertical();
    }

    private void DrawMainContent()
    {
        var filteredIndices = GetFilteredAndSortedIndices();

        GUILayout.BeginVertical("box");
        {
            mainFoldout = EditorGUILayout.Foldout(mainFoldout, $"📋 Items ({filteredIndices.Count}/{GetItemCount()})");

            if (mainFoldout)
            {
                if (filteredIndices.Count == 0)
                {
                    EditorGUILayout.HelpBox("No items match the current filter.", MessageType.Info);
                }
                else
                {
                    foreach (int index in filteredIndices)
                    {
                        DrawItemElement(index);
                    }
                }

                EditorGUILayout.Space(5);
                DrawBottomButtons();
            }
        }
        GUILayout.EndVertical();
    }

    private void DrawItemElement(int index)
    {
        var items = GetAllItems();
        if (items == null || index >= items.Count) return;

        var item = items[index] as BaseTableData;
        bool hasError = validationErrors.ContainsKey(index);
        bool isDuplicate = item != null && duplicateIDs.Contains(item.ID);

        // 색상 설정
        Color originalColor = GUI.backgroundColor;
        if (hasError || isDuplicate)
            GUI.backgroundColor = Color.red;
        else if (item == null)
            GUI.backgroundColor = Color.yellow;

        GUILayout.BeginVertical("window");
        {
            GUI.backgroundColor = originalColor;

            GUILayout.BeginHorizontal();
            {
                // 폴드아웃과 아이템 정보
                string itemName = GetItemDisplayName(item);
                string itemId = item != null ? $"[ID: {item.ID}]" : "[No ID]";
                string statusIcon = GetStatusIcon(item, hasError, isDuplicate);

                itemFoldouts[index] = EditorGUILayout.Foldout(
                    itemFoldouts[index],
                    $"{statusIcon} {itemName} {itemId}");

                GUILayout.FlexibleSpace();

                // 아이템 타입 표시 (enum 필드가 있는 경우)
                if (item != null && enumType != null)
                {
                    var typeValue = GetItemTypeValue(item);
                    if (typeValue != null)
                    {
                        GUI.color = GetTypeColor(typeValue);
                        EditorGUILayout.LabelField(typeValue.ToString(), EditorStyles.miniLabel, GUILayout.Width(60));
                        GUI.color = Color.white;
                    }
                }

                // 삭제 버튼
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("✖", GUILayout.Width(25), GUILayout.Height(18)))
                {
                    if (EditorUtility.DisplayDialog("Delete Data",
                        $"Delete data '{itemName}'?", "Delete", "Cancel"))
                    {
                        DeleteItem(index);
                        return;
                    }
                }
                GUI.backgroundColor = originalColor;
            }
            GUILayout.EndHorizontal();

            // 오류 메시지 표시
            if (hasError)
            {
                EditorGUILayout.HelpBox(validationErrors[index], MessageType.Error);
            }

            // 아이템 세부 정보
            if (itemFoldouts[index])
            {
                DrawItemDetails(index);
            }
        }
        GUILayout.EndVertical();
    }

    private void DrawItemDetails(int index)
    {
        var items = GetAllItems();
        if (items == null || index >= items.Count) return;

        var item = items[index] as BaseTableData;
        if (item == null)
        {
            EditorGUILayout.HelpBox("Data is null. Delete this entry.", MessageType.Warning);
            return;
        }

        EditorGUI.indentLevel++;

        // ID 필드 (중복 체크 + 범위 체크) - BaseTableData의 공통 필드
        bool isDuplicateID = duplicateIDs.Contains(item.ID);
        bool isOutOfRange = !IsInIDRange(item.ID);

        if (isDuplicateID || isOutOfRange)
            GUI.color = Color.red;
        else
            GUI.color = Color.white;

        GUILayout.BeginHorizontal();
        {
            uint newId = (uint) EditorGUILayout.IntField("ID", (int) item.ID);

            if (GUILayout.Button("Auto", GUILayout.Width(50)))
            {
                newId = GetNextAutoID();
                if (newId == 0)
                {
                    EditorUtility.DisplayDialog("Error", "ID range is exhausted!", "OK");
                }
            }

            if (newId != item.ID && newId != 0)
            {
                Undo.RecordObject(item, "Change Data ID");
                SetItemID(item, newId);
                EditorUtility.SetDirty(item);
            }
        }
        GUILayout.EndHorizontal();
        GUI.color = Color.white;

        // ID 상태 표시
        if (isDuplicateID)
        {
            EditorGUILayout.HelpBox("⚠️ Duplicate ID detected!", MessageType.Error);
        }
        if (isOutOfRange)
        {
            EditorGUILayout.HelpBox($"⚠️ ID is out of range! Expected: {GetIDRangeInfo()}", MessageType.Warning);
        }

        EditorGUILayout.Space(5);

        // 나머지 필드들은 기본 PropertyField로 표시
        EditorGUILayout.LabelField("Data Properties", EditorStyles.boldLabel);

        var serializedItem = new SerializedObject(item);
        var iterator = serializedItem.GetIterator();

        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            // ID 필드는 이미 위에서 처리했으므로 스킵
            if (iterator.name == "id" || iterator.name == "m_Script")
                continue;

            EditorGUILayout.PropertyField(iterator, true);
        }

        serializedItem.ApplyModifiedProperties();

        EditorGUI.indentLevel--;
    }

    private void DrawBottomButtons()
    {
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("+ Add Data", GUILayout.Height(25)))
            {
                CreateNewItem();
            }

            if (GUILayout.Button("🔧 Auto Fix IDs", GUILayout.Height(25)))
            {
                AutoFixDuplicateIDs();
            }

            if (GUILayout.Button("🗑 Clean Nulls", GUILayout.Height(25)))
            {
                CleanNullItems();
            }
        }
        GUILayout.EndHorizontal();
    }

    private string GetItemDisplayName(BaseTableData item)
    {
        if (item == null) return "Null Data";

        var field = dataType.GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(string))
        {
            var value = (string) field.GetValue(item);
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        
        return $"{dataType.Name}";
    }

    private object GetItemTypeValue(BaseTableData item)
    {
        if (item == null || enumType == null) return null;

        var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.FieldType == enumType)
            {
                return field.GetValue(item);
            }
        }

        return null;
    }

    private string GetStatusIcon(BaseTableData item, bool hasError, bool isDuplicate)
    {
        if (item == null) return "❌";
        if (hasError || isDuplicate) return "⚠️";
        return "✅";
    }

    private Color GetTypeColor(object typeValue)
    {
        if (typeValue == null) return Color.gray;

        // enum 값에 따라 다른 색상 반환 (기본적으로 해시코드 기반으로 색상 생성)
        int hash = typeValue.GetHashCode();
        float hue = (hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.6f, 0.8f);
    }

    private List<int> GetFilteredAndSortedIndices()
    {
        var items = GetAllItems();
        if (items == null) return new List<int>();

        var indices = new List<int>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i] as BaseTableData;

            // 필터 적용
            if (!string.IsNullOrEmpty(searchText))
            {
                string displayName = GetItemDisplayName(item);
                if (!displayName.ToLower().Contains(searchText.ToLower()))
                    continue;
            }

            // 타입 필터 적용
            if (enumType != null && filterEnumValue != null)
            {
                var defaultEnumValue = Enum.GetValues(enumType).GetValue(0);
                if (!filterEnumValue.Equals(defaultEnumValue))
                {
                    var itemTypeValue = GetItemTypeValue(item);
                    if (itemTypeValue == null || !itemTypeValue.Equals(filterEnumValue))
                        continue;
                }
            }

            indices.Add(i);
        }

        // 정렬
        indices.Sort((a, b) =>
        {
            var itemA = items[a] as BaseTableData;
            var itemB = items[b] as BaseTableData;

            int comparison = sortMode switch
            {
                SortMode.Index => a.CompareTo(b),
                SortMode.ID => CompareIDs(itemA, itemB),
                SortMode.Name => CompareNames(itemA, itemB),
                SortMode.Type => CompareTypes(itemA, itemB),
                _ => a.CompareTo(b)
            };

            return sortAscending ? comparison : -comparison;
        });

        return indices;
    }

    private int CompareIDs(BaseTableData a, BaseTableData b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;
        return a.ID.CompareTo(b.ID);
    }

    private int CompareNames(BaseTableData a, BaseTableData b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        string nameA = GetItemDisplayName(a);
        string nameB = GetItemDisplayName(b);
        return string.Compare(nameA, nameB);
    }

    private int CompareTypes(BaseTableData a, BaseTableData b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        var typeA = GetItemTypeValue(a);
        var typeB = GetItemTypeValue(b);

        if (typeA == null && typeB == null) return 0;
        if (typeA == null) return -1;
        if (typeB == null) return 1;

        return string.Compare(typeA.ToString(), typeB.ToString());
    }

    private void CreateNewItem()
    {
        if (dataType == null) return;

        var newItem = CreateInstance(dataType) as BaseTableData;
        uint nextId = GetNextAutoID();

        if (nextId == 0)
        {
            EditorUtility.DisplayDialog("Error",
                "Cannot create new data: ID range is exhausted!\n" +
                $"Current range: {GetIDRangeInfo()}", "OK");
            return;
        }

        newItem.name = $"{dataType.Name.Replace("TableData", "")}_{nextId}";
        SetItemID(newItem, nextId);

        // 기본값 설정 (name 필드가 있으면 설정)
        var nameField = dataType.GetField("itemName", BindingFlags.Public | BindingFlags.Instance);
        if (nameField != null && nameField.FieldType == typeof(string))
        {
            nameField.SetValue(newItem, $"New {dataType.Name}");
        }

        // Sub-Asset으로 추가
        AssetDatabase.AddObjectToAsset(newItem, baseTable);

        // 목록에 추가
        var items = GetAllItems();
        if (items != null)
        {
            items.Add(newItem);
        }

        RefreshItemFoldouts();
        ValidateAllItems();

        EditorUtility.SetDirty(baseTable);
        EditorUtility.SetDirty(newItem);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created new {dataType.Name}: {newItem.name}");
    }

    private void DeleteItem(int index)
    {
        var items = GetAllItems();
        if (items == null || index < 0 || index >= items.Count) return;

        var item = items[index] as BaseTableData;
        if (item != null)
        {
            DestroyImmediate(item, true);
        }

        items.RemoveAt(index);
        RefreshItemFoldouts();
        ValidateAllItems();

        EditorUtility.SetDirty(baseTable);
        AssetDatabase.SaveAssets();
    }

    private uint GetNextAvailableID()
    {
        var items = GetAllItems();
        if (items == null) return 1;

        var usedIDs = new HashSet<uint>();

        foreach (var item in items)
        {
            if (item is BaseTableData data)
                usedIDs.Add(data.ID);
        }

        uint nextId = 1;
        while (usedIDs.Contains(nextId))
        {
            nextId++;
        }

        return nextId;
    }

    private void SetItemID(BaseTableData item, uint newId)
    {
        // BaseTableData의 protected id 필드를 리플렉션으로 설정
        var field = typeof(BaseTableData).GetField("id",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(item, newId);
    }

    private void ValidateAllItems()
    {
        duplicateIDs.Clear();
        validationErrors.Clear();

        var items = GetAllItems();
        if (items == null) return;

        var idCounts = new Dictionary<uint, int>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i] as BaseTableData;

            if (item == null)
            {
                validationErrors[i] = "Data is null";
                continue;
            }

            // ID 검증
            if (item.ID == 0)
            {
                validationErrors[i] = "ID cannot be 0";
            }
            else if (!IsInIDRange(item.ID))
            {
                validationErrors[i] = validationErrors.ContainsKey(i)
                    ? validationErrors[i] + "; ID out of range"
                    : "ID out of range";
            }

            if (idCounts.ContainsKey(item.ID))
            {
                idCounts[item.ID]++;
            }
            else
            {
                idCounts[item.ID] = 1;
            }

            // 이름 검증 (이름 필드가 있으면 검증)
            string displayName = GetItemDisplayName(item);
            if (displayName == $"{dataType.Name}" || string.IsNullOrEmpty(displayName))
            {
                validationErrors[i] = validationErrors.ContainsKey(i)
                    ? validationErrors[i] + "; Name is empty"
                    : "Name is empty";
            }
        }

        // 중복 ID 찾기
        foreach (var kvp in idCounts)
        {
            if (kvp.Value > 1)
            {
                duplicateIDs.Add(kvp.Key);
            }
        }
    }

    private void ShowValidationResults()
    {
        if (duplicateIDs.Count == 0 && validationErrors.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Complete",
                "✅ All data entries are valid!", "OK");
        }
        else
        {
            string message = "";

            if (duplicateIDs.Count > 0)
            {
                message += $"⚠️ Found {duplicateIDs.Count} duplicate IDs: " +
                          string.Join(", ", duplicateIDs) + "\n\n";
            }

            if (validationErrors.Count > 0)
            {
                message += $"❌ Found {validationErrors.Count} validation errors.\n";
                message += "Check the inspector for details.";
            }

            EditorUtility.DisplayDialog("Validation Issues Found", message, "OK");
        }
    }

    private void AutoFixDuplicateIDs()
    {
        if (duplicateIDs.Count == 0)
        {
            EditorUtility.DisplayDialog("Auto Fix", "No duplicate IDs found.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Auto Fix IDs",
            $"This will reassign {duplicateIDs.Count} duplicate IDs. Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        var items = GetAllItems();
        if (items == null) return;

        var usedIDs = new HashSet<uint>();

        // 중복되지 않는 ID들을 먼저 수집
        foreach (var item in items)
        {
            if (item is BaseTableData data && !duplicateIDs.Contains(data.ID))
            {
                usedIDs.Add(data.ID);
            }
        }

        // 중복 ID들을 새로운 ID로 할당
        uint nextId = 1;
        foreach (var item in items)
        {
            if (item is BaseTableData data && duplicateIDs.Contains(data.ID))
            {
                while (usedIDs.Contains(nextId))
                {
                    nextId++;
                }

                Undo.RecordObject(data, "Auto Fix ID");
                SetItemID(data, nextId);
                usedIDs.Add(nextId);
                EditorUtility.SetDirty(data);

                nextId++;
            }
        }

        ValidateAllItems();
        EditorUtility.SetDirty(baseTable);

        Debug.Log("Auto-fixed duplicate IDs");
    }

    private void CleanNullItems()
    {
        var items = GetAllItems();
        if (items == null) return;

        int nullCount = 0;
        foreach (var item in items)
        {
            if (item == null) nullCount++;
        }

        if (nullCount == 0)
        {
            EditorUtility.DisplayDialog("Clean Nulls", "No null data entries found.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Clean Null Data",
            $"Remove {nullCount} null data entries?", "Yes", "Cancel"))
        {
            return;
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] == null)
            {
                items.RemoveAt(i);
            }
        }

        RefreshItemFoldouts();
        ValidateAllItems();
        EditorUtility.SetDirty(baseTable);

        Debug.Log($"Removed {nullCount} null data entries");
    }

    private void ShowIDSettingsDialog()
    {
        var idBandwidthProp = serializedObject.FindProperty("idBandwidth");
        var idRangeSizeProp = serializedObject.FindProperty("idRangeSize");

        if (idBandwidthProp == null || idRangeSizeProp == null)
        {
            EditorUtility.DisplayDialog("Error", "Cannot access ID settings properties.", "OK");
            return;
        }

        // 현재 값들 가져오기
        uint currentBandwidth = (uint) idBandwidthProp.intValue;
        uint currentSize = (uint) idRangeSizeProp.intValue;

        // 다이얼로그 내용 생성
        string message = $"현재 ID 설정:\n\n" +
                        $"🔢 대역폭 기준값: {currentBandwidth:N0}\n" +
                        $"📏 범위 크기: {currentSize:N0}개\n" +
                        $"🎯 실제 ID 범위: {currentBandwidth + 1:N0} ~ {currentBandwidth + currentSize - 1:N0}\n\n" +
                        $"💡 권장 대역폭 기준값:\n" +
                        $"• 1,000,000 → 아이템 (Item)\n" +
                        $"• 2,000,000 → 퀘스트 (Quest)\n" +
                        $"• 3,000,000 → 스킬 (Skill)\n" +
                        $"• 4,000,000 → NPC\n" +
                        $"• 5,000,000 → 장비 (Equipment)\n\n" +
                        $"⚙️ Inspector의 'ID Bandwidth' 필드에서\n대역폭 기준값을 직접 설정할 수 있습니다.";

        EditorUtility.DisplayDialog("ID Range Settings", message, "OK");

        // Inspector를 다시 그리도록 강제
        Repaint();
    }

    private int CountOutOfRangeItems()
    {
        var items = GetAllItems();
        if (items == null) return 0;

        int count = 0;
        foreach (var item in items)
        {
            if (item is BaseTableData data && !IsInIDRange(data.ID))
            {
                count++;
            }
        }
        return count;
    }

    private void AutoFixOutOfRangeIDs()
    {
        var items = GetAllItems();
        if (items == null) return;

        var outOfRangeItems = new List<BaseTableData>();

        // 범위를 벗어난 아이템들 수집
        foreach (var item in items)
        {
            if (item is BaseTableData data && !IsInIDRange(data.ID))
            {
                outOfRangeItems.Add(data);
            }
        }

        if (outOfRangeItems.Count == 0)
            return;

        string message = $"범위를 벗어난 {outOfRangeItems.Count}개 아이템의 ID를\n" +
                        $"새로운 대역폭 범위로 자동 이전하시겠습니까?\n\n" +
                        $"현재 범위: {GetIDRangeInfo()}";

        if (!EditorUtility.DisplayDialog("Auto Fix Out of Range IDs", message, "Fix", "Cancel"))
            return;

        // 범위 내 사용 가능한 ID들 찾기
        var usedIDs = new HashSet<uint>();
        foreach (var item in items)
        {
            if (item is BaseTableData data && IsInIDRange(data.ID))
            {
                usedIDs.Add(data.ID);
            }
        }

        int fixedCount = 0;
        foreach (var item in outOfRangeItems)
        {
            uint newId = GetNextAutoID();
            if (newId == 0)
            {
                Debug.LogWarning($"Cannot fix item '{GetItemDisplayName(item)}': ID range exhausted");
                break;
            }

            Undo.RecordObject(item, "Fix Out of Range ID");
            SetItemID(item, newId);
            EditorUtility.SetDirty(item);
            fixedCount++;

            // 사용된 ID 추가
            usedIDs.Add(newId);
        }

        ValidateAllItems();
        EditorUtility.SetDirty(baseTable);

        string resultMessage = $"✅ {fixedCount}개 아이템의 ID를 수정했습니다.";
        if (fixedCount < outOfRangeItems.Count)
        {
            resultMessage += $"\n⚠️ {outOfRangeItems.Count - fixedCount}개는 범위 부족으로 수정하지 못했습니다.";
        }

        EditorUtility.DisplayDialog("Auto Fix Complete", resultMessage, "OK");
    }

    private void ShowOutOfRangeDetails()
    {
        var items = GetAllItems();
        if (items == null) return;

        var outOfRangeItems = new List<string>();

        foreach (var item in items)
        {
            if (item is BaseTableData data && !IsInIDRange(data.ID))
            {
                string itemName = GetItemDisplayName(data);
                outOfRangeItems.Add($"• {itemName} (ID: {data.ID:N0})");
            }
        }

        if (outOfRangeItems.Count == 0)
        {
            EditorUtility.DisplayDialog("Out of Range Items", "모든 아이템이 범위 내에 있습니다.", "OK");
            return;
        }

        string message = $"범위를 벗어난 아이템들:\n\n" +
                        $"현재 유효 범위: {GetIDRangeInfo()}\n\n" +
                        string.Join("\n", outOfRangeItems.Take(10));

        if (outOfRangeItems.Count > 10)
        {
            message += $"\n... 그 외 {outOfRangeItems.Count - 10}개 더";
        }

        EditorUtility.DisplayDialog("Out of Range Details", message, "OK");
    }
}