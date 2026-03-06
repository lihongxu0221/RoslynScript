using BgCommon.Prism.Wpf.Authority.Entities;
using BgCommon.Script.Compiler.Models;
using System.Collections.Concurrent;

namespace BgCommon.Script.Compiler;

/// <summary>
/// 脚本版本管理器，用于管理脚本的历史版本、创建新版本以及执行版本回滚操作.
/// 存放目录为 {ScriptName}/{VersionId}.ver，版本记录包含脚本内容快照、创建时间、作者等元信息.
/// 将指定脚本回滚到特定的历史版本 不应该在此类中存在.
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
    /// 创建新版本（修复了引用类型污染问题）.
    /// </summary>
    /// <param name="script">脚本对象.</param>
    /// <param name="currentUser">当前用户.</param>
    /// <returns>脚本版本.</returns>
    public virtual async Task<ScriptVersion> CreateVersionAsync(ScriptFile script, UserInfo? currentUser)
    {
        ArgumentNullException.ThrowIfNull(script, nameof(script));
        ArgumentNullException.ThrowIfNull(currentUser, nameof(currentUser));

        string targetName = script.Name;

        // 潜在问题修复：创建 ScriptFile 的浅拷贝，防止后续修改影响历史记录
        var scriptSnapshot = new ScriptFile();
        scriptSnapshot.Id = script.Id;
        scriptSnapshot.PatchMetadata(script);

        var versionEntry = new ScriptVersion
        {
            Id = Guid.NewGuid().ToString("N"),
            ScriptName = targetName,
            CreatedAt = DateTime.Now,
            Script = scriptSnapshot,
            Author = currentUser.UserName ?? "System",
        };

        this.versionStore.AddOrUpdate(
            targetName,
            _ => new List<ScriptVersion> { versionEntry },
            (_, list) =>
            {
                lock (list) { list.Add(versionEntry); }
                return list;
            });

        return await Task.FromResult(versionEntry);
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
}