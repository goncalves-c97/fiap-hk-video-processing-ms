using Dapper;
using System.Reflection;

namespace Infra.Data.SqlServer
{
    public class SnakeCaseTypeMapper<T> : FallbackTypeMapper
    {
        public SnakeCaseTypeMapper()
            : base(new SqlMapper.ITypeMap[]
            {
            new CustomPropertyTypeMap(typeof(T),
                (type, columnName) =>
                    type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(prop => ToSnakeCase(prop.Name) == columnName)),
            new DefaultTypeMap(typeof(T))
            })
        {
        }

        private static string ToSnakeCase(string str)
        {
            return string.Concat(str.Select((ch, i) =>
                i > 0 && char.IsUpper(ch) ? "_" + char.ToLower(ch) : char.ToLower(ch).ToString()));
        }
    }

    public class FallbackTypeMapper : SqlMapper.ITypeMap
    {
        private readonly SqlMapper.ITypeMap[] _mappers;

        public FallbackTypeMapper(SqlMapper.ITypeMap[] mappers)
        {
            _mappers = mappers;
        }

        public ConstructorInfo FindConstructor(string[] names, Type[] types) =>
            _mappers.Select(mapper => mapper.FindConstructor(names, types)).FirstOrDefault(c => c != null);

        public ConstructorInfo FindExplicitConstructor() =>
            _mappers.Select(mapper => mapper.FindExplicitConstructor()).FirstOrDefault(c => c != null);

        public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName) =>
            _mappers.Select(mapper => mapper.GetConstructorParameter(constructor, columnName))
                    .FirstOrDefault(result => result != null);

        public SqlMapper.IMemberMap GetMember(string columnName) =>
            _mappers.Select(mapper => mapper.GetMember(columnName)).FirstOrDefault(result => result != null);
    }
}
