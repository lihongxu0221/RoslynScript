using DryIoc;
using Prism.DryIoc;
using System.Reflection;
using Expression = System.Linq.Expressions.Expression;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 负责脚本的隔离运行环境、DI 作用域及高性能调用缓存.
/// </summary>
internal sealed class ScriptRuntime : IDisposable
{
    private readonly IResolverContext scope;
    private readonly ScriptAssemblyLoadContext alc;
    private readonly Dictionary<string, Func<object, object?[], Task<object?>>> methodCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptRuntime"/> class.
    /// </summary>
    /// <remarks>This constructor creates an isolated dependency injection scope and loads the provided
    /// assembly into a collectible AssemblyLoadContext, enabling dynamic script execution and unloading. The container
    /// provider must be compatible with DryIoc.</remarks>
    /// <param name="scriptName">The name of the script to associate with this runtime instance. Used for identification and context.</param>
    /// <param name="containerProvider">The container provider used to resolve dependencies and manage the script's service scope. Must not be null.</param>
    /// <param name="assemblyBytes">A byte array containing the compiled assembly to be loaded into the runtime. Must not be null.</param>
    /// <param name="pdbBytes">A byte array containing the debug symbols (PDB) for the assembly, or null if no symbols are available.</param>
    public ScriptRuntime(
        string scriptName,
        IContainerProvider containerProvider,
        byte[] assemblyBytes,
        byte[]? pdbBytes)
    {
        // Prism 9 获取底层 DryIoc 容器并开启独立作用域
        var rootContainer = ((DryIocContainerExtension)containerProvider).Instance;
        this.scope = rootContainer.OpenScope();

        // 初始化可回收的程序集加载上下文
        this.alc = new ScriptAssemblyLoadContext($"ALC_{scriptName}_{Guid.NewGuid():N}");

        using var ms = new MemoryStream(assemblyBytes);
        using var pdbMs = pdbBytes != null ? new MemoryStream(pdbBytes) : null;
        this.Assembly = alc.LoadFromStream(ms, pdbMs);
    }

    /// <summary>
    /// Gets the <see cref="Assembly"/> instance representing the loaded script assembly.
    /// </summary>
    public Assembly Assembly { get; private set; }

    /// <summary>
    /// 调用脚本方法.
    /// </summary>
    /// <remarks>The target type is resolved using dependency injection. If the type is not registered, it is
    /// resolved with constructor injection. Method invocations are cached for performance. This method is typically
    /// used for dynamic or script-based method execution scenarios.</remarks>
    /// <param name="typeName">The full name or simple name of the type containing the method to invoke. The type must be available in the
    /// current assembly.</param>
    /// <param name="methodName">The name of the method to invoke on the resolved type.</param>
    /// <param name="parameters">An array of arguments to pass to the method. The order and types must match the method's signature.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the return value of the invoked
    /// method, or null if the method has no return value.</returns>
    /// <exception cref="TypeLoadException">Thrown if the specified type cannot be found in the current assembly.</exception>
    public async Task<object?> InvokeAsync(string typeName, string methodName, object?[] parameters)
    {
        var cacheKey = $"{typeName}.{methodName}";
        if (!methodCache.TryGetValue(cacheKey, out var invoker))
        {
            invoker = CreateFastInvoker(typeName, methodName);
            methodCache[cacheKey] = invoker;
        }

        // 获取类型并自动注入
        var targetType = this.Assembly.GetType(typeName) ??
                         this.Assembly.GetTypes().FirstOrDefault(t => t.Name == typeName) ??
                         throw new TypeLoadException($"未找到类型: {typeName}");

        // DryIoc 解析逻辑：如果脚本类型未在容器注册，使用解构解析并注入
        // 修正之前的 New<object> 错误
        var instance = scope.Resolve(targetType, IfUnresolved.Throw);

        return await invoker(instance, parameters);
    }

    private Func<object, object?[], Task<object?>> CreateFastInvoker(string typeName, string methodName)
    {
        var type = this.Assembly.GetType(typeName) ?? this.Assembly.GetTypes().First(t => t.Name == typeName);
        var method = type.GetMethod(methodName) ?? throw new ArgumentNullException($"方法 {methodName} 不存在");

        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var argsParam = Expression.Parameter(typeof(object?[]), "args");

        var argExpressions = method.GetParameters().Select((p, i) =>
        {
            var argExp = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            return Expression.Convert(argExp, p.ParameterType);
        }).ToArray();

        var callExp = Expression.Call(Expression.Convert(instanceParam, type), method, argExpressions);

        // 包装异步返回结果，实现自动脱壳
        var wrappedCall = Expression.Call(
            typeof(ScriptRuntime),
            nameof(HandleResult),
            null,
            Expression.Convert(callExp, typeof(object)));

        return Expression.Lambda<Func<object, object?[], Task<object?>>>(wrappedCall, instanceParam, argsParam).Compile();
    }

    public static async Task<object?> HandleResult(object? result)
    {
        if (result is Task task)
        {
            await task;
            var prop = task.GetType().GetProperty("Result");
            return prop?.GetValue(task);
        }

        return result;
    }

    public void Dispose()
    {
        methodCache.Clear();
        scope.Dispose(); // 关键：释放作用域内的所有 DryIoc 对象
        alc.Unload();    // 关键：卸载程序集上下文
    }
}