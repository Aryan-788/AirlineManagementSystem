using NUnit.Framework;
using OpenQA.Selenium;
using Airline.SeleniumTests.Drivers;
using Airline.SeleniumTests.Pages;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Tests;

[TestFixture]
[Category("Search")]
public class SearchFlightTests
{
    private IWebDriver       _driver     = null!;
    private LoginPage        _loginPage  = null!;
    private SearchFlightPage _searchPage = null!;

    [SetUp]
    public void SetUp()
    {
        _driver     = WebDriverFactory.CreateChromeDriver(headless: false);
        _loginPage  = new LoginPage(_driver);
        _searchPage = new SearchFlightPage(_driver);

        var passenger = ConfigurationHelper.Passenger;
        _loginPage.Open(ConfigurationHelper.BaseUrl)
                  .Login(passenger.Email, passenger.Password);

        _loginPage.WaitForSuccessfulRedirect();
        _driver.Navigate().GoToUrl($"{ConfigurationHelper.BaseUrl}/passenger/search");
        _searchPage.WaitForPageLoad();
    }

    [TearDown]
    public void TearDown()
    {
        WebDriverFactory.QuitDriver(_driver);
    }

    [Test]
    public void SearchPage_Rendering_Check()
    {
        Assert.That(_driver.Url, Does.Contain("/search"));
        Assert.That(_driver.FindElement(By.CssSelector("h1.hero-title")).Displayed, Is.True);
    }

    [Test]
    public void Browse_All_Flights_Display()
    {
        _searchPage.ClickBrowseAll();
        Assert.That(_searchPage.WaitForResults(), Is.True, "Flights should be displayed after clicking Browse All.");
    }

    [Test]
    public void Search_By_Route_Validation()
    {
        var date = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
        _searchPage.EnterSource("Mumbai")
                   .EnterDestination("Delhi")
                   .SelectDepartureDate(date)
                   .ClickFindFlights();

        Assert.That(_searchPage.WaitForResults() || _searchPage.IsNoResultsMessageVisible(), Is.True);
    }

    [Test]
    public void Search_No_Results_Scenario()
    {
        var date = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
        _searchPage.EnterSource("NonExistentCity")
                   .EnterDestination("AnotherCity")
                   .SelectDepartureDate(date)
                   .ClickFindFlights();
        
        Assert.That(_searchPage.IsNoResultsMessageVisible(), Is.True, "No results message should be visible for invalid cities.");
    }
}
