using System;
using System.IO;
using System.Threading.Tasks;

namespace MultiDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            FilesDownloader filesDownloader = new FilesDownloader();
            bool noIssues = await filesDownloader.GetFilesAsync(Singleton.Settings.URIs);
            Console.WriteLine($"Finished getting files {(noIssues ? "without" : "with")} issues");
            Console.ReadLine();
        }
    }
}
