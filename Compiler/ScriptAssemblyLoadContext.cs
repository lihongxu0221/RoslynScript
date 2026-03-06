using System.Reflection;
using System.Runtime.Loader;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 自定义的程序集加载上下文，支持可回收特性.
/// </summary>
internal sealed class ScriptAssemblyLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptAssemblyLoadContext"/> class.
    /// </summary>
    /// <param name="name"> The value for System.Runtime.Loader.AssemblyLoadContext.Name in the new instance.Its value can be null.</param>
    public ScriptAssemblyLoadContext(string name)
        : base(name, isCollectible: true)
    {
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 默认返回 null，让系统从默认上下文中解析已加载的库
        return null;
    }
}