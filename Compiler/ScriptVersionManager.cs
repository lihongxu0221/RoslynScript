using BgCommon.Prism.Wpf.Authority.Entities;
using BgCommon.Script.Compiler.Models;
using System.Collections.Concurrent;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 脚本版本管理器，用于管理脚本的历史版本、创建新版本以及执行版本回滚操作.
/// 存放目录为 {ScriptName}/{VersionId}.ver，版本记录包含脚本内容快照、创建时间、作者等元信息.
/// </summary>
public class ScriptVersionManager
{
    // 内部版本存储结构：以脚本名称为键，保存该脚本的所有版本记录列表
    private readonly ConcurrentDictionary<string, List<ScriptVersion>> versionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptVersionManager"/> class.
    /// </summary>
    public ScriptVersionManager()
    {
        // 初始化版本存储字典，用于在内存中保存脚本及其版本列表
        this.versionStore = new ConcurrentDictionary<string, List<ScriptVersion>>();
    }

    /// <summary>
    /// 为指定的脚本创建一个新版本.
    /// </summary>
    /// <param name="script">目标脚本对象.</param>
    /// <param name="currentUser">当前操作用户.</param>
    /// <returns>返回新创建的脚本版本对象.</returns>
    public virtual async Task<ScriptVersion> CreateVersionAsync(ScriptFile script, UserInfo? currentUser)
    {
        // 验证输入参数
        ArgumentNullException.ThrowIfNull(script, nameof(script));
        ArgumentNullException.ThrowIfNull(currentUser, nameof(currentUser));

        string targetScriptName = script.Name;

        // 构建新的脚本版本实体，记录当前脚本状态快照
        var newScriptVersion = new ScriptVersion
        {
            Id = Guid.NewGuid().ToString("N"),
            ScriptName = targetScriptName,
            CreatedAt = DateTime.Now,
            Script = script,
            Author = currentUser.UserName ?? "System",
        };

        // 如果脚本尚未在字典中记录，则为其初始化版本列表
        if (!this.versionStore.TryGetValue(targetScriptName, out List<ScriptVersion>? historyList))
        {
            historyList = new List<ScriptVersion>();
            this.versionStore[targetScriptName] = historyList;
        }

        // 将新版本添加至历史记录列表
        historyList.Add(newScriptVersion);

        return await Task.FromResult(newScriptVersion);
    }

    /// <summary>
    /// 获取指定脚本的特定版本记录.
    /// </summary>
    /// <param name="targetScriptName">脚本名称.</param>
    /// <param name="targetVersionId">版本唯一标识符.</param>
    /// <returns>返回匹配的脚本版本；如果未找到则返回 null.</returns>
    public virtual async Task<ScriptVersion?> GetVersionAsync(string targetScriptName, string targetVersionId)
    {
        // 验证参数非空
        ArgumentNullException.ThrowIfNull(targetScriptName, nameof(targetScriptName));
        ArgumentNullException.ThrowIfNull(targetVersionId, nameof(targetVersionId));

        // 尝试从存储中获取该脚本的所有版本列表
        if (this.versionStore.TryGetValue(targetScriptName, out var historyList))
        {
            // 在列表中查找 ID 匹配的版本
            var matchedVersion = historyList.FirstOrDefault(v => v.Id == targetVersionId);
            return await Task.FromResult(matchedVersion);
        }

        return await Task.FromResult<ScriptVersion?>(null);
    }

    /// <summary>
    /// 获取指定脚本的所有历史版本集合.
    /// </summary>
    /// <param name="targetScriptName">脚本名称.</param>
    /// <returns>返回脚本版本集合；如果无记录则返回空集合.</returns>
    public virtual async Task<IEnumerable<ScriptVersion>> GetHistoryAsync(string targetScriptName)
    {
        // 验证脚本名称
        ArgumentNullException.ThrowIfNull(targetScriptName, nameof(targetScriptName));

        // 如果字典中存在记录则返回副本，否则返回空列表
        if (this.versionStore.TryGetValue(targetScriptName, out var historyList))
        {
            return await Task.FromResult(historyList.AsEnumerable());
        }

        return await Task.FromResult(Enumerable.Empty<ScriptVersion>());
    }

    /// <summary>
    /// 将指定脚本回滚到特定的历史版本（待完善）.
    /// </summary>
    /// <param name="targetScriptName">脚本名称.</param>
    /// <param name="targetVersionId">要回滚到的目标版本标识符.</param>
    /// <param name="currentUser">执行回滚操作的用户.</param>
    /// <returns>表示异步操作的任务.</returns>
    public virtual async Task RevertToVersionAsync(string targetScriptName, string targetVersionId, UserInfo currentUser)
    {
        // 验证回滚参数
        ArgumentNullException.ThrowIfNull(targetScriptName, nameof(targetScriptName));
        ArgumentNullException.ThrowIfNull(targetVersionId, nameof(targetVersionId));
        ArgumentNullException.ThrowIfNull(currentUser, nameof(currentUser));

        // 1. 检索目标版本记录
        var targetVersion = await this.GetVersionAsync(targetScriptName, targetVersionId).ConfigureAwait(false);
        if (targetVersion == null || targetVersion.Script == null)
        {
            // 若版本或关联脚本内容不存在，则中断回滚操作
            return;
        }

        // 2. 执行回滚逻辑：创建一个基于旧版本内容的新版本（类似于 Git 的 Revert 提交）
        // 这样可以保留回滚动作本身的历史轨迹
        await this.CreateVersionAsync(targetVersion.Script, currentUser).ConfigureAwait(false);
    }
}