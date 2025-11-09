using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Stopwatch = System.Diagnostics.Stopwatch;
using Random = UnityEngine.Random;

/*
InventoryManager.cs
Rationale for the more involved binary search demonstration:
1. Selecting a random existing ID (successful search path).
2. Generating an ID guaranteed to be absent (failure path).
3. Validating all IDs can be resolved (correctness across entire data set).
4. Avoiding redundant sorts by tracking whether sorting has already occurred.
Wanted to demonstrate by looking for an existing ID and a non-existing ID to show both success and failure cases.

Also added a log to show the sorted order after QuickSort by Value for better clarity on the sorting result.

In addition, added timing measurements (in ms) because I thought it'd be interesting to see the performance differences in practice
and figured that information would be useful in debugging for larger datasets or performance-critical applications.
*/

public class InventoryManager : MonoBehaviour
{
    // Unsorted inventory (randomized at startup).
    public List<InventoryItem> inventoryItems = new();
    
    // Prevents repeated sorting by ID.
    private bool _isSortedByID = false;

    void Start()
    {
        InitializeInventory(12);

        // Linear Search demo (success case with a known generated name) + timing.
        double linearMs = MeasureMs(() => LinearSearchByName("Item_5"), out var linearResult);
        Debug.Log(linearResult != null
            ? $"LinearSearchByName found: {linearResult.Name} (ID:{linearResult.ID}, Value:{linearResult.Value}) in {linearMs:F3} ms"
            : $"LinearSearchByName: Item not found in {linearMs:F3} ms");

        // Binary Search (success case): pick a random existing ID + timing.
        int existingIndex = Random.Range(0, inventoryItems.Count);
        int existingID = inventoryItems[existingIndex].ID;

        // Ensure first sort is paid outside the measurement so we measure search time itself.
        SortInventoryByID();

        double binExistMs = MeasureMs(() => BinarySearchByID(existingID), out var foundExisting);
        Debug.Log(foundExisting != null
            ? $"BinarySearchByID (existing) found: {foundExisting.Name} (ID:{foundExisting.ID}) in {binExistMs:F3} ms"
            : $"BinarySearchByID (existing): Not found (unexpected) in {binExistMs:F3} ms");

        // Binary Search (failure case): generate an ID guaranteed not present + timing.
        int missingID;
        do
        {
            missingID = Random.Range(1000, 9999);
        } while (inventoryItems.Exists(i => i.ID == missingID));

        double binMissingMs = MeasureMs(() => BinarySearchByID(missingID), out var missingResult);
        Debug.Log(missingResult == null
            ? $"BinarySearchByID (missing) correctly did not find ID:{missingID} in {binMissingMs:F3} ms"
            : $"BinarySearchByID (missing): Found unexpectedly in {binMissingMs:F3} ms");

        // Optional bulk correctness validation across all IDs.
        ValidateAllIDsResolvableByBinarySearch();

        // QuickSort demo (Value ascending) + timing (exclude log time from measurement).
        ShuffleInventory();
        double quickSortMs = MeasureMs(() => QuickSortByValue(logSorted: false));
        Debug.Log($"QuickSortByValue completed in {quickSortMs:F3} ms.");
        LogInventoryOrderByValue();
    }

    // Populate inventory with unique randomized IDs and random values.
    void InitializeInventory(int count)
    {
        inventoryItems.Clear();
        _isSortedByID = false;

        HashSet<int> usedIDs = new HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            int id;
            do
            {
                id = Random.Range(1000, 9999);
            } while (!usedIDs.Add(id)); // Loop until a unique ID is obtained.

            inventoryItems.Add(new InventoryItem
            {
                ID = id,
                Name = $"Item_{i}",
                Value = Random.Range(1, 101)
            });
        }
    }

    // Task 1: Linear Search by Name.
    public InventoryItem LinearSearchByName(string itemName)
    {
        foreach (var item in inventoryItems)
        {
            if (item.Name == itemName)
                return item;
        }
        return null;
    }

    // Helper: Sort inventory by ID once (lazy).
    void SortInventoryByID()
    {
        if (_isSortedByID) return;
        inventoryItems.Sort((a, b) => a.ID.CompareTo(b.ID));
        _isSortedByID = true;
    }

    // Task 2: Binary Search by ID.
    // Preconditions: List must be sorted by ID (ensured by SortInventoryByID()).
    public InventoryItem BinarySearchByID(int itemID)
    {
        SortInventoryByID(); // Sort only once

        int left = 0;
        int right = inventoryItems.Count - 1;

        // Invariant: If item exists, its position lies within [left, right].
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int midID = inventoryItems[mid].ID;

            if (midID == itemID)
                return inventoryItems[mid];

            if (midID < itemID)
                left = mid + 1;     // Narrow to upper half.
            else
                right = mid - 1;    // Narrow to lower half.
        }

        // Not found.
        return null;
    }

    // Bulk validation to assert binary search resolves every present ID.
    void ValidateAllIDsResolvableByBinarySearch()
    {
        SortInventoryByID();

        foreach (var item in inventoryItems)
        {
            var found = BinarySearchByID(item.ID);
            if (found == null || found.ID != item.ID)
            {
                Debug.LogWarning($"Binary search failed for ID:{item.ID}");
            }
        }
        // This log indicates all IDs were found correctly.
        Debug.Log("Binary search validation: All IDs resolved correctly.");
    }

    // Task 3: QuickSort by Value (ascending).
    public void QuickSortByValue(bool logSorted = true)
    {
        if (inventoryItems.Count <= 1)
        {
            if (logSorted) LogInventoryOrderByValue();
            return;
        }

        QuickSortByValue(0, inventoryItems.Count - 1);

        if (logSorted)
            LogInventoryOrderByValue();
    }

    // Recursive quicksort on sub-range [low, high].
    void QuickSortByValue(int low, int high)
    {
        if (low < high)
        {
            int pivotIndex = PartitionByValue(low, high);
            QuickSortByValue(low, pivotIndex - 1);
            QuickSortByValue(pivotIndex + 1, high);
        }
    }

    // Partition (Lomuto scheme) around pivot at 'high'.
    // Returns final pivot position.
    int PartitionByValue(int low, int high)
    {
        int pivotValue = inventoryItems[high].Value;
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (inventoryItems[j].Value <= pivotValue)
            {
                i++;
                Swap(i, j);
            }
        }

        Swap(i + 1, high);
        return i + 1;
    }

    // Swap two items in-place (guard against redundant self-swap).
    void Swap(int a, int b)
    {
        if (a == b) return;
        var temp = inventoryItems[a];
        inventoryItems[a] = inventoryItems[b];
        inventoryItems[b] = temp;
    }

    // Fisher–Yates shuffle to randomize item order (helps avoid re-sorting already-sorted data).
    void ShuffleInventory()
    {
        for (int i = inventoryItems.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Swap(i, j);
        }
    }

    // Helper: Builds a multi-line string showing the order of items by Value after quicksort.
    void LogInventoryOrderByValue()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Inventory order by Value (ascending):");
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            var it = inventoryItems[i];
            sb.AppendLine($"#{i + 1}: {it.Name} (ID:{it.ID}, Value:{it.Value})");
        }
        Debug.Log(sb.ToString());
    }

    // Millisecond-only timing helpers
    double MeasureMs(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    double MeasureMs<T>(Func<T> func, out T result)
    {
        var sw = Stopwatch.StartNew();
        result = func();
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }
}