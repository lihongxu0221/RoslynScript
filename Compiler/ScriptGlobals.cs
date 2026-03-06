using System.Collections.Concurrent;
using System.Reflection;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 脚本执行时的全局寄宿对象基类.
/// 负责处理脚本顶层执行、深层方法反射、参数自动装配及异步结果脱壳.
/// </summary>
public abstract class ScriptGlobals
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> TaskResultCache = new ConcurrentDictionary<string, PropertyInfo?>();
    private static readonly ConcurrentDictionary<string, object?> SharedProperties = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptGlobals"/> class.
    /// </summary>
    protected ScriptGlobals()
    {
    }

    /// <summary>
    /// Gets or sets 日志接口，供脚本内部使用.
    /// </summary>
    public Action<string>? Log { get; set; }

    /// <summary>
    /// Gets 输出参数池：脚本执行过程中产生的额外结果或 out 参数放入此处.
    /// </summary>
    public ConcurrentDictionary<string, object?> Outputs { get; } = new ConcurrentDictionary<string, object?>();

    /// <summary>
    /// Gets Roslyn 脚本原生执行产生的返回值（脚本最后一行的表达式结果）.
    /// </summary>
    public object? ScriptReturnValue { get; private set; }

    /// <summary>
    /// Gets 通过反射调用特定方法产生的返回值.
    /// </summary>
    public object? MethodReturnValue { get; private set; }

    /// <summary>
    /// Gets or sets 宿主上下文数据.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets 允许脚本检查是否已被请求取消执行.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets 最终被确定的目标类型全名.
    /// </summary>
    public string? ResolvedTypeName { get; private set; }

    /// <summary>
    /// Gets 最终被确定的目标方法名.
    /// </summary>
    public string? ResolvedMethodName { get; private set; }

    /// <summary>
    /// 设定脚本编译结果（内部调用）.
    /// </summary>
    /// <param name="value">脚本编译结果.</param>
    internal void SetScriptResult(object? value)
    {
        this.ScriptReturnValue = value;
    }

    /// <summary>
    /// 从脚本程序集中解析目标类型（内部调用）.
    /// </summary>
    /// <param name="scriptAssembly">脚本生成的程序集.</param>
    /// <returns>解析出的目标类型.</returns>
    internal Type InternalResolveType(Assembly scriptAssembly) => ResolveTargetType(scriptAssembly);

    /// <summary>
    /// 从目标类型中解析需要执行的方法信息（内部调用）.
    /// </summary>
    /// <param name="targetType">目标类型.</param>
    /// <returns>方法反射信息.</returns>
    internal MethodInfo InternalResolveMethod(Type targetType) => ResolveTargetMethod(targetType);

    /// <summary>
    /// 获取指定的共享属性值.
    /// </summary>
    /// <param name="propertyKey">属性的唯一键名.</param>
    /// <returns>返回找到的对象；如果未找到，则返回 null.</returns>
    public object? GetShared(string propertyKey)
    {
        // 尝试从共享属性集合中获取与键关联的值
        return SharedProperties.TryGetValue(propertyKey, out var sharedValue) ? sharedValue : null;
    }

    /// <summary>
    /// 设置共享属性值.
    /// </summary>
    /// <param name="propertyKey">属性的唯一键名.</param>
    /// <param name="propertyValue">要存储的属性值对象.</param>
    public void SetShared(string propertyKey, object? propertyValue)
    {
        // 将指定的值通过键名存入共享属性集合中
        SharedProperties[propertyKey] = propertyValue;
    }

    /// <summary>
    /// 将对象序列化为 JSON 字符串.
    /// </summary>
    /// <param name="targetObject">需要序列化的目标对象.</param>
    /// <returns>返回序列化后的 JSON 字符串.</returns>
    public string ToJson(object targetObject)
    {
        // 调用系统内置的 JSON 序列化工具
        return System.Text.Json.JsonSerializer.Serialize(targetObject);
    }

    /// <summary>
    /// 将 JSON 字符串反序列化为指定的泛型对象.
    /// </summary>
    /// <typeparam name="T">目标对象类型.</typeparam>
    /// <param name="jsonString">要解析的 JSON 字符串.</param>
    /// <returns>返回反序列化后的对象实例.</returns>
    public T? FromJson<T>(string jsonString)
    {
        // 调用系统内置的 JSON 反序列化工具并返回指定类型
        return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
    }

    /// <summary>
    /// 提供一个通用的异步输出方法.
    /// </summary>
    /// <param name="message">日志信息.</param>
    /// <returns>返回Task.</returns>
    public async Task WriteLogAsync(string message)
    {
        await Task.Run(() => Log?.Invoke(message));
    }

    /// <summary>
    /// 核心执行方法。接收 Roslyn 运行后的 ScriptState 并通过反射执行特定的入口方法.
    /// </summary>
    /// <param name="targetType">目标类型.</param>
    /// <param name="targetMethod">目标方法.</param>
    /// <returns>表示异步操作的任务.</returns>
    public async Task<object?> ExecuteStateAsync(Type? targetType, MethodInfo? targetMethod)
    {
        // 防御性：如果传入的 state 已经携带异常，则不执行反射
        if (targetType == null || targetMethod == null)
        {
            return null;
        }

        this.ResolvedTypeName = targetType.FullName;
        this.ResolvedMethodName = targetMethod.Name;

        // 1. 实例化
        var instance = ResolveTargetInstance(targetType, targetMethod.IsStatic);

        // 2. 参数装配
        var paramInfos = targetMethod.GetParameters();
        var args = PrepareParameters(paramInfos);

        // 3. 反射调用
        // 注意：这里可能抛出 TargetInvocationException，由外层 RunAsync 捕获并解包
        var result = targetMethod.Invoke(instance, args);

        // 4. 同步 Out/Ref 参数
        for (int i = 0; i < paramInfos.Length; i++)
        {
            if (paramInfos[i].ParameterType.IsByRef)
            {
                var paramName = paramInfos[i].Name ?? $"arg_{i}";
                Outputs[paramName] = args[i];
            }
        }

        // 5. 异步脱壳
        this.MethodReturnValue = await HandleResultAsync(result);
        return this.MethodReturnValue;
    }

    /// <summary>
    /// 从脚本程序集中解析目标类型.
    /// </summary>
    /// <param name="scriptAssembly">脚本生成的程序集.</param>
    /// <returns>解析出的目标类型.</returns>
    protected abstract Type ResolveTargetType(Assembly scriptAssembly);

    /// <summary>
    /// 从目标类型中解析需要执行的方法信息.
    /// </summary>
    /// <param name="targetType">目标类型.</param>
    /// <returns>方法反射信息.</returns>
    protected abstract MethodInfo ResolveTargetMethod(Type targetType);

    /// <summary>
    /// 从脚本程序集中解析目标类型.
    /// </summary>
    /// <param name="scriptAssembly">脚本生成的程序集.</param>
    /// <param name="targetTypeName">目标类型名称，支持简单类名匹配.</param>
    /// <returns>解析出的目标类型.</returns>
    protected virtual Type ResolveTargetType(Assembly scriptAssembly, string targetTypeName)
    {
        // 尝试直接获取（处理标准 DLL 或顶级类的情况）
        var type = scriptAssembly.GetType(targetTypeName);

        if (type == null)
        {
            // 遍历所有类型，匹配类名
            // Roslyn 脚本定义的类通常是 Submission#0 的嵌套类 (FullName = "Submission#0+MathService")
            // 我们通过 t.Name 来匹配，或者判断 FullName 是否以 "+targetTypeName" 结尾
            type = scriptAssembly.GetTypes().FirstOrDefault(t =>
                t.IsClass &&
                (t.Name.Equals(targetTypeName, StringComparison.OrdinalIgnoreCase) ||
                 (t.FullName != null && t.FullName.EndsWith("+" + targetTypeName, StringComparison.Ordinal))));
        }

        // 3. 兜底策略：如果没指定名称或找不到，取脚本中第一个定义的业务类
        if (type == null)
        {
            type = scriptAssembly.GetTypes().FirstOrDefault(t =>
                t.IsClass &&
                !t.Name.StartsWith("Submission#") && // 排除 Roslyn 包装类
                !t.Name.Contains('<') && // 排除 匿名类型或编译器生成类型
                !t.IsNested); // 优先找非嵌套的（如果是脚本类，这一条可能需要去掉）

            // 如果上述过滤后为空，则放宽条件取第一个 ExportedType
            type ??= scriptAssembly.GetExportedTypes().FirstOrDefault(t =>
                t.IsClass && !t.Name.StartsWith("Submission#"));
        }

        if (type == null)
        {
            throw new InvalidOperationException($"脚本中未定义有效的类，且找不到名为 {targetTypeName} 的类型。");
        }

        return type;
    }

    /// <summary>
    /// 基类提供的工具方法：根据方法名查找最匹配的公有方法（优先选择参数最多的重载）.
    /// </summary>
    /// <param name="targetType">目标类型.</param>
    /// <param name="methodName">方法名称.</param>
    /// <returns>方法反射信息.</returns>
    protected virtual MethodInfo ResolveTargetMethod(Type targetType, string methodName)
    {
        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                                .ToList();

        if (methods.Count == 0)
        {
            throw new InvalidOperationException($"在类型 {targetType.Name} 中找不到公有方法: {methodName}");
        }

        // 策略：优先选择参数最多的重载，以便最大限度利用参数注入
        return methods.OrderByDescending(m => m.GetParameters().Length).First();
    }

    /// <summary>
    /// 根据方法参数信息准备执行所需的参数数组.
    /// </summary>
    /// <param name="parameters">方法参数定义信息.</param>
    /// <returns>构造好的参数对象数组集.</returns>
    protected virtual object?[] PrepareParameters(ParameterInfo[] parameters)
    {
        var args = new object?[parameters.Length];

        // 预解析 Data 数据源
        var namedData = this.Data as IDictionary<string, object?>;
        var listData = (this.Data is not string && this.Data is IEnumerable enumerable)
                       ? enumerable.Cast<object?>().ToList()
                       : null;

        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            var pName = p.Name ?? string.Empty;
            var pType = p.ParameterType;

            // 获取真实类型（去除 ref/out 标记）
            var effectiveType = pType.IsByRef ? pType.GetElementType()! : pType;

            // --- 优先级 1: 自动注入系统对象 ---
            // 注入 ScriptGlobals 自身
            if (effectiveType.IsAssignableFrom(this.GetType()))
            {
                args[i] = this;
                continue;
            }

            // 注入 取消令牌
            if (effectiveType == typeof(CancellationToken))
            {
                args[i] = this.CancellationToken;
                continue;
            }

            // --- 优先级 2: 处理 Out 参数初始化 ---
            if (p.IsOut)
            {
                args[i] = effectiveType.IsValueType ? Activator.CreateInstance(effectiveType) : null;
                continue;
            }

            // --- 优先级 3: 从 Data 数据映射 ---
            object? rawValue = null;
            bool found = false;

            // A. 键值对匹配 (按参数名)
            if (namedData != null)
            {
                var key = namedData.Keys.FirstOrDefault(k => k.Equals(pName, StringComparison.OrdinalIgnoreCase));
                if (key != null)
                {
                    rawValue = namedData[key];
                    found = true;
                }
            }

            // B. 列表匹配 (按位置索引)
            if (!found && listData != null && i < listData.Count)
            {
                rawValue = listData[i];
                found = true;
            }

            // C. 特殊匹配：名为 "data" 的参数直接接收整个 Data
            if (!found && pName.Equals("data", StringComparison.OrdinalIgnoreCase))
            {
                rawValue = this.Data;
                found = true;
            }

            // --- 优先级 4: 类型转换与默认值处理 ---
            if (found)
            {
                args[i] = TryConvertValue(rawValue, effectiveType);
            }
            else
            {
                // 没找到数据，使用 C# 默认值或创建零值
                args[i] = p.HasDefaultValue ? p.DefaultValue : (effectiveType.IsValueType ? Activator.CreateInstance(effectiveType) : null);
            }
        }

        return args;
    }

    /// <summary>
    /// 从脚本程序集中解析目标类型实例, 默认为无参构造.
    /// </summary>
    /// <param name="targetType">目标类型.</param>
    /// <param name="isStatic">要调用的方法是否为静态方法.</param>
    /// <returns>解析出的目标类型.</returns>
    protected virtual object? ResolveTargetInstance(Type targetType, bool isStatic)
    {
        return isStatic ? null : Activator.CreateInstance(targetType);
    }

    /// <summary>
    /// 处理执行结果，支持 Task, ValueTask 以及常规对象.
    /// </summary>
    /// <param name="result">待处理的原始结果.</param>
    /// <returns>处理后的异步结果对象.</returns>
    private async Task<object?> HandleResultAsync(object? result)
    {
        if (result == null)
        {
            return null;
        }

        // 处理标准的 Task 和 Task<T>
        if (result is Task task)
        {
            try
            {
                await task;

                // 仅在成功完成时访问 Result，避免 Faulted/Canceled 状态下的反射风险
                if (task.IsCompletedSuccessfully)
                {
                    return GetTaskResult(task);
                }
            }
            catch
            {
                // 任务失败时，异常已由 await 抛出，此处通常不执行
                throw;
            }

            return null;
        }

        // 处理 ValueTask<T>
        Type resultType = result.GetType();
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            // 增加对 ValueTask 的支持
            // 利用 dynamic 简化 ValueTask<T> 的处理，或使用反射
            dynamic valueTaskDynamic = result;
            return await valueTaskDynamic;
        }

        if (result is ValueTask vTask)
        {
            await vTask;
            return null;
        }

        return result;
    }

    /// <summary>
    /// 尝试将输入值安全转换为目标类型.
    /// </summary>
    /// <param name="value">原始值.</param>
    /// <param name="targetType">目标类型.</param>
    /// <returns>转换后的值.</returns>
    protected object? TryConvertValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return null;
        }

        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        try
        {
            // 使用 TypeDescriptor 增强对 Guid, DateTime 等类型的支持
            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(value.GetType()))
            {
                return converter.ConvertFrom(value);
            }

            // 基础类型转换
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            // 转换失败则传回原值，让反射调用层在 Invoke 阶段报出具体的参数不匹配错误
            return value;
        }
    }

    /// <summary>
    /// 通过反射获取 Task 的 Result 属性值.
    /// </summary>
    /// <param name="task">Task 实例对象.</param>
    /// <returns>Task 执行的结果.</returns>
    protected object? GetTaskResult(Task task)
    {
        var taskType = task.GetType();
        if (!taskType.IsGenericType)
        {
            return null;
        }

        // 使用 AssemblyQualifiedName 作为缓存 key,确保跨 AssemblyLoadContext 的类型隔离
        // 这避免了 PropertyInfo 在不同上下文中使用时的潜在问题
        var typeKey = taskType.AssemblyQualifiedName ?? taskType.FullName ?? taskType.Name;
        var prop = TaskResultCache.GetOrAdd(typeKey, _ => taskType.GetProperty("Result"));

        return prop?.GetValue(task);
    }
}