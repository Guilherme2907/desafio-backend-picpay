namespace PipcPaySimplified.Domain.ValueObjects;

public class Cpf(string value)
{
    public string Value { get; set; } = value;
}
