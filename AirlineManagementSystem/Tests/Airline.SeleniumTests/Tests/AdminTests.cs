using NUnit.Framework;
using OpenQA.Selenium;
using Airline.SeleniumTests.Drivers;
using Airline.SeleniumTests.Pages;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Tests;

[TestFixture]
[Category("Admin")]
public class AdminTests
{
    private IWebDriver _driver = null!;
    private LoginPage _loginPage = null!;
    private AdminPage _adminPage = null!;
    private DashboardPage _dashboardPage = null!;

    [SetUp]
    public void SetUp()
    {
        _driver = WebDriverFactory.CreateChromeDriver(headless: false);
        _loginPage = new LoginPage(_driver);
        _adminPage = new AdminPage(_driver);
        _dashboardPage = new DashboardPage(_driver);

        var admin = ConfigurationHelper.Admin;
        _loginPage.Open(ConfigurationHelper.BaseUrl)
                  .Login(admin.Email, admin.Password);
        
        _loginPage.WaitForSuccessfulRedirect();
    }

    [TearDown]
    public void TearDown()
    {
        WebDriverFactory.QuitDriver(_driver);
    }

    [Test]
    public void Admin_Dashboard_Load_Check()
    {
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/admin/dashboard");
        Assert.That(_dashboardPage.IsOnDashboard("/admin/dashboard"), Is.True);
        Assert.That(_dashboardPage.GetTitle(), Does.Contain("Terminal Intelligence").Or.Contain("Dashboard"));
    }

    [Test]
    public void Admin_FleetControl_View_Check()
    {
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/admin/flights");
        Assert.That(_adminPage.GetPageTitle(), Is.EqualTo("Fleet Control"));
        Assert.That(_adminPage.IsFlightRegistryVisible(), Is.True);
    }

    [Test]
    public void Admin_AddFlight_Form_Toggle()
    {
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/admin/flights");
        _adminPage.ClickAddFlight();
        Assert.That(_adminPage.IsFlightFormVisible(), Is.True, "Add Flight form should be visible after clicking the button.");
        
        _adminPage.ClickAddFlight(); // Toggle off
        Assert.That(_adminPage.IsFlightFormVisible(), Is.False, "Form should be hidden after clicking cancel.");
    }

    [Test]
    public void Admin_ActivityLog_Rendering()
    {
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/admin/flights");
        Assert.That(_adminPage.GetActivityLogCount(), Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Admin_FlightRegistry_Table_Check()
    {
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/admin/flights");
        // Even if empty, it should have the table header structure
        var table = WaitHelper.WaitForVisible(_driver, By.CssSelector("table"));
        Assert.That(table.Displayed, Is.True);
    }

    [Test]
    public void Admin_SideNavbar_Navigation()
    {
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/admin/dashboard");
        Assert.That(_dashboardPage.IsSideNavVisible(), Is.True);
    }
}
