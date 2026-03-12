using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Dapper;
using Infra.Data.SqlServer;

namespace Test.Infra.Data.SqlServer;

public class SnakeCaseTypeMapperTests
{
    private sealed class Sample
    {
        public int IdColaborador { get; set; }
        public string? EmailAddress { get; set; }
    }

    private static MemberInfo GetMappedMemberInfo(SqlMapper.IMemberMap map)
    {
        var t = map.GetType();

        // Dapper 2.x SimpleMemberMap uses these:
        var memberProp = t.GetProperty("Member"); // <-- add support for Dapper's "Member"
        var propProp = t.GetProperty("Property");
        var fieldProp = t.GetProperty("Field");
        var paramProp = t.GetProperty("Parameter");

        var mi =
            (memberProp?.GetValue(map) as MemberInfo) ??
            (propProp?.GetValue(map) as MemberInfo) ??
            (fieldProp?.GetValue(map) as MemberInfo) ??
            (paramProp?.GetValue(map) as MemberInfo);

        Assert.NotNull(mi);
        return mi!;
    }

    [Fact]
    public void GetMember_WhenSnakeCaseColumn_MapsToMatchingProperty()
    {
        var mapper = new SnakeCaseTypeMapper<Sample>();

        var member = mapper.GetMember("id_colaborador");

        Assert.NotNull(member);
        var mi = GetMappedMemberInfo(member);
        Assert.Equal(nameof(Sample.IdColaborador), mi.Name);
        Assert.IsAssignableFrom<PropertyInfo>(mi); // <-- was IsType<PropertyInfo>
    }

    [Fact]
    public void GetMember_WhenSnakeCaseColumnWithMultipleWords_MapsToMatchingProperty()
    {
        var mapper = new SnakeCaseTypeMapper<Sample>();

        var member = mapper.GetMember("email_address");

        Assert.NotNull(member);
        var mi = GetMappedMemberInfo(member);
        Assert.Equal(nameof(Sample.EmailAddress), mi.Name);
    }

    [Fact]
    public void GetMember_WhenNoMatch_ReturnsNull()
    {
        var mapper = new SnakeCaseTypeMapper<Sample>();

        var member = mapper.GetMember("does_not_exist");

        Assert.Null(member);
    }

    [Fact]
    public void FallbackTypeMapper_TriesMappersInOrder()
    {
        var expected = new SnakeCaseTypeMapper<Sample>().GetMember("id_colaborador");
        Assert.NotNull(expected);

        SqlMapper.ITypeMap nullMap = new NullMemberTypeMap();
        SqlMapper.ITypeMap okMap = new FixedMemberTypeMap(expected);
        var sut = new FallbackTypeMapper(new[] { nullMap, okMap });

        // act
        var member = sut.GetMember("anything");

        // assert
        Assert.Same(expected, member);
    }

    private sealed class NullMemberTypeMap : SqlMapper.ITypeMap
    {
        public ConstructorInfo? FindConstructor(string[] names, Type[] types) => null;
        public ConstructorInfo? FindExplicitConstructor() => null;
        public SqlMapper.IMemberMap? GetConstructorParameter(ConstructorInfo constructor, string columnName) => null;
        public SqlMapper.IMemberMap? GetMember(string columnName) => null;
    }

    private sealed class FixedMemberTypeMap(SqlMapper.IMemberMap fixedMember) : SqlMapper.ITypeMap
    {
        public ConstructorInfo? FindConstructor(string[] names, Type[] types) => null;
        public ConstructorInfo? FindExplicitConstructor() => null;
        public SqlMapper.IMemberMap? GetConstructorParameter(ConstructorInfo constructor, string columnName) => null;
        public SqlMapper.IMemberMap? GetMember(string columnName) => fixedMember;
    }
}
