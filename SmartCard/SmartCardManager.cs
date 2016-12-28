using System;
using System.Collections;
using System.Collections.Generic;
using SmartCard.Utils;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using SmartCard.Exceptions;

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
        private List<string> _readers;
        private string _cspProviderName;
        private string _readerName;
        private uint _lastError;

        public SmartCardManager() : this(SmartCardConst.SCOPE.User)
        {
        }

        public SmartCardManager(SmartCardConst.SCOPE scope)
        {
            _smartCardScope = scope;
            _context = IntPtr.Zero;
            _readers = new List<string>();
        }

        public SmartCardManager(string cspProviderName) : this(SmartCardConst.SCOPE.User)
        {
            _cspProviderName = cspProviderName;
        }

        public SmartCardManager(SmartCardConst.SCOPE scope, string cspProviderName)
        {
            _smartCardScope = scope;
            _context = IntPtr.Zero;
            _cspProviderName = cspProviderName;
        }

        public SmartCardConst.CardErrorCode LastError
        {
            get { return (SmartCardConst.CardErrorCode)((uint)_lastError); }
        }

        public uint LastErrorRaw
        {
            get { return _lastError; }
        }

        public bool EstablishContext()
        {
            if (HasContext)
            {
                return true;
            }

            _lastError = (uint)PCSC.SCardEstablishContext((uint)_smartCardScope, IntPtr.Zero, IntPtr.Zero, ref _context);
            RaiseExceptionIfFailed();
            return _lastError == (uint)SmartCardConst.CardErrorCode.None;
        }

        public ICollection Readers
        {
            get { return _readers; }
        }

        public ICollection ListReaders()
        {
            _readers.Clear();
            if (EstablishContext())
            {
                uint size = GetReaderListBufferSize();

                IntPtr szListReaders = IntPtr.Zero;
                szListReaders = Marshal.AllocHGlobal((int)size);
                _lastError = (uint)PCSC.SCardListReaders(_context, null, szListReaders, out size);
                RaiseExceptionIfFailed();

                if (_lastError == (uint)SmartCardConst.CardErrorCode.None)
                {
                    char[] caReadersData = new char[size];
                    int nbReaders = 0;
                    for (int nI = 0; nI < size; nI++)
                    {
                        caReadersData[nI] = (char)Marshal.ReadByte(szListReaders, nI);

                        if (caReadersData[nI] == 0)
                            nbReaders++;
                    }
                    string[] readerName = new string(caReadersData).Split(new char[] { '\0' },
                        StringSplitOptions.RemoveEmptyEntries);
                    //  string[] readerName = readerList.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    _readers.AddRange(readerName);
                }
                Marshal.FreeHGlobal(szListReaders);
            }

            if (!string.IsNullOrEmpty(_cspProviderName))
            {
                foreach (string reader in _readers)
                {
                    var containerName = string.Format("\\\\.\\{0}\\", reader);
                    IntPtr hProvParent = (IntPtr)0;
                    uint pdwProvDataLen = 0;
                    byte[] pbProvData = null;
                    byte[] ProvData = null;
                    uint PROV_RSA_FULL = 1;
                    var GetProvParamRet = CryptoApi.CryptAcquireContext(ref hProvParent, containerName,
                        _cspProviderName, PROV_RSA_FULL, 0);

                    GetProvParamRet = CryptoApi.CryptGetProvParam(hProvParent,
                        (uint)CryptoApi.CryptGetParam.PP_NAME, pbProvData, ref pdwProvDataLen, 0);
                    if (pdwProvDataLen > 0)
                    {
                        ProvData = new byte[pdwProvDataLen];
                        var GetKeyParamRet = CryptoApi.CryptGetProvParam(hProvParent,
                            (uint)CryptoApi.CryptGetParam.PP_NAME, ProvData,
                            ref pdwProvDataLen, 0);

                        _readerName = reader;

                        CryptoApi.CryptReleaseContext(hProvParent, 0);
                        break;
                    }
                }

            }

            if (!string.IsNullOrEmpty(_cspProviderName) && string.IsNullOrWhiteSpace(_readerName))
            {
                throw new SmartCardException(string.Format("CSP not found - {0}", _cspProviderName));
            }

            return _readers;
        }

        public bool CheckSmartCardCertificate(string subjectName)
        {
            IsPreferedCspInitialized();
            return CheckSmartCardCertificate(_readerName, _cspProviderName, subjectName);
        }

        public bool CheckSmartCardCertificate(string readerName, string cspProviderName, string subjectName)
        {
            var smartCardStore = GetCertificatesFromSmartCard(readerName, cspProviderName);
            if (smartCardStore.Certificates.Count > 0)
            {
                foreach (var cert in smartCardStore.Certificates)
                {
                    if (cert.SubjectName.Name != null && cert.SubjectName.Name.Contains(subjectName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ImportCertificateFromSmartCard()
        {
            IsPreferedCspInitialized();
            return ImportCertificateFromSmartCard(_readerName, _cspProviderName);
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

        public X509Store GetCertificatesFromSmartCard()
        {
            IsPreferedCspInitialized();
            return GetCertificatesFromSmartCard(_readerName, _cspProviderName);
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

        public void ReleaseContext()
        {
            if (HasContext)
            {
                PCSC.SCardReleaseContext(_context);
            }
        }

        #region Private

        private uint GetReaderListBufferSize()
        {
            if (!HasContext)
            {
                return 0;
            }

            uint bufferSize = 0;

            _lastError = (uint)PCSC.SCardListReaders(_context, null, IntPtr.Zero, out bufferSize);
            RaiseExceptionIfFailed();
            return bufferSize;
        }

        private void RaiseExceptionIfFailed()
        {
            if (_lastError != (uint)SmartCardConst.CardErrorCode.None)
            {
                var error = ((SmartCardConst.CardErrorCode)_lastError).GetDisplayAttributeFrom();
                throw new SmartCardException(string.Format("Error occured: {0} - Code: {1}", error, _lastError), _lastError);
            }
        }

        private void IsPreferedCspInitialized()
        {
            if (String.IsNullOrWhiteSpace(_cspProviderName))
            {
                throw new SmartCardException("Provide preffered cspProviderName using constructor");
            }

            if (String.IsNullOrWhiteSpace(_readerName))
            {
                throw new SmartCardException(string.Format("CSP not found - {0}", _cspProviderName));
            }
        }

        #endregion

    }
}
