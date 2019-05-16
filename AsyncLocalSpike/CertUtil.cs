using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace AsyncLocalSpike
{
    class CertUtil
    {
        public static X509Certificate2 GetCertificate(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
            var certs = collection.Find(X509FindType.FindByThumbprint, thumbprint, false);
            return certs.OfType<X509Certificate2>().Single();
        }
    }
}
