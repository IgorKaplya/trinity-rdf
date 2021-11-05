using System;
using System.Collections.Generic;

namespace Semiodesk.Trinity.Test.Linq
{
    public class OnlineAccount: Resource
    {
        public override IEnumerable<Class> GetTypes()
        {
            yield return foaf.OnlineAccount;
        }

        private readonly PropertyMapping<string> _accountName = new PropertyMapping<string>(nameof(AccountName), foaf.accountName);
        public string AccountName
        {
            get => GetValue(_accountName);
            set => SetValue(_accountName, value);
        }

        public OnlineAccount(UriRef uri) : base(uri)
        {
        }

        public OnlineAccount(Uri uri) : base(uri)
        {
        }

        public OnlineAccount(string uriString) : base(uriString)
        {
        }

        public OnlineAccount(Resource other) : base(other)
        {
        }
    }
}