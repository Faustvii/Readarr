using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Dapper;
using NzbDrone.Core.Instrumentation;

namespace NzbDrone.Core.Datastore
{
    public static class SqlMapperExtensions
    {
        private static readonly ActivitySource DatabaseActivitySource = new ActivitySource("Readarr.Database");

        public static IEnumerable<T> RawQuery<T>(this IDatabase db, string sql, object param = null)
        {
            var sw = Stopwatch.StartNew();
            var success = true;

            using var activity = DatabaseActivitySource.StartActivity($"Database.RawQuery<{typeof(T).Name}>");
            try
            {
                activity?.SetTag("db.type", db.DatabaseType.ToString());
                activity?.SetTag("db.statement", sql);
                activity?.SetTag("db.operation", "raw_query");
                activity?.SetTag("db.system", db.DatabaseType == DatabaseType.SQLite ? "sqlite" : "postgresql");
                activity?.SetTag("db.param_count", param != null ? new DynamicParameters(param).ParameterNames.Count() : 0);

                using var conn = db.OpenConnection();
                IEnumerable<T> items;
                try
                {
                    items = SqlMapper.Query<T>(conn, sql, param);
                }
                catch (Exception e)
                {
                    e.Data.Add("SQL", SqlBuilderExtensions.GetSqlLogString(sql, param));
                    success = false;
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", e.Message);
                    throw;
                }

                if (TableMapping.Mapper.LazyLoadList.TryGetValue(typeof(T), out var lazyProperties))
                {
                    foreach (var item in items)
                    {
                        ApplyLazyLoad(db, item, lazyProperties);
                    }
                }

                return items;
            }
            finally
            {
                activity?.SetTag("db.duration_ms", sw.Elapsed.TotalMilliseconds);
                ReadarrMetrics.Instance?.RecordDatabaseQuery($"RawQuery<{typeof(T).Name}>", sw.Elapsed.TotalMilliseconds, success);
            }
        }

        public static IEnumerable<T> Query<T>(this IDatabase db, string sql, object param = null)
        {
            var sw = Stopwatch.StartNew();
            var success = true;

            using var activity = DatabaseActivitySource.StartActivity($"Database.Query<{typeof(T).Name}>");
            try
            {
                activity?.SetTag("db.type", db.DatabaseType.ToString());
                activity?.SetTag("db.statement", sql);
                activity?.SetTag("db.operation", "query");
                activity?.SetTag("db.system", db.DatabaseType == DatabaseType.SQLite ? "sqlite" : "postgresql");
                activity?.SetTag("db.param_count", param != null ? new DynamicParameters(param).ParameterNames.Count() : 0);

                using var conn = db.OpenConnection();
                IEnumerable<T> items;
                try
                {
                    items = SqlMapper.Query<T>(conn, sql, param);
                }
                catch (Exception e)
                {
                    e.Data.Add("SQL", SqlBuilderExtensions.GetSqlLogString(sql, param));
                    success = false;
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", e.Message);
                    throw;
                }

                if (TableMapping.Mapper.LazyLoadList.TryGetValue(typeof(T), out var lazyProperties))
                {
                    foreach (var item in items)
                    {
                        ApplyLazyLoad(db, item, lazyProperties);
                    }
                }

                return items;
            }
            finally
            {
                activity?.SetTag("db.duration_ms", sw.Elapsed.TotalMilliseconds);
                ReadarrMetrics.Instance?.RecordDatabaseQuery(typeof(T).Name, sw.Elapsed.TotalMilliseconds, success);
            }
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(this IDatabase db, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var sw = Stopwatch.StartNew();
            var success = true;
            using var activity = DatabaseActivitySource.StartActivity($"Database.Query<{typeof(TFirst).Name}+{typeof(TSecond).Name}>");
            TReturn MapWithLazy(TFirst first, TSecond second)
            {
                ApplyLazyLoad(db, first);
                ApplyLazyLoad(db, second);
                return map(first, second);
            }

            try
            {
                activity?.SetTag("db.type", db.DatabaseType.ToString());
                activity?.SetTag("db.statement", sql);
                activity?.SetTag("db.operation", "query");
                activity?.SetTag("db.system", db.DatabaseType == DatabaseType.SQLite ? "sqlite" : "postgresql");

                using var conn = db.OpenConnection();
                try
                {
                    return SqlMapper.Query<TFirst, TSecond, TReturn>(conn, sql, MapWithLazy, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
                catch (Exception e)
                {
                    e.Data.Add("SQL", SqlBuilderExtensions.GetSqlLogString(sql, param));
                    success = false;
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", e.Message);
                    throw;
                }
            }
            finally
            {
                activity?.SetTag("db.duration_ms", sw.Elapsed.TotalMilliseconds);
                ReadarrMetrics.Instance?.RecordDatabaseQuery($"{typeof(TFirst).Name}+{typeof(TSecond).Name}", sw.Elapsed.TotalMilliseconds, success);
            }
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(this IDatabase db, string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var sw = Stopwatch.StartNew();
            var success = true;
            using var activity = DatabaseActivitySource.StartActivity($"Database.Query<{typeof(TFirst).Name}+{typeof(TSecond).Name}+{typeof(TThird).Name}>");
            TReturn MapWithLazy(TFirst first, TSecond second, TThird third)
            {
                ApplyLazyLoad(db, first);
                ApplyLazyLoad(db, second);
                ApplyLazyLoad(db, third);
                return map(first, second, third);
            }

            try
            {
                activity?.SetTag("db.type", db.DatabaseType.ToString());
                activity?.SetTag("db.statement", sql);
                activity?.SetTag("db.operation", "query");
                activity?.SetTag("db.system", db.DatabaseType == DatabaseType.SQLite ? "sqlite" : "postgresql");

                using var conn = db.OpenConnection();
                try
                {
                    return SqlMapper.Query<TFirst, TSecond, TThird, TReturn>(conn, sql, MapWithLazy, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
                catch (Exception e)
                {
                    e.Data.Add("SQL", SqlBuilderExtensions.GetSqlLogString(sql, param));
                    success = false;
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", e.Message);
                    throw;
                }
            }
            finally
            {
                activity?.SetTag("db.duration_ms", sw.Elapsed.TotalMilliseconds);
                ReadarrMetrics.Instance?.RecordDatabaseQuery($"{typeof(TFirst).Name}+{typeof(TSecond).Name}+{typeof(TThird).Name}", sw.Elapsed.TotalMilliseconds, success);
            }
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(this IDatabase db, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var sw = Stopwatch.StartNew();
            var success = true;
            using var activity = DatabaseActivitySource.StartActivity($"Database.Query<{typeof(TFirst).Name}+{typeof(TSecond).Name}+{typeof(TThird).Name}+{typeof(TFourth).Name}>");
            TReturn MapWithLazy(TFirst first, TSecond second, TThird third, TFourth fourth)
            {
                ApplyLazyLoad(db, first);
                ApplyLazyLoad(db, second);
                ApplyLazyLoad(db, third);
                ApplyLazyLoad(db, fourth);
                return map(first, second, third, fourth);
            }

            try
            {
                activity?.SetTag("db.type", db.DatabaseType.ToString());
                activity?.SetTag("db.statement", sql);
                activity?.SetTag("db.operation", "query");
                activity?.SetTag("db.system", db.DatabaseType == DatabaseType.SQLite ? "sqlite" : "postgresql");

                using var conn = db.OpenConnection();
                try
                {
                    return SqlMapper.Query<TFirst, TSecond, TThird, TFourth, TReturn>(conn, sql, MapWithLazy, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
                catch (Exception e)
                {
                    e.Data.Add("SQL", SqlBuilderExtensions.GetSqlLogString(sql, param));
                    success = false;
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", e.Message);
                    throw;
                }
            }
            finally
            {
                activity?.SetTag("db.duration_ms", sw.Elapsed.TotalMilliseconds);
                ReadarrMetrics.Instance?.RecordDatabaseQuery($"{typeof(TFirst).Name}+{typeof(TSecond).Name}+{typeof(TThird).Name}+{typeof(TFourth).Name}", sw.Elapsed.TotalMilliseconds, success);
            }
        }

        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDatabase db, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            var sw = Stopwatch.StartNew();
            var success = true;
            using var activity = DatabaseActivitySource.StartActivity($"Database.Query<{typeof(TFirst).Name}+{typeof(TSecond).Name}+{typeof(TThird).Name}+{typeof(TFourth).Name}+{typeof(TFifth).Name}>");
            TReturn MapWithLazy(TFirst first, TSecond second, TThird third, TFourth fourth, TFifth fifth)
            {
                ApplyLazyLoad(db, first);
                ApplyLazyLoad(db, second);
                ApplyLazyLoad(db, third);
                ApplyLazyLoad(db, fourth);
                ApplyLazyLoad(db, fifth);
                return map(first, second, third, fourth, fifth);
            }

            try
            {
                activity?.SetTag("db.type", db.DatabaseType.ToString());
                activity?.SetTag("db.statement", sql);
                activity?.SetTag("db.operation", "query");
                activity?.SetTag("db.system", db.DatabaseType == DatabaseType.SQLite ? "sqlite" : "postgresql");

                using var conn = db.OpenConnection();
                try
                {
                    return SqlMapper.Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(conn, sql, MapWithLazy, param, transaction, buffered, splitOn, commandTimeout, commandType);
                }
                catch (Exception e)
                {
                    e.Data.Add("SQL", SqlBuilderExtensions.GetSqlLogString(sql, param));
                    success = false;
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", e.Message);
                    throw;
                }
            }
            finally
            {
                activity?.SetTag("db.duration_ms", sw.Elapsed.TotalMilliseconds);
                ReadarrMetrics.Instance?.RecordDatabaseQuery($"{typeof(TFirst).Name}+{typeof(TSecond).Name}+{typeof(TThird).Name}+{typeof(TFourth).Name}+{typeof(TFifth).Name}", sw.Elapsed.TotalMilliseconds, success);
            }
        }

        public static IEnumerable<T> Query<T>(this IDatabase db, SqlBuilder builder)
        {
            var type = typeof(T);
            var sql = builder.Select(type).AddSelectTemplate(type);

            return db.Query<T>(sql.RawSql, sql.Parameters);
        }

        public static IEnumerable<T> QueryDistinct<T>(this IDatabase db, SqlBuilder builder)
        {
            var type = typeof(T);
            var sql = builder.SelectDistinct(type).AddSelectTemplate(type);

            return db.Query<T>(sql.RawSql, sql.Parameters);
        }

        public static IEnumerable<T> QueryJoined<T, T2>(this IDatabase db, SqlBuilder builder, Func<T, T2, T> mapper)
        {
            var type = typeof(T);
            var sql = builder.Select(type, typeof(T2)).AddSelectTemplate(type);

            return db.Query(sql.RawSql, mapper, sql.Parameters);
        }

        public static IEnumerable<T> QueryJoined<T, T2, T3>(this IDatabase db, SqlBuilder builder, Func<T, T2, T3, T> mapper)
        {
            var type = typeof(T);
            var sql = builder.Select(type, typeof(T2), typeof(T3)).AddSelectTemplate(type);

            return db.Query(sql.RawSql, mapper, sql.Parameters);
        }

        public static IEnumerable<T> QueryJoined<T, T2, T3, T4>(this IDatabase db, SqlBuilder builder, Func<T, T2, T3, T4, T> mapper)
        {
            var type = typeof(T);
            var sql = builder.Select(type, typeof(T2), typeof(T3), typeof(T4)).AddSelectTemplate(type);

            return db.Query(sql.RawSql, mapper, sql.Parameters);
        }

        public static IEnumerable<T> QueryJoined<T, T2, T3, T4, T5>(this IDatabase db, SqlBuilder builder, Func<T, T2, T3, T4, T5, T> mapper)
        {
            var type = typeof(T);
            var sql = builder.Select(type, typeof(T2), typeof(T3), typeof(T4), typeof(T5)).AddSelectTemplate(type);

            return db.Query(sql.RawSql, mapper, sql.Parameters);
        }

        public static T Scalar<T>(this IDatabase db, string sql, object param = null)
        {
            var sw = Stopwatch.StartNew();
            var success = true;

            using var activity = DatabaseActivitySource.StartActivity($"Database.Scalar<{typeof(T).Name}>");
            try
            {
                activity?.SetTag("db.type", db.DatabaseType.ToString());
                activity?.SetTag("db.statement", sql);
                activity?.SetTag("db.operation", "scalar");
                activity?.SetTag("db.system", db.DatabaseType == DatabaseType.SQLite ? "sqlite" : "postgresql");
                activity?.SetTag("db.param_count", param != null ? new DynamicParameters(param).ParameterNames.Count() : 0);

                using var conn = db.OpenConnection();
                T result;
                try
                {
                    result = conn.ExecuteScalar<T>(sql, param);
                }
                catch (Exception e)
                {
                    e.Data.Add("SQL", SqlBuilderExtensions.GetSqlLogString(sql, param));
                    success = false;
                    activity?.SetTag("error", true);
                    activity?.SetTag("error.message", e.Message);
                    throw;
                }

                return result;
            }
            finally
            {
                activity?.SetTag("db.duration_ms", sw.Elapsed.TotalMilliseconds);
                ReadarrMetrics.Instance?.RecordDatabaseQuery($"Scalar<{typeof(T).Name}>", sw.Elapsed.TotalMilliseconds, success);
            }
        }

        private static void ApplyLazyLoad<TModel>(IDatabase db, TModel model)
        {
            if (TableMapping.Mapper.LazyLoadList.TryGetValue(typeof(TModel), out var lazyProperties))
            {
                ApplyLazyLoad(db, model, lazyProperties);
            }
        }

        private static void ApplyLazyLoad<TModel>(IDatabase db, TModel model, List<LazyLoadedProperty> lazyProperties)
        {
            if (model == null)
            {
                return;
            }

            foreach (var lazyProperty in lazyProperties)
            {
                var lazy = (ILazyLoaded)lazyProperty.LazyLoad.Clone();
                lazy.Prepare(db, model);
                lazyProperty.Property.SetValue(model, lazy);
            }
        }
    }
}
