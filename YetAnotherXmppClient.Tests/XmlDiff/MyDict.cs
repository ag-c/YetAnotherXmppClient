using System;
using System.Collections.Generic;
using System.Text;

namespace OLEDB.Test.ModuleCore
{
    public class MyDict<Type1, Type2> : Dictionary<Type1, Type2>
    {
        public new Type2 this[Type1 key]
        {
            get
            {
                if (ContainsKey(key))
                {
                    return base[key];
                }
                return default(Type2);
            }
            set
            {
                base[key] = value;
            }
        }
    }
}
