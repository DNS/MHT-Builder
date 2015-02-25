using System;
using System.Text;
using CustomExceptionEnumerators;

namespace CustomException
{
    /// <summary>
    /// InvalidFileNameException to be thrown when HTMLBulider
    /// is saving the files
    /// </summary>
    [Serializable]
    public class BuilderInvalidFileNameException:CustomException
    {
        private String fileName;
        private String fileExtensions;
        private InvalidFileNameExceptionType  ExceptionType;

        public BuilderInvalidFileNameException()
        {
        }

        public BuilderInvalidFileNameException(String message):base(message)
        {
        }

        public BuilderInvalidFileNameException(String fileName, String fileExtensions, InvalidFileNameExceptionType ExceptionType ) 
        {
            this.fileName = fileName;
            this.fileExtensions = fileExtensions;
            this.ExceptionType = ExceptionType;

            if (ExceptionType == InvalidFileNameExceptionType.InvalidFileName) 
            {
                SetInvalidFileNameCustomMessage(this.fileName, this.fileExtensions);
            }
            else if (ExceptionType == InvalidFileNameExceptionType.InvalideExtension) 
            {
                SetInvalidExtensionCustomMessage(this.fileName, this.fileExtensions);
            }
            
        }

        public string FileName
        {
            get 
            {
                return fileName;
            }

            set 
            {
                fileName = value;
            }
        }

        public string FileExtensions 
        {
            get 
            {
                return fileExtensions;
            }

            set 
            {
                fileExtensions = value;
            }
        }

        private void SetInvalidFileNameCustomMessage(String fileName, String fileExtension)
        {
            StringBuilder message = new StringBuilder();

            message.Append("The filename provided, ");
            message.Append(fileName);
            message.Append(" , has no extension. If are specifying a folder, make sure it ends in a trailing slash. ");
            message.Append("The expected file extension(s) are ");
            message.Append(fileExtension);

            CustomMessage = message.ToString();

                                 
        }

        private void SetInvalidExtensionCustomMessage(String fileName, String fileExtension)
        {
            StringBuilder message = new StringBuilder();

            message.Append("The extension of the filename provided, ");
            message.Append(fileName);
            message.Append(fileExtension  + " ', does not have the expected extension(s)");
            message.Append("The expected file extension(s) are ");
            message.Append(fileExtensions);

            CustomMessage = message.ToString();


        }
    }
}
