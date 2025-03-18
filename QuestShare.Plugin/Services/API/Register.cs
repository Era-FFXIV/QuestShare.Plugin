using Newtonsoft.Json;

namespace QuestShare.Services.API
{
    internal class Register_Client
    {
        public static void HandleDispatch()
        {
            var api = (ApiService)Plugin.GetService<ApiService>();
            _ = api.Invoke(nameof(Register), new Common.API.Register.Request
            {
                Version = Constants.Version,
                Token = ApiService.Token,
            });
        }

        public void HandleDispatch(dynamic? data)
        {
            throw new NotImplementedException();
        }

        public static Task HandleResponse(Register.Response response)
        {
            if (response.Success)
            {
                var host = (HostService)Plugin.GetService<HostService>();
                host.Start(response.ShareCode);
                UiService.LastErrorMessage = "";
            }
            else
            {
                Log.Error("Failed to register as host: {0}", response.Error);
                UiService.LastErrorMessage = $"Failed to register as host: {response.Error}";
            }
            return Task.CompletedTask;
        }
    }
}
