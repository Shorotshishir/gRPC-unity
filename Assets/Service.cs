using Client;
using Grpc.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Service : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private TextMeshProUGUI _unary;

    [SerializeField]
    private TextMeshProUGUI _clientStream;

    [SerializeField]
    private TextMeshProUGUI _serverStream;

    private Greeter.GreeterClient client;

    private void Start()
    {
        Debug.Log("start");
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        var localIp = GetLocalIp();
        Debug.Log($"localIp : {localIp}");
        client = CreateClient(localIp);

        /*UnaryCall(client);
        Task.Run(async () => await ServerStreaming(client));
        Task.Run(async () => await ClientStreaming(client));*/
    }

    public void Unary()
    {
        UnaryCall(client);
    }

    public void ServerStream()
    {
        var task = Task.Run(async () => await ServerStreaming(client));
        task.Wait();
        if (task.IsCompleted)
        {
            _serverStream.SetText(task.Result);
        }
    }

    public void ClientStream()
    {
        var task = Task.Run(async () => await ClientStreaming(client));
        task.Wait();
        if (task.IsCompleted)
        {
            _clientStream.SetText(task.Result);
        }
    }

    private string GetLocalIp()
    {
        string localIP;
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
        return localIP;
    }

    private async Task<string> ClientStreaming(Greeter.GreeterClient client)
    {
        var call = client.SayHello();
        var message = string.Empty;
        message = "Request";
        try
        {
            for (int i = 0; i < 5; i++)
            {
                var request = new HelloRequest { Name = "Shorotshishir" };
                message += $"\n{request.Name}";
                //_clientStream.SetText(message);
                await call.RequestStream.WriteAsync(request);
            }
            await call.RequestStream.CompleteAsync();
            var response = await call;
            message += $"\nResponse {response.Message}";
            //Debug.Log($" Did you receive ? {response.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            //throw;
        }
        return message;
    }

    private async Task<string> ServerStreaming(Greeter.GreeterClient client)
    {
        var request = new HelloRequest { Name = "Shorotshishir" };
        var call = client.SayHelloServerStream(request);
        var message = string.Empty;
        message = $"Request : {request.Name}\nResponse:";
        try
        {
            while (await call.ResponseStream.MoveNext())
            {
                message += $"\n{call.ResponseStream.Current.Message}";
                //Debug.Log($"Greetings {message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return message;
    }

    private void UnaryCall(Greeter.GreeterClient client)
    {
        try
        {
            _unary.SetText(string.Empty);
            var request = new HelloRequest { Name = "Shorotshishir" };
            var reply = client.SayHelloUnary(request);
            //Debug.Log($"Unary Call: {reply.Message}");
            _unary.SetText($"Request:{request.Name}\nResponse{reply.Message}");
        }
        catch (RpcException e)
        {
            Debug.LogError(e);
        }
    }

    private Greeter.GreeterClient CreateClient(string ip)
    {
        var channel = new Channel($"{ip}:5002", ChannelCredentials.Insecure);
        var client = new Greeter.GreeterClient(channel);
        return client;
    }
}