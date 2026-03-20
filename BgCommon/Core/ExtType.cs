namespace BgCommon.Core;

/// <summary>
/// 扩展名类型枚举.
/// </summary>
public enum ExtType
{
    /// <summary>
    /// 项目文件扩展名.
    /// </summary>
    Project = 0,

    /// <summary>
    /// 项目运行结果扩展名.
    /// </summary>
    ProjectResult = 1,

    /// <summary>
    /// 视觉文件扩展名.
    /// </summary>
    Vision = 2,

    /// <summary>
    /// CAD文件扩展名.
    /// </summary>
    Cad = 3,

    /// <summary>
    /// C#脚本文件扩展名.
    /// </summary>
    ScriptCSharp = 4,

    /// <summary>
    /// 自定义脚本文件扩展名.
    /// </summary>
    Script = 5,
}

/// <summary>
/// ExtType扩展方法类.
/// </summary>
public static class ExtTypeExtensions
{
    /// <summary>
    /// 获取扩展名字符串.
    /// </summary>
    /// <param name="extType">扩展名类型.</param>
    /// <returns>扩展名字符串.</returns>
    public static string ToExtensionString(this ExtType extType)
    {
        return extType switch
        {
            ExtType.Project => ".prj",
            ExtType.ProjectResult => ".dat",
            ExtType.Vision => ".vsprj",
            ExtType.Cad => ".dxf",
            ExtType.ScriptCSharp => ".cs",
            ExtType.Script => ".csx",
            _ => throw new ArgumentOutOfRangeException(nameof(extType), extType, null),
        };
    }
}