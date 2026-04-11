using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Airline.SeleniumTests.Drivers;

/// <summary>
/// Factory responsible for creating and configuring the WebDriver instance.
/// </summary>
public static class WebDriverFactory
{
    /// <summary>
    /// Creates a fully configured ChromeDriver instance.
    /// The browser window is maximized on launch.
    /// </summary>
    /// <param name="headless">Set to true to run in headless mode (e.g. CI/CD).</param>
    /// <returns>A ready-to-use IWebDriver instance.</returns>
    public static IWebDriver CreateChromeDriver(bool headless = false)
    {
        var options = new ChromeOptions();

        if (headless)
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--window-size=1920,1080");
        }

        // Stability arguments for CI and Docker environments
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--remote-debugging-port=9222");

        var driver = new ChromeDriver(options);

        if (!headless)
        {
            driver.Manage().Window.Maximize();
        }

        // Global implicit wait — WebDriverWait (explicit) is used in pages,
        // but this acts as a sensible baseline.
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

        return driver;
    }

    /// <summary>
    /// Safely disposes the driver and closes the browser window.
    /// </summary>
    public static void QuitDriver(IWebDriver? driver)
    {
        if (driver == null) return;
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Suppress teardown exceptions to not mask real test failures
        }
    }
}
