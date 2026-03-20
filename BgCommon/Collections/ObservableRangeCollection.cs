namespace BgCommon.Collections;

/// <summary>
/// 支持批量操作并可抑制通知的可观察集合.
/// </summary>
/// <typeparam name="T">集合内数据类型.</typeparam>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// 是否抑制集合变更通知的标志.
    /// </summary>
    private bool suppressNotification;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableRangeCollection{T}"/> class.
    /// </summary>
    public ObservableRangeCollection()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableRangeCollection{T}"/> class.
    /// </summary>
    /// <param name="collection">要复制元素的集合.</param>
    public ObservableRangeCollection(IEnumerable<T> collection)
        : base(collection)
    {
    }

    /// <summary>
    /// 集合变更时触发通知.
    /// </summary>
    /// <param name="e">变更事件参数.</param>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }

    /// <summary>
    /// 属性变更时触发通知.
    /// </summary>
    /// <param name="e">属性变更事件参数.</param>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!suppressNotification)
        {
            base.OnPropertyChanged(e);
        }
    }

    /// <summary>
    /// 批量添加元素.
    /// </summary>
    /// <param name="index">要插入的起始索引.</param>
    /// <param name="items">要添加的元素集合.</param>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        // 如果传入集合为 null，直接返回
        if (items == null)
        {
            return;
        }

        // 将集合转为列表以避免多次枚举
        var itemList = (items as ICollection<T>) ?? items.ToList();
        if (itemList.Count == 0)
        {
            return;
        }

        suppressNotification = true;

        // 批量插入元素
        foreach (T item in itemList)
        {
            Insert(index++, item);
        }

        suppressNotification = false;

        // 触发一次重置通知，告知UI整个集合已变更
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// 批量添加元素.
    /// </summary>
    /// <param name="list">要添加的元素集合.</param>
    public void AddRange(IEnumerable<T> list)
    {
        // 如果传入集合为 null，直接返回
        if (list == null)
        {
            return;
        }

        // 将集合转为列表以避免多次枚举
        var items = list as ICollection<T> ?? list.ToList();
        if (items.Count == 0)
        {
            return;
        }

        suppressNotification = true;

        // 批量添加元素
        foreach (T item in items)
        {
            Add(item);
        }

        suppressNotification = false;

        // 触发一次重置通知，告知UI整个集合已变更
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// 批量移除指定范围的元素，并触发一次重置通知.
    /// </summary>
    /// <param name="startIndex">起始索引.</param>
    /// <param name="length">要移除的元素数量.</param>
    public void RemoveRange(int startIndex, int length)
    {
        // 参数校验
        if (startIndex < 0 || startIndex >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "起始索引超出范围.");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "长度不能为负数.");
        }

        // 调整长度以避免越界
        if (startIndex + length > Count)
        {
            length = Count - startIndex;
        }

        if (length == 0)
        {
            return; // 没有元素可移除
        }

        suppressNotification = true;

        // 逆序移除，避免索引偏移
        for (int i = length - 1; i >= 0; i--)
        {
            RemoveAt(startIndex + i);
        }

        suppressNotification = false;

        // 触发一次重置通知，告知UI集合已变更
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// 高效地清除当前集合，并用新集合替换其内容.
    /// </summary>
    /// <param name="range">要替换当前内容的新元素集合.</param>
    public void ReplaceRange(IEnumerable<T> range)
    {
        try
        {
            // 确保此操作在UI线程上是安全的
            CheckReentrancy();

            // 直接操作内部的 Items 列表，这是 List<T> 类型
            suppressNotification = true;

            Clear();
            AddRange(range);
            suppressNotification = false;

            // 触发一次重置通知，告诉UI整个集合已更改
            // 这是最高效的刷新方式
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        finally
        {
            suppressNotification = false;
        }
    }

    /// <summary>
    /// 抑制集合变更通知，返回 IDisposable，释放时恢复通知并触发重置.
    /// </summary>
    /// <returns>IDisposable 对象.</returns>
    public IDisposable SuppressNotifications()
    {
        suppressNotification = true;
        return new SuppressNotificationsDisposable(this);
    }

    /// <summary>
    /// 用于自动恢复通知的 IDisposable 实现.
    /// </summary>
    private class SuppressNotificationsDisposable : IDisposable
    {
        /// <summary>
        /// 关联的集合.
        /// </summary>
        private readonly ObservableRangeCollection<T> collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuppressNotificationsDisposable"/> class.
        /// </summary>
        /// <param name="collection">目标集合.</param>
        public SuppressNotificationsDisposable(ObservableRangeCollection<T> collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// 释放时恢复通知并触发重置.
        /// </summary>
        public void Dispose()
        {
            collection.suppressNotification = false;
            collection.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}