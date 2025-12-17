using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;

namespace TailscaleOnlineChecker
{



    public class TailscaleWorker
    {
        private const string OAuthTokenUrl = "https://api.tailscale.com/api/v2/oauth/token";
        private readonly string TailscaleApiUrl;
        private readonly string PushdeerApiUrl;
        private readonly string TailscaleOAuthKey;
        private readonly string DeviceNameFilter;

        private const string TailscaleDeviceApiUrl = "https://api.tailscale.com/api/v2/device/";

        public TailscaleWorker()
        {
            // 从环境变量读取敏感信息
            TailscaleOAuthKey = Environment.GetEnvironmentVariable("TAILSCALE_OAUTH_KEY");
            var pushdeerPushkey = Environment.GetEnvironmentVariable("PUSHDEER_PUSHKEY");
            var tailnetName = Environment.GetEnvironmentVariable("TAILSCALE_TAILNET");
            DeviceNameFilter = Environment.GetEnvironmentVariable("DEVICE_NAME_FILTER");

            // 验证必需的环境变量
            if (string.IsNullOrEmpty(TailscaleOAuthKey))
            {
                throw new InvalidOperationException("环境变量 TAILSCALE_OAUTH_KEY 未设置");
            }
            if (string.IsNullOrEmpty(pushdeerPushkey))
            {
                throw new InvalidOperationException("环境变量 PUSHDEER_PUSHKEY 未设置");
            }
            if (string.IsNullOrEmpty(tailnetName))
            {
                throw new InvalidOperationException("环境变量 TAILSCALE_TAILNET 未设置");
            }
            if (string.IsNullOrEmpty(DeviceNameFilter))
            {
                throw new InvalidOperationException("环境变量 DEVICE_NAME_FILTER 未设置");
            }

            // 使用环境变量构建URL
            TailscaleApiUrl = $"https://api.tailscale.com/api/v2/tailnet/{tailnetName}/devices";
            PushdeerApiUrl = $"https://api2.pushdeer.com/message/push?pushkey={pushdeerPushkey}&text=";
        }

        public async Task HandleRequestAsync()
        {
            try
            {
                var tokenResponse = await OAuthTokenUrl.WithOAuthBearerToken(TailscaleOAuthKey).PostAsync(null);
                if (tokenResponse.StatusCode != 200)
                {
                    Console.WriteLine("获取OAuth令牌时遇到错误。");
                    return;
                }

                var tokenStr = await tokenResponse.GetStringAsync();
                var tokenData = JsonConvert.DeserializeObject<OAuthToken>(tokenStr);
                var accessToken = tokenData?.AccessToken;

                var devicesResponse = await TailscaleApiUrl.WithOAuthBearerToken(accessToken).GetAsync();
                if (tokenResponse.StatusCode != 200)
                {
                    Console.WriteLine("获取设备时遇到错误。");
                    return;
                }

                var devicesStr = await devicesResponse.GetStringAsync();
                var devices = JsonConvert.DeserializeObject<DevicesResponse>(devicesStr);

                foreach (var device in devices.Devices)
                {

                    var deviceResponse = await (TailscaleDeviceApiUrl + device.Id).WithOAuthBearerToken(accessToken).GetAsync();
                    var deviceStr = await deviceResponse.GetStringAsync();


                    if (device.Name.Contains(DeviceNameFilter))
                    {
                        var lastSeen = DateTime.Parse(device.LastSeen);
                        var now = DateTime.Now;
                        var timeDifference = now - lastSeen;

                        if (timeDifference.TotalMinutes > 60)
                        {
                            var encodedDeviceName = Uri.EscapeDataString(device.Name);
                            await $"{PushdeerApiUrl}设备：{encodedDeviceName} 离线时间：{Math.Round(timeDifference.TotalMinutes)}分钟".GetAsync();
                            Console.WriteLine("推送了");
                        }
                        else
                        {
                            Console.WriteLine("设备在线，无需推送");
                        }
                    }
                }
               
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex}");
            }
        }

        private class OAuthToken
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
        }

        private class DevicesResponse
        {
            [JsonProperty("devices")]
            public Device[] Devices { get; set; }
        }

        private class Device
        {
            
            public long Id { get; set; }

            public string Name { get; set; }
        
            public string LastSeen { get; set; }
        }
    }
}
