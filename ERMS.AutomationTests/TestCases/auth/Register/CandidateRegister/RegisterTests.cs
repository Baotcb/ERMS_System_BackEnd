using ERMS.AutomationTests.Core;
using ERMS.AutomationTests.PageObjects.Home;
using ERMS.AutomationTests.Utilities;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.TestCases.auth.Register.CandidateRegister
{
    [TestFixture]
    public class RegisterTests : BaseTest
    {
        [Test]
        public void Test_UserCanRegisterSuccessfully()
        {
            
            var candidateData = TestDataGenerator.GenerateRandomCandidate();

            var homePage = new HomePage(Driver);
            homePage.GoToHomePage();
            homePage.ClickRegisterNavigationButton();

            var registerPage = new RegisterPage(Driver);
            registerPage.Register(
                fullName: candidateData.FullName,
                email: candidateData.Email,
                password: candidateData.Password,
                confirmPassword: candidateData.Password
            );

            bool isMessageShown = registerPage.IsSuccessMessageDisplayed();
            Assert.That(isMessageShown, "Lỗi: Không hiển thị thông báo 'Đăng ký thành công!'.");

            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            bool isRedirected = wait.Until(d => d.Url.Contains("/login"));
            Assert.That(isRedirected, $"Lỗi: Không tự động chuyển về trang Đăng nhập. URL hiện tại: {Driver.Url}");
        }

    

        [Test]
        public void Test_UserCannotRegisterWithMismatchedPasswords()
        {
            var candidateData = TestDataGenerator.GenerateRandomCandidate();

            var homePage = new HomePage(Driver);
            homePage.GoToHomePage();
            homePage.ClickRegisterNavigationButton();

            var registerPage = new RegisterPage(Driver);
            registerPage.Register(
                fullName: candidateData.FullName,
                email: candidateData.Email,
                password: candidateData.Password,
                confirmPassword: "DifferentPassword123!" // Different confirm password
            );

            // Assert error message is displayed
            bool isErrorShown = registerPage.IsPasswordMismatchErrorDisplayed();
            Assert.That(isErrorShown, "Lỗi: Không hiển thị thông báo lỗi khi mật khẩu không khớp.");
        }

        [Test]
        [TestCase(5)] // Run test with 5 different random users
        public void Test_MultipleUsersCanRegisterSuccessfully(int numberOfUsers)
        {
            for (int i = 0; i < numberOfUsers; i++)
            {
                // Generate unique test data for each user
                var candidateData = TestDataGenerator.GenerateRandomCandidate();

                var homePage = new HomePage(Driver);
                homePage.GoToHomePage();
                homePage.ClickRegisterNavigationButton();

                var registerPage = new RegisterPage(Driver);
                registerPage.Register(
                    fullName: candidateData.FullName,
                    email: candidateData.Email,
                    password: candidateData.Password,
                    confirmPassword: candidateData.Password
                );

                bool isMessageShown = registerPage.IsSuccessMessageDisplayed();
                Assert.That(isMessageShown, $"Lỗi: Người dùng thứ {i + 1} - Không hiển thị thông báo 'Đăng ký thành công!'.");

                WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
                bool isRedirected = wait.Until(d => d.Url.Contains("/login"));
                Assert.That(isRedirected, $"Lỗi: Người dùng thứ {i + 1} - Không chuyển hướng về trang đăng nhập.");
            }
        }
    }
}
