using Core.Helpers;

namespace Test.Core.Helpers;

public class ValidatorClassTests
{
    private sealed class DummyValidator : ValidatorClass
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public string? Name { get; set; }
        public byte[]? Token { get; set; }

        protected override void Validate()
        {
            IdValidation(Id, nameof(Id), validateZero: true);
            PositiveValueValidation(nameof(Amount), Amount, validateZero: true);
            NotEmptyStringValidation(nameof(Name), Name);
            ByteArraySizeValidation(nameof(Token), Token, expectedSize: 4);
        }
    }

    [Fact]
    public void Errors_Summary_ContainsRegisteredErrors()
    {
        var v = new DummyValidator { Id = -1, Amount = -1, Name = null, Token = null };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.Contains(GenericErrors.NegativeIdError.ToString(), v.Errors.Summary);
        Assert.Contains(GenericErrors.NegativeValueError.ToString(), v.Errors.Summary);
        Assert.Contains(GenericErrors.EmptyStringError.ToString(), v.Errors.Summary);
        Assert.Contains(GenericErrors.ByteArraySizeError.ToString(), v.Errors.Summary);
    }

    [Fact]
    public void ContainsError_WhenErrorWasRegistered_ReturnsTrue()
    {
        var v = new DummyValidator { Id = 0, Amount = 1, Name = "ok", Token = new byte[] { 1, 2, 3, 4 } };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.True(v.ContainsError(GenericErrors.IdZeroError, nameof(DummyValidator.Id)));
    }

    [Fact]
    public void IdValidation_WhenZeroNotValidated_DoesNotRegisterIdZeroError()
    {
        var v = new ZeroIdAllowedValidator { Id = 0 };
        v.ValidateValueObjectsForTests();

        Assert.True(v.IsValid);
        Assert.DoesNotContain(GenericErrors.IdZeroError.ToString(), v.Errors.Summary);
    }

    [Fact]
    public void IdValidation_WhenZeroValidated_DoesRegisterIdZeroError()
    {
        var v = new ZeroIdNotAllowedValidator { Id = 0 };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.Contains(GenericErrors.IdZeroError.ToString(), v.Errors.Summary);
    }

    [Fact]
    public void IdValidation_WhenNegative_DoesRegisterNegativeIdError()
    {
        var v = new DummyValidator { Id = -1, Amount = 1, Name = "ok", Token = new byte[] { 1, 2, 3, 4 } };

        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.Contains(GenericErrors.NegativeIdError.ToString(), v.Errors.Summary);
    }

    private sealed class ZeroIdAllowedValidator : ValidatorClass
    {
        public int Id { get; set; }
        protected override void Validate() => IdValidation(Id, nameof(Id), validateZero: false);
    }

    private sealed class ZeroIdNotAllowedValidator : ValidatorClass
    {
        public int Id { get; set; }
        protected override void Validate() => IdValidation(Id, nameof(Id), validateZero: true);
    }

    [Fact]
    public void PositiveValueValidation_WhenZero_RegistersValueZeroError()
    {
        var v = new AmountOnlyValidator { Amount = 0 };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.Contains(GenericErrors.ValueZeroError.ToString(), v.Errors.Summary);
        Assert.True(v.ContainsError(GenericErrors.ValueZeroError, nameof(AmountOnlyValidator.Amount)));
    }

    [Fact]
    public void PositiveValueValidation_WhenBellowZero_RegistersValueZeroError()
    {
        var v = new AmountOnlyValidator { Amount = -1 };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.Contains(GenericErrors.NegativeValueError.ToString(), v.Errors.Summary);
        Assert.True(v.ContainsError(GenericErrors.NegativeValueError, nameof(AmountOnlyValidator.Amount)));
    }

    private sealed class AmountOnlyValidator : ValidatorClass
    {
        public int Amount { get; set; }
        protected override void Validate() => PositiveValueValidation(nameof(Amount), Amount, validateZero: true);
    }

    [Fact]
    public void NotEmptyStringValidation_WhenEmpty_RegistersEmptyStringError()
    {
        var v = new NameOnlyValidator { Name = "" };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.Contains(GenericErrors.EmptyStringError.ToString(), v.Errors.Summary);
        Assert.True(v.ContainsError(GenericErrors.EmptyStringError, nameof(NameOnlyValidator.Name)));
    }

    private sealed class NameOnlyValidator : ValidatorClass
    {
        public string? Name { get; set; }
        protected override void Validate() => NotEmptyStringValidation(nameof(Name), Name);
    }

    [Fact]
    public void ByteArraySizeValidation_WhenWrongSize_RegistersError()
    {
        var v = new DummyValidator { Id = 1, Amount = 1, Name = "ok", Token = new byte[] { 1, 2, 3 } };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.Contains(GenericErrors.ByteArraySizeError.ToString(), v.Errors.Summary);
    }

    [Fact]
    public void ByteArraySizeValidation_WhenCorrectSize_DoesNotRegisterError()
    {
        var v = new DummyValidator { Id = 1, Amount = 1, Name = "ok", Token = new byte[] { 1, 2, 3, 4 } };
        v.ValidateValueObjectsForTests();

        Assert.True(v.IsValid);
        Assert.Empty(v.Errors);
    }

    [Fact]
    public void ByteArraySizeValidation_WhenIncorrectSize_DoesRegisterError()
    {
        var v = new DummyValidator { Id = 1, Amount = 1, Name = "ok", Token = new byte[] { 1, 2, 3 } };
        v.ValidateValueObjectsForTests();

        Assert.False(v.IsValid);
        Assert.NotEmpty(v.Errors);
    }

    [Fact]
    public void Errors_IsEnumerable_AndToStringMatchesSummary()
    {
        var v = new DummyValidator { Id = -1, Amount = 1, Name = "ok", Token = new byte[] { 1, 2, 3, 4 } };
        v.ValidateValueObjectsForTests();

        // IEnumerable coverage
        Assert.Single(v.Errors);

        // ToString coverage
        Assert.Equal(v.Errors.Summary, v.Errors.ToString());
    }

    [Fact]
    public void Error_PropertiesNamesSummary_WhenNoProperties_ReturnsEmpty()
    {
        var e = new Error(GenericErrors.InvalidObjectError, "msg");
        Assert.Equal(string.Empty, e.PropertiesNamesSummary);
    }

    [Fact]
    public void Error_PropertiesNamesSummary_ReturnsCommaSeparatedNames()
    {
        var e = new Error(GenericErrors.InvalidObjectError, "msg", "A", "B");
        Assert.Equal("A, B", e.PropertiesNamesSummary);
    }
}

internal static class ValidatorExtensions
{
    public static void ValidateValueObjectsForTests(this ValidatorClass validator)
    {
        // force Validate() execution by invoking a protected workflow via reflection
        var method = validator.GetType().GetMethod("Validate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method!.Invoke(validator, null);
    }
}
