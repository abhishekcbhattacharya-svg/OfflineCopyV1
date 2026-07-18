// See https://aka.ms/new-console-template for more information
using Website.utlities;
using Website.utlities.Helpers;

Console.WriteLine("Hello, Please enter webite you want to read and download for offline \nThis program is for educational and research purpose only! \nPlease enter y/Y to continue.");
int key = Console.Read();
bool valid = (key == 121 || key == 89 || key == 13);
if (!valid)
{
    Console.WriteLine("exiting for non agreement ...");
    return;
}

bool TryRead(string term, out string? _input)
{
    Console.WriteLine($"Please enter {term}.\n");
    _input = Console.ReadLine();
    while (string.IsNullOrEmpty(_input))
    {
        _input = Console.ReadLine();
    }
    bool validFolder = string.IsNullOrEmpty(_input) == false;
    if (_input == null && !validFolder)
    {
        Console.WriteLine($"No valid {term}");
        return false;
    }
    return true;
}

if (TryRead("website", out string? website) && TryRead("folder", out string? folder))
{
    Console.WriteLine("Website:" + website);

    Action<Exception> logEx = (ex) =>
    {
        string error = ex.ToString();
        Console.WriteLine(error);
    };

    TaskPageDI router = new TaskPageDI(logEx);

    //read all links
    //save all screenshot

    var screenLink = new ScreenLink(router);

    ScreenLinkConfig config = new ScreenLinkConfig 
    { 
        Domain = website,
        SnapshotFolder = folder,
    };
    screenLink.ExecuteAsync(config, logEx).Wait();
    Console.WriteLine("Website completed");
}

Console.ReadKey();