
using Microsoft.Playwright;

namespace Website.utlities
{
    public class TaskPageDI
    {
        private readonly Action<Exception> logEx;
        public TaskPageDI(Action<Exception> logEx)
        {
            this.logEx = logEx;
        }
        public async Task Execute(params Func<IPage, Task>[] steps)
        {
            // Initialize Playwright and launch browser
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false // Set to true if you don't want to see the browser
            });

            // Create a new browser context and page
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            //Loop through tasks
            for (int stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                try
                {
                    Func<IPage, Task> step = steps[stepIndex];
                    await step(page);
                }
                catch (Exception ex)
                {
                    logEx(ex);
                    //throw;
                }

            }

            //// Navigate to the login page
            //await page.GotoAsync("https://example.com/login");

            //// Fill in login credentials
            //await page.FillAsync("#username", "yourUsername");
            //await page.FillAsync("#password", "yourPassword");

            //// Click the login button
            //await page.ClickAsync("#loginButton");

            //// Wait for navigation or some element that confirms login
            //await page.WaitForURLAsync("https://example.com/dashboard");

            //// Optional: Take a screenshot after login
            //await page.ScreenshotAsync(new PageScreenshotOptions { Path = "loggedin.png" });

            // Close browser
            await browser.CloseAsync();
        }
    }
}
