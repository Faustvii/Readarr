using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using VersOne.Epub.Schema;

namespace NzbDrone.Core.Test.MediaFiles.AudioTagServiceFixture
{
    [TestFixture]
    public class EbookTagServiceFixture : CoreTest<EBookTagService>
    {
        [Test]
        public void should_prefer_isbn13()
        {
            var ids = Builder<EpubMetadataIdentifier>
                .CreateListOfSize(2)
                .TheFirst(1)
                .With(x => x.Identifier = "4087738574")
                .TheNext(1)
                .With(x => x.Identifier = "9781455546176")
                .Build()
                .ToList();

            EBookTagService.GetIsbn(ids).Should().Be("9781455546176");
        }

        [Test]
        public void should_prefer_b0asin()
        {
            var ids = Builder<EpubMetadataIdentifier>
                .CreateListOfSize(2)
                .TheFirst(1)
                .With(x => x.Identifier = "0123456789")
                .TheNext(1)
                .With(x => x.Identifier = "B012345678")
                .Build()
                .ToList();

            Subject.GetAsin(ids).Should().Be("B012345678");
        }

        [Test]
        public void should_prefer_asin_schema()
        {
            var ids = Builder<EpubMetadataIdentifier>
                .CreateListOfSize(2)
                .TheFirst(1)
                .With(x =>
                {
                    x.Identifier = "0123456789";
                    x.Scheme = "ASIN";
                    return x;
                })
                .TheNext(1)
                .With(x => x.Identifier = "B012345678")
                .Build()
                .ToList();

            Subject.GetAsin(ids).Should().Be("0123456789");
        }
    }
}
