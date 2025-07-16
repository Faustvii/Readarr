using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Datastore
{
    public class JsonValueConverter<T> : ValueConverter<T, string>
    {
        public JsonValueConverter(JsonSerializerOptions options = null)
            : base(
                v => JsonSerializer.Serialize(v, options),
                v => JsonSerializer.Deserialize<T>(v, options))
        {
        }
    }

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Series> Series { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Edition> Editions { get; set; }
        public DbSet<BookFile> BookFiles { get; set; }
        public DbSet<AuthorMetadata> AuthorMetadata { get; set; }
        public DbSet<SeriesBookLink> SeriesBookLinks { get; set; }
        public DbSet<CustomFormat> CustomFormats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Only configure relationships using scalar FKs and standard collections
            // TODO: Custom navigation properties (LazyLoaded<T>) require custom handling or explicit ignore

            // SeriesBookLink: FK to Series and Book
            modelBuilder.Entity<SeriesBookLink>()
                .HasOne<Series>()
                .WithMany()
                .HasForeignKey(l => l.SeriesId);

            modelBuilder.Entity<SeriesBookLink>()
                .HasOne<Book>()
                .WithMany()
                .HasForeignKey(l => l.BookId);

            // Edition: FK to Book
            modelBuilder.Entity<Edition>()
                .HasOne<Book>()
                .WithMany()
                .HasForeignKey(e => e.BookId);

            // BookFile: FK to Edition
            modelBuilder.Entity<BookFile>()
                .HasOne<Edition>()
                .WithMany()
                .HasForeignKey(bf => bf.EditionId);

            // Book: FK to AuthorMetadata
            modelBuilder.Entity<Book>()
                .HasOne<AuthorMetadata>()
                .WithMany()
                .HasForeignKey(b => b.AuthorMetadataId);

            // Author: FK to AuthorMetadata
            modelBuilder.Entity<Author>()
                .HasOne<AuthorMetadata>()
                .WithMany()
                .HasForeignKey(a => a.AuthorMetadataId);

            RegisterEmbeddedConverters(modelBuilder);

            // --- End EmbeddedDocument Mapping ---

            // Ignore all LazyLoaded<T> and LazyLoaded<List<T>> navigation properties on root entities
            modelBuilder.Entity<Book>().Ignore(b => b.AuthorMetadata);
            modelBuilder.Entity<Book>().Ignore(b => b.Author);
            modelBuilder.Entity<Book>().Ignore(b => b.Editions);
            modelBuilder.Entity<Book>().Ignore(b => b.BookFiles);
            modelBuilder.Entity<Book>().Ignore(b => b.SeriesLinks);
            modelBuilder.Entity<Author>().Ignore(a => a.Metadata);
            modelBuilder.Entity<Author>().Ignore(a => a.QualityProfile);
            modelBuilder.Entity<Author>().Ignore(a => a.MetadataProfile);
            modelBuilder.Entity<Author>().Ignore(a => a.Books);
            modelBuilder.Entity<Author>().Ignore(a => a.Series);
            modelBuilder.Entity<Edition>().Ignore(e => e.Book);
            modelBuilder.Entity<Edition>().Ignore(e => e.BookFiles);
            modelBuilder.Entity<Series>().Ignore(s => s.LinkItems);
            modelBuilder.Entity<Series>().Ignore(s => s.Books);
            modelBuilder.Entity<SeriesBookLink>().Ignore(sbl => sbl.Series);
            modelBuilder.Entity<SeriesBookLink>().Ignore(sbl => sbl.Book);
            modelBuilder.Entity<BookFile>().Ignore(bf => bf.Author);
            modelBuilder.Entity<BookFile>().Ignore(bf => bf.Edition);
        }

        private static void RegisterEmbeddedConverters(ModelBuilder modelBuilder)
        {
            var serializerSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
            serializerSettings.Converters.Add(new STJTimeSpanConverter());
            serializerSettings.Converters.Add(new STJUtcConverter());
            serializerSettings.Converters.Add(new QualityIntConverter());
            serializerSettings.Converters.Add(new CustomFormatIntConverter());
            serializerSettings.Converters.Add(new CustomFormatSpecificationListConverter());

            var embeddedDocType = typeof(Datastore.IEmbeddedDocument);
            var listTypeDef = typeof(List<>);

            var rootEntityTypes = new[]
            {
                typeof(Series),
                typeof(Book),
                typeof(Author),
                typeof(Edition),
                typeof(BookFile),
                typeof(AuthorMetadata),
                typeof(SeriesBookLink),
                typeof(CustomFormat)
            };

            var simpleCollectionTypes = new[]
            {
                typeof(List<int>),
                typeof(List<string>),
                typeof(List<KeyValuePair<string, int>>),
                typeof(HashSet<int>),
                typeof(Dictionary<string, string>),
                typeof(IDictionary<string, string>)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                if (!rootEntityTypes.Contains(clrType))
                {
                    continue;
                }

                foreach (var prop in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var propType = prop.PropertyType;

                    if (propType == typeof(List<ICustomFormatSpecification>))
                    {
                        modelBuilder.Entity(clrType).Property(prop.Name).HasConversion((ValueConverter)Activator.CreateInstance(typeof(JsonValueConverter<>).MakeGenericType(propType), serializerSettings));
                        continue;
                    }

                    if (simpleCollectionTypes.Contains(propType))
                    {
                        modelBuilder.Entity(clrType).Property(prop.Name).HasConversion((ValueConverter)Activator.CreateInstance(typeof(JsonValueConverter<>).MakeGenericType(propType), serializerSettings));
                        continue;
                    }

                    if (embeddedDocType.IsAssignableFrom(propType) && propType != embeddedDocType)
                    {
                        modelBuilder.Entity(clrType).Property(prop.Name).HasConversion((ValueConverter)Activator.CreateInstance(typeof(JsonValueConverter<>).MakeGenericType(propType), serializerSettings));
                        continue;
                    }

                    if (propType.IsGenericType && propType.GetGenericTypeDefinition() == listTypeDef)
                    {
                        var elemType = propType.GetGenericArguments()[0];
                        if (embeddedDocType.IsAssignableFrom(elemType) && elemType != embeddedDocType)
                        {
                            modelBuilder.Entity(clrType).Property(prop.Name).HasConversion((ValueConverter)Activator.CreateInstance(typeof(JsonValueConverter<>).MakeGenericType(propType), serializerSettings));
                        }
                    }
                }
            }
        }
    }
}
