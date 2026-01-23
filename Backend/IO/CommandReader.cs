using System.Collections.Concurrent;

namespace Backend.IO
{
    internal class CommandReader(IInput input, ILogger logger)
    {
        public bool TryGetCommand(out string? command)
            => _commandQueue.TryDequeue(out command);

        public void StartReader()
        {
            if (_readerTask is not null)
            {
                throw new InvalidOperationException("Reader already started.");
            }

            _cts = new CancellationTokenSource();

            _readerTask = Task.Run(async () =>
            {
                try
                {
                    // Use a TaskCompletionSource to signal cancellation
                    TaskCompletionSource tcs = new();
                    _cts.Token.Register(() => tcs.SetResult());

                    Task<string?> readTask;
                    while (!_cts.IsCancellationRequested)
                    {
                        // Wrap ReadLineAsync in a task as calls to Console.In.ReadLineAsync may be blocking on some platforms (Windows)
                        readTask = Task.Run(() => input.ReadLineAsync(), _cts.Token);

                        // Check if the read completed (or was cancelled) or cancellation was requested
                        if (await Task.WhenAny(readTask, tcs.Task) == readTask)
                        {
                            _cts.Token.ThrowIfCancellationRequested();
                            var line = await readTask;
                            if (line is null) break; // EOF
                            _commandQueue.Enqueue(line);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.Log("Command reader cancelled.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Command reader error: {ex.Message}");
                }
            }, _cts.Token);

            logger.Log("Command reader started, waiting for commands on stdin.");
        }

        public void StopReader()
        {
            if (_readerTask is null)
            {
                throw new InvalidOperationException("Reader not started.");
            }

            _cts?.Cancel();
            try
            {
                _readerTask.Wait();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
            {
                // Expected cancellation
            }
            finally
            {
                _readerTask = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private Task? _readerTask = null;
        private readonly ConcurrentQueue<string> _commandQueue = new();
        private CancellationTokenSource? _cts = null;
    }
}
