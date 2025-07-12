using System;
using System.Collections.Generic;

namespace NzbDrone.Core.Books
{
    public class BookWithRelatedData
    {
        // Book data
        public int Id { get; set; }
        public int AuthorMetadataId { get; set; }
        public string ForeignBookId { get; set; }
        public string ForeignEditionId { get; set; }
        public string TitleSlug { get; set; }
        public string Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<Links> Links { get; set; }
        public List<string> Genres { get; set; }
        public List<int> RelatedBooks { get; set; }
        public Ratings Ratings { get; set; }
        public DateTime? LastSearchTime { get; set; }
        public string CleanTitle { get; set; }
        public bool Monitored { get; set; }
        public bool AnyEditionOk { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public DateTime Added { get; set; }
        public AddBookOptions AddOptions { get; set; }

        // Author data
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorSortName { get; set; }
        public string AuthorSortNameLastFirst { get; set; }
        public string AuthorNameLastFirst { get; set; }

        // Selected edition data (monitored edition)
        public string SelectedEditionTitle { get; set; }
        public string SelectedEditionForeignEditionId { get; set; }
        public string SelectedEditionDisambiguation { get; set; }
        public int SelectedEditionPageCount { get; set; }
        public List<NzbDrone.Core.MediaCover.MediaCover> SelectedEditionImages { get; set; }
        public List<Links> SelectedEditionLinks { get; set; }
        public Ratings SelectedEditionRatings { get; set; }

        // Series data
        public string SeriesTitle { get; set; }

        // Statistics data
        public int BookFileCount { get; set; }
        public int BookCount { get; set; }
        public int TotalBookCount { get; set; }
        public long SizeOnDisk { get; set; }

        public BookWithRelatedData()
        {
            Links = new List<Links>();
            Genres = new List<string>();
            RelatedBooks = new List<int>();
            Ratings = new Ratings();
            SelectedEditionImages = new List<NzbDrone.Core.MediaCover.MediaCover>();
            SelectedEditionLinks = new List<Links>();
            SelectedEditionRatings = new Ratings();
            AddOptions = new AddBookOptions();
        }
    }
}
