using System.Diagnostics;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Magic
{
    public class AutoUpdateNET
    {
        public Form? CallingClass { get; set; }

        public string LatestVersionCheckURL { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = "1.0.0";

        public UpdateInfo updateInfo { get; set; } = new UpdateInfo();

        public string NewMsiPath { get; set; } = string.Empty;

        public AutoUpdateNET()
        {

        } // end of method

        public AutoUpdateNET(Form callingClass)
        {

            CallingClass = callingClass;

        } // end of method

        public async Task GetLatestVersion(string latestVersionCheckURL, string purchasingEmail)
        {

            await ProcessingFormNET.ExecuteAsync("Melakukan cek update...", async () =>
            {

                this.LatestVersionCheckURL = latestVersionCheckURL;

                if (string.IsNullOrEmpty(this.LatestVersionCheckURL) || this.CallingClass == null)
                {
                    this.updateInfo = new UpdateInfo();
                    return;
                }

                this.updateInfo = await GetRemoteVersion(purchasingEmail);

            });

        } // end of method

        public async Task Update(string latestVersionFileSavePath)
        {
            this.NewMsiPath = latestVersionFileSavePath;

            await ProcessingFormNET.ExecuteAsync("Mendownload versi terbaru...", async () =>
            {
                try
                {
                    if (File.Exists(latestVersionFileSavePath))
                    {
                        File.Delete(latestVersionFileSavePath);
                    }

                    using (Magic.SystemAddonsNET.HTTP httpClientHelper = new Magic.SystemAddonsNET.HTTP())
                    {
                        httpClientHelper.DownloadProgressChanged += (progress) =>
                        {
                            ProcessingFormNET.UpdateLabel($"Mendownload versi terbaru... ({progress}%)");
                        };

                        await httpClientHelper.DownloadFileAsync(this.updateInfo!.DownloadURL, latestVersionFileSavePath);
                    }

                    // Akses elemen UI di thread UI menggunakan Invoke
                    this.CallingClass!.Invoke((MethodInvoker)delegate
                    {
                        this.CallingClass.FormClosed += ExecuteInstaller;
                        this.CallingClass.FormClosed += (sender, e) => Application.Exit();
                        this.CallingClass.Close();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during update: {ex.Message}");
                }
            });

        } // end of method

        /*
        public async Task Update_(string latestVersionFileSavePath)
        {

            this.NewMsiPath = latestVersionFileSavePath;

            await ProcessingFormNET.ExecuteAsync("Mendownload versi terbaru...", async () =>
            {

                try
                {
                    if (File.Exists(latestVersionFileSavePath))
                    {
                        File.Delete(latestVersionFileSavePath);
                    }

                    using (WebClient binClient = new WebClient())
                    {
                        binClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) =>
                        {
                            ProcessingFormNET.UpdateLabel($"Mendownload versi terbaru... ({e.ProgressPercentage}%)");
                        });

                        await binClient.DownloadFileTaskAsync(new Uri(this.updateInfo!.DownloadURL), latestVersionFileSavePath);
                    }

                    // Akses elemen UI di thread UI menggunakan Invoke
                    this.CallingClass!.Invoke((MethodInvoker)delegate
                    {
                        this.CallingClass.FormClosed += ExecuteInstaller;
                        this.CallingClass.FormClosed += (sender, e) => Application.Exit();
                        this.CallingClass.Close();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during update: {ex.Message}");
                }

            });

        } // end of method
        */

        private async Task<UpdateInfo> GetRemoteVersion(string purchasingEmail)
        {

            UpdateInfo updateInfo = new UpdateInfo();

            using (HttpClient client = new HttpClient())
            {
                Dictionary<string, string> postData = new Dictionary<string, string>
                {
                    { "auth", "Hallaw" },
                    { "purchasing_email", purchasingEmail }
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(postData);
                HttpResponseMessage response;
                
                try
                {
                    response = await client.PostAsync(this.LatestVersionCheckURL, content);
                }
                catch
                {
                    return updateInfo;
                }

                string updateInfoString = await response.Content.ReadAsStringAsync();

                updateInfo = JsonConvert.DeserializeObject<UpdateInfo>(updateInfoString)!;
            }

            return updateInfo!;

        } // end of method

        /// <summary>
        /// Membandingkan 2 versi dengan format xx.yy.zz. Meskipun bisa saja ada berapapun bagian.
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns>
        /// Jika version1 lebih lama dari version2, maka return -1
        /// Jika version1 sama dengan vesion2, maka return 0
        /// Jika version1 lebih baru dari version2, maka return 1
        /// </returns>
        public int CompareVersions(string version1, string version2)
        {

            string[] v1Parts = version1.Split('.');
            string[] v2Parts = version2.Split('.');

            for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
            {
                int v1Part = (i < v1Parts.Length) ? int.Parse(v1Parts[i]) : 0;
                int v2Part = (i < v2Parts.Length) ? int.Parse(v2Parts[i]) : 0;

                if (v1Part < v2Part)
                    return -1;
                else if (v1Part > v2Part)
                    return 1;
            }

            return 0; // versions are equal

        } // end of method

        public bool IsUpdate(string ExistingVersion, string LatestVersion)
        {

            return CompareVersions(ExistingVersion, LatestVersion) < 0 ? true : false;

        } // end of method

        public class UpdateInfo
        {

            public string ExistingVersion { get; set; } = "1.0.0";
            public string LatestVersion { get; set; } = "1.0.0";
            public bool ForceUpdate { get; set; } = false;
            public string DownloadURL { get; set; } = string.Empty;

        } // end of method

        private void ExecuteInstaller(object? sender, FormClosedEventArgs e)
        {

            string msiFullPath = Path.GetFullPath(this.NewMsiPath);

            Process process = new Process();
            process.StartInfo.FileName = "msiexec";
            process.StartInfo.Arguments = string.Format($"/i \"{msiFullPath}\"");

            process.Start();

        } // end of method

    } // end of class

} // end of namespace