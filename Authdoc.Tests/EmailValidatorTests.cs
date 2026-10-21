using Authdoc.Validators;

namespace Authdoc.Tests;

public class EmailValidatorTests
{
    [Fact]
    public void IsValid_WithValidEmail_ReturnsTrue()
    {
        var email = "gabriel@gmail.com";
        
        var result = EmailValidator.IsValid(email);
        
        Assert.True(result);
    }

    [Theory]
    [InlineData("gabriel@gmail..com")]
    [InlineData("gabriel@gmail.com")]
    [InlineData("gabriel@empresa.com.br")]
    [InlineData("gabriel+teste@empresa.com")]
    [InlineData("gabriel.carvalho@empresa.pt")]
    [InlineData("g_c-1@sub.dominio.org")]

    public void IsValid_WithInvalidEmail_ReturnsFalse(string email)
    {
        var result = EmailValidator.IsValid(email);
        Assert.False(result);
    }
}