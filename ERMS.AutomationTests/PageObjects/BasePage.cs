using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.AutomationTests.PageObjects
{
    public class BasePage
    {
        protected IWebDriver Driver;

        public BasePage(IWebDriver driver)
        {
            Driver = driver;
        }

        
        public bool IsElementPresent(By by)
        {
            try
            {
                Driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public bool IsAlertPresent()
        {
            try
            {
                Driver.SwitchTo().Alert();
                return true;
            }
            catch (NoAlertPresentException)
            {
                return false;
            }
        }
    }
}
