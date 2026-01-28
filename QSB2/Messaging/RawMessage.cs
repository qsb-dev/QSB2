using MessagePack;

namespace QSB2.Messaging;

[MessagePackObject]
public struct RawMessage
{
    [Key(0)] public required int From;
    [Key(1)] public required int To;
    [Key(2)] public required int Type;

    [Key(3)] public required byte[] Message;
    // in case message does bs, we dont need to deal with that when forwarding from the server
    // also keeps it open instead of closed union
}