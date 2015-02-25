namespace CustomException
{
    public class ExternalFileHTMLException : CustomException
    {
        public ExternalFileHTMLException() 
        { }

        public ExternalFileHTMLException(string message) : base(message) 
        {
            CustomMessage = message;
        }


        
    }
}
