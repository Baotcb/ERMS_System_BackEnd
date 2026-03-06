using ERMS.AutomationTests.Core;
using ERMS.AutomationTests.PageObjects.Home;
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
      
            var homePage = new HomePage(Driver);
            homePage.GoToHomePage();
            homePage.ClickRegisterNavigationButton();

          
            var registerPage = new RegisterPage(Driver);
            registerPage.Register(
                fullName: "Phan Thanh Bảo",
                email: "ironwardeneivfhl9vxb0o6112eo@outlook.com",
                password: "Password123!",
                confirmPassword: "Password123!"
            );

         
            bool isMessageShown = registerPage.IsSuccessMessageDisplayed();
            Assert.That(isMessageShown, "Lỗi: Không hiển thị thông báo 'Đăng ký thành công!'.");

           
            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            bool isRedirected = wait.Until(d => d.Url.Contains("/login"));
            Assert.That(isRedirected, $"Lỗi: Không tự động chuyển về trang Đăng nhập. URL hiện tại: {Driver.Url}");
        }
    }
}
