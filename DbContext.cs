using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EntityFrameworkIndex
{
    /// <summary>
    /// DbContext с поддержкой аттрибутов Index и FullTextIndex
    /// </summary>
    public abstract class DbContextIndexed : DbContext
    {
        private static bool Complete;
        private int? language;

        /// <summary>
        /// Код языка для полнотекстового поиска
        /// По умолчанию используется "Русский"
        /// </summary>
        public int Language
        {
            get
            {
                return language.HasValue ? language.Value : 1049; //1049 - русский язык
            }
            set
            {
                language = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!Complete)
            {
                Complete = true;
                CalculateIndexes();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Пересчитать индексы
        /// </summary>
        private void CalculateIndexes()
        {
            if (GetCompleteFlag()) return;

            //Получаем все сущности текущего DbContext
            foreach (var property in this.GetType().GetProperties().Where(f => f.PropertyType.BaseType != null && f.PropertyType.BaseType.Name == "DbQuery`1"))
            {
                var currentEntityType = property.PropertyType.GetGenericArguments().FirstOrDefault();
                if (currentEntityType == null || currentEntityType.BaseType.FullName != "System.Object")
                    continue;

                //Получаем название таблицы в БД
                var tableAttribute = currentEntityType.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
                var tableName = tableAttribute != null ? tableAttribute.Name : property.Name;

                //Получаем у сущности свойства помеченые аттрибутом Index, создаем по ним индекс
                BuildingIndexes(tableName, currentEntityType.GetProperties().Where(f => f.GetCustomAttributes(typeof(IndexAttribute), false).Any()));

                //Получаем у сущности свойства помеченые аттрибутом FullTextIndex, создаем по ним индекс
                BuildingFullTextIndexes(tableName, currentEntityType.GetProperties().Where(f => f.GetCustomAttributes(typeof(FullTextIndexAttribute), false).Any()));
            }

            CreateCompleteFlag();
        }

        /// <summary>
        /// Создание индексов
        /// </summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="propertyes">Коллекция свойств сущности</param>
        private void BuildingIndexes(string tableName, IEnumerable<PropertyInfo> propertyes)
        {
            foreach (var property in propertyes)
                Database.ExecuteSqlCommand(String.Format("CREATE INDEX IX_{0} ON {1} ({0})", property.Name, tableName)); //Создаем индекс
        }

        /// <summary>
        /// Создание полнотекстовых индексов
        /// </summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="propertyes">Коллекция свойств сущности</param>
        private void BuildingFullTextIndexes(string tableName, IEnumerable<PropertyInfo> propertyes)
        {
            var fullTextColumns = string.Empty;
            foreach (var property in propertyes)
                fullTextColumns += String.Format("{0}{1} language {2}", (string.IsNullOrWhiteSpace(fullTextColumns) ? null : ","), property.Name, Language);

            //Создаем полнотекстовый индекс
            Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction,
                String.Format("IF NOT EXISTS (SELECT * FROM sysindexes WHERE id=object_id('{1}') and name='IX_{2}') CREATE UNIQUE INDEX IX_{2} ON {1} ({2});CREATE FULLTEXT CATALOG FTXC_{1} AS DEFAULT;CREATE FULLTEXT INDEX ON {1}({0}) KEY INDEX [IX_{2}] ON ([FTXC_{1}]) WITH STOPLIST = SYSTEM;", fullTextColumns, tableName, "Id"));
        }

        private void CreateCompleteFlag()
        {
            Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction, "CREATE TABLE [dbo].[__IndexBuildingHistory]([DataContext] [nvarchar](255) NOT NULL, [Complete] [bit] NOT NULL, CONSTRAINT [PK___IndexBuildingHistory] PRIMARY KEY CLUSTERED ([DataContext] ASC))");
        }

        private bool GetCompleteFlag()
        {
            var queryResult = Database.SqlQuery(typeof(string), "IF OBJECT_ID('__IndexBuildingHistory', 'U') IS NOT NULL SELECT 'True' AS 'Result' ELSE SELECT 'False' AS 'Result'").GetEnumerator();
            queryResult.MoveNext();
            return bool.Parse(queryResult.Current as string);
        }
    }
}
