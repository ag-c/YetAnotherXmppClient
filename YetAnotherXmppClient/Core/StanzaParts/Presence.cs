namespace YetAnotherXmppClient.Core.StanzaParts
{
    //4.7.2.1.  Show Element
    public enum PresenceShow
    {
        None,

        //The entity or resource is temporarily away.
        away,

        //The entity or resource is actively interested in chatting.
        chat,

        //The entity or resource is busy (dnd = "Do Not Disturb").
        dnd,

        //The entity or resource is away for an extended period (xa = "eXtended Away")
        xa,
    }    
    //4.7.2.1.  Show Element
    //static class ShowValues
    //{
    //    //The entity or resource is temporarily away.
    //    public static readonly string away = nameof(away);

    //    //The entity or resource is actively interested in chatting.
    //    public static readonly string chat = nameof(chat);

    //    //The entity or resource is busy (dnd = "Do Not Disturb").
    //    public static readonly string dnd = nameof(dnd);

    //    //The entity or resource is away for an extended period (xa = "eXtended Away")
    //    public static readonly string xa = nameof(xa);
    //}
}
