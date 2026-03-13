using ERMS.AutomationTests.Core;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.PageObjects.Home
{
    public class HomePage : BasePage
    {
        public HomePage(IWebDriver driver) : base(driver) { }


        private By _loginNavButton = By.XPath("//a[contains(@href, '/login')]");
        private By _registerNavButton = By.XPath("//a[contains(@href, '/register')]");

        public void GoToHomePage()
        {
            Driver.Navigate().GoToUrl(ConfigurationHelper.GetClientUrl());
        }

        public void ClickLoginNavigationButton()
        {
            Driver.FindElement(_loginNavButton).Click();
        }
        public void ClickRegisterNavigationButton()
        {
            Driver.FindElement(_registerNavButton).Click();
        }

    }
}
