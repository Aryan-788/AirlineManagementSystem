using NUnit.Framework;
using OpenQA.Selenium;
using Airline.SeleniumTests.Drivers;
using Airline.SeleniumTests.Pages;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Tests;

[TestFixture]
[Category("RoleAccess")]
public class RoleAccessTests
{
    private IWebDriver _driver = null!;
    private LoginPage _loginPage = null!;
    private DashboardPage _dashboardPage = null!;
    private BaggagePage _baggagePage = null!;

    [SetUp]
    public void SetUp()
    {
        _driver = WebDriverFactory.CreateChromeDriver(headless: false);
        _loginPage = new LoginPage(_driver);
        _dashboardPage = new DashboardPage(_driver);
        _baggagePage = new BaggagePage(_driver);
    }

    [TearDown]
    public void TearDown()
    {
        WebDriverFactory.QuitDriver(_driver);
    }

    [Test]
    public void Dealer_Should_Access_DealerDashboard()
    {
        var dealer = ConfigurationHelper.Dealer;
        _loginPage.Open(ConfigurationHelper.BaseUrl).Login(dealer.Email, dealer.Password);
        _loginPage.WaitForSuccessfulRedirect();
        
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/dealer/dashboard");
        Assert.That(_dashboardPage.IsOnDashboard("/dealer"), Is.True);
    }

    [Test]
    public void GroundStaff_Should_Access_BaggageLog()
    {
        var gs = ConfigurationHelper.GroundStaff;
        _loginPage.Open(ConfigurationHelper.BaseUrl).Login(gs.Email, gs.Password);
        _loginPage.WaitForSuccessfulRedirect();
        
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/ground-staff/baggage");
        Assert.That(_baggagePage.GetPageTitle(), Is.EqualTo("Real-time Baggage Log"));
    }

    [Test]
    public void GroundStaff_Should_See_StatCards()
    {
        var gs = ConfigurationHelper.GroundStaff;
        _loginPage.Open(ConfigurationHelper.BaseUrl).Login(gs.Email, gs.Password);
        _loginPage.WaitForSuccessfulRedirect();
        
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/ground-staff/baggage");
        Assert.That(_baggagePage.GetStatCardCount(), Is.GreaterThanOrEqualTo(3), "GS should see summary stat cards.");
    }

    [Test]
    public void Passenger_Should_Not_Access_AdminDashboard()
    {
        var passenger = ConfigurationHelper.Passenger;
        _loginPage.Open(ConfigurationHelper.BaseUrl).Login(passenger.Email, passenger.Password);
        _loginPage.WaitForSuccessfulRedirect();
        
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/admin/dashboard");
        // Verify we are NOT on admin dashboard (Angular router should have redirected)
        Assert.That(_driver.Url, Does.Not.Contain("/admin"), "Passenger should be denied access to admin views.");
    }

    [Test]
    public void Dealer_SideNav_Should_Be_Visible()
    {
        var dealer = ConfigurationHelper.Dealer;
        _loginPage.Open(ConfigurationHelper.BaseUrl).Login(dealer.Email, dealer.Password);
        _loginPage.WaitForSuccessfulRedirect();
        
        Assert.That(_dashboardPage.IsSideNavVisible(), Is.True, "Dealer should see a side navbar.");
    }

    [Test]
    public void GroundStaff_SideNav_Should_Be_Visible()
    {
        var gs = ConfigurationHelper.GroundStaff;
        _loginPage.Open(ConfigurationHelper.BaseUrl).Login(gs.Email, gs.Password);
        _loginPage.WaitForSuccessfulRedirect();
        
        Assert.That(_dashboardPage.IsSideNavVisible(), Is.True, "Ground Staff should see a side navbar.");
    }
}
