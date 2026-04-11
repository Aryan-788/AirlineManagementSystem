using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Airline.SeleniumTests.Utils;

/// <summary>
/// Centralised wait helpers built on top of WebDriverWait.
/// All methods guarantee no Thread.Sleep usage.
/// Uses native Selenium 4 expected conditions via lambda delegates.
/// </summary>
public static class WaitHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DefaultPolling = TimeSpan.FromMilliseconds(300);

    /// <summary>Creates a standard WebDriverWait with the configured timeout and polling.</summary>
    public static WebDriverWait CreateWait(IWebDriver driver, TimeSpan? timeout = null)
    {
        var wait = new WebDriverWait(driver, timeout ?? DefaultTimeout)
        {
            PollingInterval = DefaultPolling
        };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        return wait;
    }

    /// <summary>Waits until an element is visible and returns it.</summary>
    public static IWebElement WaitForVisible(IWebDriver driver, By locator, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        return wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(locator);
                return el.Displayed ? el : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        })!;
    }

    /// <summary>Waits until an element is present in the DOM (not necessarily visible).</summary>
    public static IWebElement WaitForPresent(IWebDriver driver, By locator, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        return wait.Until(d =>
        {
            try { return d.FindElement(locator); }
            catch (NoSuchElementException) { return null; }
        })!;
    }

    /// <summary>Waits until an element is clickable (visible + enabled).</summary>
    public static IWebElement WaitForClickable(IWebDriver driver, By locator, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        return wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(locator);
                return (el.Displayed && el.Enabled) ? el : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        })!;
    }

    /// <summary>Waits until the page URL contains any of the specified fragments.</summary>
    public static void WaitForUrlContains(IWebDriver driver, string fragment, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        wait.Until(d => d.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Waits until the page URL contains any of the specified fragments.</summary>
    public static void WaitForAnyUrlContains(IWebDriver driver, string[] fragments, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        wait.Until(d => fragments.Any(f => d.Url.Contains(f, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Waits until the page title contains the specified text.</summary>
    public static void WaitForTitleContains(IWebDriver driver, string text, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        wait.Until(d => d.Title.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Waits until specified text is present in an element's text content.</summary>
    public static void WaitForTextInElement(IWebDriver driver, By locator, string text, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(locator);
                return el.Text.Contains(text, StringComparison.OrdinalIgnoreCase);
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        });
    }

    /// <summary>Waits until an element disappears from view.</summary>
    public static void WaitForInvisible(IWebDriver driver, By locator, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(locator);
                return !el.Displayed;
            }
            catch (NoSuchElementException)
            {
                return true; // element gone entirely = invisible
            }
        });
    }

    /// <summary>
    /// Waits until any element matching the locator is present and contains non-whitespace text.
    /// </summary>
    public static string WaitForNonEmptyText(IWebDriver driver, By locator, TimeSpan? timeout = null)
    {
        var wait = CreateWait(driver, timeout);
        return wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(locator);
                var text = el.Text?.Trim();
                return string.IsNullOrEmpty(text) ? null : text;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        })!;
    }
}
