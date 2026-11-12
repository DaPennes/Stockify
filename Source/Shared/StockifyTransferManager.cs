namespace Stockify;

/// <summary>
/// Manages material transfers between shelves and workbenches.
/// Handles reservations, transfers, and cancellation logic.
/// </summary>
public class StockifyTransferManager
{
    /// <summary>
    /// Active transfer requests currently in progress.
    /// </summary>
    public List<TransferRequest> ActiveTransfers { get; } = new List<TransferRequest>();

    /// <summary>
    /// Creates a new transfer request for materials.
    /// </summary>
    /// <param name="materialLabel">The material label to transfer.</param>
    /// <param name="quantity">The quantity to transfer.</param>
    /// <param name="sourceShelf">The source shelf.</param>
    /// <param name="destination">The destination for the transfer.</param>
    /// <returns>The transfer request, or null if creation failed.</returns>
    public TransferRequest CreateTransferRequest(string materialLabel, int quantity, StockifyShelf sourceShelf, StockifyDestination destination)
    {
        // Check if we have enough available materials
        if (sourceShelf?.Network == null || !sourceShelf.Network.TryReserveMaterials(materialLabel, quantity))
        {
            return null;
        }

        var request = new TransferRequest
        {
            MaterialLabel = materialLabel,
            Quantity = quantity,
            SourceShelf = sourceShelf,
            Destination = destination,
            Status = TransferStatus.Pending,
            CreatedTick = Find.TickManager.TicksGame
        };

        this.ActiveTransfers.Add(request);
        return request;
    }

    /// <summary>
    /// Processes all active transfers for this tick.
    /// Should be called once per game tick from the map component.
    /// </summary>
    public void ProcessTransfers()
    {
        var completed = new List<TransferRequest>();

        foreach (var request in this.ActiveTransfers)
        {
            if (request.Status == TransferStatus.Completed || request.Status == TransferStatus.Cancelled || request.Status == TransferStatus.Failed)
            {
                completed.Add(request);
                continue;
            }

            // Try to complete the transfer
            if (this.TryCompleteTransfer(request))
            {
                request.Status = TransferStatus.Completed;
                completed.Add(request);
            }
            else if (Find.TickManager.TicksGame - request.CreatedTick > request.TimeoutTicks)
            {
                // Transfer timed out, rollback
                this.RollbackTransfer(request);
                request.Status = TransferStatus.Timeout;
                completed.Add(request);
            }
        }

        // Remove completed/cancelled/failed transfers
        foreach (var completedRequest in completed)
        {
            this.ActiveTransfers.Remove(completedRequest);
        }
    }

    /// <summary>
    /// Tries to complete a single transfer request.
    /// </summary>
    /// <param name="request">The transfer request.</param>
    /// <returns>True if the transfer was completed successfully.</returns>
    private bool TryCompleteTransfer(TransferRequest request)
    {
        // Find the source shelf's items
        var sourceItems = request.SourceShelf.StoredItems
            .Where(i => i.def != null && i.def.label == request.MaterialLabel)
            .ToList();

        // Find available (non-reserved) items
        var availableItems = sourceItems.Where(i => i.stackCount > 0).ToList();

        if (availableItems.Count == 0 || availableItems.Sum(i => i.stackCount) < request.Quantity)
        {
            // Not enough available materials, release reservation
            request.SourceShelf.Network.ReleaseReservation(request.MaterialLabel, request.Quantity);
            return false;
        }

        // Transfer the required quantity
        var transferred = 0;
        var itemsToTransfer = availableItems.TakeWhile((_) => transferred < request.Quantity).ToList();

        foreach (var item in itemsToTransfer)
        {
            var toTransfer = Math.Min(item.stackCount, request.Quantity - transferred);

            // Remove from shelf and transfer
            item.stackCount -= toTransfer;
            transferred += toTransfer;

            if (item.stackCount <= 0)
            {
                request.SourceShelf.StoredItems.Remove(item);
            }

            // TODO: Actually transfer the item to the destination using vanilla mechanics
            // This would involve finding a valid hauling destination, etc.
        }

        // If we transferred the full quantity, mark as complete
        if (transferred >= request.Quantity)
        {
            request.SourceShelf.Network.ReleaseReservation(request.MaterialLabel, request.Quantity);
            return true;
        }

        // Partial transfer - release what we can and keep the rest available
        request.SourceShelf.Network.ReleaseReservation(request.MaterialLabel, transferred);
        return false;
    }

    /// <summary>
    /// Rolls back a transfer request, releasing any reservations.
    /// </summary>
    /// <param name="request">The transfer request to rollback.</param>
    public void RollbackTransfer(TransferRequest request)
    {
        if (request != null && request.SourceShelf != null)
        {
            request.SourceShelf.Network.ReleaseReservation(request.MaterialLabel, request.Quantity);
        }

        this.ActiveTransfers.Remove(request);
    }

    /// <summary>
    /// Cancels a specific transfer request.
    /// </summary>
    /// <param name="request">The transfer request to cancel.</param>
    public void CancelTransfer(TransferRequest request)
    {
        if (request != null)
        {
            this.RollbackTransfer(request);
            request.Status = TransferStatus.Cancelled;
        }
    }
}