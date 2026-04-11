using NUnit.Framework;
using OpenQA.Selenium;
using Airline.SeleniumTests.Drivers;
using Airline.SeleniumTests.Pages;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Tests;

[TestFixture]
[Category("Login")]
public class LoginTests
{
    private IWebDriver _driver = null!;
    private LoginPage  _loginPage = null!;
    private DashboardPage _dashboardPage = null!;

    [SetUp]
    public void SetUp()
    {
        _driver = WebDriverFactory.CreateChromeDriver(headless: false);
        _loginPage = new LoginPage(_driver);
        _dashboardPage = new DashboardPage(_driver);
        _loginPage.Open(ConfigurationHelper.BaseUrl);
    }

    [TearDown]
    public void TearDown()
    {
        WebDriverFactory.QuitDriver(_driver);
    }

    [Test]
    public void Admin_Login_Success()
    {
        var user = ConfigurationHelper.Admin;
        _loginPage.Login(user.Email, user.Password);
        Assert.That(_loginPage.WaitForSuccessfulRedirect(), Is.True, "Admin should be redirected after login.");
    }

    [Test]
    public void Passenger_Login_Success()
    {
        var user = ConfigurationHelper.Passenger;
        _loginPage.Login(user.Email, user.Password);
        Assert.That(_loginPage.WaitForSuccessfulRedirect(), Is.True, "Passenger should be redirected after login.");
    }

    [Test]
    public void Dealer_Login_Success()
    {
        var user = ConfigurationHelper.Dealer;
        _loginPage.Login(user.Email, user.Password);
        Assert.That(_loginPage.WaitForSuccessfulRedirect(), Is.True, "Dealer should be redirected after login.");
        Assert.That(_dashboardPage.IsOnDashboard("/dealer"), Is.True, "Should be on dealer dashboard.");
    }

    [Test]
    public void GroundStaff_Login_Success()
    {
        var user = ConfigurationHelper.GroundStaff;
        _loginPage.Login(user.Email, user.Password);
        Assert.That(_loginPage.WaitForSuccessfulRedirect(), Is.True, "Ground Staff should be redirected after login.");
        Assert.That(_dashboardPage.IsOnDashboard("/ground-staff"), Is.True, "Should be on ground-staff dashboard.");
    }

    [Test]
    public void Login_Failure_WrongPassword()
    {
        var user = ConfigurationHelper.Admin;
        _loginPage.Login(user.Email, "WrongPassword123!");
        Assert.That(_loginPage.IsErrorVisible(), Is.True, "Error should be visible on wrong password.");
    }

    [Test]
    public void Login_Failure_InvalidUser()
    {
        _loginPage.Login("invalid_user_999@example.com", "SomePassword123!");
        Assert.That(_loginPage.IsErrorVisible(), Is.True, "Error should be visible for non-existent user.");
    }

    [Test]
    public void Logout_Should_Redirect_To_Login()
    {
        var user = ConfigurationHelper.Passenger;
        _loginPage.Login(user.Email, user.Password);
        _loginPage.WaitForSuccessfulRedirect();
        
        _dashboardPage.Logout();
        Assert.That(_driver.Url, Does.Contain("/login"), "Should be redirected back to login after logout.");
    }

    [Test]
    public void LoginPage_UI_Check()
    {
        Assert.That(_driver.FindElement(By.CssSelector("h1.brand-title")).Text, Is.EqualTo("SKYLEDGER"));
        Assert.That(_driver.FindElement(By.CssSelector("button[type='submit']")).Displayed, Is.True);
    }

    [Test]
    public void Tab_Switching_Verification()
    {
        var registerTab = _driver.FindElement(By.CssSelector("div.tab-toggle button:last-child"));
        registerTab.Click();
        
        var heading = WaitHelper.WaitForVisible(_driver, By.CssSelector("h2.form-title"));
        Assert.That(heading.Text, Does.Contain("Create Account"));
    }
}
