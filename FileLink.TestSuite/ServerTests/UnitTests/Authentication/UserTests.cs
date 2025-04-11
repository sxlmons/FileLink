using System;
using NUnit.Framework;
using FileLink.Server.Authentication;
using Assert = NUnit.Framework.Assert;

namespace FileLink.TestSuite.ServerTests.UnitTests.Authentication
{
    [TestFixture]
    public class UserTests
    {
        [Test]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var user = new User();
            
            // Assert
            Assert.That(user.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(user.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(5).Seconds);
            Assert.That(user.Role, Is.EqualTo("User"));
            Assert.That(user.LastLoginAt, Is.Null);
            Assert.That(user.UpdatedAt, Is.EqualTo(DateTime.MinValue));
        }
        
        [Test]
        public void ParameterizedConstructor_SetsCorrectValues()
        {
            // Arrange
            string username = "testuser";
            string email = "test@example.com";
            string role = "Admin";
            
            // Act
            var user = new User(username, email, role);
            
            // Assert
            Assert.That(user.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(user.Username, Is.EqualTo(username));
            Assert.That(user.Email, Is.EqualTo(email));
            Assert.That(user.Role, Is.EqualTo(role));
            Assert.That(user.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(5).Seconds);
            Assert.That(user.LastLoginAt, Is.Null);
        }
        
        [Test]
        public void UpdateLastLogin_SetsLastLoginToCurrentTime()
        {
            // Arrange
            var user = new User("testuser", "test@example.com", "User");
            
            // Act
            user.UpdateLastLogin();
            
            // Assert
            Assert.That(user.LastLoginAt, Is.Not.Null);
            Assert.That(user.LastLoginAt.Value, Is.EqualTo(DateTime.UtcNow).Within(5).Seconds);
        }
    }
}