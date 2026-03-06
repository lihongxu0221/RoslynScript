using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace BgCommon.Script;

class GitHubRepoExporter
{
    public class GitHubTreeItem
    {
        public string path { get; set; }
        public string type { get; set; } // "blob"=文件，"tree"=目录
        public string sha { get; set; }
        public long size { get; set; }
        public string url { get; set; }
    }

    // https://github.com/lihongxu0221/RoslynScript
    private const string GitHubToken = "GitHubToken"; // 比如 ghp_xxxxxx
    private const string Owner = "lihongxu0221"; // 比如 python
    private const string RepoName = "RoslynScript"; // 比如 cpython
    private const string Branch = "main"; // 分支名

    private static readonly HttpClient _httpClient = new HttpClient();
    private static int _processedFileCount = 0; // 已处理文件计数
    private static int _successCount = 0;       // 成功写入文件数
    private static int _failCount = 0;          // 失败文件数
    private static readonly object _fileLock = new object(); // 文件写入锁（避免多线程冲突）

    public static async Task RunAsync(params string[] args)
    {
        // 初始化 HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", GitHubToken);
        _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("C#-HttpClient");

        try
        {
            // 1. 获取仓库完整目录结构
            Console.WriteLine("正在获取仓库结构...");
            var allTreeItems = await GetRepoTree();
            if (allTreeItems == null || allTreeItems.Count == 0)
            {
                Console.WriteLine("未获取到仓库内容！");
                return;
            }

            // 保存结构到文件
            var structureContent = string.Join(Environment.NewLine,
                allTreeItems.ConvertAll(item => $"{(item.type == "tree" ? "📁" : "📄")} {item.path}"));
            System.IO.File.WriteAllText("repo_structure.txt", structureContent);
            Console.WriteLine("仓库结构已保存到 repo_structure.txt");

            var builder = new StringBuilder();
            builder.AppendLine("仓库完整目录结构如下：");
            builder.AppendLine("====================================");
            builder.AppendLine(structureContent);
            builder.AppendLine("====================================");
            builder.AppendLine($"总项数：{allTreeItems.Count}（目录+文件）");
            builder.AppendLine($"目录数：{allTreeItems.FindAll(item => item.type == "tree").Count}");
            builder.AppendLine($"文件数：{allTreeItems.FindAll(item => item.type == "blob").Count}");
            builder.AppendLine("====================================");
            builder.AppendLine("该项目基于net8，prism9.0, DryIoc，使用Roylsn 开发的CSharp脚本编译和运行的简单应用框架");
            builder.AppendLine("需求如下:");
            builder.AppendLine("1. 分析类的作用和关系");
            builder.AppendLine("2. 分析核心类存在的问题");
            builder.AppendLine("3. 给出简单使用的Demo");
            builder.AppendLine("4. 框架还需要补充安全沙盒，脚本调试，脚本安全策略，ScriptRuntime,ScriptVersionManager等功能");
            builder.AppendLine("5. 框架的实现需要更高的智能");
            builder.AppendLine("6. 序列化和反序列化保持使用功能类ConfigurationMgr");
            builder.AppendLine("7. 所有的补充和修改不能脱离已有的功能");
            builder.AppendLine("8. 需要给出一个简单的示例，展示如何使用这个框架来编译和运行一个简单的C#脚本，并且展示如何使用安全沙盒来限制脚本的权限");
            builder.AppendLine("以下为仓库内所有文件的内容");

            string outputFilePath = "GitHub_All_Files.txt";
            File.WriteAllText(outputFilePath, builder.ToString());

            // 3. 筛选出所有文件（排除目录）
            var fileItems = allTreeItems.FindAll(item => item.type == "blob");
            Console.WriteLine($"共发现 {fileItems.Count} 个文件，开始导出...\n");

            // 4. 遍历所有文件，逐个下载并写入到单个文档
            foreach (var fileItem in fileItems)
            {
                _processedFileCount++;
                try
                {
                    await DownloadAndAppendToFile(fileItem, outputFilePath);
                    _successCount++;
                    Console.WriteLine($"[{_processedFileCount}/{fileItems.Count}] 成功：{fileItem.path}");
                }
                catch (Exception ex)
                {
                    _failCount++;
                    Console.WriteLine($"[{_processedFileCount}/{fileItems.Count}] 失败：{fileItem.path} → {ex.Message}");
                    // // 记录失败信息到文档
                    // AppendFailInfoToFile(fileItem.path, ex.Message, outputFilePath);
                }
            }

            // // 5. 写入统计信息到文档末尾
            // AppendSummaryToFile(outputFilePath);

            // 6. 导出完成提示
            Console.WriteLine($"\n===== 导出完成 =====");
            Console.WriteLine($"总文件数：{fileItems.Count}");
            Console.WriteLine($"成功写入：{_successCount} 个");
            Console.WriteLine($"导出失败：{_failCount} 个");
            Console.WriteLine($"最终文件路径：{outputFilePath}");
            Console.WriteLine($"文件大小：{new FileInfo(outputFilePath).Length / 1024 / 1024:F2} MB");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"出错了：{ex.Message}");
        }
    }

    // 获取仓库完整目录树
    static async Task<List<GitHubTreeItem>> GetRepoTree()
    {
        // 获取分支最新 Commit SHA
        var branchUrl = $"https://api.github.com/repos/{Owner}/{RepoName}/branches/{Branch}";
        var branchResponse = await _httpClient.GetStringAsync(branchUrl);
        dynamic branchData = JsonConvert.DeserializeObject(branchResponse);
        string treeSha = branchData.commit.commit.tree.sha;

        // 获取递归目录树（?recursive=1 表示遍历所有子目录）
        var treeUrl = $"https://api.github.com/repos/{Owner}/{RepoName}/git/trees/{treeSha}?recursive=1";
        var treeResponse = await _httpClient.GetStringAsync(treeUrl);
        dynamic treeData = JsonConvert.DeserializeObject(treeResponse);

        return JsonConvert.DeserializeObject<List<GitHubTreeItem>>(treeData.tree.ToString());
    }

    /// <summary>
    /// 下载单个文件并保存到本地（自动创建目录）
    /// </summary>
    static async Task DownloadAndSaveFile(GitHubTreeItem fileItem)
    {
        // 1. 拼接本地完整路径
        string localFilePath = Path.Combine(Environment.ProcessPath, fileItem.path);
        string localDirPath = Path.GetDirectoryName(localFilePath);

        // 2. 自动创建不存在的目录
        if (!Directory.Exists(localDirPath))
        {
            Directory.CreateDirectory(localDirPath);
        }

        // 3. 调用 API 获取文件内容（Base64 编码）
        var fileContentUrl = $"https://api.github.com/repos/{Owner}/{RepoName}/contents/{fileItem.path}?ref={Branch}";
        var fileContentResponse = await _httpClient.GetStringAsync(fileContentUrl);
        dynamic fileContentData = JsonConvert.DeserializeObject(fileContentResponse);

        // 4. Base64 解码并保存文件
        byte[] fileBytes = Convert.FromBase64String(fileContentData.content.ToString());
        await File.WriteAllBytesAsync(localFilePath, fileBytes);
    }

    /// <summary>
    /// 下载单个文件并追加到总文档
    /// </summary>
    static async Task DownloadAndAppendToFile(GitHubTreeItem fileItem, string outputFilePath)
    {
        // 1. 调用 API 获取文件内容（Base64 编码）
        var fileContentUrl = $"https://api.github.com/repos/{Owner}/{RepoName}/contents/{fileItem.path}?ref={Branch}";
        var fileContentResponse = await _httpClient.GetStringAsync(fileContentUrl);
        dynamic fileContentData = JsonConvert.DeserializeObject(fileContentResponse);

        // 2. Base64 解码文件内容
        byte[] fileBytes = Convert.FromBase64String(fileContentData.content.ToString());
        string fileContent = Encoding.UTF8.GetString(fileBytes);

        // // 3. 拼接文件头部 + 内容 + 分隔符
        // StringBuilder contentBuilder = new StringBuilder();
        // contentBuilder.AppendLine($"========== 文件开始：{fileItem.path} ==========");
        // contentBuilder.AppendLine($"文件大小：{fileItem.size} 字节");
        // contentBuilder.AppendLine($"SHA 值：{fileItem.sha}");
        // contentBuilder.AppendLine("-------------------------------------------");
        // contentBuilder.AppendLine(fileContent);
        // contentBuilder.AppendLine($"========== 文件结束：{fileItem.path} ==========\n\n");

        // 4. 追加到总文档（加锁避免多线程写入冲突）
        lock (_fileLock)
        {
            // File.AppendAllText(outputFilePath, contentBuilder.ToString(), Encoding.UTF8);
            File.AppendAllText(outputFilePath, fileContent, Encoding.UTF8);
        }
    }

    /// <summary>
    /// 追加失败信息到文档
    /// </summary>
    static void AppendFailInfoToFile(string outputFilePath, string filePath, string errorMsg)
    {
        lock (_fileLock)
        {
            File.AppendAllText(outputFilePath,
                $"========== 文件失败：{filePath} ==========\n" +
                $"错误信息：{errorMsg}\n\n",
                Encoding.UTF8);
        }
    }

    /// <summary>
    /// 追加统计信息到文档末尾
    /// </summary>
    static void AppendSummaryToFile(string outputFilePath)
    {
        lock (_fileLock)
        {
            File.AppendAllText(outputFilePath,
                $"===== 导出统计 =====\n" +
                $"总文件数：{_processedFileCount}\n" +
                $"成功写入：{_successCount}\n" +
                $"导出失败：{_failCount}\n" +
                $"完成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"====================",
                Encoding.UTF8);
        }
    }
}