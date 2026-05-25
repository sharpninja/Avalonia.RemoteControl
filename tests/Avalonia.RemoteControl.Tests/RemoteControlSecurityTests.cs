using System.Net;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Security;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlSecurityTests
{
    [Fact]
    public async Task AuthenticationInterceptorRejectsMissingBearerToken()
    {
        var interceptor = CreateInterceptor(new AvaloniaRemoteControlOptions
        {
            AuthenticationToken = "dev-token",
        });

        var context = new TestServerCallContext(new global::Grpc.Core.Metadata());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                new GetCapabilitiesRequest(),
                context,
                (_, _) => Task.FromResult(new GetCapabilitiesResponse())));

        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task AuthenticationInterceptorRejectsInvalidBearerToken()
    {
        var interceptor = CreateInterceptor(new AvaloniaRemoteControlOptions
        {
            AuthenticationToken = "dev-token",
        });

        var context = new TestServerCallContext(new global::Grpc.Core.Metadata
        {
            { "authorization", "Bearer wrong-token" },
        });

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                new GetCapabilitiesRequest(),
                context,
                (_, _) => Task.FromResult(new GetCapabilitiesResponse())));

        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task AuthenticationInterceptorAllowsValidBearerToken()
    {
        var interceptor = CreateInterceptor(new AvaloniaRemoteControlOptions
        {
            AuthenticationToken = "dev-token",
        });

        var context = new TestServerCallContext(new global::Grpc.Core.Metadata
        {
            { "authorization", "Bearer dev-token" },
        });

        var response = await interceptor.UnaryServerHandler(
            new GetCapabilitiesRequest(),
            context,
            (_, _) => Task.FromResult(new GetCapabilitiesResponse { ProtocolVersion = "test" }));

        Assert.Equal("test", response.ProtocolVersion);
    }

    [Fact]
    public void StartupPolicyAcceptsEnabledLoopbackWithToken()
    {
        var result = Validate(new AvaloniaRemoteControlOptions
        {
            IsEnabled = true,
            AuthenticationToken = "dev-token",
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void StartupPolicyRejectsEnabledServerWithoutAuthenticationToken()
    {
        var result = Validate(new AvaloniaRemoteControlOptions
        {
            IsEnabled = true,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StartupPolicyRejectsNonLoopbackCleartext()
    {
        var result = Validate(new AvaloniaRemoteControlOptions
        {
            IsEnabled = true,
            AuthenticationToken = "dev-token",
            Host = IPAddress.Parse("192.168.10.5"),
            RequireTlsForNonLoopback = false,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("cleartext", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StartupPolicyRequiresCertificateForNonLoopbackTls()
    {
        var result = Validate(new AvaloniaRemoteControlOptions
        {
            IsEnabled = true,
            AuthenticationToken = "dev-token",
            Host = IPAddress.Parse("192.168.10.5"),
            RequireTlsForNonLoopback = true,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("certificate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StartupPolicyKeepsAuthenticationRequiredForAdbTunnel()
    {
        var result = Validate(new AvaloniaRemoteControlOptions
        {
            IsEnabled = true,
            IsAdbTunnel = true,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    private static RemoteControlAuthenticationInterceptor CreateInterceptor(
        AvaloniaRemoteControlOptions options)
    {
        return new RemoteControlAuthenticationInterceptor(
            Options.Create(options),
            NullLogger<RemoteControlAuthenticationInterceptor>.Instance);
    }

    private static RemoteControlStartupValidationResult Validate(AvaloniaRemoteControlOptions options)
    {
        return new RemoteControlStartupValidator(Options.Create(options)).Validate();
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly global::Grpc.Core.Metadata requestHeaders;
        private readonly global::Grpc.Core.Metadata responseTrailers = [];
        private readonly Dictionary<object, object> userState = [];
        private Status status;
        private WriteOptions? writeOptions;

        public TestServerCallContext(global::Grpc.Core.Metadata requestHeaders)
        {
            this.requestHeaders = requestHeaders;
        }

        protected override string MethodCore => "avalonia.remotecontrol.v1.RemoteControl/GetCapabilities";

        protected override string HostCore => "localhost";

        protected override string PeerCore => "ipv4:127.0.0.1:50000";

        protected override DateTime DeadlineCore => DateTime.MaxValue;

        protected override global::Grpc.Core.Metadata RequestHeadersCore => requestHeaders;

        protected override CancellationToken CancellationTokenCore => CancellationToken.None;

        protected override global::Grpc.Core.Metadata ResponseTrailersCore => responseTrailers;

        protected override Status StatusCore
        {
            get => status;
            set => status = value;
        }

        protected override WriteOptions? WriteOptionsCore
        {
            get => writeOptions;
            set => writeOptions = value;
        }

        protected override AuthContext AuthContextCore { get; } = new(
            string.Empty,
            new Dictionary<string, List<AuthProperty>>());

        protected override IDictionary<object, object> UserStateCore => userState;

        protected override ContextPropagationToken CreatePropagationTokenCore(
            ContextPropagationOptions? options)
        {
            throw new NotSupportedException();
        }

        protected override Task WriteResponseHeadersAsyncCore(global::Grpc.Core.Metadata responseHeaders)
        {
            return Task.CompletedTask;
        }
    }
}
