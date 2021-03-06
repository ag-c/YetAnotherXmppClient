﻿namespace YetAnotherXmppClient.Core
{
    // subset of https://xmpp.org/registrar/namespaces.html
    public static class ProtocolNamespaces
    {
        public const string LastActivity = "jabber:iq:last";
        public const string SoftwareVersion = "jabber:iq:version";
        public const string EntityTime = "urn:xmpp:time";
        public const string Blocking = "urn:xmpp:blocking";
        public const string ChatStateNotifications = "http://jabber.org/protocol/chatstates";
        public const string PrivateXmlStorage = "jabber:iq:private";
        public const string MultiUserChat = "http://jabber.org/protocol/muc";
        public const string Conference = "jabber:x:conference";
    }
}
