using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Maze.Modules.Api.Response;

namespace Maze.Server.Service.Commander
{
    public class HttpMazeResponseWrapper : MazeResponse
    {
        private readonly HttpResponse _httpResponse;

        public HttpMazeResponseWrapper(HttpResponse httpResponse)
        {
            _httpResponse = httpResponse;
        }

        public override int StatusCode
        {
            get => _httpResponse.StatusCode;
            set => _httpResponse.StatusCode = value;
        }

        public override IHeaderDictionary Headers => _httpResponse.Headers;

        public override Stream Body
        {
            get => _httpResponse.Body;
            set => _httpResponse.Body = value;
        }

        public override long? ContentLength
        {
            get => _httpResponse.ContentLength;
            set => _httpResponse.ContentLength = value;
        }

        public override string ContentType
        {
            get => _httpResponse.ContentType;
            set => _httpResponse.ContentType = value;
        }

        public override bool HasStarted => _httpResponse.HasStarted;

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            _httpResponse.OnStarting(callback, state);
        }

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            _httpResponse.OnCompleted(callback, state);
        }
    }
}