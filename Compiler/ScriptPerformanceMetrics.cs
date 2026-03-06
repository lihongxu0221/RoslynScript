namespace BgCommon.Script.Compiler;

/// <summary>
/// 表示在脚本编译和执行期间收集的一组性能指标.
/// </summary>
/// <remarks>
/// 使用此类分析和监控脚本性能特征，如编译时间、执行时间、内存使用量以及每个方法的执行持续时间.
/// 这些信息有助于识别性能瓶颈并优化脚本行为.
/// </remarks>
public class ScriptPerformanceMetrics : ObservableObject
{
    private TimeSpan compilationTime;
    private TimeSpan executionTime;
    private long totalExecutionTime;
    private long memoryUsed;
    private int assemblySize;
    private Dictionary<string, TimeSpan> methodExecutionTimes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptPerformanceMetrics"/> class.
    /// </summary>
    public ScriptPerformanceMetrics()
    {
        this.methodExecutionTimes = new Dictionary<string, TimeSpan>();
    }

    /// <summary>
    /// Gets or sets 脚本编译所消耗的时间.
    /// </summary>
    public TimeSpan CompilationTime
    {
        get => this.compilationTime;
        set => SetProperty(ref compilationTime, value);
    }

    /// <summary>
    /// Gets or sets 脚本执行所消耗的时间.
    /// </summary>
    public TimeSpan ExecutionTime
    {
        get => this.executionTime;
        set => SetProperty(ref executionTime, value);
    }

    /// <summary>
    /// Gets or sets 总执行时间的刻度或毫秒值.
    /// </summary>
    public long TotalExecutionTime
    {
        get => this.totalExecutionTime;
        set => SetProperty(ref totalExecutionTime, value);
    }

    /// <summary>
    /// Gets or sets 脚本执行期间使用的内存量（字节）.
    /// </summary>
    public long MemoryUsed
    {
        get => this.memoryUsed;
        set => SetProperty(ref memoryUsed, value);
    }

    /// <summary>
    /// Gets or sets 生成的程序集大小（字节）.
    /// </summary>
    public int AssemblySize
    {
        get => this.assemblySize;
        set => SetProperty(ref assemblySize, value);
    }

    /// <summary>
    /// Gets or sets 每个具体方法的执行时间映射表.
    /// </summary>
    public Dictionary<string, TimeSpan> MethodExecutionTimes
    {
        get => this.methodExecutionTimes;
        set => SetProperty(ref methodExecutionTimes, value);
    }
}