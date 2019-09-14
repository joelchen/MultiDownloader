using MultiDownloader;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.IO;
using Xunit;

namespace MultiDownloaderUnitTests
{
    public class Tests
    {
        [Fact]
        public async Task TestNull_FTPDownloader_DownloadFilesAsync()
        {
            FTPDownloader ftpDownloader = new FTPDownloader();
            var ane = await Assert.ThrowsAsync<ArgumentNullException>(() => ftpDownloader.DownloadFilesAsync(null));
            Assert.Equal("Value cannot be null. (Parameter 'source')", ane.Message);
        }

        [Fact]
        public async Task TestEmpty_FTPDownloader_DownloadFilesAsync()
        {
            FTPDownloader ftpDownloader = new FTPDownloader();
            var ufe = await Assert.ThrowsAsync<UriFormatException>(() => ftpDownloader.DownloadFilesAsync(new string[] { "" }));
            Assert.Equal("Invalid URI: The URI is empty.", ufe.Message);
        }

        [Fact]
        public async Task TestInvalid_FTPDownloader_DownloadFilesAsync()
        {
            FTPDownloader ftpDownloader = new FTPDownloader();
            var ufe = await Assert.ThrowsAsync<UriFormatException>(() => ftpDownloader.DownloadFilesAsync(new string[] { "Invalid" }));
            Assert.Equal("Invalid URI: The format of the URI could not be determined.", ufe.Message);
        }

        [Fact]
        public async Task TestValid_FTPDownloader_DownloadFilesAsync()
        {
            FTPDownloader ftpDownloader = new FTPDownloader();
            await ftpDownloader.DownloadFilesAsync(new string[] { "ftp://speedtest.tele2.net/1KB.zip" });
            using (SHA256Managed sha = new SHA256Managed())
            {
                var hash = sha.ComputeHash(new FileStream("./Download/1KB.zip", FileMode.Open, FileAccess.Read));
                Assert.Equal("X3C/GKCGAHAW6UiwSu07ghA6Nr6kF1W2zd+vEKzjxu8=", Convert.ToBase64String(hash));
            }
        }

        [Fact]
        public async Task TestNull_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync(null);
            Assert.False(loaded);
        }

        [Fact]
        public async Task TestEmpty_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("");
            Assert.False(loaded);
        }

        [Fact]
        public async Task TestInvalid_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("abc://xyz.com/nosuchfile.txt");
            Assert.False(loaded);
        }

        [Fact]
        public async Task TestNotFile_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("https://www.google.com.sg");
            Assert.False(loaded);
        }

        [Fact]
        public async Task TestValid_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("https://www.google.com.sg/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png");
            Assert.True(loaded);
        }

        [Fact]
        public async Task TestNoAuthentication_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("http://httpbin.org/basic-auth/tester/9876543210");
            Assert.False(loaded);
        }

        [Fact]
        public async Task TestInvalidAuthentication_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("http://hello:world@httpbin.org/basic-auth/tester/9876543210");
            Assert.False(loaded);
        }

        [Fact]
        public async Task TestValidAuthentication_HTTPDownload_LoadFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("http://tester:9876543210@httpbin.org/basic-auth/tester/9876543210");
            Assert.True(loaded);
        }

        [Fact]
        public async Task Test_HTTPDownload_FetchFileAsync()
        {
            HTTPDownload httpDownload = new HTTPDownload();
            bool loaded = await httpDownload.LoadFileAsync("https://www.google.com/doodles/static/sprites/sprites_v4.png");
            string s = null;
            if (loaded) await httpDownload.FetchFileAsync(p =>
            {
                s = ($"{string.Format("{0:N2}% downloaded", p)}");
            });
            Assert.Equal("100.00% downloaded", s);
            using (SHA256Managed sha = new SHA256Managed())
            {
                var hash = sha.ComputeHash(new FileStream("./Download/sprites_v4.png", FileMode.Open, FileAccess.Read));
                Assert.Equal("OL7kQNdZtmgJRoJiT3yMjAWjq5WFW74bEdVbg6ozFLw=", Convert.ToBase64String(hash));
            }
        }

        [Fact]
        public async Task TestNull_HTTPDownloader_DownloadFilesAsync()
        {
            HTTPDownloader httpDownloader = new HTTPDownloader();
            var ane = await Assert.ThrowsAsync<ArgumentNullException>(() => httpDownloader.DownloadFilesAsync(null));
            Assert.Equal("Value cannot be null. (Parameter 'source')", ane.Message);
        }

        [Fact]
        public async Task TestZeroLength_HTTPDownloader_DownloadFilesAsync()
        {
            HTTPDownloader httpDownloader = new HTTPDownloader();
            var e = await Record.ExceptionAsync(() => httpDownloader.DownloadFilesAsync(new string[0]));
            Assert.Null(e);
        }

        [Fact]
        public async Task TestEmpty_HTTPDownloader_DownloadFilesAsync()
        {
            HTTPDownloader httpDownloader = new HTTPDownloader();
            var ufe = await Assert.ThrowsAsync<UriFormatException>(() => httpDownloader.DownloadFilesAsync(new string[] { "" }));
            Assert.Equal("Invalid URI: The URI is empty.", ufe.Message);
        }

        [Fact]
        public async Task TestInvalid_HTTPDownloader_DownloadFilesAsync()
        {
            HTTPDownloader httpDownloader = new HTTPDownloader();
            var ufe = await Assert.ThrowsAsync<UriFormatException>(() => httpDownloader.DownloadFilesAsync(new string[] { "Invalid" }));
            Assert.Equal("Invalid URI: The format of the URI could not be determined.", ufe.Message);
        }

        [Fact]
        public async Task TestValid_HTTPDownloader_DownloadFilesAsync()
        {
            HTTPDownloader httpDownloader = new HTTPDownloader();
            await httpDownloader.DownloadFilesAsync(new string[] { "https://www.google.com.sg/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png" });
            using (SHA256Managed sha = new SHA256Managed())
            {
                var hash = sha.ComputeHash(new FileStream("./Download/googlelogo_color_272x92dp.png", FileMode.Open, FileAccess.Read));
                Assert.Equal("V3bNh2F+rOw7wA689TDRkkAmAz7ahS9wbBpnWpiRWCY=", Convert.ToBase64String(hash));
            }
        }

        [Fact]
        public async Task TestNull_FilesDownloader_GetFilesAsync()
        {
            FilesDownloader filesDownloader = new FilesDownloader();
            bool noIssues = await filesDownloader.GetFilesAsync(null);
            Assert.False(noIssues);
        }

        [Fact]
        public async Task TestZeroLength_FilesDownloader_GetFilesAsync()
        {
            FilesDownloader filesDownloader = new FilesDownloader();
            bool noIssues = await filesDownloader.GetFilesAsync(new string[0]);
            Assert.False(noIssues);
        }

        [Fact]
        public async Task TestEmpty_FilesDownloader_GetFilesAsync()
        {
            FilesDownloader filesDownloader = new FilesDownloader();
            bool noIssues = await filesDownloader.GetFilesAsync(new string[] { "" });
            Assert.False(noIssues);
        }

        [Fact]
        public async Task TestValid_FilesDownloader_GetFilesAsync()
        {
            FilesDownloader filesDownloader = new FilesDownloader();
            bool noIssues = await filesDownloader.GetFilesAsync(new string[] {
                "ftp://speedtest.tele2.net/3MB.zip",
                "http://ipv4.download.thinkbroadband.com/5MB.zip",
                "ftp://demo-user:demo-user@demo.wftpserver.com/download/manual_en.pdf",
                "https://www.google-analytics.com/ga.js" });
            Assert.True(noIssues);
            using (SHA256Managed sha = new SHA256Managed())
            {
                var hash = sha.ComputeHash(new FileStream("./Download/3MB.zip", FileMode.Open, FileAccess.Read));
                Assert.Equal("u9Bc9gl6ybH4nqKdJULBt7Z+5GhIOTiV9ankP6H2IeU=", Convert.ToBase64String(hash));
                hash = sha.ComputeHash(new FileStream("./Download/5MB.zip", FileMode.Open, FileAccess.Read));
                Assert.Equal("wN4QTB5oYlYpZGAl0VphKaK0tkls2c6s1/e1B44YSbo=", Convert.ToBase64String(hash));
                hash = sha.ComputeHash(new FileStream("./Download/manual_en.pdf", FileMode.Open, FileAccess.Read));
                Assert.Equal("UesaE+0pmXaEniJhDi1GHcPxoMdo+R0zfOeYr9R/9KM=", Convert.ToBase64String(hash));
                hash = sha.ComputeHash(new FileStream("./Download/ga.js", FileMode.Open, FileAccess.Read));
                Assert.Equal("Elnqmb12WWI5v9MQLGeesKUFJXjcUmsEUvTUL4vN1F8=", Convert.ToBase64String(hash));
            }
        }
    }
}
