namespace Test.UnitTests
{
    public class InventoryTests
    {
        [Fact]
        public void TryAddItem_NewItem_AddsAndIncreasesWeight()
        {
            // Arrange
            var inv = new Inventory();
            var item = new Item { Name = "Sword", Weight = 30 };

            // Act
            var ok = inv.TryAddItem(item);

            // Assert
            Assert.True(ok);
            Assert.Single(inv.Items);
            Assert.Equal("Sword", inv.Items[0].Name);
            Assert.Equal(30, inv.Items[0].Weight);
            Assert.Equal(30, inv.CurrentWeight);
        }

        [Fact]
        public void TryAddItem_DuplicateName_MergesWeight_NoDuplicateCreated()
        {
            // Arrange
            var inv = new Inventory();
            Assert.True(inv.TryAddItem(new Item { Name = "Apple", Weight = 10 }));

            // Act
            var ok = inv.TryAddItem(new Item { Name = "apple", Weight = 15 }); 

            // Assert
            Assert.True(ok);
            Assert.Single(inv.Items);
            Assert.Equal("Apple", inv.Items[0].Name);
            Assert.Equal(25, inv.Items[0].Weight);
            Assert.Equal(25, inv.CurrentWeight);
        }

        [Fact]
        public void TryAddItem_OverMaxWeight_ReturnsFalse_DoesNotChangeState()
        {
            // Arrange
            var inv = new Inventory();
            Assert.True(inv.TryAddItem(new Item { Name = "A", Weight = 90 }));

            // Act
            var ok = inv.TryAddItem(new Item { Name = "B", Weight = 11 }); // 90 + 11 > 100

            // Assert
            Assert.False(ok);
            Assert.Single(inv.Items);
            Assert.Equal(90, inv.CurrentWeight);
        }

        [Fact]
        public void TryAddItem_DuplicateWouldOverflow_ReturnsFalse_DoesNotMerge()
        {
            // Arrange
            var inv = new Inventory();
            Assert.True(inv.TryAddItem(new Item { Name = "Apple", Weight = 80 }));

            // Act
            var ok = inv.TryAddItem(new Item { Name = "Apple", Weight = 25 }); // 80 + 25 > 100

            // Assert
            Assert.False(ok);
            Assert.Single(inv.Items);
            Assert.Equal(80, inv.Items[0].Weight);
            Assert.Equal(80, inv.CurrentWeight);
        }

        [Fact]
        public void RemoveItem_RemovesExistingByName_ReturnsTrue_UpdatesWeight()
        {
            // Arrange
            var inv = new Inventory();
            Assert.True(inv.TryAddItem(new Item { Name = "Sword", Weight = 30 }));
            Assert.True(inv.TryAddItem(new Item { Name = "Shield", Weight = 20 }));

            // Act
            var ok = inv.RemoveItem("sword");

            // Assert
            Assert.True(ok);
            Assert.Single(inv.Items);
            Assert.Equal("Shield", inv.Items[0].Name);
            Assert.Equal(20, inv.CurrentWeight);
        }

        [Fact]
        public void RemoveItem_NotFound_ReturnsFalse_StateUnchanged()
        {
            // Arrange
            var inv = new Inventory();
            Assert.True(inv.TryAddItem(new Item { Name = "Sword", Weight = 30 }));

            // Act
            var ok = inv.RemoveItem("Bow");

            // Assert
            Assert.False(ok);
            Assert.Single(inv.Items);
            Assert.Equal(30, inv.CurrentWeight);
        }

        [Fact]
        public void FindItems_BySubstring_ReturnsMatchingItems_CaseInsensitive()
        {
            // Arrange
            var inv = new Inventory();
            Assert.True(inv.TryAddItem(new Item { Name = "Iron Sword", Weight = 30 }));
            Assert.True(inv.TryAddItem(new Item { Name = "Wooden Shield", Weight = 20 }));
            Assert.True(inv.TryAddItem(new Item { Name = "Iron Helmet", Weight = 10 }));

            // Act
            var found = inv.FindItems("iRoN");

            // Assert
            Assert.Equal(2, found.Count);
            var names = found.Select(x => x.Name).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { "Iron Helmet", "Iron Sword" }, names);
        }

        [Fact]
        public void Items_ReturnsCopies_ExternalMutationDoesNotAffectInventory()
        {
            // Arrange
            var inv = new Inventory();
            Assert.True(inv.TryAddItem(new Item { Name = "Apple", Weight = 10 }));

            // Act
            var snapshot = inv.Items;
            snapshot[0].Weight = 999; // пытаемся испортить внешний объект

            // Assert
            var actual = inv.Items.Single();
            Assert.Equal(10, actual.Weight); // внутри не изменилось
            Assert.Equal(10, inv.CurrentWeight);
        }

        [Fact]
        public void TryAddItem_Null_Throws()
        {
            var inv = new Inventory();
            Assert.Throws<ArgumentNullException>(() => inv.TryAddItem(null!));
        }

        [Fact]
        public void TryAddItem_EmptyName_Throws()
        {
            var inv = new Inventory();
            Assert.Throws<ArgumentException>(() => inv.TryAddItem(new Item { Name = "  ", Weight = 1 }));
        }

        [Fact]
        public void RemoveItem_EmptyName_Throws()
        {
            var inv = new Inventory();
            Assert.Throws<ArgumentException>(() => inv.RemoveItem(" "));
        }

        [Fact]
        public void FindItems_Empty_Throws()
        {
            var inv = new Inventory();
            Assert.Throws<ArgumentException>(() => inv.FindItems(" "));
        }

        [Fact]
        public async Task Concurrent_AddSameName_DoesNotCreateDuplicates_AndDoesNotExceedMax()
        {
            // Arrange
            var inv = new Inventory();

            // 20 параллельных попыток добавить по 5 веса = 100 (лимит)
            var tasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() => inv.TryAddItem(new Item { Name = "Apple", Weight = 5 })))
                .ToArray();

            // Act
            await Task.WhenAll(tasks);

            // Assert
            Assert.Single(inv.Items);
            Assert.Equal("Apple", inv.Items[0].Name);
            Assert.Equal(100, inv.Items[0].Weight);
            Assert.Equal(100, inv.CurrentWeight);
        }
    }
}
