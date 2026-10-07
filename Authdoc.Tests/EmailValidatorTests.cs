using Authdoc.Validators;

namespace Authdoc.Tests;

public class EmailValidatorTests
{
    [Fact]
    public void IsValid_WithAllowedDomain_ReturnsTrue()
    {
        // Arrange
        var email = "gabriel@gmail.com";
        
        // Act
        var result = EmailValidator.IsValid(email);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithNotAllowedDomain_ReturnsFalse()
    {
        var email = "gabriel@yahoo.com";
        
        var result = EmailValidator.IsValid(email);
        
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithoutAt_ReturnsFalse()
    {
        var email = "gabrielgmail.com";
        
        var result = EmailValidator.IsValid(email);
        
        Assert.False(result);
    }

    [Fact]
    public void Normalize_WithSpacesAndUppercase_ReturnsCleanEmail()
    {
        var email = " Gabrielgmail.com ";
        var result = EmailValidator.Normalize(email);
        Assert.Equal("gabrielgmail.com", result);
    }
}