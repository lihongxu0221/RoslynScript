using DryIoc;
using Prism.DryIoc;
using System.Reflection;
using Expression = System.Linq.Expressions.Expression;

namespace BgCommon.Script.Compiler;

internal sealed class ScriptRuntime : IDisposable
{
    private readonly IResolverContext _scope;
    private readonly ScriptAssemblyLoadContext _alc;
    private readonly Dictionary<string, Func<object, object?[], Task<object?>>> invokerCache = new();

    public Assembly Assembly { get; private set; }

    public ScriptRuntime(
        string scriptName,
        IContainerProvider containerProvider,
        byte[] assemblyBytes,
        byte[]? pdbBytes)
    {
        // 核心：从 Prism 9 获取 DryIoc 根容器并开启独立作用域
        var rootContainer = ((DryIocContainerExtension)containerProvider).Instance;
        this._scope = rootContainer.OpenScope();

        this._alc = new ScriptAssemblyLoadContext($"ALC_{scriptName}_{Guid.NewGuid():N}");

        using var ms = new MemoryStream(assemblyBytes);
        using var pdbMs = pdbBytes != null ? new MemoryStream(pdbBytes) : null;
        this.Assembly = _alc.LoadFromStream(ms, pdbMs);
    }

    public async Task<object?> InvokeAsync(string typeName, string methodName, object?[] parameters)
    {
        var cacheKey = $"{typeName}.{methodName}";
        if (!invokerCache.TryGetValue(cacheKey, out var invoker))
        {
            invoker = CreateFastInvoker(typeName, methodName);
            invokerCache[cacheKey] = invoker;
        }

        var targetType = this.Assembly.GetType(typeName) ??
                         this.Assembly.GetTypes().FirstOrDefault(t => t.Name == typeName) ??
                         throw new TypeLoadException($"未找到类型: {typeName}");

        // 使用 DryIoc 作用域解析对象（自动构造函数注入）
        var instance = _scope.New<object>(targetType);
        return await invoker(instance, parameters);
    }

    private Func<object, object?[], Task<object?>> CreateFastInvoker(string typeName, string methodName)
    {
        var type = this.Assembly.GetType(typeName) ?? this.Assembly.GetTypes().First(t => t.Name == typeName);
        var method = type.GetMethod(methodName) ?? throw new Exception($"方法 {methodName} 不存在");

        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var argsParam = Expression.Parameter(typeof(object?[]), "args");

        var argExpressions = method.GetParameters().Select((p, i) =>
        {
            var argExp = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            return Expression.Convert(argExp, p.ParameterType);
        }).ToArray();

        var callExp = Expression.Call(Expression.Convert(instanceParam, type), method, argExpressions);

        // 包装异步返回结果
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
        invokerCache.Clear();
        _scope.Dispose(); // 释放该脚本作用域内的所有 DryIoc 资源
        _alc.Unload();    // 卸载 ALC
    }
}