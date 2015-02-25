using System;
using System.Text;

namespace CustomException
{

    /// <summary>
    /// Custome exception class to be thrown when HTML Builder
    /// fails to DL HTML
    /// </summary>
    [Serializable]
    public class BuilderDownLoadHTMLException:CustomException
    {

        private String Url;
        
        
        public BuilderDownLoadHTMLException() 
        { }

        public BuilderDownLoadHTMLException(String message) : base(message) 
        { }

        public BuilderDownLoadHTMLException(String url,String message, Exception innerException): base(message, innerException)
        {
            Url = url;
            SetInvalidFileNameCustomMessage(url, message);
        }
    

        public String URL 
        {
            get 
            {
                return Url;
            }

            set 
            {
                Url = value;
            }
        }

        private void SetInvalidFileNameCustomMessage(String url, String errorMessage)
        {
            StringBuilder message = new StringBuilder();

            if (url == null)
            {
                message.Append("URL is NULL ");
            }
            else
            {
                message.Append("unable to download ");
                message.Append(url);
                message.Append(" :  ");
                message.Append(errorMessage);
                message.Append("InnerException is: ");
                message.Append(InnerException.Message);
            }

            CustomMessage = message.ToString();


        }

    }
}
