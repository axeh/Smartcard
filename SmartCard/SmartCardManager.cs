using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SmartCard
{
    public class SmartCardManager
    {
        private IntPtr _context;
        private bool HasContext
        {
            get
            {
                return _context != IntPtr.Zero;
            }
        }



        private SmartCardConst.SCOPE _smartCardScope;
        private uint _lastError;

        public SmartCardManager() : this(SmartCardConst.SCOPE.User)
        {
        }

        public SmartCardManager(SmartCardConst.SCOPE scope)
        {
            _smartCardScope = scope;
        }


        public SmartCardConst.CardErrorCode LastError
        {
            get { return (SmartCardConst.CardErrorCode)((uint)_lastError); }
        }

        public ArrayList ListReaders()
        {
            ArrayList result = new ArrayList();
            if (EstablishContext())
            {
                uint size = GetReaderListBufferSize();

                IntPtr szListReaders = IntPtr.Zero;
                szListReaders = Marshal.AllocHGlobal((int)size);
                if ((PCSC.SCardListReaders(_context, null, szListReaders, out size) == PCSC.SCARD_S_SUCCESS))
                {
                    char[] caReadersData = new char[size];
                    int nbReaders = 0;
                    for (int nI = 0; nI < size; nI++)
                    {
                        caReadersData[nI] = (char)Marshal.ReadByte(szListReaders, nI);

                        if (caReadersData[nI] == 0)
                            nbReaders++;
                    }
                    string[] readerName = new string(caReadersData).Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    //  string[] readerName = readerList.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    result.AddRange(readerName);
                }
                Marshal.FreeHGlobal(szListReaders);
            }
            return result;
        }

        public X509Store GetCertificatesFromSmartCard(string readerName, string cspProviderName)
        {
            uint PROV_RSA_FULL = 1;
            uint CRYPT_VERIFYCONTEXT = 0xF0000000;

            IntPtr hProvParent = (IntPtr)0;
            uint pdwProvDataLen = 0;
            byte[] pbProvData = null;
            byte[] ProvData = null;
            IntPtr hwStore = new IntPtr(0);

            X509Store smartCardStore = null;
            //Gemalto USB SmartCard SmartCardReader 0
            var containerName = string.Format("\\\\.\\{0}\\", readerName);
            try
            {
                CryptoApi.CryptAcquireContext(ref hProvParent, containerName, cspProviderName, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT);

                var GetProvParamRet = CryptoApi.CryptGetProvParam(hProvParent, (uint)CryptoApi.CryptGetParam.PP_USER_CERTSTORE, pbProvData, ref pdwProvDataLen, 0);
                if (pdwProvDataLen > 0)
                {
                    ProvData = new byte[pdwProvDataLen];
                    var GetKeyParamRet = CryptoApi.CryptGetProvParam(hProvParent, (uint)CryptoApi.CryptGetParam.PP_USER_CERTSTORE, ProvData,
                        ref pdwProvDataLen, 0);
                    uint provdataInt = BitConverter.ToUInt32(ProvData, 0);
                    hwStore = (IntPtr)provdataInt;

                    if (hwStore == IntPtr.Zero)
                    {
                        throw new Exception("Error reading X509Store from smart card!");
                    }
                }

                smartCardStore = new X509Store(hwStore);

            }
            finally
            {
                CryptoApi.CryptReleaseContext(hProvParent, 0);
            }


            return smartCardStore;

        }

        public bool CheckSmartCard(string readerName, string cspProviderName, string mboDjelatnik)
        {
            var smartCardStore = GetCertificatesFromSmartCard(readerName, cspProviderName);
            if (smartCardStore.Certificates.Count > 0)
            {
                foreach (var cert in smartCardStore.Certificates)
                {
                    if (cert.SubjectName.Name != null && cert.SubjectName.Name.Contains(mboDjelatnik))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ImportCertificateFromSmartCard(string readerName, string cspProviderName)
        {
            var smartCardStore = GetCertificatesFromSmartCard(readerName, cspProviderName);
            if (smartCardStore.Certificates.Count > 0)
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                foreach (var cert in smartCardStore.Certificates)
                {
                    foreach (var oldCert in store.Certificates)
                    {
                        if (oldCert.Subject == cert.Subject)
                        {
                            store.Remove(oldCert);
                            break;
                        }
                    }
                    store.Add(cert);
                }
                store.Close();
            }
            return true;
        }

        #region Private

        private bool EstablishContext()
        {
            if (HasContext)
            {
                return true;
            }

            _lastError = (uint)PCSC.SCardEstablishContext((uint)_smartCardScope, IntPtr.Zero, IntPtr.Zero, ref _context);
            return _lastError == PCSC.SCARD_S_SUCCESS;
        }

        private uint GetReaderListBufferSize()
        {
            if (HasContext)
            {
                return 0;
            }
            uint bufferSize = 0;

            _lastError = (uint)PCSC.SCardListReaders(_context, null, IntPtr.Zero, out bufferSize);
            return bufferSize;
        }
        #endregion

    }
}
