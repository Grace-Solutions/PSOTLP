using System;
using System.Runtime.InteropServices;
using System.Security;

namespace PSOTLP.Common
{
    /// <summary>
    /// Centralized conversion helpers for SecureString. Plaintext is materialized only at the
    /// request creation boundary and the caller is responsible for clearing the returned string
    /// reference as quickly as practical.
    /// </summary>
    public static class OTLPSecureStringUtility
    {
        public static SecureString ToSecureString(string plaintext)
        {
            if (plaintext == null) { return null; }
            var secure = new SecureString();
            foreach (var c in plaintext) { secure.AppendChar(c); }
            secure.MakeReadOnly();
            return secure;
        }

        public static string ToPlainText(SecureString secure)
        {
            if (secure == null) { return null; }
            IntPtr bstr = IntPtr.Zero;
            try
            {
                bstr = Marshal.SecureStringToBSTR(secure);
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                if (bstr != IntPtr.Zero) { Marshal.ZeroFreeBSTR(bstr); }
            }
        }
    }
}
