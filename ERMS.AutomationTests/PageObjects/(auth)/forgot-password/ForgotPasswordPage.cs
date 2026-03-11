using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;
using OpenQA.Selenium;

namespace ERMS.AutomationTests.PageObjects._auth_.forgot_password
{
    public class ForgotPasswordPage : BasePage
    {
        public ForgotPasswordPage(IWebDriver driver) : base(driver) { }


        private By _pageTitle = By.XPath("//h1[text()='Quên mật khẩu ERMS']");

      
        private By _emailInput = By.Id("email");
        private By _submitButton = By.XPath("//button[@type='submit']");
        private By _successAlert = By.XPath("//p[contains(text(), 'Vui lòng kiểm tra email.')]");

     
        public void WaitForPageLoad()
        {
           
            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5));
            wait.Until(d => d.FindElement(_pageTitle).Displayed);

            
            wait.Until(d => d.FindElement(_emailInput).Enabled);
        }

        public void EnterEmail(string email)
        {
            var field = Driver.FindElement(_emailInput);
            field.Clear();
            field.SendKeys(email);
        }

        public void ClickSubmit()
        {
            Driver.FindElement(_submitButton).Click();
        }

       
        public void RequestPasswordReset(string email)
        {
            WaitForPageLoad(); 
            EnterEmail(email);
            ClickSubmit();
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
    }
}
