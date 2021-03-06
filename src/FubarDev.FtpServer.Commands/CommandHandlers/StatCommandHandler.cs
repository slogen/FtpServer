// <copyright file="StatCommandHandler.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DotNet.Globbing;

using FubarDev.FtpServer.ListFormatters;

using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// The <code>STAT</code> command handler.
    /// </summary>
    public class StatCommandHandler : FtpCommandHandler
    {
        [NotNull]
        private readonly IFtpServer _server;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatCommandHandler"/> class.
        /// </summary>
        /// <param name="connection">The connection to create this command handler for.</param>
        /// <param name="server">The FTP server.</param>
        public StatCommandHandler([NotNull] IFtpConnection connection, [NotNull] IFtpServer server)
            : base(connection, "STAT")
        {
            _server = server;
        }

        /// <inheritdoc/>
        public override async Task<FtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(command.Argument))
            {
                var taskStates = _server.GetBackgroundTaskStates();
                var statusMessage = new StringBuilder();
                statusMessage.AppendFormat("Server functional, {0} open connections", _server.Statistics.ActiveConnections);
                if (taskStates.Count != 0)
                {
                    statusMessage.AppendFormat(", {0} active background transfers", taskStates.Count);
                }

                return new FtpResponse(211, statusMessage.ToString());
            }

            var mask = command.Argument;
            if (!mask.EndsWith("*"))
            {
                mask += "*";
            }

            var globOptions = new GlobOptions();
            globOptions.Evaluation.CaseInsensitive = Data.FileSystem.FileSystemEntryComparer.Equals("a", "A");

            var glob = Glob.Parse(mask, globOptions);

            var formatter = new LongListFormatter();
            await Connection.WriteAsync($"211-STAT {command.Argument}", cancellationToken).ConfigureAwait(false);

            var entries = await Data.FileSystem.GetEntriesAsync(Data.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);
            foreach (var entry in entries.Where(x => glob.IsMatch(x.Name)))
            {
                var line = formatter.Format(entry, entry.Name);
                Connection.Log?.LogDebug(line);
                await Connection.WriteAsync($" {line}", cancellationToken).ConfigureAwait(false);
            }

            return new FtpResponse(211, "STAT");
        }
    }
}
