using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Pages;

/// <summary>
/// Base class for all Page Objects. Provides common driver actions,
/// explicit waits, and navigation utilities shared across all pages.
/// </summary>
public abstract class BasePage
{
    protected readonly IWebDriver Driver;
    protected readonly WebDriverWait Wait;

    protected BasePage(IWebDriver driver)
    {
        Driver = driver;
        Wait = WaitHelper.CreateWait(driver);
    }

    // ─── Navigation ────────────────────────────────────────────────────────────

    public void NavigateTo(string url) => Driver.Navigate().GoToUrl(url);

    public string CurrentUrl => Driver.Url;

    public string PageTitle => Driver.Title;

    // ─── Element Interaction ───────────────────────────────────────────────────

    /// <summary>Waits for the element to be clickable, then clicks it.</summary>
    protected void Click(By locator)
    {
        var element = WaitHelper.WaitForClickable(Driver, locator);
        element.Click();
    }

    /// <summary>Waits for the element to be visible, clears it, then types the value.</summary>
    protected void SendKeys(By locator, string value)
    {
        var element = WaitHelper.WaitForVisible(Driver, locator);
        element.Clear();
        element.SendKeys(value);
    }

    /// <summary>Clears the field and types character-by-character (useful for Angular reactive inputs).</summary>
    protected void SendKeysSlowly(By locator, string value)
    {
        var element = WaitHelper.WaitForVisible(Driver, locator);
        element.Clear();
        // Use Actions to ensure Angular change detection picks up each keystroke
        var actions = new Actions(Driver);
        actions.Click(element).SendKeys(value).Perform();
    }

    /// <summary>Retrieves the visible text of an element after it becomes visible.</summary>
    protected string GetText(By locator)
    {
        var element = WaitHelper.WaitForVisible(Driver, locator);
        return element.Text.Trim();
    }

    /// <summary>Checks whether an element is present and visible on the page.</summary>
    protected bool IsDisplayed(By locator)
    {
        try
        {
            return Driver.FindElement(locator).Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    /// <summary>Selects an option from a &lt;select&gt; element by visible text.</summary>
    protected void SelectByText(By locator, string text)
    {
        var element = WaitHelper.WaitForVisible(Driver, locator);
        var select = new SelectElement(element);
        select.SelectByText(text);
    }

    /// <summary>Selects an option from a &lt;select&gt; element by value attribute.</summary>
    protected void SelectByValue(By locator, string value)
    {
        var element = WaitHelper.WaitForVisible(Driver, locator);
        var select = new SelectElement(element);
        select.SelectByValue(value);
    }

    // ─── Wait Shortcuts ────────────────────────────────────────────────────────

    protected IWebElement WaitForVisible(By locator, TimeSpan? timeout = null)
        => WaitHelper.WaitForVisible(Driver, locator, timeout);

    protected void WaitForUrlContains(string fragment, TimeSpan? timeout = null)
        => WaitHelper.WaitForUrlContains(Driver, fragment, timeout);

    protected void WaitForTextInElement(By locator, string text, TimeSpan? timeout = null)
        => WaitHelper.WaitForTextInElement(Driver, locator, text, timeout);
}
