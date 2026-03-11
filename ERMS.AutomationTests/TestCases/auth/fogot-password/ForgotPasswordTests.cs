using ERMS.AutomationTests.Core;
using ERMS.AutomationTests.PageObjects._auth_.forgot_password;
using ERMS.AutomationTests.PageObjects._auth_.login;
using ERMS.AutomationTests.PageObjects.Home;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.TestCases.auth.fogot_password
{
    [TestFixture]
    public class ForgotPasswordTests : BaseTest
    {
        [Test]
        public void Test_UserCanRequestPasswordResetSuccessfully()
        {
            // 1. Arrange: Đi từ Trang chủ -> Trang Đăng nhập
            var homePage = new HomePage(Driver);
            homePage.GoToHomePage();
            homePage.ClickLoginNavigationButton();

            // 2. Act: Từ trang Đăng nhập -> Click Quên mật khẩu
            var loginPage = new LoginPage(Driver);
            loginPage.ClickForgotPassword();

            // 3. Act: Điền email khôi phục và Gửi
            var forgotPasswordPage = new ForgotPasswordPage(Driver);
            forgotPasswordPage.RequestPasswordReset("brightbindereuglk3i2k15149@outlook.com");

            // 4. Assert: Kiểm tra xem thẻ HTML thông báo thành công có xuất hiện không
            bool isMessageShown = forgotPasswordPage.IsSuccessMessageDisplayed();

            Assert.That(isMessageShown, "Lỗi: Hệ thống không hiển thị thông báo 'Vui lòng kiểm tra email.' sau khi submit.");
        }
    }
}
