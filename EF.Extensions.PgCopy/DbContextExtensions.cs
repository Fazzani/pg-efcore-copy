using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NeoSmart.AsyncLock;
using Npgsql;
using NpgsqlTypes;

namespace EF.Extensions.PgCopy
{
    public static class DbContextExtensions
    {
        static ConcurrentDictionary<Type, MethodInfo> _castMethodDict = new ConcurrentDictionary<Type, MethodInfo>();
        static ConcurrentDictionary<Type, object> _copyHelperDict = new ConcurrentDictionary<Type, object>();
        private const int MaxRetry = 3;
        private static readonly AsyncLock ConnAsyncLock = new AsyncLock();

        /// <summary>
        /// Saving entities extension based on Postgres Copy command
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<int> SaveByCopyChangesAsync(this DbContext dbContext,
            CancellationToken cancellationToken=default)
        {
            if (!dbContext.ChangeTracker.HasChanges()) return await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var entityEntry in dbContext.ChangeTracker.Entries().DistinctBy(x => x.Entity.GetType()))
            {
                var copyHelper = PostgreSqlCopyHelperFactory(entityEntry.Metadata);
                var maxRetry = MaxRetry;
                do
                {
                    if (_copyHelperDict.TryAdd(entityEntry.Entity.GetType(), copyHelper))
                        break;
                    maxRetry--;
                } while (maxRetry >= 0);
            }

            return (await Task.WhenAll(_copyHelperDict
                .Select(x => SaveByCopyEntityAsync(dbContext, x.Key, x.Value, cancellationToken)))).Sum();
        }

        /// <summary>
        ///  Saving entities extension based on Postgres Copy command
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static int SaveByCopyChanges(this DbContext dbContext)
        {
            if (!dbContext.ChangeTracker.HasChanges()) return 0;

            foreach (var entityEntry in dbContext.ChangeTracker.Entries().DistinctBy(x => x.Entity.GetType()))
            {
                var copyHelper = PostgreSqlCopyHelperFactory(entityEntry.Metadata);
                var maxRetry = MaxRetry;
                do
                {
                    if (_copyHelperDict.TryAdd(entityEntry.Entity.GetType(), copyHelper))
                        break;
                    maxRetry--;
                } while (maxRetry >= 0);
            }

            var result = 0;
            foreach (var (entityType, copyHelper) in _copyHelperDict)
            {
                result += SaveByCopyEntity(dbContext, entityType, copyHelper);
                // Untrack saved entities
                // foreach (var o in dbContext.ChangeTracker.Entries().Where(x => x.Entity.GetType() == entityType))
                // {
                //     o.State = EntityState.Unchanged;
                // }
            }

            return result;
        }

        private static async Task<int> SaveByCopyEntityAsync(DbContext dbContext,
            Type entityType, object copyHelper, CancellationToken cancellationToken)
        {
            var castMethod = _castMethodDict.GetOrAdd(entityType, type => typeof(Enumerable).GetMethod("Cast")
                .MakeGenericMethod(type));

            using (await ConnAsyncLock.LockAsync(cancellationToken))
            {
                do
                {
                    await Task.Delay(200, cancellationToken);
                } while (dbContext.Database.GetDbConnection().State == ConnectionState.Connecting);

                if (dbContext.Database.GetDbConnection().State == ConnectionState.Closed ||
                    dbContext.Database.GetDbConnection().State == ConnectionState.Broken)
                {
                    await dbContext.Database.GetDbConnection().OpenAsync(cancellationToken);
                }
            }

            var saveAllMethodInfo = copyHelper.GetType()
                .GetMethods()
                .FirstOrDefault(x => x.Name.Equals("SaveAllAsync", StringComparison.Ordinal));
            var entities =
                castMethod.Invoke(null,
                    new object[]
                    {
                        dbContext.ChangeTracker.Entries().Where(x => x.Entity.GetType() == entityType)
                            .Select(x => x.Entity)
                    });
            return Convert.ToInt32(await (ValueTask<UInt64>) saveAllMethodInfo?.Invoke(copyHelper,
                new[] {(NpgsqlConnection) dbContext.Database.GetDbConnection(), entities, cancellationToken}));
        }

        private static int SaveByCopyEntity(DbContext dbContext,
            Type entityType, object copyHelper)
        {
            var castMethod = _castMethodDict.GetOrAdd(entityType, type => typeof(Enumerable).GetMethod("Cast")
                .MakeGenericMethod(type));

            if (dbContext.Database.GetDbConnection().State == ConnectionState.Closed ||
                dbContext.Database.GetDbConnection().State == ConnectionState.Broken)
            {
                dbContext.Database.GetDbConnection().Open();
            }

            var saveAllMethodInfo = copyHelper.GetType()
                .GetMethod("SaveAll", BindingFlags.Instance | BindingFlags.Public);
            
            var entities =
                castMethod.Invoke(null, new object[] {dbContext
                    .ChangeTracker
                    .Entries()
                    .Where(x => x.Entity.GetType() == entityType)
                    .Select(x => x.Entity)});
            
            return Convert.ToInt32(saveAllMethodInfo?.Invoke(copyHelper,
                new[] {(NpgsqlConnection) dbContext.Database.GetDbConnection(), entities}));
        }

        private static object PostgreSqlCopyHelperFactory(IEntityType entityType)
        {
            var openType = typeof(PostgreSQLCopyHelper.PostgreSQLCopyHelper<>);

            Type[] tArgs = {entityType.ClrType};
            var target = openType.MakeGenericType(tArgs);

            var c = target.GetConstructor(new[] {typeof(string), typeof(string)});
            if (c == null)
                throw new InvalidOperationException("A constructor for type was not found.");

            var copyHelper = c.Invoke(new object[] {entityType.GetSchema(), QuoteIdentifier(entityType.GetTableName())});

            var mapMethodInfo = copyHelper.GetType().GetMethods().First(n =>
                n.Name == "Map" && n.GetGenericArguments().Length == 1 && n.GetParameters().Length == 3 &&
                n.GetParameters()[2].ParameterType == typeof(NpgsqlDbType));

            var textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;

            foreach (var propertyType in entityType.GetProperties()
                .Where(p => p.ValueGenerated == ValueGenerated.Never))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "__prop");
                var property = Expression.Property(parameter, propertyType.Name);

                var lambda = Expression.Lambda(property, parameter).Compile();

                var columnName = QuoteIdentifier(propertyType.GetColumnName());
                var colType = textInfo.ToTitleCase(propertyType.GetColumnType());
                var npgsqlDbType = GetNpgsqlDbType(colType);
                var method = mapMethodInfo?.MakeGenericMethod(propertyType.ClrType);

                method?.Invoke(copyHelper,
                    new object[]
                    {
                        columnName, lambda,
                        npgsqlDbType
                    });
            }

            return copyHelper;
        }

        private static Lazy<ReadOnlyDictionary<string, NpgsqlDbType>> _npgsqlDbTypeMapping =
            new Lazy<ReadOnlyDictionary<string, NpgsqlDbType>>(GetNpgsqlTypeMapping);

        private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\\\"") + "\"";

        private static NpgsqlDbType GetNpgsqlDbType(string colType)
        {
            if (Enum.TryParse<NpgsqlDbType>(colType, out var npgsqlDbType))
                return npgsqlDbType;

            if (_npgsqlDbTypeMapping.Value.ContainsKey(colType.ToLowerInvariant()))
                return _npgsqlDbTypeMapping.Value[colType.ToLowerInvariant()];

            var internalName = TranslateInternalName(colType.ToLowerInvariant());
            if (_npgsqlDbTypeMapping.Value.ContainsKey(internalName.ToLowerInvariant()))
                return _npgsqlDbTypeMapping.Value[internalName.ToLowerInvariant()];
            throw new Exception($"{colType} cannot be mapped to Postgres type");
        }

        /// <summary>
        /// @see https://sourcegraph.com/github.com/npgsql/npgsql@361ea2e8d615f5a26e2a9eabecd3f80e1a88217c/-/blob/src/Npgsql/PostgresTypes/PostgresBaseType.cs#L29
        /// </summary>
        /// <returns></returns>
        private static ReadOnlyDictionary<string, NpgsqlDbType> GetNpgsqlTypeMapping()
        {
            var dict = new Dictionary<string, NpgsqlDbType>();
            var t = typeof(NpgsqlDbType).Assembly.GetType("NpgsqlTypes.BuiltInPostgresType");

            if (t == null) return new ReadOnlyDictionary<string, NpgsqlDbType>(dict);

            var fields = typeof(NpgsqlDbType).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields.Select(x => new {x, Attr = x.GetCustomAttribute(t)})
                .Where(x => x.Attr != null))
            {
                var prop = field.Attr
                    .GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(x => x.Name == "Name");

                if (prop?.GetValue(field.Attr) != null)
                    dict.TryAdd((string) prop.GetValue(field.Attr), Enum.Parse<NpgsqlDbType>(field.x.Name, true));
            }

            return new ReadOnlyDictionary<string, NpgsqlDbType>(dict);
        }

        static string TranslateInternalName(string internalName)
        {
            return internalName switch
            {
                "boolean" => "bool",
                "character" => "bpchar",
                "numeric" => "decimal",
                "float4" => "real",
                "real" => "float4",
                "double precision" => "float8",
                "smallint" => "int2",
                "integer" => "int4",
                "bigint" => "int8",
                "time with time zone" => "timetz",
                "timestamp with time zone" => "timestamp",
                "time without time zone" => "timetz",
                "timestamp without time zone" => "timestamp",
                "bit varying" => "varbit",
                "character varying" => "varchar",
                _ => internalName
            };
        }
    }
}