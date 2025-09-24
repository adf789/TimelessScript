public class StringDefine
{
    #region DEFINE
    public const string DEFINE_CONTROLLER_TYPE_NAME = "{0}Controller, HighLevel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
    #endregion

    #region PATH
    public const string PATH_VIEW_PREFAB = "Assets/TS/Resources/Prefabs/UI/{0}/";
    public const string PATH_LOAD_VIEW_PREFAB = "Prefabs/UI/{0}{1}";
    public const string PATH_SCRIPT = "Assets/TS/Scripts/{0}/";
    public const string PATH_RESOURCES_REGISTRY = "ScriptableObjects/ResourcesPath/ResourcesPathRegistry";
    #endregion PATH

    #region AUTO MOVING
    public const float AUTO_MOVE_SAME_HEIGHT_THRESHOLD = 0.5f;
    public const float AUTO_MOVE_MAX_HORIZONTAL_REACH = 10.0f;
    public const float AUTO_MOVE_MAX_VERTICAL_REACH = 8.0f;
    public const float AUTO_MOVE_WAYPOINT_ARRIVAL_DISTANCE = 0.2f;
    public const float AUTO_MOVE_MINIMUM_DISTANCE = 0.5f;
    #endregion
}