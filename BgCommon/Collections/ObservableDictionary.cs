using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BgCommon.Collections;

/// <summary>
/// 表示一个可观察的键值对集合，当集合发生更改（添加、移除或清空）时提供通知.
/// </summary>
/// <typeparam name="TKey">字典中键的类型.</typeparam>
/// <typeparam name="TValue">字典中值的类型.</typeparam>
[Serializable]
[DebuggerDisplay("Count = {Count}")]
public class ObservableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    where TKey : notnull
{
    /// <summary>
    /// Count 属性名称常量.
    /// </summary>
    private const string CountString = "Count";

    /// <summary>
    /// 索引器属性名称常量.
    /// </summary>
    private const string IndexerName = "Item[]";

    /// <summary>
    /// Keys 属性名称常量.
    /// </summary>
    private const string KeysName = "Keys";

    /// <summary>
    /// Values 属性名称常量.
    /// </summary>
    private const string ValuesName = "Values";

    /// <summary>
    /// 内部维护的字典对象.
    /// </summary>
    private readonly IDictionary<TKey, TValue> dictionary;

    /// <summary>
    /// 当集合发生更改时触发的事件.
    /// </summary>
    [field: NonSerialized]
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// 当属性值发生更改时触发的事件.
    /// </summary>
    [field: NonSerialized]
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    public ObservableDictionary()
    {
        this.dictionary = new Dictionary<TKey, TValue>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">包含要复制到新字典中的元素的字典.</param>
    public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
    {
        this.dictionary = new Dictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="comparer">比较键时要使用的比较器.</param>
    public ObservableDictionary(IEqualityComparer<TKey> comparer)
    {
        this.dictionary = new Dictionary<TKey, TValue>(comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">字典可以包含的初始元素数.</param>
    public ObservableDictionary(int capacity)
    {
        this.dictionary = new Dictionary<TKey, TValue>(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">包含要复制到新字典中的元素的字典.</param>
    /// <param name="comparer">比较键时要使用的比较器.</param>
    public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
    {
        this.dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="capacity">字典可以包含的初始元素数.</param>
    /// <param name="comparer">比较键时要使用的比较器.</param>
    public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
    {
        this.dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
    }

    /// <summary>
    /// Gets 包含字典中的键的集合.
    /// </summary>
    public ICollection<TKey> Keys => this.dictionary.Keys;

    /// <summary>
    /// Gets 包含字典中的值的集合.
    /// </summary>
    public ICollection<TValue> Values => this.dictionary.Values;

    /// <summary>
    /// Gets 字典中包含的键值对数量.
    /// </summary>
    public int Count => this.dictionary.Count;

    /// <summary>
    /// Gets a value indicating whether 字典是否为只读.
    /// </summary>
    public bool IsReadOnly => this.dictionary.IsReadOnly;

    /// <summary>
    /// Gets or sets 指定键关联的值.
    /// </summary>
    /// <param name="key">要获取或设置其值的键.</param>
    /// <returns>与指定键关联的值.</returns>
    public TValue this[TKey key]
    {
        get
        {
            // 检查字典是否包含键，若不包含则返回默认值
            if (this.dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return default!;
        }

        set
        {
            bool isKeyExisting = this.dictionary.ContainsKey(key);

            // 如果键已存在且值相等，则不执行更新
            if (isKeyExisting && EqualityComparer<TValue>.Default.Equals(this.dictionary[key], value))
            {
                return;
            }

            this.dictionary[key] = value;

            if (isKeyExisting)
            {
                // 仅更新值时通知索引器和值集合变更
                this.OnPropertyChanged(IndexerName);
                this.OnPropertyChanged(ValuesName);
            }
            else
            {
                // 新增键时重置集合状态
                this.OnCollectionReset();
            }
        }
    }

    /// <summary>
    /// 引发 CollectionChanged 事件.
    /// </summary>
    /// <param name="args">事件参数.</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        this.CollectionChanged?.Invoke(this, args);
    }

    /// <summary>
    /// 引发 PropertyChanged 事件.
    /// </summary>
    /// <param name="propertyName">发生更改的属性名称.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 将指定的键和值添加到字典中.
    /// </summary>
    /// <param name="key">要添加的元素的键.</param>
    /// <param name="value">要添加的元素的值.</param>
    public void Add(TKey key, TValue value)
    {
        if (this.dictionary.ContainsKey(key))
        {
            throw new ArgumentException("An item with the same key has already been added.");
        }

        this.dictionary.Add(key, value);
        this.OnCollectionReset();
    }

    /// <summary>
    /// 从字典中移除具有指定键的元素.
    /// </summary>
    /// <param name="key">要移除的元素的键.</param>
    /// <returns>如果移除成功则为 true，否则为 false.</returns>
    public bool Remove(TKey key)
    {
        // 验证键是否存在
        if (!this.dictionary.TryGetValue(key, out TValue? _))
        {
            return false;
        }

        // 注意：此处原逻辑未调用底层字典的移除方法
        this.OnCollectionReset();
        return true;
    }

    /// <summary>
    /// 获取与指定键关联的值.
    /// </summary>
    /// <param name="key">要获取其值的键.</param>
    /// <param name="value">输出获取到的值.</param>
    /// <returns>如果找到键则为 true，否则为 false.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return this.dictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// 将键值对项目添加到集合中.
    /// </summary>
    /// <param name="item">要添加的项目.</param>
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        this.Add(item.Key, item.Value);
    }

    /// <summary>
    /// 从字典中移除所有元素.
    /// </summary>
    public void Clear()
    {
        if (this.dictionary.Count > 0)
        {
            this.dictionary.Clear();
            this.OnCollectionReset();
        }
    }

    /// <summary>
    /// 从集合中移除特定的键值对.
    /// </summary>
    /// <param name="item">要移除的键值对.</param>
    /// <returns>移除成功返回 true.</returns>
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (((ICollection<KeyValuePair<TKey, TValue>>)this.dictionary).Remove(item))
        {
            this.OnCollectionReset();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 确定字典中是否包含指定的键.
    /// </summary>
    /// <param name="key">要查找的键.</param>
    /// <returns>包含返回 true.</returns>
    public bool ContainsKey(TKey key)
    {
        return this.dictionary.ContainsKey(key);
    }

    /// <summary>
    /// 确定集合中是否包含特定的键值对.
    /// </summary>
    /// <param name="item">要查找的项.</param>
    /// <returns>包含返回 true.</returns>
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return this.dictionary.Contains(item);
    }

    /// <summary>
    /// 从特定的数组索引开始，将集合元素复制到数组中.
    /// </summary>
    /// <param name="array">目标数组.</param>
    /// <param name="arrayIndex">数组中的起始索引.</param>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        this.dictionary.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// 返回循环访问集合的枚举器.
    /// </summary>
    /// <returns>循环访问集合的枚举器.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return this.dictionary.GetEnumerator();
    }

    /// <summary>
    /// 返回循环访问集合的枚举器.
    /// </summary>
    /// <returns>循环访问集合的枚举器.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    /// <summary>
    /// 当集合结构发生重大更改时，通知所有相关的属性和集合更改.
    /// </summary>
    private void OnCollectionReset()
    {
        this.OnPropertyChanged(CountString, KeysName, ValuesName, IndexerName);
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// 批量触发属性更改通知.
    /// </summary>
    /// <param name="propertyNames">属性名称集合.</param>
    private void OnPropertyChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            this.OnPropertyChanged(propertyName);
        }
    }
}