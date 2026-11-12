namespace Stockify;

/// <summary>
/// Transfer information for shelf-to-destination moves.
/// Contains all data needed for a material transfer operation.
/// </summary>
public class StockifyTransferInfo
{
    /// <summary>
    /// The material label being transferred.
    /// </summary>
    public string MaterialLabel { get; set; }

    /// <summary>
    /// The quantity to transfer.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The source network for this transfer.
    /// </summary>
    public StockifyNetwork SourceNetwork { get; set; }

    /// <summary>
    /// Creates default transfer info.
    /// </summary>
    public StockifyTransferInfo()
    {
        MaterialLabel = "";
        Quantity = 0;
        SourceNetwork = null;
    }

    /// <summary>
    /// Creates transfer info with specified values.
    /// </summary>
    /// <param name="materialLabel">The material label.</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="sourceNetwork">The source network.</param>
    public StockifyTransferInfo(string materialLabel, int quantity, StockifyNetwork sourceNetwork)
    {
        MaterialLabel = materialLabel;
        Quantity = quantity;
        SourceNetwork = sourceNetwork;
    }
}

public class TransferRequest
{
    public string MaterialLabel { get; set; }
    public int Quantity { get; set; }
    public StockifyShelf SourceShelf { get; set; }
    public StockifyDestination Destination { get; set; }
    public TransferStatus Status { get; set; }
    public int CreatedTick { get; set; }
    public int TimeoutTicks { get; set; } = 60000;
}

public enum TransferStatus
{
    Pending,
    Completed,
    Cancelled,
    Failed,
    Timeout
}