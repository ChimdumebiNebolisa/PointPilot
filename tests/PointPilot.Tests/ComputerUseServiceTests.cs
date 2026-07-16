using System.Net;
using System.Net.Http;
using System.Text;
using PointPilot.Core;
using PointPilot.Infrastructure.OpenAI;

namespace PointPilot.Tests;

public sealed class ComputerUseServiceTests
{
    [Fact]
    public async Task ComputerLoop_ExecutesOrderedBatchAndReturnsOriginalDetailScreenshot()
    {
        var handler = new SequentialHandler(
            """{"id":"resp_1","output":[{"type":"computer_call","call_id":"call_1","actions":[{"type":"click","x":10,"y":20},{"type":"keypress","keys":["CTRL","Z"]}]}]}""",
            """{"id":"resp_2","output":[{"type":"message","content":[{"type":"output_text","text":"Complete."}]}]}""");
        using var client = new HttpClient(handler);
        using var tasks = new TaskCoordinator();
        tasks.Start("Undo a visible edit");
        var executor = new RecordingExecutor();
        var service = new ComputerUseService(client, new OpenAiOptions("test-key", "gpt-5.6", "gpt-realtime-2.1", new Uri("https://example.invalid/v1/")), new FakeWindows(), executor, tasks);

        var result = await service.RunAsync(tasks.GetLease(), "Undo a visible edit", [], CancellationToken.None);

        Assert.True(result.Completed);
        Assert.Equal(new[] { ComputerActionType.Click, ComputerActionType.Keypress }, executor.Actions);
        Assert.Equal(2, handler.Bodies.Count);
        Assert.Contains("\"detail\":\"original\"", handler.Bodies[0], StringComparison.Ordinal);
        Assert.Contains("\"previous_response_id\":\"resp_1\"", handler.Bodies[1], StringComparison.Ordinal);
    }

    private sealed class RecordingExecutor : IComputerActionExecutor
    {
        internal List<ComputerActionType> Actions { get; } = [];
        public Task ExecuteAsync(TaskLease lease, WindowSnapshot target, ComputerAction action, CancellationToken cancellationToken)
        {
            Actions.Add(action.Type);
            return Task.CompletedTask;
        }
    }

    private sealed class SequentialHandler(params string[] responses) : HttpMessageHandler
    {
        private int _index;
        internal List<string> Bodies { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responses[_index++], Encoding.UTF8, "application/json") };
        }
    }
}
