using OpenQA.Selenium;
using Airline.SeleniumTests.Utils;

namespace Airline.SeleniumTests.Pages;

/// <summary>
/// Page Object for the Login / Authentication screen.
///
/// Selectors are derived from the real Angular template:
///   src/app/features/auth/login/login.html
///
/// The form uses ngModel bindings (no element IDs), so we use
/// attribute-based selectors and CSS class selectors.
/// </summary>
public class LoginPage : BasePage
{
    // ─── Locators ──────────────────────────────────────────────────────────────

    // Input fields are plain <input> elements inside the .auth-form
    // The email field has type="email" and is always rendered first.
    private readonly By _emailInput        = By.CssSelector("form.auth-form input[type='email']");
    private readonly By _passwordInput     = By.CssSelector("form.auth-form input[type='password']");
    private readonly By _submitButton      = By.CssSelector("form.auth-form button[type='submit']");

    // The tab toggle: "Login" is the first button inside .tab-toggle
    private readonly By _loginTab          = By.CssSelector("div.tab-toggle button:first-child");

    // Error and success messages rendered with *ngIf directives
    private readonly By _errorMessage      = By.CssSelector("div.error-msg");
    private readonly By _successMessage    = By.CssSelector("div.error-msg[style*='#16a34a']");

    // The brand title on the hero side — confirms the page has loaded
    private readonly By _brandTitle        = By.CssSelector("h1.brand-title");

    // ─── Constructor ───────────────────────────────────────────────────────────

    public LoginPage(IWebDriver driver) : base(driver) { }

    // ─── Page Actions ──────────────────────────────────────────────────────────

    /// <summary>Navigates directly to the login page URL.</summary>
    public LoginPage Open(string baseUrl = "http://localhost:4200")
    {
        NavigateTo($"{baseUrl}/login");
        // Wait for the brand title to confirm Angular has bootstrapped
        WaitHelper.WaitForVisible(Driver, _brandTitle, TimeSpan.FromSeconds(20));
        return this;
    }

    /// <summary>Ensures the Login tab is active (not Registration).</summary>
    public LoginPage EnsureLoginTabActive()
    {
        try
        {
            var tab = WaitHelper.WaitForClickable(Driver, _loginTab, TimeSpan.FromSeconds(5));
            // Check if tab is already active — looking for the .active class
            if (!tab.GetDomAttribute("class")!.Contains("active"))
            {
                tab.Click();
            }
        }
        catch (WebDriverTimeoutException)
        {
            // Tab toggle may not be present on some states; safe to ignore
        }
        return this;
    }

    /// <summary>Enters the provided email address into the email field.</summary>
    public LoginPage EnterEmail(string email)
    {
        SendKeysSlowly(_emailInput, email);
        return this;
    }

    /// <summary>Enters the provided password into the password field.</summary>
    public LoginPage EnterPassword(string password)
    {
        SendKeysSlowly(_passwordInput, password);
        return this;
    }

    /// <summary>Clicks the primary submit / authorize button.</summary>
    public LoginPage ClickLogin()
    {
        Click(_submitButton);
        return this;
    }

    /// <summary>
    /// Full login sequence: enter email → password → click submit.
    /// </summary>
    public LoginPage Login(string email, string password)
    {
        EnsureLoginTabActive();
        EnterEmail(email);
        EnterPassword(password);
        ClickLogin();
        return this;
    }

    // ─── Assertions / Queries ──────────────────────────────────────────────────

    /// <summary>
    /// Waits up to <paramref name="timeout"/> for the browser URL to change
    /// away from /login, which signals a successful login redirect.
    /// </summary>
    public bool WaitForSuccessfulRedirect(TimeSpan? timeout = null)
    {
        try
        {
            var successPaths = new[] { "/dashboard", "/passenger", "/admin", "/search", "/ground-staff", "/dealer" };
            WaitHelper.WaitForAnyUrlContains(Driver, successPaths, timeout ?? TimeSpan.FromSeconds(15));
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    /// <summary>Returns true if an error message element is visible.</summary>
    public bool IsErrorVisible()
    {
        try
        {
            var el = WaitHelper.WaitForVisible(Driver, _errorMessage, TimeSpan.FromSeconds(5));
            return el.Displayed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns the text content of the error message element.</summary>
    public string GetErrorMessage()
    {
        try
        {
            return WaitHelper.WaitForNonEmptyText(Driver, _errorMessage, TimeSpan.FromSeconds(5));
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Returns the current value of the email input field.</summary>
    public string GetEmailValue()
    {
        var el = WaitHelper.WaitForVisible(Driver, _emailInput);
        return el.GetDomProperty("value") ?? string.Empty;
    }

    /// <summary>Returns true if the submit button is disabled (processing state).</summary>
    public bool IsSubmitButtonDisabled()
    {
        try
        {
            var btn = Driver.FindElement(_submitButton);
            return btn.GetDomProperty("disabled") != null;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }
}
