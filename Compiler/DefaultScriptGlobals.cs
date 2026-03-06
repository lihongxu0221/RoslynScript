using System.Reflection;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 脚本全局对象的默认简单实现.
/// 约定：脚本中应包含一个名为 "ScriptEntryPoint" 的类，并包含一个名为 "Run" 的方法.
/// </summary>
public class DefaultScriptGlobals : ScriptGlobals
{
    private readonly string targetTypeName;
    private readonly string methodName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultScriptGlobals"/> class.
    /// </summary>
    /// <param name="targetTypeName">默认要反射的类名.</param>
    /// <param name="methodName">默认要反射执行的方法名.</param>
    public DefaultScriptGlobals(string targetTypeName = "ScriptEntryPoint", string methodName = "Run")
    {
        this.targetTypeName = targetTypeName;
        this.methodName = methodName;
    }

    /// <inheritdoc/>
    protected override Type ResolveTargetType(Assembly scriptAssembly)
    {
        return this.ResolveTargetType(scriptAssembly, this.targetTypeName);
    }

    /// <inheritdoc/>
    protected override MethodInfo ResolveTargetMethod(Type targetType)
    {
        return this.ResolveTargetMethod(targetType, methodName);
    }

    /// <inheritdoc/>
    protected override object?[] PrepareParameters(ParameterInfo[] parameters)
    {
        return base.PrepareParameters(parameters);
    }
}