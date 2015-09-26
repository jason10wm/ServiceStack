﻿using System;
using System.Diagnostics;
using System.Net;
using Check.ServiceInterface;
using Funq;
using ServiceStack;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;

namespace CheckHttpListener
{
    [Route("/request")]
    public class Request : IReturn<Response> { }
    public class Response { }

    public class MyServices : Service
    {
        public object Any(Request request)
        {
            return new Response();
        }
    }

    public interface IDependency
    {
        Guid GetInstanceId();
    }

    public class SomeDependency : IDependency//, IDisposable
    {
        private readonly Guid _id = Guid.NewGuid();

        public Guid GetInstanceId()
        {
            return _id;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing: " + _id);
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("Check HttpListener Tests", 
            typeof(ErrorsService).Assembly,
            typeof(UserService).Assembly) { }

        public override void Configure(Container container)
        {
            //RawHttpHandlers.Add(_ => new CustomActionHandler((req, res) =>
            //{
            //    var bytes = req.InputStream.ReadFully();
            //    res.OutputStream.Write(bytes, 0, bytes.Length);
            //}));

            this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = null;

            SetConfig(new HostConfig
            {
                DebugMode = true,
                SkipFormDataInCreatingRequest = true,
            });

            //Plugins.Add(new NativeTypesFeature());

            container.RegisterAs<SomeDependency, IDependency>().ReusedWithin(ReuseScope.Request);

            if (container.TryResolve<IDependency>() != default(IDependency)) {}

            GlobalRequestFilters.Add((req, res, requestDto) =>
            {
                var thatDependency = container.Resolve<IDependency>();
                Console.WriteLine("Filter received: " + thatDependency.GetInstanceId());
            });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //Licensing.RegisterLicenseFromFileIfExists(@"c:\src\appsettings.license.txt");

            new AppHost()
                .Init()
                .Start("http://*:2020/");

            //TestService();

            Process.Start("http://localhost:2020/upload.html");

            //var response = "http://localhost:2020".PostToUrl(new { a = "foo", b = "bar" });
            //"Response: {0}".Print(response);

            
            Console.ReadLine();
        }

        private async static void TestService()
        {
            var client = new JsonServiceClient("http://localhost:2020/");

            try
            {
                var request = new UpdateAddressVoid();
                request.ToGetUrl().Print();
                request.ToPostUrl().Print();
                await client.PostAsync(request);
                //var response = client.Post(request);
            }
            catch (WebServiceException ex)
            {
                ex.StatusCode.ToString().Print();
                ex.StatusDescription.Print();
                ex.ResponseBody.Print();
            }
        }
    }

    [Route("/user/{UserId}/Address")]
    public class UpdateAddress : IReturn<AddressResponse>
    {
        public int UserId { get; set; }

        public string Address { get; set; }
    }

    [Route("/user/{UserId}/AddressVoid")]
    public class UpdateAddressVoid : IReturnVoid
    {
        public int UserId { get; set; }

        public string Address { get; set; }
    }

    public class AddressResponse
    {
        public string Address { get; set; }
    }

    public class UserService : Service
    {
        public object Post(UpdateAddress request)
        {
            if (request.UserId > 0)
            {
                return new HttpResult(request.Address, HttpStatusCode.OK);
            }

            //throw new UnauthorizedAccessException("Unauthorized UserId");
            //throw HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
            return HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
        }

        public void Post(UpdateAddressVoid request)
        {
            //throw new UnauthorizedAccessException("Unauthorized UserId");
            throw HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
            //return HttpError.Unauthorized(request.Address ?? "Unauthorized UserId");
        }
    }

    [Route("/upload", "POST")]
    public class UploadFileRequest { }

    public class TestController : Service
    {
        public void Any(UploadFileRequest request)
        {
            int filesCount = Request.Files.Length;
            Console.WriteLine(filesCount); // Always 0
        }
    }
}
