using NUnit.Framework;
using SWLOR.Shared.UI.Model;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiBindingListTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void Constructor_CreatesEmptyList()
        {
            // Act
            var list = new GuiBindingList<string>();

            // Assert
            Assert.That(list, Is.Not.Null);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_WithValidItem_AddsItem()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            string item = "Test Item";

            // Act
            list.Add(item);

            // Assert
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(item));
        }

        [Test]
        public void Add_WithMultipleItems_AddsAllItems()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            string[] items = { "Item 1", "Item 2", "Item 3" };

            // Act
            foreach (var item in items)
            {
                list.Add(item);
            }

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo("Item 1"));
            Assert.That(list[1], Is.EqualTo("Item 2"));
            Assert.That(list[2], Is.EqualTo("Item 3"));
        }

        [Test]
        public void Remove_WithExistingItem_RemovesItem()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");
            list.Add("Item 3");

            // Act
            bool result = list.Remove("Item 2");

            // Assert
            Assert.That(result, Is.True);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo("Item 1"));
            Assert.That(list[1], Is.EqualTo("Item 3"));
        }

        [Test]
        public void Remove_WithNonExistingItem_ReturnsFalse()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");

            // Act
            bool result = list.Remove("Non-existing Item");

            // Assert
            Assert.That(result, Is.False);
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void Clear_WithItems_ClearsList()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");

            // Act
            list.Clear();

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Contains_WithExistingItem_ReturnsTrue()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");

            // Act
            bool result = list.Contains("Item 1");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Contains_WithNonExistingItem_ReturnsFalse()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");

            // Act
            bool result = list.Contains("Non-existing Item");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IndexOf_WithExistingItem_ReturnsIndex()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");
            list.Add("Item 3");

            // Act
            int index = list.IndexOf("Item 2");

            // Assert
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void IndexOf_WithNonExistingItem_ReturnsNegativeOne()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");

            // Act
            int index = list.IndexOf("Non-existing Item");

            // Assert
            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void Insert_WithValidIndex_InsertsItem()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 3");

            // Act
            list.Insert(1, "Item 2");

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo("Item 1"));
            Assert.That(list[1], Is.EqualTo("Item 2"));
            Assert.That(list[2], Is.EqualTo("Item 3"));
        }

        [Test]
        public void RemoveAt_WithValidIndex_RemovesItem()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");
            list.Add("Item 3");

            // Act
            list.RemoveAt(1);

            // Assert
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo("Item 1"));
            Assert.That(list[1], Is.EqualTo("Item 3"));
        }

        [Test]
        public void Indexer_Get_ReturnsItem()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");

            // Act
            string item = list[1];

            // Assert
            Assert.That(item, Is.EqualTo("Item 2"));
        }

        [Test]
        public void Indexer_Set_UpdatesItem()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");

            // Act
            list[1] = "Updated Item";

            // Assert
            Assert.That(list[1], Is.EqualTo("Updated Item"));
        }

        [Test]
        public void Count_WithItems_ReturnsCorrectCount()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");
            list.Add("Item 3");

            // Act
            int count = list.Count;

            // Assert
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void PropertyName_CanBeSetAndRetrieved()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            string propertyName = "TestProperty";

            // Act
            list.PropertyName = propertyName;

            // Assert
            Assert.That(list.PropertyName, Is.EqualTo(propertyName));
        }

        [Test]
        public void CopyTo_WithValidArray_CopiesItems()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");
            list.Add("Item 3");
            string[] array = new string[3];

            // Act
            list.CopyTo(array, 0);

            // Assert
            Assert.That(array[0], Is.EqualTo("Item 1"));
            Assert.That(array[1], Is.EqualTo("Item 2"));
            Assert.That(array[2], Is.EqualTo("Item 3"));
        }

        [Test]
        public void GetEnumerator_WithItems_ReturnsEnumerator()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            list.Add("Item 2");

            // Act
            var enumerator = list.GetEnumerator();

            // Assert
            Assert.That(enumerator, Is.Not.Null);
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo("Item 1"));
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo("Item 2"));
            Assert.That(enumerator.MoveNext(), Is.False);
        }

        [Test]
        public void PropertyName_GetSet_WorksCorrectly()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            string propertyName = "TestProperty";

            // Act
            list.PropertyName = propertyName;

            // Assert
            Assert.That(list.PropertyName, Is.EqualTo(propertyName));
        }

        [Test]
        public void MaxSize_GetSet_WorksCorrectly()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            int maxSize = 100;

            // Act
            list.MaxSize = maxSize;

            // Assert
            Assert.That(list.MaxSize, Is.EqualTo(maxSize));
        }

        [Test]
        public void ListItemVisibility_GetSet_WorksCorrectly()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            var visibility = new GuiBindingList<bool> { true, false, true };

            // Act
            list.ListItemVisibility = visibility;

            // Assert
            Assert.That(list.ListItemVisibility, Is.EqualTo(visibility));
        }

        [Test]
        public void ListChanged_Event_IsRaisedOnAdd()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            bool eventRaised = false;
            list.ListChanged += (sender, args) => eventRaised = true;

            // Act
            list.Add("Test Item");

            // Assert
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void ListChanged_Event_IsRaisedOnRemove()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Test Item");
            bool eventRaised = false;
            list.ListChanged += (sender, args) => eventRaised = true;

            // Act
            list.Remove("Test Item");

            // Assert
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void ListChanged_Event_IsRaisedOnClear()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Test Item");
            bool eventRaised = false;
            list.ListChanged += (sender, args) => eventRaised = true;

            // Act
            list.Clear();

            // Assert
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void ListChanged_Event_IsRaisedOnInsert()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            bool eventRaised = false;
            list.ListChanged += (sender, args) => eventRaised = true;

            // Act
            list.Insert(0, "Item 0");

            // Assert
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void ListChanged_Event_IsRaisedOnRemoveAt()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            bool eventRaised = false;
            list.ListChanged += (sender, args) => eventRaised = true;

            // Act
            list.RemoveAt(0);

            // Assert
            Assert.That(eventRaised, Is.True);
        }

        [Test]
        public void ListChanged_Event_IsRaisedOnIndexerSet()
        {
            // Arrange
            var list = new GuiBindingList<string>();
            list.Add("Item 1");
            bool eventRaised = false;
            list.ListChanged += (sender, args) => eventRaised = true;

            // Act
            list[0] = "Updated Item";

            // Assert
            Assert.That(eventRaised, Is.True);
        }
    }
}
