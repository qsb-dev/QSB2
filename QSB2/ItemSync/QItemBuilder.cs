using QSB2.QObject;

namespace QSB2.ItemSync;

public class QItemBuilder : QObjectBuilder
{
    public override void Create()
    {
        SendCreated<QGenericItem>(true);
    }

    public override void Destroy()
    {
        SendCreated<QGenericItem>(false);
    }
}