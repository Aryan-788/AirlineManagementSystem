using OpenQA.Selenium;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Pages;

public class DashboardPage : BasePage
{
    private readonly By _dashboardTitle = By.CssSelector("h1.pg-title, h1.dash-title, h1.hero-title, h1.admin-title, h1.d-title");
    private readonly By _sideNavItems = By.CssSelector("a.nav-item");
    private readonly By _logoutButton = By.CssSelector(".nav-item.logout");

    public DashboardPage(IWebDriver driver) : base(driver) { }

    public string GetTitle()
    {
        return GetText(_dashboardTitle);
    }

    public bool IsSideNavVisible()
    {
        return IsDisplayed(_sideNavItems);
    }

    public void Logout()
    {
        Click(_logoutButton);
        WaitHelper.WaitForUrlContains(Driver, "/login");
    }

    public bool IsOnDashboard(string rolePathPart)
    {
        return Driver.Url.Contains(rolePathPart, StringComparison.OrdinalIgnoreCase);
    }
}
