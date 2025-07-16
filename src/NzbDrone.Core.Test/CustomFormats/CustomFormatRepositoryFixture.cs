using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats
{
    [TestFixture]
    public class CustomFormatRepositoryFixture : DbTest<EFCoreCustomFormatRepository, CustomFormat>
    {
        private List<CustomFormat> _customFormats;

        [SetUp]
        public void Setup()
        {
            _customFormats = new List<CustomFormat>
            {
                new CustomFormat
                {
                    Name = "Test Format 1",
                    IncludeCustomFormatWhenRenaming = true,
                    Specifications = new List<ICustomFormatSpecification>
                    {
                        new SizeSpecification { Name = "SizeSpec1", Min = 1, Max = 10, Negate = false, Required = false },
                        new IndexerFlagSpecification { Name = "FlagSpec1", Value = 1, Negate = false, Required = false }
                    }
                },
                new CustomFormat
                {
                    Name = "Test Format 2",
                    IncludeCustomFormatWhenRenaming = false,
                    Specifications = new List<ICustomFormatSpecification>()
                }
            };
        }

        [Test]
        public void should_be_able_to_insert()
        {
            Subject.Insert(_customFormats[0]);
            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_be_able_to_insert_many()
        {
            Subject.InsertMany(_customFormats);
            Subject.All().Should().HaveCount(2);
        }

        [Test]
        public void should_be_able_to_get_count()
        {
            Subject.InsertMany(_customFormats);
            Subject.Count().Should().Be(_customFormats.Count);
        }

        [Test]
        public void should_be_able_to_find_by_id()
        {
            Subject.InsertMany(_customFormats);
            var storeObject = Subject.Get(_customFormats[1].Id);
            storeObject.Should().BeEquivalentTo(_customFormats[1], o => o.IncludingAllRuntimeProperties());
        }

        [Test]
        public void should_be_able_to_update()
        {
            Subject.InsertMany(_customFormats);
            var item = _customFormats[1];
            item.Name = "Updated Format";
            Subject.Update(item);
            var allResult = Subject.All().ToList();
            allResult.Should().BeEquivalentTo(_customFormats);
            allResult[1].Name.Should().Be("Updated Format");
        }

        [Test]
        public void should_be_able_to_upsert_new()
        {
            Subject.Upsert(_customFormats[0]);
            Subject.All().Should().HaveCount(1);
        }

        [Test]
        public void should_be_able_to_upsert_existing()
        {
            Subject.InsertMany(_customFormats);
            var item = _customFormats[1];
            item.Name = "Upserted Format";
            Subject.Upsert(item);
            Subject.All().Should().BeEquivalentTo(_customFormats);
        }

        [Test]
        public void should_be_able_to_delete_by_id()
        {
            Subject.InsertMany(_customFormats);
            Subject.Delete(_customFormats[0].Id);
            Subject.All().Select(x => x.Id).Should().BeEquivalentTo(_customFormats.Skip(1).Select(x => x.Id));
        }

        [Test]
        public void should_be_able_to_delete_by_object()
        {
            Subject.InsertMany(_customFormats);
            Subject.Delete(_customFormats[0]);
            Subject.All().Select(x => x.Id).Should().BeEquivalentTo(_customFormats.Skip(1).Select(x => x.Id));
        }

        [Test]
        public void should_be_able_to_get_many()
        {
            Subject.InsertMany(_customFormats);
            var storeObjects = Subject.Get(_customFormats.Select(x => x.Id));
            storeObjects.Select(x => x.Id).Should().BeEquivalentTo(_customFormats.Select(x => x.Id));
        }

        [Test]
        public void should_be_able_to_update_many()
        {
            Subject.InsertMany(_customFormats);
            _customFormats.ForEach(x => x.Name += " Updated");
            Subject.UpdateMany(_customFormats);
            Subject.All().Should().BeEquivalentTo(_customFormats);
        }

        [Test]
        public void should_be_able_to_delete_many_by_object()
        {
            Subject.InsertMany(_customFormats);
            Subject.DeleteMany(_customFormats);
            Subject.All().Should().BeEmpty();
        }

        [Test]
        public void should_be_able_to_delete_many_by_id()
        {
            Subject.InsertMany(_customFormats);
            Subject.DeleteMany(_customFormats.Select(x => x.Id).ToList());
            Subject.All().Should().BeEmpty();
        }

        [Test]
        public void purge_should_delete_all()
        {
            Subject.InsertMany(_customFormats);
            Subject.Purge();
            Subject.All().Should().BeEmpty();
        }

        [Test]
        public void has_items_should_return_false_with_no_items()
        {
            Subject.HasItems().Should().BeFalse();
        }

        [Test]
        public void has_items_should_return_true_with_items()
        {
            Subject.InsertMany(_customFormats);
            Subject.HasItems().Should().BeTrue();
        }

        [Test]
        public void single_should_throw_on_empty()
        {
            Assert.Throws<InvalidOperationException>(() => Subject.Single());
        }

        [Test]
        public void should_be_able_to_get_single()
        {
            Subject.Insert(_customFormats[0]);
            Subject.Single().Should().BeEquivalentTo(_customFormats[0]);
        }

        [Test]
        public void single_or_default_on_empty_table_should_return_null()
        {
            Subject.SingleOrDefault().Should().BeNull();
        }
    }
}
