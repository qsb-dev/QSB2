using QSB2.QObject;

namespace QSB2.ItemSync;

public class QDropTarget : QObject.QObject
{
}

public class QRigidbody : QObject<OWRigidbody>;

public class QRigidbodyBuilder : QObjectBuilder<QRigidbody, OWRigidbody>;