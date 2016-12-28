using System;
using SmartCard.Utils;

namespace SmartCard.Exceptions
{
    public class SmartCardException : Exception
    {
        private uint _error;

        public SmartCardException(string message) : base(message)
        {
            _error = (uint)SmartCardConst.CardErrorCode.UnknownError;
        }

        public SmartCardException(string message, uint error)
        {
            _error = error;
        }

        public override string Message
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(base.Message))
                {
                    return ((SmartCardConst.CardErrorCode)((uint)_error)).GetDisplayAttributeFrom();
                }
                return base.Message;
            }
        }

        public SmartCardConst.CardErrorCode Error
        {
            get { return (SmartCardConst.CardErrorCode)((uint)_error); }
        }

        public uint ErrorRaw
        {
            get { return _error; }
        }
    }
}
