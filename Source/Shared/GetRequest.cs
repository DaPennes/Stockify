namespace Stockify;

/// <summary>
/// Represents a GET request for materials from a storage network.
/// Tracks request status, quantities, source, and destination.
/// </summary>
public class GetRequest
{
    /// <summary>
    /// Unique identifier for this request.
    /// </summary>
    public int ID { get; }

    /// <summary>
    /// The material label being requested.
    /// </summary>
    public string MaterialLabel { get; }

    /// <summary>
    /// The requested quantity.
    /// </summary>
    public int RequestedQuantity { get; }

    /// <summary>
    /// The actual quantity that will be transferred (may be less if insufficient).
    /// </summary>
    public int ActualQuantity { get; private set; }

    /// <summary>
    /// The source network for this request.
    /// </summary>
    public StockifyNetwork SourceNetwork { get; }

    /// <summary>
    /// The source shelf for this request.
    /// </summary>
    public StockifyShelf SourceShelf { get; private set; }

    /// <summary>
    /// The destination for the transferred materials.
    /// </summary>
    public StockifyDestination Destination { get; set; }

    /// <summary>
    /// Current status of the request.
    /// </summary>
    public GetRequestStatus Status { get; set; }

    /// <summary>
    /// When the request was created (game ticks).
    /// </summary>
    public int CreatedTick { get; }

    /// <summary>
    /// Timeout ticks after which the request times out.
    /// </summary>
    public int TimeoutTicks { get; } = 60000; // 1 minute default

    /// <summary>
    /// Creates a new GET request.
    /// </summary>
    /// <param name="materialLabel">The material label.</param>
    /// <param name="requestedQuantity">The requested quantity.</param>
    /// <param name="sourceNetwork">The source network.</param>
    /// <param name="destination">The destination.</param>
    public GetRequest(string materialLabel, int requestedQuantity, StockifyNetwork sourceNetwork, StockifyDestination destination)
    {
        this.ID = UnityEngine.Random.Range(100000, 999999);
        this.MaterialLabel = materialLabel;
        this.RequestedQuantity = requestedQuantity;
        this.SourceNetwork = sourceNetwork;
        this.Destination = destination;
        this.Status = GetRequestStatus.Pending;
        this.CreatedTick = Find.TickManager.TicksGame;

        // Attempt to find a source shelf and reserve materials
        this.SourceShelf = FindBestSourceShelf(materialLabel, sourceNetwork);
        if (SourceShelf != null)
        {
            // Try to reserve the materials
            var network = sourceNetwork;
            if (network.TryReserveMaterials(materialLabel, requestedQuantity))
            {
                this.ActualQuantity = requestedQuantity;
                this.Status = GetRequestStatus.Reserved;
            }
            else
            {
                // Partial availability - try to reserve what we can
                var available = network.GetAvailableQuantity(materialLabel);
                if (available > 0)
                {
                    network.ReleaseReservation(materialLabel, available);
                    // We'll handle partial in process
                    this.ActualQuantity = available;
                    this.Status = GetRequestStatus.PendingPartial;
                }
                else
                {
                    this.Status = GetRequestStatus.Failed;
                }
            }
        }
        else
        {
            this.Status = GetRequestStatus.Failed;
        }
    }

    /// <summary>
    /// Finds the best source shelf for this request.
    /// </summary>
    /// <param name="materialLabel">The material label.</param>
    /// <param name="network">The network to search.</param>
    /// <returns>The best source shelf, or null.</returns>
    private StockifyShelf FindBestSourceShelf(string materialLabel, StockifyNetwork network)
    {
        var manager = new StockifyStorageManager();
        return manager.FindBestSourceShelf(materialLabel, network, StockifySelectionMode.Balanced);
    }

    /// <summary>
    /// Processes this request for the current game tick.
    /// </summary>
    /// <returns>True if the request was fully processed.</returns>
    public bool Process()
    {
        if (Status == GetRequestStatus.Complete || Status == GetRequestStatus.Cancelled || Status == GetRequestStatus.Failed)
        {
            return true; // Already terminal state
        }

        // If we have a shelf and destination, attempt transfer
        if (SourceShelf != null && Destination != null)
        {
            var completed = SourceShelf.TryTransferToDestination(new StockifyTransferInfo
            {
                MaterialLabel = MaterialLabel,
                Quantity = ActualQuantity,
                SourceNetwork = SourceNetwork
            }, Destination);
            if (completed)
            {
                Status = GetRequestStatus.Complete;
                SourceNetwork?.ReleaseReservation(MaterialLabel, ActualQuantity);
            }

            return completed;
        }

        // If reserved but not yet transferred, mark as in progress
        if (Status == GetRequestStatus.Reserved)
        {
            Status = GetRequestStatus.InProgress;
        }

        return false;
    }

    /// <summary>
    /// Cancels this request and releases reservations.
    /// </summary>
    public void Cancel()
    {
        if (Status == GetRequestStatus.Complete || Status == GetRequestStatus.Cancelled || Status == GetRequestStatus.Failed)
        {
            return;
        }

        // Release the reservation
        SourceNetwork?.ReleaseReservation(MaterialLabel, ActualQuantity);

        Status = GetRequestStatus.Cancelled;
    }

    /// <summary>
    /// Gets the status description for display.
    /// </summary>
    /// <returns>Status description string.</returns>
    public string GetStatusDescription()
    {
        return Status switch
        {
            GetRequestStatus.Pending => "Pending selection",
            GetRequestStatus.Reserved => $"Reserved {ActualQuantity} {MaterialLabel}",
            GetRequestStatus.InProgress => "In progress - transferring",
            GetRequestStatus.PendingPartial => $"Partial: {ActualQuantity}/{RequestedQuantity} {MaterialLabel}",
            GetRequestStatus.Complete => $"Complete: {ActualQuantity} {MaterialLabel} delivered",
            GetRequestStatus.Failed => $"Failed: Insufficient {MaterialLabel}",
            GetRequestStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Information about a GET request for display.
    /// </summary>
    public class GetRequestInfo
    {
        /// <summary>
        /// The material label.
        /// </summary>
        public string MaterialLabel { get; set; }

        /// <summary>
        /// Requested quantity.
        /// </summary>
        public int RequestedQuantity { get; set; }

        /// <summary>
        /// Actual quantity that will be transferred.
        /// </summary>
        public int ActualQuantity { get; set; }

        /// <summary>
        /// Status description.
        /// </summary>
        public string StatusDescription { get; set; }

        /// <summary>
        /// Source shelf label.
        /// </summary>
        public string SourceShelfLabel { get; set; }

        /// <summary>
        /// Destination label.
        /// </summary>
        public string DestinationLabel { get; set; }
    }

    /// <summary>
    /// Converts this request to display info.
    /// </summary>
    /// <returns>GetRequestInfo for UI display.</returns>
    public GetRequestInfo ToDisplayInfo()
    {
        return new GetRequestInfo
        {
            MaterialLabel = MaterialLabel,
            RequestedQuantity = RequestedQuantity,
            ActualQuantity = ActualQuantity,
            StatusDescription = GetStatusDescription(),
            SourceShelfLabel = SourceShelf?.Thing?.Label ?? "Unknown Shelf",
            DestinationLabel = Destination?.ToString() ?? "Not set"
        };
    }
}

/// <summary>
/// Status of a GET request.
/// </summary>
public enum GetRequestStatus
{
    /// <summary>
    /// Request is pending selection/processing.
    /// </summary>
    Pending,

    /// <summary>
    /// Materials have been reserved.
    /// </summary>
    Reserved,

    /// <summary>
    /// Request is in progress (transferring).
    /// </summary>
    InProgress,

    /// <summary>
    /// Partial availability - some but not all requested.
    /// </summary>
    PendingPartial,

    /// <summary>
    /// Request fully completed.
    /// </summary>
    Complete,

    /// <summary>
    /// Request failed (insufficient materials).
    /// </summary>
    Failed,

    /// <summary>
    /// Request was cancelled.
    /// </summary>
    Cancelled
}