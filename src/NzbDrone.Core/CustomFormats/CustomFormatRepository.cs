using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatRepository : IBasicRepository<CustomFormat>
    {
    }

    public class CustomFormatRepository : BasicRepository<CustomFormat>, ICustomFormatRepository
    {
        public CustomFormatRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }

    public class EFCoreCustomFormatRepository : ICustomFormatRepository
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public EFCoreCustomFormatRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public IEnumerable<CustomFormat> All()
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.ToList();
        }

        public int Count()
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.Count();
        }

        public CustomFormat Find(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.Find(id);
        }

        public CustomFormat Get(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.FirstOrDefault(x => x.Id == id);
        }

        public CustomFormat Insert(CustomFormat model)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.Add(model);
            context.SaveChanges();
            return model;
        }

        public CustomFormat Update(CustomFormat model)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.Update(model);
            context.SaveChanges();
            return model;
        }

        public CustomFormat Upsert(CustomFormat model)
        {
            if (model.Id == 0)
            {
                return Insert(model);
            }
            else
            {
                return Update(model);
            }
        }

        public void SetFields(CustomFormat model, params System.Linq.Expressions.Expression<Func<CustomFormat, object>>[] properties)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.Attach(model);
            foreach (var property in properties)
            {
                context.Entry(model).Property(property).IsModified = true;
            }

            context.SaveChanges();
        }

        public void Delete(CustomFormat model)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.Remove(model);
            context.SaveChanges();
        }

        public void Delete(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            var entity = context.CustomFormats.Find(id);
            if (entity != null)
            {
                context.CustomFormats.Remove(entity);
                context.SaveChanges();
            }
        }

        public IEnumerable<CustomFormat> Get(IEnumerable<int> ids)
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.Where(x => ids.Contains(x.Id)).ToList();
        }

        public void InsertMany(IList<CustomFormat> models)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.AddRange(models);
            context.SaveChanges();
        }

        public void UpdateMany(IList<CustomFormat> models)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.UpdateRange(models);
            context.SaveChanges();
        }

        public void SetFields(IList<CustomFormat> models, params System.Linq.Expressions.Expression<Func<CustomFormat, object>>[] properties)
        {
            using var context = _contextFactory.CreateDbContext();
            foreach (var model in models)
            {
                context.CustomFormats.Attach(model);
                foreach (var property in properties)
                {
                    context.Entry(model).Property(property).IsModified = true;
                }
            }

            context.SaveChanges();
        }

        public void DeleteMany(List<CustomFormat> models)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.RemoveRange(models);
            context.SaveChanges();
        }

        public void DeleteMany(IEnumerable<int> ids)
        {
            using var context = _contextFactory.CreateDbContext();
            var entities = context.CustomFormats.Where(x => ids.Contains(x.Id)).ToList();
            context.CustomFormats.RemoveRange(entities);
            context.SaveChanges();
        }

        public void Purge(bool vacuum = false)
        {
            using var context = _contextFactory.CreateDbContext();
            context.CustomFormats.RemoveRange(context.CustomFormats);
            context.SaveChanges();
        }

        public bool HasItems()
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.Any();
        }

        public CustomFormat Single()
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.Single();
        }

        public CustomFormat SingleOrDefault()
        {
            using var context = _contextFactory.CreateDbContext();
            return context.CustomFormats.SingleOrDefault();
        }

        public Datastore.PagingSpec<CustomFormat> GetPaged(Datastore.PagingSpec<CustomFormat> pagingSpec)
        {
            using var context = _contextFactory.CreateDbContext();
            pagingSpec.Records = context.CustomFormats.ToList();
            pagingSpec.TotalRecords = pagingSpec.Records.Count;
            return pagingSpec;
        }
    }
}
