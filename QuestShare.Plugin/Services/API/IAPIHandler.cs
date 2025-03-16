namespace QuestShare.Services.API
{
    internal interface IAPIHandler
    {
        string Method => GetType().Name;
        void HandleDispatch();
        void HandleDispatch(dynamic? data);
        Task HandleResponse(IResponse response);
        void InvokeHandler(IAPIHandler handler, IRequest request)
        {
            _ = ((ApiService)Plugin.GetService<ApiService>()).Invoke(Method, request);
        }
    }
}
