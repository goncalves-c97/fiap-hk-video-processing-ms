using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace Core.Helpers;

public class Errors : IEnumerable<Error>
{
    private readonly ICollection<Error> errors = [];

    public void RegisterError(Enum errorEnum, string message) => errors.Add(new Error(errorEnum, message));
    public void RegisterError(Enum errorEnum, string message, params string?[] propertyName) => errors.Add(new Error(errorEnum, message, propertyName));

    public IEnumerator<Error> GetEnumerator()
    {
        return errors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return errors.GetEnumerator();
    }

    public string Summary
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var item in errors)
                sb.AppendLine($"{item.ErrorEnum} - {item.Message}");
            return sb.ToString();
        }
    }

    public override string ToString()
    {
        return $"{Summary}";
    }
}

public class Error()
{

    public Enum ErrorEnum { get; set; }

    public string?[] PropertyNames { get; set; }

    public string Message { get; set; }

    public string PropertiesNamesSummary
    {
        get
        {
            StringBuilder sb = new StringBuilder();

            if (PropertyNames == null || PropertyNames.Length == 0)
                return string.Empty;

            foreach (string propertyName in PropertyNames)
            {
                sb.Append($", {propertyName}");
            }

            return sb.ToString()[2..];
        }
    }
    public Error(Enum errorEnum, string message) : this()
    {
        ErrorEnum = errorEnum;
        Message = message;
    }

    public Error(Enum errorEnum, string message, params string[] propertyName) : this()
    {
        ErrorEnum = errorEnum;
        PropertyNames = propertyName;
        Message = message;
    }
}

public enum GenericErrors
{
    NegativeIdError,
    IdZeroError,
    NegativeValueError,
    ValueZeroError,
    EmptyStringError,
    StringSizeExcedeedError,
    StringMinSizeNotReachedError,
    StringOutOfSizeRangeError,
    NullValueError,
    InvalidObjectError,
    InvalidBsonIdError,
    SameDateTimeError,
    StartBiggerThanEndDateTimeError,
    DateTimeBiggerThanNowError,
    InvalidIpError,
    InvalidTokenError,
    ByteArraySizeError
}

/// <summary>
/// Classe para validação de entidades, com diversos métodos para validação de forma mais simplificada
/// </summary>
public abstract class ValidatorClass
{
    private readonly Errors errors = [];

    [JsonIgnore]
    public bool IsValid => !errors.Any();
    [JsonIgnore, NotMapped]
    public Errors Errors => errors;

    protected abstract void Validate();

    public bool ContainsError(Enum errorEnum, string propertyName) => errors.Any(x => x.ErrorEnum.ToString() == errorEnum.ToString() && x.PropertyNames.Contains(propertyName));

    protected void IdValidation(int id, string? propertyName = null, bool validateZero = false)
    {
        string propertyNameToConsider = propertyName != null ? propertyName : "Id";

        if (!int.IsPositive(id))
            Errors.RegisterError(GenericErrors.NegativeIdError, $"'{propertyNameToConsider}' não pode ser negativo.", propertyName);

        if (validateZero && id == 0)
            Errors.RegisterError(GenericErrors.IdZeroError, $"'{propertyNameToConsider}' não pode ser 0", propertyName);
    }

    protected void PositiveValueValidation(string propertyName, int value, bool validateZero = false)
    {
        if (!int.IsPositive(value))
            Errors.RegisterError(GenericErrors.NegativeValueError, $"'{propertyName}' não pode ser negativo(a).", propertyName);

        if (validateZero && value == 0)
            Errors.RegisterError(GenericErrors.ValueZeroError, $"'{propertyName}' não pode ser 0", propertyName);
    }
    
    protected void NotEmptyStringValidation(string propertyName, string? propertyValue)
    {
        if (string.IsNullOrEmpty(propertyValue))
            Errors.RegisterError(GenericErrors.EmptyStringError, $"'{propertyName}' não informado(a)!", propertyName);
    }
    
    protected void ByteArraySizeValidation(string propertyName, byte[]? propertyValue, int expectedSize)
    {
        if (propertyValue == null || propertyValue.Length != expectedSize)
            Errors.RegisterError(GenericErrors.ByteArraySizeError, $"'{propertyName}' deve conter exatamente {expectedSize} bytes.", propertyName);
    }
}
