using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SmartCard
{
    public class CryptoApi
    {
        private const string ADVAPI32_DLL = "advapi32.dll";

        public enum CryptGetParam : uint
        {
            /// <summary>
            /// The unique container name of the current key container in the form of a null-terminated CHAR string. 
            /// For many CSPs, this name is the same name returned when the PP_CONTAINER value is used. 
            /// The CryptAcquireContext function must work with this container name.
            /// </summary>
            PP_CONTAINER = 6,
            /// <summary>
            /// Specifies that the key exchange PIN is contained in pbData. The PIN is represented as a null-terminated ASCII string.
            /// </summary>
            PP_KEYEXCHANGE_PIN = 32,
            /// <summary>
            /// Specifies that the key signature PIN is contained in pbData. The PIN is represented as a null-terminated ASCII string.
            /// </summary>
            PP_SIGNATURE_PIN = 33,
            /// <summary>
            /// Obtains the user certificate store for the smart card. 
            /// This certificate store contains all of the user certificates that are stored on the smart card. 
            /// The certificates in this store are encoded by using PKCS_7_ASN_ENCODING or X509_ASN_ENCODING encoding and should contain the CERT_KEY_PROV_INFO_PROP_ID property.
            /// </summary>
            PP_USER_CERTSTORE = 42,
            /// <summary>
            /// The name of the CSP in the form of a null-terminated CHAR string. 
            /// This string is identical to the one passed in the pszProvider parameter of the CryptAcquireContext function to specify that the current CSP be used.
            /// </summary>
            PP_NAME = 4
        }

        [DllImport(ADVAPI32_DLL, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptAcquireContext(
            ref IntPtr hProv,
            string pszContainer,
            string pszProvider,
            uint dwProvType,
            uint dwFlags);

        [DllImport(ADVAPI32_DLL, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CryptGetProvParam(
            IntPtr hProv,
            uint dwParam,
            byte[] pbProvData,
            ref uint pdwProvDataLen,
            uint dwFlags);

        [DllImport(ADVAPI32_DLL, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CryptSetProvParam(
            IntPtr hProv,
            uint dwParam,
            byte[] pbProvData,
            uint dwFlags);


        [DllImport(ADVAPI32_DLL, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CryptReleaseContext(IntPtr hProv, uint dwFlags);

    }
}
