namespace Stockify;

public class StockifyMainButtonWorker : MainButtonWorker
{
    public override void Activate()
    {
        Find.WindowStack.Add(new StockifyMainWindow());
    }
}
