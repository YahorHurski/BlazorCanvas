using BlazorCanvas.Data.V11;
using BlazorCanvas.Geometry;
using BlazorCanvas.Shapes;
using BlazorCanvas.Sync;

namespace BlazorCanvas.Components.Pages;

/// <summary>
/// Circuit-local state and behaviour for the v1.11 canvas. Keeping it independent of Razor makes
/// the cross-tab protocol executable without a renderer while Home remains the pointer adapter.
/// </summary>
internal sealed class CanvasInteractionCoordinator
{
    private readonly FigureInputGateway _gateway;
    private readonly CanvasSyncNotifier _notifier;
    private readonly int _ownerId;
    private readonly Guid _canvasId;
    private readonly Func<CancellationToken, Task<IReadOnlyList<FigureRow>>> _load;
    private readonly Func<ValidatedFigureInput, decimal, decimal, CancellationToken, Task<FigureRow>> _insert;
    private readonly Func<Guid, decimal, decimal, CancellationToken, Task<int>> _move;
    private readonly Func<Guid, CancellationToken, Task<int>> _delete;
    private readonly Func<long> _clock;
    private readonly Guid _sender = Guid.NewGuid();
    private long _lastPublication;
    private Guid? _dragId;
    private FigureRow? _dragOriginal;
    private FigureRow? _dragCurrent;
    private CanvasPoint _dragPress;
    private bool _dragMoved;

    public CanvasInteractionCoordinator(
        FigureInputGateway gateway,
        CanvasSyncNotifier notifier,
        int ownerId,
        Guid canvasId,
        Func<CancellationToken, Task<IReadOnlyList<FigureRow>>> load,
        Func<ValidatedFigureInput, decimal, decimal, CancellationToken, Task<FigureRow>> insert,
        Func<Guid, decimal, decimal, CancellationToken, Task<int>> move,
        Func<Guid, CancellationToken, Task<int>> delete,
        Func<long>? clock = null)
    {
        _gateway = gateway;
        _notifier = notifier;
        _ownerId = ownerId;
        _canvasId = canvasId;
        _load = load;
        _insert = insert;
        _move = move;
        _delete = delete;
        _clock = clock ?? (() => Environment.TickCount64);
    }

    public List<FigureRow> Figures { get; private set; } = [];
    public Guid? SelectedId { get; private set; }
    public bool IsDragging => _dragId.HasValue;
    public bool ShowSaveFailedModal { get; private set; }

    public void Deselect() => SelectedId = null;

    public async Task LoadAsync(CancellationToken ct = default) => Figures = (await _load(ct)).ToList();

    public async Task DrawAsync(string type, CanvasPoint press, CanvasPoint cursor, CancellationToken ct = default)
    {
        if (!_gateway.TryValidateGesture(type, press, cursor, null, out var input, out var x, out var y)
            || input is null)
        {
            return;
        }

        try
        {
            var row = await _insert(input, (decimal)x, (decimal)y, ct);
            Figures.Add(row);
            SelectedId = row.Id;
            _notifier.Publish(_ownerId, SyncMessage.Draw(row, _sender));
        }
        catch (Exception)
        {
            ShowSaveFailedModal = true;
        }
    }

    public void BeginDrag(Guid id, CanvasPoint press)
    {
        var row = Figures.FirstOrDefault(figure => figure.Id == id);
        SelectedId = id;
        if (row is null)
        {
            return;
        }

        _dragId = id;
        _dragOriginal = row;
        _dragCurrent = row;
        _dragPress = press;
        _dragMoved = false;
        _lastPublication = 0;
    }

    public void ContinueDrag(CanvasPoint cursor)
    {
        if (_dragId is null || _dragOriginal is null)
        {
            return;
        }

        var dx = cursor.X - _dragPress.X;
        var dy = cursor.Y - _dragPress.Y;
        _dragMoved |= Math.Sqrt((dx * dx) + (dy * dy)) >= 3;
        var (x, y) = V11Movement.ClampPosition(_dragOriginal, _dragOriginal.X + (decimal)dx, _dragOriginal.Y + (decimal)dy);
        _dragCurrent = _dragOriginal with { X = x, Y = y };
        Replace(_dragCurrent);

        var now = _clock();
        if (_dragMoved && (_lastPublication == 0 || now - _lastPublication >= 50))
        {
            PublishPosition(_dragCurrent);
            _lastPublication = now;
        }
    }

    public async Task CommitDragAsync(CancellationToken ct = default)
    {
        var id = _dragId;
        var original = _dragOriginal;
        var current = _dragCurrent;
        var moved = _dragMoved;
        _dragId = null;
        _dragOriginal = null;
        _dragCurrent = null;

        if (id is null || original is null || current is null || !moved)
        {
            return;
        }

        // The trailing edge is deliberately unconditional: the last mouse coordinate may have
        // arrived inside the 50ms throttle window and must reach peers before persistence.
        PublishPosition(current);
        try
        {
            if (await _move(id.Value, current.X, current.Y, ct) == 0)
            {
                Figures.RemoveAll(figure => figure.Id == id.Value);
                if (SelectedId == id.Value) SelectedId = null;
                _notifier.Publish(_ownerId, SyncMessage.Delete(id.Value, _sender));
            }
        }
        catch (Exception)
        {
            Replace(original);
            _notifier.Publish(_ownerId, SyncMessage.Rollback(id.Value, original.X, original.Y, _sender));
            ShowSaveFailedModal = true;
        }
    }

    public async Task DeleteAsync(CancellationToken ct = default)
    {
        if (SelectedId is not { } id) return;
        try
        {
            await _delete(id, ct);
            Figures.RemoveAll(figure => figure.Id == id);
            SelectedId = null;
            _notifier.Publish(_ownerId, SyncMessage.Delete(id, _sender));
        }
        catch (Exception)
        {
            ShowSaveFailedModal = true;
        }
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        var oldIds = Figures.Select(figure => figure.Id).ToHashSet();
        Figures = (await _load(ct)).ToList();
        foreach (var id in oldIds.Except(Figures.Select(figure => figure.Id)))
            _notifier.Publish(_ownerId, SyncMessage.Delete(id, _sender));
        foreach (var row in Figures)
        {
            _notifier.Publish(_ownerId, SyncMessage.Draw(row, _sender));
            PublishPosition(row);
        }
        SelectedId = null;
        ShowSaveFailedModal = false;
    }

    public void ApplyRemoteMessage(SyncMessage message)
    {
        if (message.Sender == _sender || IsDragging) return;
        switch (message.Kind)
        {
            case "draw" when message.Figure is not null && !Figures.Any(figure => figure.Id == message.Id):
                Figures.Add(message.Figure);
                break;
            case "move":
            case "rollback":
                if (message.X is { } x && message.Y is { } y)
                {
                    var existing = Figures.FirstOrDefault(figure => figure.Id == message.Id);
                    if (existing is not null) Replace(existing with { X = x, Y = y });
                }
                break;
            case "delete":
                Figures.RemoveAll(figure => figure.Id == message.Id);
                if (SelectedId == message.Id) SelectedId = null;
                break;
        }
    }

    private void PublishPosition(FigureRow row) => _notifier.Publish(_ownerId, SyncMessage.Move(row.Id, row.X, row.Y, _sender));

    private void Replace(FigureRow row)
    {
        var index = Figures.FindIndex(figure => figure.Id == row.Id);
        if (index >= 0) Figures[index] = row;
    }
}
