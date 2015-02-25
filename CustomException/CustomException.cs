using System;
using System.Runtime.Serialization;
using CustomExceptionEnumerators;


namespace CustomException
{
    /// <summary>
    /// Custome exception to manage execptions for the 
    /// Messaging Management system.
    /// </summary>
    [Serializable]
    public class CustomException:Exception
    {

        private SeverityLevel severityLevelOfException;

        private LogLevel logLevelOfException;

        private Exception innerException;

        private String customMessage;


        public CustomException() 
        { 
        }

        public CustomException(String errorMessage):base(errorMessage)
        {
        }

        public CustomException(String errorMessage, Exception innerExeption): base(errorMessage, innerExeption)
        {
        }

        protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context) 
        { 
        }

        public CustomException(SeverityLevel severityLevel, LogLevel logLevel, Exception exception, String customMessage)
        {
            severityLevelOfException = severityLevel;
            logLevelOfException = logLevel;
            innerException = exception;
            this.customMessage = customMessage;

        }    

        public String CustomMessage
        {
            get 
            {
                return customMessage;
            }

            set 
            {
                customMessage = value;
            }
        }

        public String ErrorMessage 
        {
            get 
            {
                return base.Message.ToString();
            }
        }


    }


}
