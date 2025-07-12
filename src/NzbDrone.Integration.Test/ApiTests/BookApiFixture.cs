using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Qualities;
using Readarr.Api.V1.RootFolders;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class BookApiFixture : IntegrationTest
    {
        [SetUp]
        public void Setup()
        {
            RootFolders.Post(new RootFolderResource
            {
                Name = "TestLibrary",
                Path = AuthorRootFolder,
                DefaultMetadataProfileId = 1,
                DefaultQualityProfileId = 1,
                DefaultMonitorOption = MonitorTypes.All
            });
        }

        [Test]
        public void get_books_fallback_should_return_books()
        {
            // Ensure at least one author and book exist
            var foreignEditionId = "179778914";
            var author = EnsureAuthor("3910549", foreignEditionId, "Actus", true);
            EnsureBookFile(author, 1, foreignEditionId, Quality.MOBI);

            // Call the /api/v1/book endpoint with no filters (fallback path)
            var books = Books.All();

            // Assert that the response contains at least one book for the author
            books.Should().NotBeNullOrEmpty();
            var book = books.FirstOrDefault(b => b.AuthorId == author.Id && b.ForeignEditionId == foreignEditionId);
            book.Should().NotBeNull();
            book.Title.Should().NotBeNullOrWhiteSpace();
            book.AuthorTitle.Should().NotBeNullOrWhiteSpace();
            book.SeriesTitle.Should().NotBeNull();
            book.Disambiguation.Should().NotBeNull();
            book.AuthorId.Should().Be(author.Id);
            book.ForeignBookId.Should().NotBeNullOrWhiteSpace();
            book.ForeignEditionId.Should().NotBeNullOrWhiteSpace();
            book.TitleSlug.Should().NotBeNullOrWhiteSpace();
            book.Monitored.Should().BeTrue();
            book.AnyEditionOk.Should().BeTrue();
            book.Ratings.Should().NotBeNull();
            book.Ratings.Votes.Should().BeGreaterOrEqualTo(0);
            book.Ratings.Value.Should().BeGreaterOrEqualTo(0);
            book.Ratings.Popularity.Should().BeGreaterOrEqualTo(0);
            book.ReleaseDate.Should().NotBe(default(DateTime));
            book.PageCount.Should().BeGreaterOrEqualTo(0);
            book.Genres.Should().NotBeNull();
            book.Images.Should().NotBeNull();
            book.Links.Should().NotBeNull();
            book.Statistics.Should().NotBeNull();
            book.Statistics.BookFileCount.Should().BeGreaterThan(0);
            book.Statistics.BookCount.Should().BeGreaterThan(0);
            book.Statistics.TotalBookCount.Should().BeGreaterThan(0);
            book.Statistics.SizeOnDisk.Should().BeGreaterThan(0);
            book.Statistics.PercentOfBooks.Should().BeGreaterThan(0);
            book.Added.Should().NotBe(default(DateTime));
            book.Grabbed.Should().BeFalse();
            book.Id.Should().BeGreaterThan(0);
        }

        [Test]
        public void get_books_with_paging_should_return_paged_response()
        {
            // Ensure at least one author and book exist
            EnsureAuthor("14586394", "43765115", "Andrew Hunter Murray", true);

            // Call the /api/v1/book endpoint with paging (sortKey and sortDir required)
            var paged = Books.GetPaged(1, 1, "title", "asc");

            // Assert paged response structure
            paged.Should().NotBeNull();
            paged.Page.Should().Be(1);
            paged.PageSize.Should().Be(1);
            paged.Records.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void get_books_with_authorId_should_return_books_for_author()
        {
            var author = EnsureAuthor("14586394", "43765115", "Andrew Hunter Murray", true);

            // Call the /api/v1/book endpoint with authorId
            var books = Books.GetBooksInAuthor(author.Id);

            books.Should().NotBeNullOrEmpty();
            books.All(b => b.AuthorId == author.Id).Should().BeTrue();
        }
    }
}
