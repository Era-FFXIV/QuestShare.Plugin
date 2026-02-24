namespace QuestShare.Services.API
{
    internal class AuthRequest_Client
    {

        public static Task HandleResponse(AuthRequest.Response response)
        {
            var api = Plugin.GetService<ApiService>();
            ApiService.DispatchAuthorize();
            return Task.CompletedTask;
        }
    }
}
