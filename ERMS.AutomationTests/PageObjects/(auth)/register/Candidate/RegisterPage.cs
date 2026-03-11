using ERMS.AutomationTests.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.TestCases.auth.Register.CandidateRegister
{
    public class RegisterPage : BasePage
    {
        public RegisterPage(IWebDriver driver) : base(driver) { }

        // --- LOCATORS ---
        private By _fullNameInput = By.Id("fullName");
        private By _emailInput = By.Id("email");
        private By _passwordInput = By.Id("password");
        private By _confirmPasswordInput = By.Id("confirmPassword");

   
        private By _termsCheckboxLabel = By.XPath("(.//*[normalize-space(text()) and normalize-space(.)='Xác nhận mật khẩu'])[1]/following::label[1]");

        private By _submitButton = By.XPath("//button[@type='submit']");

       
        private By _successAlert = By.XPath("//*[contains(text(), 'Đăng ký thành công')]");


        public void EnterFullName(string fullName)
        {
            var field = Driver.FindElement(_fullNameInput);
            field.Clear();
            field.SendKeys(fullName);
        }

        public void EnterEmail(string email)
        {
            var field = Driver.FindElement(_emailInput);
            field.Clear();
            field.SendKeys(email);
        }

        public void EnterPassword(string password)
        {
            var field = Driver.FindElement(_passwordInput);
            field.Clear();
            field.SendKeys(password);
        }

        public void EnterConfirmPassword(string confirmPassword)
        {
            var field = Driver.FindElement(_confirmPasswordInput);
            field.Clear();
            field.SendKeys(confirmPassword);
        }

        public void CheckTermsAndConditions()
        {
            Driver.FindElement(_termsCheckboxLabel).Click();
        }

        public void ClickSubmit()
        {
            Driver.FindElement(_submitButton).Click();
        }

        
        public void Register(string fullName, string email, string password, string confirmPassword)
        {
            EnterFullName(fullName);
            EnterEmail(email);
            EnterPassword(password);
            EnterConfirmPassword(confirmPassword);
            CheckTermsAndConditions();
            ClickSubmit();
        }

        // Hàm kiểm tra kết quả
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
    }
}
