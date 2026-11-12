namespace Stockify;

public class NetworkMaterialInfo
{
    public ThingDef MaterialDef { get; private set; }
    public string Label { get; private set; }
    public int StoredCount { get; private set; }
    public int TotalCount { get; private set; }

    public NetworkMaterialInfo(ThingDef materialDef)
    {
        MaterialDef = materialDef;
        Label = materialDef?.label;
    }

    public void AddItem(Thing thing)
    {
        if (thing?.def != null)
            AddItem(thing.def, thing.stackCount);
    }

    public void AddItem(ThingDef thingDef, int stackCount)
    {
        if (thingDef == null || stackCount <= 0)
            return;

        MaterialDef = thingDef;
        Label = thingDef.label;
        StoredCount += stackCount;
        TotalCount += stackCount;
    }
}
