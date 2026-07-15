// Made by Dadpul

namespace Dadpul.Jarvis.Docker.Agent;

using System.IO.Pipes;
using System.Net.Sockets;

internal static class DockerEngineTransport
{
   #region Public Methods and Operators

   public static HttpClient CreateHttpClient(string endpoint)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

      var endpointUri = new Uri(endpoint);

      var handler = new SocketsHttpHandler
      {
         ConnectCallback = endpointUri.Scheme switch
         {
            "unix" => (_, cancellationToken) => ConnectUnixSocketAsync(endpointUri.LocalPath, cancellationToken),

            "npipe" => (_, cancellationToken) => ConnectNamedPipeAsync(GetPipeName(endpointUri), cancellationToken),

            _ => throw new NotSupportedException($"Unsupported Docker endpoint scheme '{endpointUri.Scheme}'.")
         }
      };

      return new HttpClient(handler, disposeHandler: true)
      {
         // The hostname is ignored because ConnectCallback supplies the stream.
         BaseAddress = new Uri("http://docker")
      };
   }

   #endregion

   #region Methods

   private static async ValueTask<Stream> ConnectNamedPipeAsync(string pipeName, CancellationToken cancellationToken)
   {
      var pipe = new NamedPipeClientStream(serverName: ".", pipeName: pipeName, direction: PipeDirection.InOut, options: PipeOptions.Asynchronous);

      try
      {
         await pipe.ConnectAsync(cancellationToken);
         return pipe;
      }
      catch
      {
         await pipe.DisposeAsync();
         throw;
      }
   }

   private static async ValueTask<Stream> ConnectUnixSocketAsync(string socketPath, CancellationToken cancellationToken)
   {
      var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

      try
      {
         await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken);

         return new NetworkStream(socket, ownsSocket: true);
      }
      catch
      {
         socket.Dispose();
         throw;
      }
   }

   private static string GetPipeName(Uri endpointUri)
   {
      var segments = endpointUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

      if (segments.Length == 0)
      {
         throw new InvalidOperationException($"Invalid Docker named-pipe endpoint '{endpointUri}'.");
      }

      return segments[^1];
   }

   #endregion
}