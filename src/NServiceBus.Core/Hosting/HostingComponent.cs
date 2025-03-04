﻿namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Runtime;
    using System.Threading;
    using System.Threading.Tasks;
    using Installation;
    using Microsoft.Extensions.DependencyInjection;
    using Support;

    partial class HostingComponent
    {
        public HostingComponent(Configuration configuration) => this.configuration = configuration;

        public static HostingComponent Initialize(Configuration configuration)
        {
            var serviceCollection = configuration.Services;
            serviceCollection.AddSingleton(_ => configuration.HostInformation);
            serviceCollection.AddSingleton(_ => configuration.CriticalError);

            foreach (var installerType in configuration.AvailableTypes.Where(t => IsINeedToInstallSomething(t)))
            {
                serviceCollection.AddTransient(installerType);
                serviceCollection.AddTransient(sp => (INeedToInstallSomething)sp.GetRequiredService(installerType));
            }

            // Apply user registrations last, so that user overrides win.
            foreach (var registration in configuration.UserRegistrations)
            {
                registration(serviceCollection);
            }

            configuration.AddStartupDiagnosticsSection("Hosting", new
            {
                configuration.HostInformation.HostId,
                HostDisplayName = configuration.HostInformation.DisplayName,
                RuntimeEnvironment.MachineName,
                OSPlatform = Environment.OSVersion.Platform,
                OSVersion = Environment.OSVersion.VersionString,
                GCSettings.IsServerGC,
                GCLatencyMode = GCSettings.LatencyMode,
                Environment.ProcessorCount,
                Environment.Is64BitProcess,
                CLRVersion = Environment.Version,
                Environment.WorkingSet,
                Environment.SystemPageSize,
                HostName = Dns.GetHostName(),
                Environment.UserName,
                PathToExe = PathUtilities.SanitizedPath(Environment.CommandLine)
            });

            return new HostingComponent(configuration);
        }

        public async Task RunInstallers(IServiceProvider builder, CancellationToken cancellationToken = default)
        {
            if (!configuration.ShouldRunInstallers)
            {
                return;
            }

            var installationUserName = GetInstallationUserName();

            foreach (var installer in builder.GetServices<INeedToInstallSomething>())
            {
                await installer.Install(installationUserName, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task WriteDiagnosticsFile(CancellationToken cancellationToken = default)
        {
            var hostStartupDiagnosticsWriter = HostStartupDiagnosticsWriterFactory.GetDiagnosticsWriter(configuration);

            await hostStartupDiagnosticsWriter.Write(configuration.StartupDiagnostics.entries, cancellationToken).ConfigureAwait(false);
        }

        public void SetupCriticalErrors(IEndpointInstance endpointInstance, CancellationToken cancellationToken = default) =>
            configuration.CriticalError.SetEndpoint(endpointInstance, cancellationToken);

        string GetInstallationUserName()
        {
            if (configuration.InstallationUserName != null)
            {
                return configuration.InstallationUserName;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return $"{Environment.UserDomainName}\\{Environment.UserName}";
            }

            return Environment.UserName;
        }

        readonly Configuration configuration;
    }
}