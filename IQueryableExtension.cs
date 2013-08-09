using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EntityFrameworkIndex
{
    public static class IQueryableExtension
    {
        public static IQueryable<T> FullTextSearch<T>(this DbSet<T> queryable, Expression<Func<T, bool>> func, FullTextIndex.SearchAlgorithm algorithm = FullTextIndex.SearchAlgorithm.FreeText) where T : class
        {
            var internalSet = queryable.AsQueryable().GetType().GetProperty("System.Data.Entity.Internal.Linq.IInternalSetAdapter.InternalSet", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(queryable.AsQueryable());
            var entitySet = (EntitySet)internalSet.GetType().GetProperty("EntitySet").GetValue(internalSet);

            var searchType = algorithm == FullTextIndex.SearchAlgorithm.Contains ? "CONTAINS" : "FREETEXT";
            var columnName = ((MemberExpression)((BinaryExpression)func.Body).Left).Member.Name;
            var searchPattern = ((ConstantExpression)((BinaryExpression)func.Body).Right).Value;

            return queryable.SqlQuery(String.Format("SELECT * FROM {0} WHERE {1};", entitySet.Name, String.Format("{0}({1},'{2}')", searchType, columnName, searchPattern))).AsQueryable();
        }
    }
}
