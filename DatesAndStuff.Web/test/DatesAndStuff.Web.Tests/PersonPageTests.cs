using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class PersonPageTests
{
    private IWebDriver driver;
    private StringBuilder verificationErrors;
    private const string BaseURL = "http://localhost:5091";
    private bool acceptNextAlert = true;

    private Process? _blazorProcess;

    [OneTimeSetUp]
    public void StartBlazorServer()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            Assembly.GetExecutingAssembly().Location,
            "../../../../../../src/DatesAndStuff.Web/DatesAndStuff.Web.csproj"
            ));

        var webProjFolderPath = Path.GetDirectoryName(webProjectPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            //Arguments = $"run --project \"{webProjectPath}\"",
            Arguments = "dotnet run --no-build",
            WorkingDirectory = webProjFolderPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _blazorProcess = Process.Start(startInfo);

        // Wait for the app to become available
        var client = new HttpClient();
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.Now;

        while (DateTime.Now - start < timeout)
        {
            try
            {
                var result = client.GetAsync(BaseURL).Result;
                if (result.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Thread.Sleep(1000);
            }
        }
    }

    [OneTimeTearDown]
    public void StopBlazorServer()
    {
        if (_blazorProcess != null && !_blazorProcess.HasExited)
        {
            _blazorProcess.Kill(true);
            _blazorProcess.Dispose();
        }
    }

    [SetUp]
    public void SetupTest()
    {
        driver = new ChromeDriver();
        verificationErrors = new StringBuilder();
    }

    [TearDown]
    public void TeardownTest()
    {
        try
        {
            driver.Quit();
            driver.Dispose();
        }
        catch (Exception)
        {
            // Ignore errors if unable to close the browser
        }
        Assert.That(verificationErrors.ToString(), Is.EqualTo(""));
    }

    [Test]
    [TestCase(5, 5250)]
    [TestCase(10, 5500)]
    [TestCase(0, 5000)]
    public void Person_SalaryIncrease_ShouldIncrease(double percentage, double expectedSalary)
    {
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));
        input.Clear();
        input.SendKeys(percentage.ToString());

        var submitButton = driver.FindElement(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']"));
        submitButton.Click();

        var salaryLabel = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='DisplayedSalary']")));
        double.Parse(salaryLabel.Text).Should().BeApproximately(expectedSalary, 0.001);
    }

    [Test]
    public void Person_SalaryIncrease_InvalidValue_ShouldShowErrorMessages()
    {
        driver.Navigate().GoToUrl(BaseURL);
        driver.FindElement(By.XPath("//*[@data-test='PersonPageNavigation']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        var input = wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@data-test='SalaryIncreasePercentageInput']")));

        input.Clear();
        input.SendKeys("-11");

        var submitButton = driver.FindElement(By.XPath("//*[@data-test='SalaryIncreaseSubmitButton']"));
        submitButton.Click();

        // Verify ValidationSummary (top of the page)
        var summary = wait.Until(ExpectedConditions.ElementExists(By.ClassName("validation-errors")));
        summary.Text.Should().Contain("The specified percentag should be between -10 and infinity.");

        // Verify ValidationMessage (below the field)
        var fieldError = driver.FindElement(By.ClassName("validation-message"));
        fieldError.Text.Should().Be("The specified percentag should be between -10 and infinity.");
    }

    [Test]
    public void BlazeDemo_CheckFlightsBetweenMexicoCityAndDublin()
    {
        driver.Navigate().GoToUrl("https://blazedemo.com");

        driver.FindElement(By.Name("fromPort")).Click();
        driver.FindElement(By.XPath("//select[@name='fromPort']/option[@value='Mexico City']")).Click();

        driver.FindElement(By.Name("toPort")).Click();
        driver.FindElement(By.XPath("//select[@name='toPort']/option[@value='Dublin']")).Click();

        driver.FindElement(By.CssSelector("input[type='submit']")).Click();

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        var rows = wait.Until(d => d.FindElements(By.CssSelector("table.table tbody tr")));

        rows.Count.Should().BeGreaterThanOrEqualTo(3, "there should be at least three flights between Mexico City and Dublin");
    }
    private bool IsElementPresent(By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private bool IsAlertPresent()
    {
        try
        {
            driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    private string CloseAlertAndGetItsText()
    {
        try
        {
            IAlert alert = driver.SwitchTo().Alert();
            string alertText = alert.Text;
            if (acceptNextAlert)
            {
                alert.Accept();
            }
            else
            {
                alert.Dismiss();
            }
            return alertText;
        }
        finally
        {
            acceptNextAlert = true;
        }
    }
}