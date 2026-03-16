using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.Core
{
    public class BaseTest
    {
        protected IWebDriver Driver;
        protected string ClientUrl => ConfigurationHelper.GetClientUrl();

        [SetUp]
        public void SetupTest()
        {
            
            Driver = new ChromeDriver();
            Driver.Manage().Window.Maximize();
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        [TearDown]
        public void TeardownTest()
        {
            if (Driver != null)
            {
                Driver.Quit();
                Driver.Dispose();
            }
        }

    }
}
