using System.Linq.Expressions;
using System.Reflection;
using DB.Abstract;
using Application.DataAccessLayer.Interface.Common;

namespace Application.DataAccessLayer.Service.Common
{
    public class FilterService<T> : IFilterService<T> where T : IEntity
    {
        public FilterResult<T> ApplyFilters(IQueryable<T> query, FilterParams parameters)
        {
            try
            {
                // Основной фильтр
                if (!string.IsNullOrEmpty(parameters.SearchString) && !string.IsNullOrEmpty(parameters.SearchProperty))
                {
                    query = ApplyFilter(query, parameters.SearchProperty, parameters.SearchString, "==");
                }

                // Дополнительные фильтры
                if (parameters.AdditionalFilters != null)
                {
                    foreach (var filter in parameters.AdditionalFilters)
                    {
                        query = ApplyFilter(query, filter.PropertyPath, filter.SearchValue, filter.Operator);
                    }
                }

                // Сортировка
                if (!string.IsNullOrEmpty(parameters.SortProperty))
                {
                    query = ApplySorting(query, parameters.SortProperty, parameters.SortDirection);
                }
                else
                {
                    query = ApplySorting(query, "Id", "asc");
                }

                // Пагинация
                var totalItems = query.Count();
                var result = query
                    .Skip((parameters.Page - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToList();

                return new FilterResult<T>(result, totalItems, parameters);
            }
            catch (Exception ex)
            {
                return new FilterResult<T>(new List<T>(), 0, parameters)
                {
                    ErrorMessage = ex.Message
                };
            }
        }

        public FilterResult<T> ApplyFilters(IEnumerable<T> collection, FilterParams parameters)
        {
            return ApplyFilters(collection.AsQueryable(), parameters);
        }

        private IQueryable<T> ApplyFilter(IQueryable<T> query, string propertyPath, string searchValue, string @operator)
        {
            if (string.IsNullOrWhiteSpace(propertyPath) || string.IsNullOrWhiteSpace(searchValue))
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = GetPropertyExpression(parameter, propertyPath);

            // Для строковых свойств и оператора "==" используем Contains для поиска по содержанию
            if (property.Type == typeof(string) && @operator == "==")
            {
                // Регистронезависимый поиск с использованием ToUpper или ToLower
                var searchValueUpper = searchValue.ToUpper();

                // Получаем методы для работы со строками
                var toUpperMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                // Приводим свойство к верхнему регистру
                var propertyToUpper = Expression.Call(property, toUpperMethod);

                // Создаем константу с поисковым значением в верхнем регистре
                var constantUpper = Expression.Constant(searchValueUpper, typeof(string));

                // Создаем условие: property.ToUpper().Contains(searchValueUpper)
                var condition = Expression.Call(propertyToUpper, containsMethod, constantUpper);

                var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);
                return query.Where(lambda);
            }
            else
            {
                // Для остальных типов данных и операторов используем стандартные сравнения
                try
                {
                    object convertedValue;

                    // Пробуем преобразовать значение в тип свойства
                    if (property.Type.IsEnum)
                    {
                        // Для Enum пытаемся преобразовать как число или по имени
                        if (int.TryParse(searchValue, out int enumValue))
                        {
                            convertedValue = Enum.ToObject(property.Type, enumValue);
                        }
                        else
                        {
                            convertedValue = Enum.Parse(property.Type, searchValue, true);
                        }
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(searchValue, property.Type);
                    }

                    var constant = Expression.Constant(convertedValue, property.Type);

                    // Создаем выражение на основе оператора
                    Expression condition = @operator switch
                    {
                        ">=" => Expression.GreaterThanOrEqual(property, constant),
                        "<=" => Expression.LessThanOrEqual(property, constant),
                        ">" => Expression.GreaterThan(property, constant),
                        "<" => Expression.LessThan(property, constant),
                        "==" => Expression.Equal(property, constant),
                        "!=" => Expression.NotEqual(property, constant),
                        _ => throw new NotSupportedException($"Оператор '{@operator}' не поддерживается")
                    };

                    var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);
                    return query.Where(lambda);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Невозможно преобразовать значение '{searchValue}' к типу '{property.Type.Name}': {ex.Message}");
                }
            }
        }

        private IQueryable<T> ApplySorting(IQueryable<T> query, string propertyPath, string sortDirection)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = GetPropertyExpression(parameter, propertyPath);
            var lambda = Expression.Lambda(property, parameter);

            string methodName = sortDirection == "desc" ? "OrderByDescending" : "OrderBy";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), property.Type },
                query.Expression,
                Expression.Quote(lambda));

            return query.Provider.CreateQuery<T>(resultExpression);
        }

        private Expression GetPropertyExpression(ParameterExpression parameter, string propertyPath)
        {
            Expression property = parameter;
            foreach (var member in propertyPath.Split('.'))
            {
                var propInfo = property.Type.GetProperty(member,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propInfo == null)
                    throw new ArgumentException($"Property '{member}' not found in path '{propertyPath}'");

                property = Expression.Property(property, propInfo);
            }
            return property;
        }
    }

    public class FilterParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public string? SearchString { get; set; }
        public string? SearchProperty { get; set; }
        public string? SortProperty { get; set; }
        public string SortDirection { get; set; } = "asc";
        public List<FilterCondition> AdditionalFilters { get; set; } = new();
    }

    public class FilterCondition
    {
        public string PropertyPath { get; set; } = string.Empty;
        public string SearchValue { get; set; } = string.Empty;
        public string Operator { get; set; } = "==";
    }

    public class FilterResult<T>
    {
        public List<T> Data { get; set; }
        public int TotalItems { get; set; }
        public FilterParams FilterParams { get; set; }
        public string? ErrorMessage { get; set; }

        public FilterResult(List<T> data, int totalItems, FilterParams filterParams)
        {
            Data = data;
            TotalItems = totalItems;
            FilterParams = filterParams;
        }
    }
}