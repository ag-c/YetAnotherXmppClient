using System;

namespace YetAnotherXmppClient.Extensions
{
    static class EnumHelper
    {
        public static TEnum? Parse<TEnum>(string memberName) where TEnum : struct, Enum
        {
            if (memberName == null || !Enum.TryParse(memberName, out TEnum val))
                return null;

            return val;
        }
    }
}
