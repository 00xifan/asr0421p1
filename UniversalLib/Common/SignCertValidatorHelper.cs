// Copyright (c) 2025-present Lenovo.  All rights reserverd
// Confidential and Restricted
using System;
using System.Xml;
using Lenovo.CertificateValidation;

namespace UniversalLib.Common
{
    public class SignCertValidatorHelper
    {
        private static readonly Lazy<SignCertValidatorHelper> _instanceLock = new Lazy<SignCertValidatorHelper>(() => new SignCertValidatorHelper());
        public static SignCertValidatorHelper Instance
        {
            get
            {
                return _instanceLock.Value;
            }
        }

        /// <summary>
        /// Verify binary stream files
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool StreamValidator(XmlDocument stream)
        {
            bool isExeValid;
            try
            {
                XMLFileValidator fileValidator = new XMLFileValidator();
                var status = fileValidator.GetTrustStatus(stream);
                if (status is TrustStatus.FileTrusted)
                    isExeValid = true;
                else
                    isExeValid = false;
            }
            catch (Exception ex)
            {
                isExeValid = false;
                LogsHelper.Instance.ErrorWrite($"LibCertValidatorAPI.StreamValidator.Exception:{ex.Message}");
            }
            return isExeValid;
        }

        /// <summary>
        /// Signature used to verify XML files
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool XMLFileValidator(string fileName)
        {
            LogsHelper.Instance.DebugWrite($"Begin XMLFileValidator fileName:{fileName}");
            bool isExeValid = true;
            try
            {
                XMLFileValidator fileValidator = new XMLFileValidator();
                var status = fileValidator.GetTrustStatus(fileName);
                LogsHelper.Instance.DebugWrite($"XMLFileValidator status:{status}");
                if (status is TrustStatus.FileTrusted)
                    isExeValid = true;
                else
                    isExeValid = false;
            }
            catch (Exception ex)
            {
                isExeValid = false;
                LogsHelper.Instance.ErrorWrite($"LibCertValidatorAPI.XMLFileValidator.Exception:{ex.Message}");
            }
            LogsHelper.Instance.DebugWrite($"End XMLFileValidator");
            return isExeValid;
        }

        /// <summary>
        /// Used to verify the signature of binary files (. exe,. dl, cab)
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool FileValidator(string fileName)
        {
            LogsHelper.Instance.DebugWrite($"Begin FileValidator fileName:{fileName}");
            bool isExeValid;
            try
            {
                var status = Lenovo.CertificateValidation.FileValidator.GetTrustStatus(fileName);
                LogsHelper.Instance.DebugWrite($"FileValidator status:{status}");
                if (status is TrustStatus.FileTrusted)
                    isExeValid = true;
                else
                    isExeValid = false;
            }
            catch (Exception ex)
            {
                isExeValid = false;
                LogsHelper.Instance.ErrorWrite($"LibCertValidatorAPI.FileValidator.Exception:{ex.Message}");
            }
            LogsHelper.Instance.DebugWrite($"End FileValidator");
            return isExeValid;
        }


    }
}
