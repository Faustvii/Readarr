using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Books
{
    public interface IBookRepository : IBasicRepository<Book>
    {
        List<Book> GetBooks(int authorId);
        List<Book> GetLastBooks(IEnumerable<int> authorMetadataIds);
        List<Book> GetNextBooks(IEnumerable<int> authorMetadataIds);
        List<Book> GetBooksByAuthorMetadataId(int authorMetadataId);
        List<Book> GetBooksForRefresh(int authorMetadataId, List<string> foreignIds);
        List<Book> GetBooksByFileIds(IEnumerable<int> fileIds);
        Book FindByTitle(int authorMetadataId, string title);
        Book FindById(string foreignBookId);
        Book FindBySlug(string titleSlug);
        PagingSpec<Book> BooksWithoutFiles(PagingSpec<Book> pagingSpec);
        PagingSpec<Book> BooksWhereCutoffUnmet(PagingSpec<Book> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff);
        List<Book> BooksBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored);
        List<Book> AuthorBooksBetweenDates(Author author, DateTime startDate, DateTime endDate, bool includeUnmonitored);
        void SetMonitoredFlat(Book book, bool monitored);
        void SetMonitored(IEnumerable<int> ids, bool monitored);
        List<Book> GetAuthorBooksWithFiles(Author author);
        List<BookWithRelatedData> GetAllBooksWithRelatedData();
    }

    public class BookRepository : BasicRepository<Book>, IBookRepository
    {
        public BookRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Book> GetBooks(int authorId)
        {
            var joinedBuilder = Builder()
                .Join<Book, AuthorMetadata>((book, meta) => book.AuthorMetadataId == meta.Id)
                .Join<AuthorMetadata, Author>((meta, author) => author.AuthorMetadataId == meta.Id)
                .Where<Author>(x => x.Id == authorId);

            var result = _database.QueryJoined<Book, AuthorMetadata>(joinedBuilder, (book, metadata) =>
            {
                book.AuthorMetadata = metadata;
                return book;
            }).ToList();

            return result;
        }

        public List<Book> GetLastBooks(IEnumerable<int> authorMetadataIds)
        {
            var now = DateTime.UtcNow;

            var inner = Builder()
                .Select("MIN(\"Books\".\"Id\") as id, MAX(\"Books\".\"ReleaseDate\") as date")
                .Where<Book>(x => authorMetadataIds.Contains(x.AuthorMetadataId) && x.ReleaseDate < now)
                .GroupBy<Book>(x => x.AuthorMetadataId)
                .AddSelectTemplate(typeof(Book));

            var outer = Builder()
                .Join($"({inner.RawSql}) ids on ids.id = \"Books\".\"Id\" and ids.date = \"Books\".\"ReleaseDate\"")
                .AddParameters(inner.Parameters);

            return Query(outer);
        }

        public List<Book> GetNextBooks(IEnumerable<int> authorMetadataIds)
        {
            var now = DateTime.UtcNow;

            var inner = Builder()
                .Select("MIN(\"Books\".\"Id\") as id, MIN(\"Books\".\"ReleaseDate\") as date")
                .Where<Book>(x => authorMetadataIds.Contains(x.AuthorMetadataId) && x.ReleaseDate > now)
                .GroupBy<Book>(x => x.AuthorMetadataId)
                .AddSelectTemplate(typeof(Book));

            var outer = Builder()
                .Join($"({inner.RawSql}) ids on ids.id = \"Books\".\"Id\" and ids.date = \"Books\".\"ReleaseDate\"")
                .AddParameters(inner.Parameters);

            return Query(outer);
        }

        public List<Book> GetBooksByAuthorMetadataId(int authorMetadataId)
        {
            return Query(s => s.AuthorMetadataId == authorMetadataId);
        }

        public List<Book> GetBooksForRefresh(int authorMetadataId, List<string> foreignIds)
        {
            return Query(a => a.AuthorMetadataId == authorMetadataId || foreignIds.Contains(a.ForeignBookId));
        }

        public List<Book> GetBooksByFileIds(IEnumerable<int> fileIds)
        {
            return Query(new SqlBuilder(_database.DatabaseType)
                         .Join<Book, Edition>((b, e) => b.Id == e.BookId)
                         .Join<Edition, BookFile>((l, r) => l.Id == r.EditionId)
                         .Where<BookFile>(f => fileIds.Contains(f.Id)))
                .DistinctBy(x => x.Id)
                .ToList();
        }

        public Book FindById(string foreignBookId)
        {
            return Query(s => s.ForeignBookId == foreignBookId).SingleOrDefault();
        }

        public Book FindBySlug(string titleSlug)
        {
            return Query(s => s.TitleSlug == titleSlug).SingleOrDefault();
        }

        //x.Id == null is converted to SQL, so warning incorrect
#pragma warning disable CS0472
        private SqlBuilder BooksWithoutFilesBuilder(DateTime currentTime) => Builder()
            .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
            .Join<Author, AuthorMetadata>((l, r) => l.AuthorMetadataId == r.Id)
            .Join<Book, Edition>((b, e) => b.Id == e.BookId)
            .LeftJoin<Edition, BookFile>((t, f) => t.Id == f.EditionId)
            .Where<BookFile>(f => f.Id == null)
            .Where<Edition>(e => e.Monitored == true)
            .Where<Book>(a => a.ReleaseDate <= currentTime);
#pragma warning restore CS0472

        public PagingSpec<Book> BooksWithoutFiles(PagingSpec<Book> pagingSpec)
        {
            var currentTime = DateTime.UtcNow;

            pagingSpec.Records = GetPagedRecords(BooksWithoutFilesBuilder(currentTime), pagingSpec, PagedQuery);
            pagingSpec.TotalRecords = GetPagedRecordCount(BooksWithoutFilesBuilder(currentTime).SelectCountDistinct<Book>(x => x.Id), pagingSpec);

            return pagingSpec;
        }

        private SqlBuilder BooksWhereCutoffUnmetBuilder(List<QualitiesBelowCutoff> qualitiesBelowCutoff) => Builder()
            .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
            .Join<Author, AuthorMetadata>((l, r) => l.AuthorMetadataId == r.Id)
            .Join<Book, Edition>((b, e) => b.Id == e.BookId)
            .LeftJoin<Edition, BookFile>((t, f) => t.Id == f.EditionId)
            .Where<Edition>(e => e.Monitored == true)
            .Where(BuildQualityCutoffWhereClause(qualitiesBelowCutoff));

        private string BuildQualityCutoffWhereClause(List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            var clauses = new List<string>();

            foreach (var profile in qualitiesBelowCutoff)
            {
                foreach (var belowCutoff in profile.QualityIds)
                {
                    clauses.Add(string.Format("(\"Authors\".\"QualityProfileId\" = {0} AND \"BookFiles\".\"Quality\" LIKE '%_quality_: {1},%')", profile.ProfileId, belowCutoff));
                }
            }

            return string.Format("({0})", string.Join(" OR ", clauses));
        }

        public PagingSpec<Book> BooksWhereCutoffUnmet(PagingSpec<Book> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            pagingSpec.Records = GetPagedRecords(BooksWhereCutoffUnmetBuilder(qualitiesBelowCutoff), pagingSpec, PagedQuery);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM \"{TableMapping.Mapper.TableNameMapping(typeof(Book))}\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/) AS \"Inner\"";
            pagingSpec.TotalRecords = GetPagedRecordCount(BooksWhereCutoffUnmetBuilder(qualitiesBelowCutoff).Select(typeof(Book)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        public List<Book> BooksBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Book>(rg => rg.ReleaseDate >= startDate && rg.ReleaseDate <= endDate);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Book>(e => e.Monitored == true)
                    .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
                    .Where<Author>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public List<Book> AuthorBooksBetweenDates(Author author, DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Book>(rg => rg.ReleaseDate >= startDate &&
                                                 rg.ReleaseDate <= endDate &&
                                                 rg.AuthorMetadataId == author.AuthorMetadataId);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Book>(e => e.Monitored == true)
                    .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
                    .Where<Author>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public void SetMonitoredFlat(Book book, bool monitored)
        {
            book.Monitored = monitored;
            SetFields(book, p => p.Monitored);

            ModelUpdated(book, true);
        }

        public void SetMonitored(IEnumerable<int> ids, bool monitored)
        {
            var books = ids.Select(x => new Book { Id = x, Monitored = monitored }).ToList();
            SetFields(books, p => p.Monitored);
        }

        public Book FindByTitle(int authorMetadataId, string title)
        {
            var cleanTitle = Parser.Parser.CleanAuthorName(title);

            if (string.IsNullOrEmpty(cleanTitle))
            {
                cleanTitle = title;
            }

            return Query(s => (s.CleanTitle == cleanTitle || s.Title == title) && s.AuthorMetadataId == authorMetadataId)
                .ExclusiveOrDefault();
        }

        public List<Book> GetAuthorBooksWithFiles(Author author)
        {
            return Query(Builder().Join<Book, Edition>((b, e) => b.Id == e.BookId)
                         .Join<Edition, BookFile>((e, f) => e.Id == f.EditionId)
                         .Where<Book>(b => b.AuthorMetadataId == author.AuthorMetadataId));
        }

        public List<BookWithRelatedData> GetAllBooksWithRelatedData()
        {
            var sql = @"
                SELECT
                    b.""Id"",
                    b.""AuthorMetadataId"",
                    b.""ForeignBookId"",
                    b.""TitleSlug"",
                    b.""Title"",
                    b.""ReleaseDate"",
                    b.""Links"",
                    b.""Genres"",
                    b.""RelatedBooks"",
                    b.""Ratings"",
                    b.""LastSearchTime"",
                    b.""CleanTitle"",
                    b.""Monitored"",
                    b.""AnyEditionOk"",
                    b.""LastInfoSync"",
                    b.""Added"",
                    b.""AddOptions"",
                    a.""Id"" as AuthorId,
                    am.""Name"" as AuthorName,
                    am.""SortName"" as AuthorSortName,
                    am.""SortNameLastFirst"" as AuthorSortNameLastFirst,
                    am.""NameLastFirst"" as AuthorNameLastFirst,
                    e.""Title"" as SelectedEditionTitle,
                    e.""ForeignEditionId"" as SelectedEditionForeignEditionId,
                    e.""Disambiguation"" as SelectedEditionDisambiguation,
                    e.""PageCount"" as SelectedEditionPageCount,
                    e.""Images"" as SelectedEditionImages,
                    e.""Links"" as SelectedEditionLinks,
                    e.""Ratings"" as SelectedEditionRatings,
                    COALESCE(s.""Title"", '') as SeriesTitle,
                    0 as BookFileCount,
                    1 as BookCount,
                    1 as TotalBookCount,
                    0 as SizeOnDisk
                FROM ""Books"" b
                INNER JOIN ""AuthorMetadata"" am ON b.""AuthorMetadataId"" = am.""Id""
                INNER JOIN ""Authors"" a ON am.""Id"" = a.""AuthorMetadataId""
                LEFT JOIN (
                    SELECT
                        e1.""BookId"",
                        e1.""Title"",
                        e1.""ForeignEditionId"",
                        e1.""Disambiguation"",
                        e1.""PageCount"",
                        e1.""Images"",
                        e1.""Links"",
                        e1.""Ratings""
                    FROM ""Editions"" e1
                    WHERE e1.""Monitored"" = 1
                    AND e1.""Id"" = (
                        SELECT MIN(e2.""Id"")
                        FROM ""Editions"" e2
                        WHERE e2.""BookId"" = e1.""BookId""
                        AND e2.""Monitored"" = 1
                    )
                ) e ON b.""Id"" = e.""BookId""
                LEFT JOIN ""SeriesBookLink"" sbl ON b.""Id"" = sbl.""BookId""
                LEFT JOIN ""Series"" s ON sbl.""SeriesId"" = s.""Id""
                ORDER BY b.""Id""";

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<BookWithRelatedData>(sql).ToList();
            }
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder(_database.DatabaseType)
              .Join<Book, AuthorMetadata>((book, meta) => book.AuthorMetadataId == meta.Id)
              .Join<AuthorMetadata, Author>((meta, author) => meta.Id == author.AuthorMetadataId)
              .Join<Book, Edition>((book, edition) => book.Id == edition.BookId && edition.Monitored);

        protected override IEnumerable<Book> PagedQuery(SqlBuilder sql) =>
             _database.QueryJoined<Book, AuthorMetadata, Author, Edition>(sql, (book, metadata, author, monitoredEdition) =>
             {
                 book.AuthorMetadata = metadata;
                 book.Author = author;
                 book.Editions = new List<Edition>() { monitoredEdition };
                 return book;
             });

        protected override string GetPagedOrderBy(PagingSpec<Book> pagingSpec)
        {
            var bookSortKey = TableMapping.Mapper.GetSortKey(nameof(Book.CleanTitle));

            var sortKey = TableMapping.Mapper.GetSortKey(pagingSpec.SortKey);
            var sortDirection = pagingSpec.SortDirection == SortDirection.Descending ? "DESC" : "ASC";
            var pagingOffset = Math.Max(pagingSpec.Page - 1, 0) * pagingSpec.PageSize;
            var sorting = $"\"{sortKey.Table ?? _table}\".\"{sortKey.Column}\" {sortDirection}, \"{_table}\".\"{bookSortKey.Column}\" {sortDirection} LIMIT {pagingSpec.PageSize} OFFSET {pagingOffset}";

            return sorting;
        }
    }
}
