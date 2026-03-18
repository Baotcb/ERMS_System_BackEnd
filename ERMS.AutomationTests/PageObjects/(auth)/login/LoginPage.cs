using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.PageObjects._auth_.login
{
    public class LoginPage : BasePage
    {
        public LoginPage(IWebDriver driver) : base(driver) { }

    
        private By _emailInput = By.Id("email");
        private By _passwordInput = By.Id("password");
        private By _submitButton = By.XPath("//button[@type='submit']");
        private By _successAlert = By.XPath("//*[contains(text(), 'Đăng nhập thành công')]");
        private By _forgotPasswordLink = By.LinkText("Quên mật khẩu?");

        public void EnterEmail(string email)
        {
            var emailField = Driver.FindElement(_emailInput);
            emailField.Clear();
            emailField.SendKeys(email);
        }
        
        public void EnterPassword(string password)
        {
            var passField = Driver.FindElement(_passwordInput);
            passField.Clear();
            passField.SendKeys(password);
        }

        public void ClickSubmit()
        {
            Driver.FindElement(_submitButton).Click();
        }

       
        public void Login(string email, string password)
        {
            var emailField = Driver.FindElement(_emailInput);
            emailField.Clear();
            emailField.SendKeys(email);

            var passField = Driver.FindElement(_passwordInput);
            passField.Clear();
            passField.SendKeys(password);

            Driver.FindElement(_submitButton).Click();
        }
        public bool IsSuccessMessageDisplayed()
        {
            try
            {
               
                WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
                return wait.Until(d => d.FindElement(_successAlert).Displayed);
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
        public void ClickForgotPassword()
        {
            Driver.FindElement(_forgotPasswordLink).Click();
        }
    }
}
