using ERMS.AutomationTests.Core;
using ERMS.AutomationTests.PageObjects._auth_.login;
using ERMS.AutomationTests.PageObjects.Home;
using NUnit.Framework.Constraints;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.TestCases.auth.Login
{
    [TestFixture]
    public class LoginTests : BaseTest
    {
        [Test]
        public void Test_UserCanLoginSuccessfully()
        {
         
            var homePage = new HomePage(Driver);
            homePage.GoToHomePage();
            homePage.ClickLoginNavigationButton();

            // 2. Act: Điền thông tin và Submit
            var loginPage = new LoginPage(Driver);
            loginPage.Login("greylakewkduodk4b762il89o0@outlook.com", "Abc123456!");

            // 3. ASSERT (ĐÁNH GIÁ KẾT QUẢ)

            // Assert 1: Kiểm tra Alert Success có hiện ra hay không
            bool isMessageShown = loginPage.IsSuccessMessageDisplayed();
            Assert.That(isMessageShown, "Lỗi: Không hiển thị thông báo 'Đăng nhập thành công!'.");

            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));

           
        }
    }
}
