namespace Test;

public class Inventory
{
    private const int MAX_WEIGHT = 100;

    private readonly object _sync = new();
    private readonly List<Item> _items = new();
    private int _currentWeight;

    public IReadOnlyList<Item> Items
    {
        get
        {
            lock (_sync)
            {
                return _items
                    .Select(i => new Item { Name = i.Name, Weight = i.Weight })
                    .ToArray();
            }
        }
    }

    public int CurrentWeight
    {
        get
        {
            lock (_sync)
            {
                return _currentWeight;
            }
        }
    }

    public bool TryAddItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Name);

        lock (_sync)
        {
            var existing = _items.FirstOrDefault(
                i => string.Equals(i.Name, item.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (ExceedsMaxWeight(item.Weight))
                    return false;

                existing.Weight += item.Weight;
                _currentWeight += item.Weight;
                return true;
            }

            if (ExceedsMaxWeight(item.Weight))
                return false;

            _items.Add(new Item
            {
                Name = item.Name,
                Weight = item.Weight
            });

            _currentWeight += item.Weight;
            return true;
        }
    }

    private bool ExceedsMaxWeight(int addedWeight)
        => _currentWeight + addedWeight > MAX_WEIGHT;


    public bool RemoveItem(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_sync)
        {
            var item = _items.FirstOrDefault(
                i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));

            if (item == null)
                return false;

            _items.Remove(item);
            _currentWeight -= item.Weight;
            return true;
        }
    }

    public IReadOnlyList<Item> FindItems(string namePart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namePart);

        lock (_sync)
        {
            return _items
                .Where(i => i.Name.Contains(namePart, StringComparison.OrdinalIgnoreCase))
                .Select(i => new Item
                {
                    Name = i.Name,
                    Weight = i.Weight
                })
                .ToArray();
        }
    }
}
