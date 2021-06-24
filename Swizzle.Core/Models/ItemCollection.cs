using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Swizzle.Models
{
    public readonly struct ItemCollection : IReadOnlyList<Item>
    {
        readonly ImmutableList<Item> _items;

        public string Key { get; }
        public int Generation { get; }

        public ItemCollection(string key)
            : this(
                ImmutableList<Item>.Empty,
                key,
                0)
        {
        }

        ItemCollection(
            ImmutableList<Item> items,
            string key,
            int generation)
        {
            _items = items;
            Key = key;
            Generation = generation;
        }

        public ItemCollection Add(Item item)
            => new(
                _items.Add(item),
                Key,
                Generation + 1);

        public ItemCollection Replace(Item oldItem, Item newItem)
            => new(
                _items.Replace(oldItem, newItem),
                Key,
                Generation + 1);

        public Item this[int index] => _items[index];
        public int Count => _items.Count;

        public bool TryGetItemBySlug(string slug, out Item item)
        {
            if (string.IsNullOrEmpty(slug))
            {
                #nullable disable
                item = null;
                #nullable restore
                return false;
            }

            #nullable disable
            item = _items.Find(item => item.Slug == slug);
            #nullable restore
            return item is not null;
        }

        public IEnumerator<Item> GetEnumerator()
            => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _items.GetEnumerator();
    }
}
