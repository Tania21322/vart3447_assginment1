// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

[Preserve]
[JsonConverter(typeof(DeserializableListConverter))]
public class DeserializableList<T> : IList<T>
{
    [Preserve]
    public DeserializableList()
    {
        Data = new List<T>();
        Paging = new PagingJSON();
        Summary = new SummaryJSON();
    }

    [JsonProperty("data")]
    [Preserve]
    public List<T> Data { get; set; }

    [JsonProperty("paging")]
    [Preserve]
    public PagingJSON Paging { get; set; }

    [JsonProperty("summary")]
    [Preserve]
    public SummaryJSON Summary { get; set; }


    // Implements IList<T> so callers can use LINQ (.Where/.Select/.ToList/...) and pass
    // these lists where an IList<T>/IEnumerable<T> is expected, matching the legacy SDK and
    // avoiding migration churn. Implementing IEnumerable<T> would normally make Newtonsoft
    // treat the type as a JSON array and break the {data,paging,summary} object response, but
    // the class-level [JsonConverter(typeof(DeserializableListConverter))] fully controls
    // (de)serialization and overrides that default contract, so the interface is safe here.
    public IEnumerator<T> GetEnumerator()
    {
        if (Data == null)
        {
            UnityEngine.Debug.LogError("[DeserializableList] Data is NULL in GetEnumerator!");
            Data = new List<T>();
        }
        return Data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public T this[int index]
    {
        get
        {
            if (Data == null)
            {
                UnityEngine.Debug.LogError("[DeserializableList] Data is NULL in indexer!");
                Data = new List<T>();
            }
            return Data[index];
        }
        set
        {
            if (Data == null)
            {
                UnityEngine.Debug.LogError("[DeserializableList] Data is NULL in indexer setter!");
                Data = new List<T>();
            }
            Data[index] = value;
        }
    }

    public int Count
    {
        get
        {
            if (Data == null)
            {
                UnityEngine.Debug.LogError("[DeserializableList] Data is NULL in Count!");
                Data = new List<T>();
            }
            return Data.Count;
        }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    public int IndexOf(T item)
    {
        return Data.IndexOf(item);
    }

    public void Add(T item)
    {
        Data.Add(item);
    }

    public void Clear()
    {
        Data.Clear();
    }

    public bool Contains(T item)
    {
        return Data.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Data.CopyTo(array, arrayIndex);
    }

    public void Insert(int index, T item)
    {
        Data.Insert(index, item);
    }

    public bool Remove(T item)
    {
        return Data.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Data.RemoveAt(index);
    }

    public bool HasNextPage
    {
        get { return !System.String.IsNullOrEmpty(Paging?.next); }
    }

    public bool HasPreviousPage
    {
        get { return !System.String.IsNullOrEmpty(Paging?.previous); }
    }

    public string NextUrl
    {
        get { return Paging?.next; }
    }

    public string PreviousUrl
    {
        get { return Paging?.previous; }
    }

    /// <summary>
    /// The total count of entries across all pages, from the summary field.
    /// </summary>
    public ulong TotalCount
    {
        get { return Summary?.total_count ?? 0; }
    }
}

[Preserve]
public class PagingJSON
{
    public string next;
    public string previous;
    public CursorsJson cursors;
}

[Preserve]
public class SummaryJSON
{
    public ulong total_count;
}

[Preserve]
public class CursorsJson
{
    public string before;
    public string after;
}
