using Bogus;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.Utilities
{
    public class RegisterAccountData
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public static class TestDataGenerator
    {
        public static RegisterAccountData GenerateRandomCandidate()
        {
            
            var faker = new Faker<RegisterAccountData>("vi")

               
                .RuleFor(u => u.FullName, f => f.Name.FullName())

               
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FullName))

               
                .RuleFor(u => u.Password, f => "P@ssw0rd123!");

            return faker.Generate();
        }
    }
}
